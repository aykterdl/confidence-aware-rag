namespace KnowledgeSystem.Application.Abstractions.TextExtraction;

/// <summary>
/// Port: Service for extracting text from documents
/// Implementation: Infrastructure layer (PdfPig, etc.)
/// </summary>
public interface ITextExtractor
{
    /// <summary>
    /// Extract text from a document stream
    /// </summary>
    /// <param name="documentStream">Document content stream (PDF, DOCX, etc.)</param>
    /// <param name="contentType">MIME type of the document (e.g., "application/pdf")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Extraction result with text and metadata</returns>
    /// <exception cref="InvalidOperationException">If document format is unsupported or extraction fails</exception>
    Task<TextExtractionResult> ExtractTextAsync(
        Stream documentStream,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the extractor supports the given content type
    /// </summary>
    /// <param name="contentType">MIME type (e.g., "application/pdf")</param>
    /// <returns>True if supported, false otherwise</returns>
    bool SupportsContentType(string contentType);
}

