using System.Text;
using System.Text.RegularExpressions;

namespace KnowledgeSystem.Api.Services;

/// <summary>
/// SEMANTIC-AWARE Text Chunking Service
/// 
/// Strategy:
/// 1. LEGAL/ARTICLE mode: Detects legal document structure (Madde X, Article Y) → one article per chunk
/// 2. PARAGRAPH mode: Splits by paragraphs if no legal structure detected
/// 3. GENERIC mode: Fallback to size-based chunking with smart boundaries
/// 
/// Why this improves retrieval:
/// - Legal documents: Each article is semantically complete → better retrieval accuracy
/// - General documents: Paragraph-level chunks preserve context
/// - Mixed content: Graceful fallback to size-based chunking
/// </summary>
public class TextChunkingService : ITextChunkingService
{
    private readonly ILogger<TextChunkingService> _logger;
    private const int MinChunkSize = 10;

    // Legal patterns (Turkish + English)
    private static readonly Regex ArticlePatternTurkish = new(
        @"^(MADDE|Madde)\s+(\d+[a-zA-Z]?)\s*[–\-—:]\s*(.+?)$",
        RegexOptions.Multiline | RegexOptions.Compiled
    );

    private static readonly Regex ArticlePatternEnglish = new(
        @"^(ARTICLE|Article)\s+(\d+[a-zA-Z]?)\s*[–\-—:\.]\s*(.+?)$",
        RegexOptions.Multiline | RegexOptions.Compiled
    );

    public TextChunkingService(ILogger<TextChunkingService> logger)
    {
        _logger = logger;
    }

    #region LEGACY API (backward compatibility)

    /// <summary>
    /// LEGACY: Returns only content strings (no metadata)
    /// </summary>
    public List<string> ChunkText(string text, int maxChunkSize = 500, int overlap = 50)
    {
        var results = ChunkTextWithMetadata(text, maxChunkSize, overlap);
        return results.Select(r => r.Content).ToList();
    }

    #endregion

    #region NEW SEMANTIC-AWARE API

