using Augustus;
using Augustus.Extensions;
using Augustus.AI;

namespace Augustus.AI.Tests;

public class AIResponseTests
{
    [Fact]
    public void AIOptions_Validate_ShouldThrowWhenApiKeyMissing()
    {
        // Arrange
        var options = new AIOptions();

        // Act & Assert
        var exception = Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(
            () => options.Validate());

        exception.Message.Should().Contain("OpenAI API key is required");
    }

    [Fact]
    public void AIOptions_Validate_ShouldPassWhenApiKeyProvided()
    {
        // Arrange
        var options = new AIOptions
        {
            OpenAIApiKey = "sk-test123"
        };

        // Act & Assert - should not throw
        options.Validate();
    }

    [Fact]
    public void AIOptions_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var options = new AIOptions();

        // Assert
        options.OpenAIModel.Should().Be("gpt-4o-mini");
        options.EnableCaching.Should().BeTrue();
        options.CacheFolderPath.Should().Be("./mocks");
        options.MaxRetries.Should().Be(5);
        options.InitialRetryDelayMs.Should().Be(1000);
        options.MaxRetryDelayMs.Should().Be(32000);
        options.MaxConcurrentRequests.Should().Be(10);
    }

    // Note: Integration tests requiring actual OpenAI API calls would need API key
    // and should be marked with traits to exclude from standard test runs:
    // [Fact(Skip = "Requires OpenAI API key")]
    // Or use [Trait("Category", "Integration")]
}
