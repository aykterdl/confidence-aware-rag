using KnowledgeSystem.Domain.ValueObjects;

namespace KnowledgeSystem.Domain.Entities;

/// <summary>
/// Aggregate Root: Represents a knowledge document in the system
/// Contains one or more ContentSections and maintains document-level invariants
/// </summary>
public sealed class KnowledgeDocument
{
    private readonly List<ContentSection> _sections = new();

    public DocumentId Id { get; }
    public string Title { get; }
    public DateTime UploadedAt { get; }
    public IReadOnlyList<ContentSection> Sections => _sections.AsReadOnly();
    public int SectionCount => _sections.Count;

    private KnowledgeDocument(DocumentId id, string title, DateTime uploadedAt)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Document title cannot be empty", nameof(title));

        Id = id;
        Title = title;
        UploadedAt = uploadedAt;
    }

    /// <summary>
    /// Factory: Create a new knowledge document
    /// </summary>
    public static KnowledgeDocument Create(string title)
    {
        return new KnowledgeDocument(
            DocumentId.New(),
            title,
            DateTime.UtcNow);
    }

    /// <summary>
    /// Factory: Reconstitute from persistence
    /// </summary>
    public static KnowledgeDocument Reconstitute(
        DocumentId id,
        string title,
        DateTime uploadedAt,
        IEnumerable<ContentSection> sections)
    {
        var document = new KnowledgeDocument(id, title, uploadedAt);
        
        foreach (var section in sections.OrderBy(s => s.Index))
        {
            document._sections.Add(section);
        }

        return document;
    }

    /// <summary>
    /// Add a content section to the document
    /// DOMAIN RULE: Sections must have unique indices
    /// </summary>
    public void AddSection(ContentSection section)
    {
        if (section == null)
            throw new ArgumentNullException(nameof(section));
        
        if (section.DocumentId != Id)
            throw new InvalidOperationException(
                $"Section belongs to document {section.DocumentId}, not {Id}");
        
        // INVARIANT: No duplicate indices
        if (_sections.Any(s => s.Index == section.Index))
            throw new InvalidOperationException(
                $"Section with index {section.Index} already exists");

        _sections.Add(section);
    }

    /// <summary>
    /// Add multiple sections in batch
    /// </summary>
    public void AddSections(IEnumerable<ContentSection> sections)
    {
        foreach (var section in sections)
        {
            AddSection(section);
        }
    }

    /// <summary>
    /// Get a specific section by index
    /// </summary>
    public ContentSection? GetSectionByIndex(int index)
    {
        return _sections.FirstOrDefault(s => s.Index == index);
    }

    /// <summary>
    /// DOMAIN RULE: Document is ready for search if all sections have embeddings
    /// </summary>
    public bool IsReadyForSearch()
    {
        if (_sections.Count == 0)
            return false;

        return _sections.All(s => s.HasEmbedding());
    }

    /// <summary>
    /// DOMAIN RULE: Document must have at least one section
    /// </summary>
    public bool IsValid() => _sections.Count > 0;
}

