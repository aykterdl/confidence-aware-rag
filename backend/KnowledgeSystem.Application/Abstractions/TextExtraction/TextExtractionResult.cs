namespace KnowledgeSystem.Application.Abstractions.TextExtraction;

/// <summary>
/// Result of text extraction from a document
/// Contains extracted text and metadata about the extraction process
/// </summary>
public sealed class TextExtractionResult
{
    /// <summary>
    /// Full extracted text content
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Total number of pages in the document (if applicable)
    /// </summary>
    public int PageCount { get; init; }

    /// <summary>
    /// Total character length of extracted text
    /// </summary>
    public int CharacterCount { get; init; }

    /// <summary>
    /// Page boundaries (optional) - maps page numbers to character positions
    /// Key: Page number (1-based)
    /// Value: Start character index in Text
    /// </summary>
    public IReadOnlyDictionary<int, int>? PageBoundaries { get; init; }

    /// <summary>
    /// Additional metadata from extraction (language, encoding, etc.)
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