    /// <summary>
    /// MAIN ENTRY POINT: Semantic-aware chunking with metadata
    /// </summary>
    public List<ChunkResult> ChunkTextWithMetadata(string text, int maxChunkSize = 500, int overlap = 50)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Empty or null text provided for chunking");
            return new List<ChunkResult>();
        }

        if (overlap >= maxChunkSize)
        {
            throw new ArgumentException("Overlap must be less than maxChunkSize", nameof(overlap));
        }

        _logger.LogInformation("📝 Starting SEMANTIC chunking: length={Length}, maxSize={MaxSize}", 
            text.Length, maxChunkSize);

        // STRATEGY 1: Legal/Article-based chunking
        var legalChunks = TryLegalChunking(text, maxChunkSize);
        if (legalChunks.Count > 0)
        {
            _logger.LogInformation("✅ LEGAL mode: {Count} articles detected", legalChunks.Count);
            return legalChunks;
        }

        // STRATEGY 2: Paragraph-based chunking
        var paragraphChunks = TryParagraphChunking(text, maxChunkSize, overlap);
        if (paragraphChunks.Count > 0)
        {
            _logger.LogInformation("✅ PARAGRAPH mode: {Count} paragraph-based chunks", paragraphChunks.Count);
            return paragraphChunks;
        }

        // STRATEGY 3: Generic size-based chunking
        _logger.LogInformation("✅ GENERIC mode: falling back to size-based chunking");
        return GenericChunking(text, maxChunkSize, overlap);
    }

    #endregion

    #region STRATEGY 1: Legal/Article-based Chunking

    /// <summary>
    /// Detects legal document structure (Madde X, Article Y)
    /// Each article becomes ONE chunk
    /// </summary>
    private List<ChunkResult> TryLegalChunking(string text, int maxChunkSize)
    {
        var chunks = new List<ChunkResult>();

        // Try Turkish pattern first
        var matches = ArticlePatternTurkish.Matches(text);
        if (matches.Count == 0)
        {
            // Try English pattern
            matches = ArticlePatternEnglish.Matches(text);
        }

        if (matches.Count == 0)
        {
            _logger.LogDebug("No legal article patterns detected");
            return chunks;
        }

        _logger.LogInformation("🔍 Detected {Count} legal articles", matches.Count);

        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var articleNumber = match.Groups[2].Value.Trim();
            var articleTitle = match.Groups[3].Value.Trim();

            // Extract article content (from this match to next match or end of text)
            var startIndex = match.Index;
            var endIndex = (i < matches.Count - 1)
                ? matches[i + 1].Index
                : text.Length;

            var articleContent = text.Substring(startIndex, endIndex - startIndex).Trim();

            // Skip if too short
            if (articleContent.Length < MinChunkSize)
            {
                _logger.LogDebug("Skipping article {Number} (too short: {Length} chars)", articleNumber, articleContent.Length);
                continue;
            }

            // If article is too long, split it but keep metadata
            if (articleContent.Length > maxChunkSize * 2)
            {
                _logger.LogDebug("Article {Number} is large ({Length} chars), splitting into sub-chunks", 
                    articleNumber, articleContent.Length);
                
                var subChunks = SplitLargeArticle(articleContent, articleNumber, articleTitle, maxChunkSize);
                chunks.AddRange(subChunks);
            }
            else
            {
                chunks.Add(new ChunkResult
                {
                    Content = articleContent,
                    Metadata = new ChunkMetadata
                    {
                        ArticleNumber = articleNumber,
                        ArticleTitle = articleTitle,
                        ChunkType = "article"
                    }
                });

                _logger.LogDebug("✅ Article {Number}: '{Title}' ({Length} chars)", 
                    articleNumber, articleTitle, articleContent.Length);
            }
        }

        return chunks;
    }

    /// <summary>
    /// Splits a large article into sub-chunks while preserving metadata
    /// </summary>
    private List<ChunkResult> SplitLargeArticle(string articleContent, string articleNumber, string articleTitle, int maxChunkSize)
    {
        var subChunks = new List<ChunkResult>();
        var position = 0;
        var subIndex = 0;

        while (position < articleContent.Length)
        {
            var remainingLength = articleContent.Length - position;
            var chunkSize = Math.Min(maxChunkSize, remainingLength);
            
            var chunk = ExtractChunk(articleContent, position, chunkSize, maxChunkSize);
            
            if (string.IsNullOrWhiteSpace(chunk) || chunk.Length < MinChunkSize)
            {
                break;
            }

            subChunks.Add(new ChunkResult
            {
                Content = chunk.Trim(),
                Metadata = new ChunkMetadata
                {
                    ArticleNumber = $"{articleNumber}.{subIndex + 1}",
                    ArticleTitle = articleTitle,
                    ChunkType = "article"
                }
            });

            position += chunk.Length;
            subIndex++;
        }

        return subChunks;
    }

    #endregion

    #region STRATEGY 2: Paragraph-based Chunking

    /// <summary>
    /// Splits text by paragraphs (double newline)
    /// Groups paragraphs into chunks up to maxChunkSize
    /// </summary>
    private List<ChunkResult> TryParagraphChunking(string text, int maxChunkSize, int overlap)
    {
        var chunks = new List<ChunkResult>();

        // Split by double newline (paragraph boundary)
        var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => p.Length >= MinChunkSize)
            .ToList();

        if (paragraphs.Count == 0)
        {
            _logger.LogDebug("No clear paragraph structure detected");
            return chunks;
        }

        // Check if paragraph-based chunking makes sense
        // (at least 2 paragraphs AND average paragraph size reasonable)
        if (paragraphs.Count < 2)
        {
            _logger.LogDebug("Only {Count} paragraph(s), not enough for paragraph mode", paragraphs.Count);
            return chunks;
        }

        var avgParagraphSize = paragraphs.Average(p => p.Length);
        if (avgParagraphSize > maxChunkSize * 2)
        {
            _logger.LogDebug("Average paragraph too large ({Size} chars), skipping paragraph mode", avgParagraphSize);
            return chunks;
        }

        _logger.LogDebug("📄 Found {Count} paragraphs (avg size: {AvgSize} chars)", paragraphs.Count, (int)avgParagraphSize);

        // Group paragraphs into chunks
        var currentChunk = new StringBuilder();
        
        foreach (var paragraph in paragraphs)
        {
            // If adding this paragraph would exceed maxChunkSize, finalize current chunk
            if (currentChunk.Length > 0 && currentChunk.Length + paragraph.Length > maxChunkSize)
            {
                var chunkContent = currentChunk.ToString().Trim();
                if (chunkContent.Length >= MinChunkSize)
                {
                    chunks.Add(new ChunkResult
                    {
                        Content = chunkContent,
                        Metadata = new ChunkMetadata { ChunkType = "paragraph" }
                    });
                }
                
                currentChunk.Clear();
                
                // Apply overlap: take last N chars from previous chunk
                if (chunks.Count > 0 && overlap > 0)
                {
                    var lastChunk = chunks[^1].Content;
                    var overlapText = lastChunk.Length > overlap 
                        ? lastChunk.Substring(lastChunk.Length - overlap) 
                        : lastChunk;
                    currentChunk.Append(overlapText);
                    currentChunk.AppendLine();
                }
            }

            currentChunk.AppendLine(paragraph);
            currentChunk.AppendLine();
        }

        // Add final chunk
        if (currentChunk.Length >= MinChunkSize)
        {
            chunks.Add(new ChunkResult
            {
                Content = currentChunk.ToString().Trim(),
                Metadata = new ChunkMetadata { ChunkType = "paragraph" }
            });
        }

        return chunks;
    }

    #endregion

    #region STRATEGY 3: Generic Size-based Chunking

    /// <summary>
    /// Fallback: Size-based chunking with smart boundaries
    /// </summary>
    private List<ChunkResult> GenericChunking(string text, int maxChunkSize, int overlap)
    {
        var chunks = new List<ChunkResult>();
        var position = 0;

        while (position < text.Length)
        {
            var remainingLength = text.Length - position;
            var chunkSize = Math.Min(maxChunkSize, remainingLength);

            var chunk = ExtractChunk(text, position, chunkSize, maxChunkSize);
            
            if (string.IsNullOrWhiteSpace(chunk) || chunk.Length < MinChunkSize)
            {
                break;
            }

            chunks.Add(new ChunkResult
            {
                Content = chunk.Trim(),
                Metadata = new ChunkMetadata { ChunkType = "generic" }
            });

            if (position + chunk.Length >= text.Length)
            {
                break;
            }

            var nextPosition = position + chunk.Length - overlap;
            
            if (nextPosition <= position)
            {
                _logger.LogWarning("Position not advancing, breaking loop");
                break;
            }
            
            position = nextPosition;
        }

        return chunks;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Extracts a chunk with smart boundary detection
    /// Priority: paragraph → sentence → word → force
    /// </summary>
    private string ExtractChunk(string text, int startPosition, int desiredSize, int maxSize)
    {
        if (startPosition >= text.Length)
            return string.Empty;

        var availableLength = text.Length - startPosition;
        var actualSize = Math.Min(desiredSize, availableLength);

        if (availableLength <= maxSize)
        {
            return text.Substring(startPosition);
        }

        var chunk = text.Substring(startPosition, actualSize);

        // 1. Paragraph boundary
        var lastParagraphBreak = chunk.LastIndexOf("\n\n", StringComparison.Ordinal);
        if (lastParagraphBreak > maxSize / 2)
        {
            return chunk.Substring(0, lastParagraphBreak + 2);
        }

        // 2. Sentence boundary
        var lastSentenceEnd = Math.Max(
            chunk.LastIndexOf(". ", StringComparison.Ordinal),
            chunk.LastIndexOf(".\n", StringComparison.Ordinal)
        );

        if (lastSentenceEnd > maxSize / 3)
        {
            return chunk.Substring(0, lastSentenceEnd + 1);
        }

        // 3. Word boundary
        var lastSpaceIndex = chunk.LastIndexOf(' ');
        if (lastSpaceIndex > maxSize / 4)
        {
            return chunk.Substring(0, lastSpaceIndex);
        }

        // 4. Force split
        return chunk;
    }

    #endregion
}
