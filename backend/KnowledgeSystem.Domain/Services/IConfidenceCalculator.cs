using KnowledgeSystem.Domain.ValueObjects;

namespace KnowledgeSystem.Domain.Services;

/// <summary>
/// Domain service interface for calculating confidence scores
/// Implementation will be in Infrastructure layer
/// </summary>
public interface IConfidenceCalculator
{
    /// <summary>
    /// Calculate confidence score from similarity values
    /// </summary>
    /// <param name="similarities">Collection of similarity scores (0-1 range)</param>
    /// <param name="minAcceptableThreshold">Minimum similarity to accept answer</param>
    /// <param name="lowConfidenceThreshold">Threshold for low vs high confidence</param>
    /// <returns>Calculated confidence score</returns>
    ConfidenceScore CalculateFromSimilarities(
        IEnumerable<double> similarities,
        double minAcceptableThreshold,
        double lowConfidenceThreshold);
}

