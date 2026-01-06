namespace KnowledgeSystem.Application.Abstractions.Chunking;

/// <summary>
/// Port: Strategy for splitting document text into semantic chunks
/// Implementation: Infrastructure layer
/// 
/// Strategies should:
/// - Prefer paragraph/section boundaries
/// - Respect semantic continuity
/// - Never break mid-sentence
/// - Include soft overlap for context
/// - Maintain readability
/// </summary>
public interface IChunkingStrategy
{
    /// <summary>
    /// Split document text into semantic chunks
    /// </summary>
    /// <param name="text">Full document text</param>
    /// <param name="maxChunkSize">Maximum characters per chunk (soft limit)</param>
    /// <param name="overlapSize">Character overlap between chunks for context continuity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document chunks with semantic boundaries</returns>
    Task<IReadOnlyList<DocumentChunk>> ChunkAsync(
        string text,
        int maxChunkSize = 1000,
        int overlapSize = 200,
        CancellationToken cancellationToken = default);
}

