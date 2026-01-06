using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using Microsoft.Extensions.Logging;
using KnowledgeSystem.Application.Abstractions.TextExtraction;

namespace KnowledgeSystem.Infrastructure.TextExtraction;

/// <summary>
/// PDF text extractor using PdfPig library
/// Extracts clean, readable text with page boundaries and metadata
/// </summary>
public sealed class PdfTextExtractor : ITextExtractor
{
    private readonly ILogger<PdfTextExtractor> _logger;
    private const string PdfContentType = "application/pdf";

    public PdfTextExtractor(ILogger<PdfTextExtractor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool SupportsContentType(string contentType)
    {
        return contentType?.Equals(PdfContentType, StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <inheritdoc />
    public async Task<TextExtractionResult> ExtractTextAsync(
        Stream documentStream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (documentStream == null)
            throw new ArgumentNullException(nameof(documentStream));

        if (!SupportsContentType(contentType))
        {
            throw new InvalidOperationException(
                $"Unsupported content type: {contentType}. This extractor only supports PDF documents.");
        }

        try
        {
            _logger.LogInformation("Starting PDF text extraction...");

            // PdfPig requires a seekable stream, so copy to MemoryStream if needed
            Stream seekableStream = documentStream;
            if (!documentStream.CanSeek)
            {
                _logger.LogDebug("Input stream is not seekable, copying to MemoryStream");
                var memoryStream = new MemoryStream();
                await documentStream.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;
                seekableStream = memoryStream;
            }

            using var pdfDocument = PdfDocument.Open(seekableStream);
            
            var totalPages = pdfDocument.NumberOfPages;
            _logger.LogInformation("PDF has {PageCount} pages", totalPages);

            var fullTextBuilder = new StringBuilder();
            var pageBoundaries = new Dictionary<int, int>();
            var currentPosition = 0;

            // Extract text from each page
            for (int pageNum = 1; pageNum <= totalPages; pageNum++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var page = pdfDocument.GetPage(pageNum);
                var pageText = ExtractPageText(page);

                // Record page boundary (start position of this page)
                pageBoundaries[pageNum] = currentPosition;

                // Append page text
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    fullTextBuilder.Append(pageText);
                    
                    // Add page separator (double newline) unless it's the last page
                    if (pageNum < totalPages)
                    {
                        fullTextBuilder.AppendLine();
                        fullTextBuilder.AppendLine();
                    }

                    currentPosition = fullTextBuilder.Length;
                }
                else
                {
                    _logger.LogWarning("Page {PageNumber} appears to be empty", pageNum);
                }
            }

            var fullText = fullTextBuilder.ToString();
            var characterCount = fullText.Length;

            _logger.LogInformation(
                "PDF text extraction completed. Pages: {PageCount}, Characters: {CharacterCount}",
                totalPages, characterCount);

            if (characterCount == 0)
            {
                throw new InvalidOperationException(
                    "No text could be extracted from the PDF. The document may be image-based or corrupted.");
            }

            return new TextExtractionResult
            {
                Text = fullText,
                PageCount = totalPages,
                CharacterCount = characterCount,
                PageBoundaries = pageBoundaries,
                Metadata = new Dictionary<string, string>
                {
                    ["extractor"] = "PdfPig",
                    ["version"] = typeof(PdfDocument).Assembly.GetName().Version?.ToString() ?? "unknown"
                }
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to extract text from PDF");
            throw new InvalidOperationException(
                $"PDF text extraction failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Extract text from a single PDF page with proper formatting
    /// </summary>
    private string ExtractPageText(Page page)
    {
        try
        {
            // Get page text using PdfPig's text extraction
            var pageText = page.Text;

            if (string.IsNullOrWhiteSpace(pageText))
                return string.Empty;

            // Basic cleanup:
            // - Normalize line endings
            // - Remove excessive whitespace
            // - Preserve paragraph breaks
            var lines = pageText
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line));

            return string.Join(Environment.NewLine, lines);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting text from page {PageNumber}", page.Number);
            return string.Empty;
        }
    }
}

