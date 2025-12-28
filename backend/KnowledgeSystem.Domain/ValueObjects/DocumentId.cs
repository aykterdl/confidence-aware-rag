namespace KnowledgeSystem.Domain.ValueObjects;

/// <summary>
/// Strongly-typed identifier for KnowledgeDocument
/// Prevents primitive obsession and enforces type safety
/// </summary>
public readonly record struct DocumentId
{
    public Guid Value { get; }

    private DocumentId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("DocumentId cannot be empty", nameof(value));
        
        Value = value;
    }

    public static DocumentId New() => new(Guid.NewGuid());
    
    public static DocumentId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

