using KnowledgeSystem.Api.Models;

namespace KnowledgeSystem.Api.Services;

/// <summary>
/// Conversation history için store interface
/// </summary>
public interface IConversationStore
{
    /// <summary>
    /// Yeni conversation oluşturur
    /// </summary>
    Guid CreateConversation();
    
    /// <summary>
    /// Conversation history'yi getirir
    /// </summary>
    ConversationHistory? GetConversation(Guid conversationId);
    
    /// <summary>
    /// Conversation'a yeni turn ekler
    /// </summary>
    void AddTurn(Guid conversationId, string question, string answer);
    
    /// <summary>
    /// Conversation'ın var olup olmadığını kontrol eder
    /// </summary>
    bool ConversationExists(Guid conversationId);
}


