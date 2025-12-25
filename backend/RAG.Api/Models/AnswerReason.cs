namespace RAG.Api.Models;

/// <summary>
/// Internal reason codes for RAG answer generation
/// Drives behavior and improves debugging
/// NOT exposed in public API (yet)
/// </summary>
public enum AnswerReason
{
    /// <summary>
    /// Strong semantic match - high confidence answer
    /// </summary>
    StrongMatch,
    
    /// <summary>
    /// Partial semantic match - low confidence answer
    /// Answer may be incomplete or uncertain
    /// </summary>
    PartialMatch,
    
    /// <summary>
    /// No relevant chunks found in vector search
    /// </summary>
    NoRelevantChunks,
    
    /// <summary>
    /// Similarity below minimum threshold
    /// LLM not called - fallback message returned
    /// </summary>
    BelowRelevanceThreshold
}

