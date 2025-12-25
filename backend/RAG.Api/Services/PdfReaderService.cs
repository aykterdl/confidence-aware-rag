using UglyToad.PdfPig;
using System.Text;

namespace RAG.Api.Services;

/// <summary>
/// PdfPig kullanarak PDF'lerden metin çıkarır
/// </summary>
public class PdfReaderService : IPdfReaderService
{
    private readonly ILogger<PdfReaderService> _logger;

    public PdfReaderService(ILogger<PdfReaderService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// PDF stream'inden tüm metni çıkarır
    /// </summary>
    public async Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var text = new StringBuilder();
                
                using var document = PdfDocument.Open(pdfStream);
                var pageCount = document.NumberOfPages;
                
                _logger.LogInformation("PDF okuma başladı - {PageCount} sayfa", pageCount);

                foreach (var page in document.GetPages())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    // Her sayfadan metni çıkar
                    var pageText = page.Text;
                    text.AppendLine(pageText);
                    
                    if (page.Number % 10 == 0)
                    {
                        _logger.LogDebug("PDF okuma ilerlemesi: {Page}/{Total}", page.Number, pageCount);
                    }
                }

                var extractedText = text.ToString();
                _logger.LogInformation("PDF okuma tamamlandı - {Length} karakter", extractedText.Length);
                
                return extractedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PDF okuma hatası");
                throw new InvalidOperationException("PDF dosyası okunamadı. Dosya bozuk veya şifreli olabilir.", ex);
            }
        }, cancellationToken);
    }
}

