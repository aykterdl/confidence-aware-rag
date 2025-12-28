namespace KnowledgeSystem.Domain.ValueObjects;

/// <summary>
/// Strongly-typed identifier for ContentSection
/// Prevents primitive obsession and enforces type safety
/// </summary>
public readonly record struct SectionId
{
    public Guid Value { get; }

    private SectionId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("SectionId cannot be empty", nameof(value));
        
        Value = value;
    }

    public static SectionId New() => new(Guid.NewGuid());
    
    public static SectionId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

