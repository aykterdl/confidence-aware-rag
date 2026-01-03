namespace KnowledgeSystem.Domain.ValueObjects;

/// <summary>
/// Domain value object representing confidence calculation policy
/// Encapsulates business rules for relevance thresholds
/// </summary>
public sealed class ConfidencePolicy
{
    /// <summary>
    /// Minimum acceptable similarity score (default: 0.04)
    /// Below this threshold: No answer is provided (ConfidenceLevel.None)
    /// </summary>
    public double MinAcceptableThreshold { get; }

    /// <summary>
    /// Threshold for low vs high confidence (default: 0.06)
    /// Below this: Low confidence (cautious answer)
    /// Above this: High confidence (direct answer)
    /// </summary>
    public double LowConfidenceThreshold { get; }

    private ConfidencePolicy(double minAcceptableThreshold, double lowConfidenceThreshold)
    {
        // DOMAIN INVARIANTS: Validate threshold values
        if (minAcceptableThreshold < 0 || minAcceptableThreshold > 1)
            throw new ArgumentOutOfRangeException(
                nameof(minAcceptableThreshold),
                "Threshold must be between 0 and 1");

        if (lowConfidenceThreshold < 0 || lowConfidenceThreshold > 1)
            throw new ArgumentOutOfRangeException(
                nameof(lowConfidenceThreshold),
                "Threshold must be between 0 and 1");

        if (lowConfidenceThreshold < minAcceptableThreshold)
            throw new ArgumentException(
                "Low confidence threshold must be >= min acceptable threshold");

        MinAcceptableThreshold = minAcceptableThreshold;
        LowConfidenceThreshold = lowConfidenceThreshold;
    }

    /// <summary>
    /// Default policy for production use
    /// DOMAIN RULE: These values are determined by business requirements
    /// </summary>
    public static ConfidencePolicy Default => new(
        minAcceptableThreshold: 0.04,
        lowConfidenceThreshold: 0.06);

    /// <summary>
    /// Factory: Create custom policy for specific use cases
    /// </summary>
    public static ConfidencePolicy Create(
        double minAcceptableThreshold,
        double lowConfidenceThreshold) =>
        new(minAcceptableThreshold, lowConfidenceThreshold);
}

