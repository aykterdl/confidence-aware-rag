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
    /// Calculate confidence based on similarity scores and policy
    /// DOMAIN RULE: Confidence level is determined by max similarity against policy thresholds
    /// </summary>
    public static ConfidenceScore Calculate(
        double maxSimilarity,
        double averageSimilarity,
        ConfidencePolicy policy)
    {
        // Validate inputs
        if (maxSimilarity < 0 || maxSimilarity > 1)
            throw new ArgumentOutOfRangeException(nameof(maxSimilarity));
        
        if (averageSimilarity < 0 || averageSimilarity > 1)
            throw new ArgumentOutOfRangeException(nameof(averageSimilarity));

        // DOMAIN LOGIC: Determine confidence level using policy
        var level = maxSimilarity switch
        {
            var s when s < policy.MinAcceptableThreshold => ConfidenceLevel.None,
            var s when s < policy.LowConfidenceThreshold => ConfidenceLevel.Low,
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

    /// <summary>
    /// Get human-readable explanation of confidence level
    /// DOMAIN RESPONSIBILITY: Domain determines how confidence is explained
    /// </summary>
    public string GetExplanation()
    {
        return Level switch
        {
            ConfidenceLevel.None =>
                $"No relevant information found. Similarity score too low ({MaxSimilarity:P1}).",

            ConfidenceLevel.Low =>
                $"Partial match found ({MaxSimilarity:P1} similarity). " +
                "The answer may be incomplete or uncertain.",

            ConfidenceLevel.High =>
                $"Strong match found ({MaxSimilarity:P1} similarity). " +
                "The answer is based on highly relevant content.",

            _ => "Unknown confidence level"
        };
    }

    public override string ToString() => 
        $"Confidence: {Level} (Max: {MaxSimilarity:P1}, Avg: {AverageSimilarity:P1})";
}

