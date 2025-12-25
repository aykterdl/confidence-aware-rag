using RAG.Api.Models;

namespace RAG.Api.Services;

/// <summary>
/// RAG (Retrieval Augmented Generation) soru-cevap servisi
/// </summary>
public interface IRagAnswerService
{
    /// <summary>
    /// Kullanıcının sorusuna vector search + LLM ile cevap üretir
    /// </summary>
    /// <param name="request">Soru ve parametreler</param>
    /// <param name="cancellationToken">İptal token</param>
    /// <returns>LLM cevabı ve kaynak chunk'lar</returns>
    Task<RagResponse> AskAsync(RagRequest request, CancellationToken cancellationToken = default);
}


