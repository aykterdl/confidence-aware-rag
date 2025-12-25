using RAG.Api.Models;

namespace RAG.Api.Services;

public interface IVectorSearchService
{
    /// <summary>
    /// Query ile en benzer chunk'ları bulur (cosine similarity)
    /// </summary>
    /// <param name="queryText">Arama sorgusu</param>
    /// <param name="topK">Döndürülecek maksimum sonuç sayısı</param>
    /// <param name="similarityThreshold">Minimum benzerlik skoru (0-1 arası)</param>
    /// <param name="cancellationToken">İptal token</param>
    /// <returns>Benzerlik skoruna göre sıralanmış chunk listesi</returns>
    Task<List<SearchResult>> SearchAsync(
        string queryText,
        int topK = 5,
        double similarityThreshold = 0.0,
        CancellationToken cancellationToken = default
    );
}




