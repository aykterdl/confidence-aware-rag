namespace KnowledgeSystem.Api.Services;

/// <summary>
/// PDF dosyalarından metin çıkarma servisi
/// </summary>
public interface IPdfReaderService
{
    /// <summary>
    /// PDF stream'inden tüm metni çıkarır
    /// </summary>
    Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken cancellationToken = default);
}


