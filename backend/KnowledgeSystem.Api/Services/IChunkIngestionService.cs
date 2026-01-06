namespace KnowledgeSystem.Api.Services;

public interface IChunkIngestionService
{
    /// <summary>
    /// Metni chunk'lara böler, embedding üretir ve veritabanına kaydeder
    /// </summary>
    /// <param name="text">İşlenecek metin</param>
    /// <param name="documentTitle">Döküman başlığı</param>
    /// <param name="metadata">Opsiyonel metadata (JSON)</param>
    /// <param name="cancellationToken">İptal token</param>
    /// <returns>Oluşturulan döküman ID ve chunk sayısı</returns>
    Task<ChunkIngestionResult> IngestTextAsync(
        string text,
        string documentTitle,
        string? metadata = null,
        CancellationToken cancellationToken = default
    );
}

public record ChunkIngestionResult(
    Guid DocumentId,
    int ChunkCount,
    string DocumentTitle
);




