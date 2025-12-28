using KnowledgeSystem.Domain.ValueObjects;

namespace KnowledgeSystem.Domain.Entities;

/// <summary>
/// Represents a semantic unit of content within a KnowledgeDocument
/// Can be: legal article, paragraph, or generic chunk
/// </summary>
public sealed class ContentSection
{
    public SectionId Id { get; }
    public DocumentId DocumentId { get; }
    public int Index { get; }
    public string Content { get; }
    public float[] EmbeddingVector { get; private set; }
    
    // Semantic metadata (optional - depends on sectioning strategy)
    public string? ArticleNumber { get; }
    public string? ArticleTitle { get; }
    public SectionType Type { get; }

    private ContentSection(
        SectionId id,
        DocumentId documentId,
        int index,
        string content,
        SectionType type,
        string? articleNumber = null,
        string? articleTitle = null)
    {
        if (index < 0)
            throw new ArgumentException("Index cannot be negative", nameof(index));
        
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be empty", nameof(content));
        
        Id = id;
        DocumentId = documentId;
        Index = index;
        Content = content;
        Type = type;
        ArticleNumber = articleNumber;
        ArticleTitle = articleTitle;
        EmbeddingVector = Array.Empty<float>(); // Will be set later
    }

    /// <summary>
    /// Factory: Create a legal article section
    /// </summary>
    public static ContentSection CreateArticle(
        DocumentId documentId,
        int index,
        string content,
        string articleNumber,
        string? articleTitle = null)
    {
        if (string.IsNullOrWhiteSpace(articleNumber))
            throw new ArgumentException("Article number cannot be empty for article sections", nameof(articleNumber));

        return new ContentSection(
            SectionId.New(),
            documentId,
            index,
            content,
            SectionType.Article,
            articleNumber,
            articleTitle);
    }

    /// <summary>
    /// Factory: Create a paragraph section
    /// </summary>
    public static ContentSection CreateParagraph(
        DocumentId documentId,
        int index,
        string content)
    {
        return new ContentSection(
            SectionId.New(),
            documentId,
            index,
            content,
            SectionType.Paragraph);
    }

    /// <summary>
    /// Factory: Create a generic section (fallback)
    /// </summary>
    public static ContentSection CreateGeneric(
        DocumentId documentId,
        int index,
        string content)
    {
        return new ContentSection(
            SectionId.New(),
            documentId,
            index,
            content,
            SectionType.Generic);
    }

    /// <summary>
    /// Assign embedding vector to this section
    /// DOMAIN RULE: Embedding vector must have consistent dimensions
    /// </summary>
    public void SetEmbedding(float[] vector)
    {
        if (vector == null || vector.Length == 0)
            throw new ArgumentException("Embedding vector cannot be null or empty", nameof(vector));
        
        // DOMAIN RULE: Embedding dimensions must be consistent (validated externally)
        EmbeddingVector = vector;
    }

    /// <summary>
    /// DOMAIN RULE: Section must have embedding before it can be searched
    /// </summary>
    public bool HasEmbedding() => EmbeddingVector.Length > 0;
}

/// <summary>
/// Type of content section
/// Determined by sectioning strategy
/// </summary>
public enum SectionType
{
    Article,    // Legal article or numbered clause
    Paragraph,  // Paragraph-based section
    Generic     // Fallback size-based section
}

