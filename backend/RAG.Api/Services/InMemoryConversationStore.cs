using System.Collections.Concurrent;
using RAG.Api.Models;

namespace RAG.Api.Services;

/// <summary>
/// In-memory conversation store (lightweight, stateless restart'da kaybolur)
/// Production'da Redis veya DB kullanılabilir
/// </summary>
public class InMemoryConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<Guid, ConversationHistory> _conversations = new();
    private readonly ILogger<InMemoryConversationStore> _logger;
    private readonly int _maxConversations;
    private readonly TimeSpan _conversationTtl;

    public InMemoryConversationStore(
        ILogger<InMemoryConversationStore> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _maxConversations = configuration.GetValue<int>("Conversation:MaxConversations", 1000);
        _conversationTtl = TimeSpan.FromHours(configuration.GetValue<int>("Conversation:TtlHours", 24));
        
        _logger.LogInformation("InMemoryConversationStore initialized: MaxConversations={Max}, TTL={Ttl}h",
            _maxConversations, _conversationTtl.TotalHours);
    }

    public Guid CreateConversation()
    {
        CleanupOldConversations();
        
        var conversationId = Guid.NewGuid();
        var history = new ConversationHistory
        {
            ConversationId = conversationId
        };
        
        _conversations.TryAdd(conversationId, history);
        _logger.LogInformation("Created new conversation: {ConversationId}", conversationId);
        
        return conversationId;
    }

    public ConversationHistory? GetConversation(Guid conversationId)
    {
        if (_conversations.TryGetValue(conversationId, out var history))
        {
            // TTL kontrolü
            if (DateTime.UtcNow - history.LastAccessedAt > _conversationTtl)
            {
                _conversations.TryRemove(conversationId, out _);
                _logger.LogWarning("Conversation expired: {ConversationId}", conversationId);
                return null;
            }
            
            history.LastAccessedAt = DateTime.UtcNow;
            return history;
        }
        
        return null;
    }

    public void AddTurn(Guid conversationId, string question, string answer)
    {
        if (_conversations.TryGetValue(conversationId, out var history))
        {
            history.AddTurn(question, answer);
            _logger.LogDebug("Added turn to conversation {ConversationId}: Q={Question}",
                conversationId, question.Substring(0, Math.Min(50, question.Length)));
        }
        else
        {
            _logger.LogWarning("Attempted to add turn to non-existent conversation: {ConversationId}",
                conversationId);
        }
    }

    public bool ConversationExists(Guid conversationId)
    {
        return _conversations.ContainsKey(conversationId);
    }

    /// <summary>
    /// Eski conversation'ları temizler (memory leak önleme)
    /// </summary>
    private void CleanupOldConversations()
    {
        if (_conversations.Count < _maxConversations)
            return;

        var expiredKeys = _conversations
            .Where(kvp => DateTime.UtcNow - kvp.Value.LastAccessedAt > _conversationTtl)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _conversations.TryRemove(key, out _);
        }

        _logger.LogInformation("Cleaned up {Count} expired conversations", expiredKeys.Count);
        
        // Hala limit aşılıyorsa, en eski conversation'ları temizle
        if (_conversations.Count >= _maxConversations)
        {
            var oldestKeys = _conversations
                .OrderBy(kvp => kvp.Value.LastAccessedAt)
                .Take(_conversations.Count - _maxConversations + 100)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in oldestKeys)
            {
                _conversations.TryRemove(key, out _);
            }
            
            _logger.LogWarning("Force-cleaned {Count} oldest conversations to prevent memory overflow",
                oldestKeys.Count);
        }
    }
}


