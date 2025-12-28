using KnowledgeSystem.Domain.Entities;
using KnowledgeSystem.Domain.ValueObjects;

namespace KnowledgeSystem.Domain.Services;

/// <summary>
/// Domain service interface for sectioning document content
/// Different strategies: legal/article-based, paragraph-based, generic size-based
/// Implementation will be in Infrastructure layer
/// </summary>
public interface IContentSectioningStrategy
{
    /// <summary>
    /// Check if this strategy can handle the given content
    /// </summary>
    /// <param name="content">Raw document content</param>
    /// <returns>True if this strategy is applicable</returns>
    bool CanHandle(string content);

    /// <summary>
    /// Section the content into ContentSection entities
    /// </summary>
    /// <param name="documentId">Document identifier</param>
    /// <param name="content">Raw document content</param>
    /// <returns>Collection of content sections</returns>
    IEnumerable<ContentSection> SectionContent(DocumentId documentId, string content);
}

