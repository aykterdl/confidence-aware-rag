using KnowledgeSystem.Domain.ValueObjects;
using Xunit;

namespace KnowledgeSystem.Domain.Tests.ValueObjects;

public class ConfidenceScoreTests
{
    [Theory]
    [InlineData(0.03, 0.02, 0.04, 0.06, ConfidenceLevel.None)]
    [InlineData(0.05, 0.04, 0.04, 0.06, ConfidenceLevel.Low)]
    [InlineData(0.07, 0.06, 0.04, 0.06, ConfidenceLevel.High)]
    [InlineData(0.15, 0.12, 0.04, 0.06, ConfidenceLevel.High)]
    public void Calculate_WithVariousSimilarities_ReturnsCorrectLevel(
        double maxSim,
        double avgSim,
        double minThreshold,
        double lowThreshold,
        ConfidenceLevel expectedLevel)
    {
        // Act
        var score = ConfidenceScore.Calculate(maxSim, avgSim, minThreshold, lowThreshold);

        // Assert
        Assert.Equal(expectedLevel, score.Level);
        Assert.Equal(maxSim, score.MaxSimilarity);
        Assert.Equal(avgSim, score.AverageSimilarity);
    }

    [Fact]
    public void Calculate_WithMaxBelowMinThreshold_ReturnsNone()
    {
        // Arrange
        var maxSim = 0.03;
        var avgSim = 0.02;
        var minThreshold = 0.04;
        var lowThreshold = 0.06;

        // Act
        var score = ConfidenceScore.Calculate(maxSim, avgSim, minThreshold, lowThreshold);

        // Assert
        Assert.Equal(ConfidenceLevel.None, score.Level);
        Assert.False(score.IsAcceptable());
    }

    [Fact]
    public void Calculate_WithMaxBetweenMinAndLowThreshold_ReturnsLow()
    {
        // Arrange
        var maxSim = 0.05;
        var avgSim = 0.04;
        var minThreshold = 0.04;
        var lowThreshold = 0.06;

        // Act
        var score = ConfidenceScore.Calculate(maxSim, avgSim, minThreshold, lowThreshold);

        // Assert
        Assert.Equal(ConfidenceLevel.Low, score.Level);
        Assert.True(score.IsAcceptable());
        Assert.True(score.RequiresCaution());
    }

    [Fact]
    public void Calculate_WithMaxAboveLowThreshold_ReturnsHigh()
    {
        // Arrange
        var maxSim = 0.10;
        var avgSim = 0.08;
        var minThreshold = 0.04;
        var lowThreshold = 0.06;

        // Act
        var score = ConfidenceScore.Calculate(maxSim, avgSim, minThreshold, lowThreshold);

        // Assert
        Assert.Equal(ConfidenceLevel.High, score.Level);
        Assert.True(score.IsAcceptable());
        Assert.False(score.RequiresCaution());
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(2.0)]
    public void Calculate_WithInvalidSimilarity_ThrowsException(double invalidSimilarity)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ConfidenceScore.Calculate(invalidSimilarity, 0.5, 0.04, 0.06));
    }

    [Fact]
    public void Calculate_WithLowThresholdBelowMinThreshold_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            ConfidenceScore.Calculate(0.5, 0.4, 0.06, 0.04)); // lowThreshold < minThreshold
    }
}

