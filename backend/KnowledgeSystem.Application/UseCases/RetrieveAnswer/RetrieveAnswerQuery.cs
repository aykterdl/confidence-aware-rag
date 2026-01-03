namespace KnowledgeSystem.Application.UseCases.RetrieveAnswer;

/// <summary>
/// Query: Retrieve an answer to a user question using RAG
/// </summary>
public sealed class RetrieveAnswerQuery
{
    public required string Question { get; init; }
    
    /// <summary>
    /// Number of similar sections to retrieve
    /// </summary>
    public int TopK { get; init; } = 5;
}

