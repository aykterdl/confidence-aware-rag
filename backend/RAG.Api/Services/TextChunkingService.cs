namespace RAG.Api.Services;

public class TextChunkingService : ITextChunkingService
{
    private readonly ILogger<TextChunkingService> _logger;
    private const int MinChunkSize = 10; // Çok kısa chunk'ları atla

    public TextChunkingService(ILogger<TextChunkingService> logger)
    {
        _logger = logger;
    }

    public List<string> ChunkText(string text, int maxChunkSize = 500, int overlap = 50)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Empty or null text provided for chunking");
            return new List<string>();
        }

        if (overlap >= maxChunkSize)
        {
            throw new ArgumentException("Overlap must be less than maxChunkSize", nameof(overlap));
        }

        _logger.LogInformation("Starting text chunking: text length={Length}, maxChunkSize={MaxSize}, overlap={Overlap}", 
            text.Length, maxChunkSize, overlap);

        var chunks = new List<string>();
        var position = 0;

        while (position < text.Length)
        {
            var remainingLength = text.Length - position;
            var chunkSize = Math.Min(maxChunkSize, remainingLength);

            // Chunk'ı al
            var chunk = ExtractChunk(text, position, chunkSize, maxChunkSize);
            
            if (string.IsNullOrWhiteSpace(chunk) || chunk.Length < MinChunkSize)
            {
                _logger.LogDebug("Skipping empty or too short chunk at position {Position}", position);
                break; // Boş chunk gelirse bitir
            }

            chunks.Add(chunk.Trim());
            _logger.LogDebug("Created chunk {Index}: length={Length}, position={Position}", 
                chunks.Count, chunk.Length, position);

            // Eğer chunk sonu text sonuna ulaştıysa bitir
            if (position + chunk.Length >= text.Length)
            {
                _logger.LogDebug("Reached end of text after chunk {Index}", chunks.Count);
                break;
            }

            // Sonraki pozisyon (overlap ile)
            var nextPosition = position + chunk.Length - overlap;
            
            // İlerleme kontrolü (sonsuz döngüyü önle)
            if (nextPosition <= position)
            {
                _logger.LogWarning("Position not advancing (current: {Current}, next: {Next}). Breaking loop.", 
                    position, nextPosition);
                break;
            }
            
            position = nextPosition;
        }

        _logger.LogInformation("Text chunking completed: {ChunkCount} chunks created", chunks.Count);
        return chunks;
    }

    private string ExtractChunk(string text, int startPosition, int desiredSize, int maxSize)
    {
        if (startPosition >= text.Length)
            return string.Empty;

        var availableLength = text.Length - startPosition;
        var actualSize = Math.Min(desiredSize, availableLength);

        // Eğer kalan metin max size'dan küçükse, hepsini al
        if (availableLength <= maxSize)
        {
            return text.Substring(startPosition);
        }

        var chunk = text.Substring(startPosition, actualSize);

        // 1. Strateji: Paragraf sınırında kes (\n\n)
        var lastParagraphBreak = chunk.LastIndexOf("\n\n", StringComparison.Ordinal);
        if (lastParagraphBreak > maxSize / 2) // En az yarısında olmalı
        {
            _logger.LogTrace("Splitting at paragraph boundary at position {Position}", lastParagraphBreak);
            return chunk.Substring(0, lastParagraphBreak + 2); // \n\n'yi dahil et
        }

        // 2. Strateji: Cümle sınırında kes (.)
        var lastSentenceEnd = Math.Max(
            chunk.LastIndexOf(". ", StringComparison.Ordinal),
            chunk.LastIndexOf(".\n", StringComparison.Ordinal)
        );

        if (lastSentenceEnd > maxSize / 3) // En az üçte birinde olmalı
        {
            _logger.LogTrace("Splitting at sentence boundary at position {Position}", lastSentenceEnd);
            return chunk.Substring(0, lastSentenceEnd + 1); // Noktayı dahil et
        }

        // 3. Strateji: Kelime sınırında kes (boşluk)
        var lastSpaceIndex = chunk.LastIndexOf(' ');
        if (lastSpaceIndex > maxSize / 4) // En az dörtte birinde olmalı
        {
            _logger.LogTrace("Splitting at word boundary at position {Position}", lastSpaceIndex);
            return chunk.Substring(0, lastSpaceIndex);
        }

        // 4. Son çare: Zorla kes
        _logger.LogTrace("Force splitting at max size");
        return chunk;
    }
}


