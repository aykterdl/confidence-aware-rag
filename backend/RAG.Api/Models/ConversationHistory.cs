namespace RAG.Api.Models;

/// <summary>
/// Bir conversation'ın tüm geçmişi
/// </summary>
public class ConversationHistory
{
    public Guid ConversationId { get; set; }
    public List<ConversationTurn> Turns { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Son N turn'ü döndürür
    /// </summary>
    public List<ConversationTurn> GetRecentTurns(int count)
    {
        return Turns.TakeLast(count).ToList();
    }
    
    /// <summary>
    /// Yeni turn ekler
    /// </summary>
    public void AddTurn(string question, string answer)
    {
        Turns.Add(new ConversationTurn
        {
            Question = question,
            Answer = answer,
            Timestamp = DateTime.UtcNow
        });
        LastAccessedAt = DateTime.UtcNow;
    }
}


