using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using KnowledgeSystem.Application.Abstractions.Chunking;

namespace KnowledgeSystem.Infrastructure.Chunking;

/// <summary>
/// Semantic-aware chunking strategy
/// Splits text at natural boundaries (paragraphs, sentences) while maintaining semantic coherence
/// 
/// Strategy:
/// 1. Split by double newlines (paragraphs)
/// 2. Combine paragraphs until max size reached
/// 3. If single paragraph exceeds max, split by sentences
/// 4. Add overlap between chunks for context
/// 5. Never break mid-sentence
/// </summary>
public sealed class SemanticChunkingStrategy : IChunkingStrategy
{
    private readonly ILogger<SemanticChunkingStrategy> _logger;

    // Sentence ending patterns (period, exclamation, question mark followed by space or end)
    private static readonly Regex SentenceEndRegex = new(
        @"(?<=[.!?])\s+(?=[A-Z])|(?<=[.!?])$",
        RegexOptions.Compiled);

    // Paragraph separator (2 or more newlines)
    private static readonly Regex ParagraphSeparatorRegex = new(
        @"\n\s*\n",
        RegexOptions.Compiled);

    public SemanticChunkingStrategy(ILogger<SemanticChunkingStrategy> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DocumentChunk>> ChunkAsync(
        string text,
        int maxChunkSize = 1000,
        int overlapSize = 200,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Empty or null text provided for chunking");
            return Task.FromResult<IReadOnlyList<DocumentChunk>>(Array.Empty<DocumentChunk>());
        }

        if (maxChunkSize <= 0)
            throw new ArgumentException("Max chunk size must be positive", nameof(maxChunkSize));

        if (overlapSize < 0 || overlapSize >= maxChunkSize)
            throw new ArgumentException("Overlap size must be non-negative and less than max chunk size", nameof(overlapSize));

        _logger.LogInformation(
            "Starting semantic chunking. Text length: {TextLength}, Max chunk: {MaxChunk}, Overlap: {Overlap}",
            text.Length, maxChunkSize, overlapSize);

        var chunks = PerformChunking(text, maxChunkSize, overlapSize);

        _logger.LogInformation(
            "Chunking completed. Created {ChunkCount} chunks. " +
            "Average size: {AvgSize:F0}, Min: {MinSize}, Max: {MaxSize}",
            chunks.Count,
            chunks.Average(c => c.Content.Length),
            chunks.Min(c => c.Content.Length),
            chunks.Max(c => c.Content.Length));

        return Task.FromResult<IReadOnlyList<DocumentChunk>>(chunks);
    }

    private List<DocumentChunk> PerformChunking(string text, int maxChunkSize, int overlapSize)
    {
        var chunks = new List<DocumentChunk>();
        
        // Step 1: Split by paragraphs
        var paragraphs = ParagraphSeparatorRegex
            .Split(text)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        _logger.LogDebug("Text split into {ParagraphCount} paragraphs", paragraphs.Count);

        // Step 2: Group paragraphs into chunks
        var currentChunk = new StringBuilder();
        var chunkIndex = 0;

        foreach (var paragraph in paragraphs)
        {
            // Check if adding this paragraph would exceed max size
            var potentialSize = currentChunk.Length + paragraph.Length + 2; // +2 for paragraph separator

            if (currentChunk.Length > 0 && potentialSize > maxChunkSize)
            {
                // Finalize current chunk
                var chunkContent = currentChunk.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(chunkContent))
                {
                    chunks.Add(CreateChunk(chunkIndex++, chunkContent));
                }

                // Start new chunk with overlap
                currentChunk.Clear();
                
                // Add overlap from previous chunk
                if (chunks.Count > 0 && overlapSize > 0)
                {
                    var overlapText = GetOverlapText(chunkContent, overlapSize);
                    if (!string.IsNullOrWhiteSpace(overlapText))
                    {
                        currentChunk.Append(overlapText);
                        currentChunk.Append("\n\n");
                    }
                }
            }

            // Add paragraph to current chunk
            if (currentChunk.Length > 0)
            {
                currentChunk.Append("\n\n");
            }

            // If single paragraph is too large, split by sentences
            if (paragraph.Length > maxChunkSize)
            {
                var sentenceChunks = SplitParagraphBySentences(paragraph, maxChunkSize);
                
                foreach (var sentenceChunk in sentenceChunks)
                {
                    if (currentChunk.Length > 0)
                    {
                        chunks.Add(CreateChunk(chunkIndex++, currentChunk.ToString().Trim()));
                        currentChunk.Clear();
                    }
                    
                    chunks.Add(CreateChunk(chunkIndex++, sentenceChunk));
                }
            }
            else
            {
                currentChunk.Append(paragraph);
            }
        }

        // Add final chunk
        if (currentChunk.Length > 0)
        {
            var finalContent = currentChunk.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(finalContent))
            {
                chunks.Add(CreateChunk(chunkIndex, finalContent));
            }
        }

        return chunks;
    }

    /// <summary>
    /// Split a large paragraph by sentences to respect max size
    /// </summary>
    private List<string> SplitParagraphBySentences(string paragraph, int maxChunkSize)
    {
        var result = new List<string>();
        var sentences = SentenceEndRegex.Split(paragraph)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        _logger.LogDebug("Large paragraph split into {SentenceCount} sentences", sentences.Count);

        var currentGroup = new StringBuilder();

        foreach (var sentence in sentences)
        {
            var trimmedSentence = sentence.Trim();
            
            // If single sentence is too large, force add it as its own chunk
            if (trimmedSentence.Length > maxChunkSize)
            {
                if (currentGroup.Length > 0)
                {
                    result.Add(currentGroup.ToString().Trim());
                    currentGroup.Clear();
                }
                
                result.Add(trimmedSentence);
                _logger.LogWarning(
                    "Single sentence exceeds max chunk size: {Length} > {MaxSize}. Adding as-is.",
                    trimmedSentence.Length, maxChunkSize);
                continue;
            }

            // Check if adding this sentence would exceed max
            var potentialSize = currentGroup.Length + trimmedSentence.Length + 1; // +1 for space

            if (currentGroup.Length > 0 && potentialSize > maxChunkSize)
            {
                result.Add(currentGroup.ToString().Trim());
                currentGroup.Clear();
            }

            if (currentGroup.Length > 0)
            {
                currentGroup.Append(' ');
            }
            
            currentGroup.Append(trimmedSentence);
        }

        if (currentGroup.Length > 0)
        {
            result.Add(currentGroup.ToString().Trim());
        }

        return result;
    }

    /// <summary>
    /// Get overlap text from the end of a chunk
    /// Tries to get complete sentences for better context
    /// </summary>
    private string GetOverlapText(string text, int overlapSize)
    {
        if (text.Length <= overlapSize)
            return text;

        var startPosition = text.Length - overlapSize;
        var overlapText = text.Substring(startPosition);

        // Try to start at a sentence boundary for cleaner overlap
        var firstSentenceEnd = overlapText.IndexOfAny(new[] { '.', '!', '?' });
        if (firstSentenceEnd > 0 && firstSentenceEnd < overlapText.Length - 1)
        {
            overlapText = overlapText.Substring(firstSentenceEnd + 1).TrimStart();
        }

        return overlapText.Trim();
    }

    /// <summary>
    /// Create a DocumentChunk with metadata
    /// </summary>
    private DocumentChunk CreateChunk(int index, string content)
    {
        return new DocumentChunk
        {
            Index = index,
            Content = content,
            Metadata = new Dictionary<string, string>
            {
                ["chunk_type"] = "semantic",
                ["strategy"] = "paragraph-first"
            }
        };
    }
}

