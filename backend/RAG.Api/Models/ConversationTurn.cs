namespace RAG.Api.Models;

/// <summary>
/// Tek bir soru-cevap çifti (conversation turn)
/// </summary>
public class ConversationTurn
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}


