namespace KnowledgeSystem.Domain.ValueObjects;

/// <summary>
/// Confidence level for answer retrieval quality
/// Encapsulates domain rules for confidence calculation
/// </summary>
public enum ConfidenceLevel
{
    None,    // No relevant information found
    Low,     // Partial match, uncertain answer
    High     // Strong match, reliable answer
}

/// <summary>
/// Immutable value object representing confidence in an answer
/// Encapsulates domain logic for confidence calculation
/// </summary>
public sealed class ConfidenceScore
{
    public ConfidenceLevel Level { get; }
    public double MaxSimilarity { get; }
    public double AverageSimilarity { get; }

    private ConfidenceScore(ConfidenceLevel level, double maxSimilarity, double averageSimilarity)
    {
        if (maxSimilarity < 0 || maxSimilarity > 1)
            throw new ArgumentOutOfRangeException(nameof(maxSimilarity), "Similarity must be between 0 and 1");
        
        if (averageSimilarity < 0 || averageSimilarity > 1)
            throw new ArgumentOutOfRangeException(nameof(averageSimilarity), "Similarity must be between 0 and 1");

        Level = level;
        MaxSimilarity = maxSimilarity;
        AverageSimilarity = averageSimilarity;
    }

    /// <summary>
    /// Calculate confidence based on similarity scores and thresholds
    /// DOMAIN RULE: Confidence level is determined by max similarity against thresholds
    /// </summary>
    public static ConfidenceScore Calculate(
        double maxSimilarity,
        double averageSimilarity,
        double minAcceptableThreshold,
        double lowConfidenceThreshold)
    {
        // Validate inputs
        if (maxSimilarity < 0 || maxSimilarity > 1)
            throw new ArgumentOutOfRangeException(nameof(maxSimilarity));
        
        if (averageSimilarity < 0 || averageSimilarity > 1)
            throw new ArgumentOutOfRangeException(nameof(averageSimilarity));
        
        if (minAcceptableThreshold < 0 || minAcceptableThreshold > 1)
            throw new ArgumentOutOfRangeException(nameof(minAcceptableThreshold));
        
        if (lowConfidenceThreshold < 0 || lowConfidenceThreshold > 1)
            throw new ArgumentOutOfRangeException(nameof(lowConfidenceThreshold));
        
        if (lowConfidenceThreshold < minAcceptableThreshold)
            throw new ArgumentException("Low confidence threshold must be >= min acceptable threshold");

        // DOMAIN LOGIC: Determine confidence level
        var level = maxSimilarity switch
        {
            var s when s < minAcceptableThreshold => ConfidenceLevel.None,
            var s when s < lowConfidenceThreshold => ConfidenceLevel.Low,
            _ => ConfidenceLevel.High
        };

        return new ConfidenceScore(level, maxSimilarity, averageSimilarity);
    }

    /// <summary>
    /// DOMAIN RULE: Answer is acceptable if confidence is not None
    /// </summary>
    public bool IsAcceptable() => Level != ConfidenceLevel.None;

    /// <summary>
    /// DOMAIN RULE: Answer requires caution if confidence is Low
    /// </summary>
    public bool RequiresCaution() => Level == ConfidenceLevel.Low;

    public override string ToString() => 
        $"Confidence: {Level} (Max: {MaxSimilarity:P1}, Avg: {AverageSimilarity:P1})";
}

