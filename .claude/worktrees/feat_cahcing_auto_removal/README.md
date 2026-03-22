# Augustus.AI 🤖

An AI-powered API simulator library for .NET that generates realistic mock responses using OpenAI, with intelligent caching for test frameworks.

[![NuGet](https://img.shields.io/nuget/v/Augustus.AI.svg)](https://www.nuget.org/packages/Augustus.AI)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Augustus.AI.svg)](https://www.nuget.org/packages/Augustus.AI)
[![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%207.0%20%7C%208.0%20%7C%209.0%20%7C%2010.0-blue.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## 🌟 Features

- **AI-Powered Responses**: Uses OpenAI to generate realistic API responses based on your instructions
- **Intelligent Caching**: Automatically caches generated responses in JSON files for faster subsequent calls
- **Multi-Framework Support**: Works with xUnit, NUnit, and MSTest
- **Easy Integration**: Simple fluent API for quick setup in your tests
- **Configurable**: Customizable OpenAI models, caching options, and ports
- **Multiple .NET Versions**: Supports .NET 6.0, 7.0, 8.0, 9.0, and 10.0

## 🚀 Quick Start

### Installation

```bash
dotnet add package Augustus.AI
```

### Basic Usage

```csharp
using Augustus.Extensions;

public class ApiTests
{
    [Fact]
    public async Task Should_Return_Customer_Data()
    {
        // Arrange
        var simulator = this.CreateStripeSimulator(options => 
        {
            options.OpenAIApiKey = "your-openai-api-key";
            options.EnableCaching = true;
        })
        .WithInstruction("Customer cus_123 exists with name 'John Doe'")
        .WithInstruction("Customer has email john.doe@example.com");

        await simulator.StartAsync();

        // Act
        var client = simulator.CreateClient();
        var response = await client.GetAsync("/v1/customers/cus_123");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("John Doe");
        
        await simulator.StopAsync();
    }
}
```

## 📖 Documentation

### Configuration Options

```csharp
var options = new APISimulatorOptions
{
    OpenAIApiKey = "your-api-key",           // Required: Your OpenAI API key
    OpenAIEndpoint = "",                     // Optional: Custom OpenAI endpoint
    OpenAIModel = "gpt-5-mini",              // Optional: Model to use (default: gpt-5-mini)
    EnableCaching = true,                    // Optional: Enable response caching (default: true)
    CacheFolderPath = "./mocks",             // Optional: Cache folder path (default: ./mocks)
    Port = 9001                              // Optional: Simulator port (default: 9001)
};
```

### Extension Methods

Augustus provides convenient extension methods for popular APIs:

```csharp
// Pre-configured simulators
var stripeSimulator = this.CreateStripeSimulator();
var paypalSimulator = this.CreatePayPalSimulator();
var twilioSimulator = this.CreateTwilioSimulator();
var slackSimulator = this.CreateSlackSimulator();

// Generic simulator
var customSimulator = this.CreateAPISimulator("MyAPI");
```

### Fluent API

Chain methods for clean, readable test setup:

```csharp
var simulator = this.CreateAPISimulator("GitHub")
    .WithInstruction("Repository octocat/Hello-World exists")
    .WithInstruction("It has 1000 stars and 500 forks")
    .WithInstructions(
        "Return realistic GitHub API responses",
        "Include proper JSON structure",
        "Use appropriate HTTP status codes"
    );

await simulator.StartSimulatorAsync(); // Extension method
```

## 🧪 Test Framework Examples

### xUnit

```csharp
using Augustus.Extensions;

public class StripeTests
{
    [Fact]
    public async Task Customer_Should_Exist()
    {
        var simulator = this.CreateStripeSimulator(options => 
        {
            options.OpenAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        });

        // Test implementation...
    }
}
```

### NUnit

```csharp
using Augustus.Extensions;
using NUnit.Framework;

[TestFixture]
public class GitHubTests
{
    private APISimulator simulator;

    [SetUp]
    public void Setup()
    {
        simulator = this.CreateAPISimulator("GitHub", options => 
        {
            options.OpenAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            options.Port = 9002;
        });
    }

    [TearDown]
    public async Task TearDown()
    {
        await simulator?.StopAsync();
    }

    [Test]
    public async Task Repository_Should_Return_Data()
    {
        // Test implementation...
    }
}
```

### MSTest

```csharp
using Augustus.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class SlackTests
{
    private APISimulator simulator;

    [TestInitialize]
    public void Initialize()
    {
        simulator = this.CreateSlackSimulator(options => 
        {
            options.OpenAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            options.Port = 9003;
        });
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await simulator?.StopAsync();
    }

    [TestMethod]
    public async Task Message_Should_Post_Successfully()
    {
        // Test implementation...
    }
}
```

## 📁 Caching System

Augustus automatically caches AI-generated responses in JSON files:

```json
{
  "RequestHash": "A1B2C3D4E5F6G7H8",
  "Response": "{\\"id\\": \\"cus_123\\", \\"name\\": \\"John Doe\\"}",
  "OriginalRequest": "curl -X GET http://localhost:9001/v1/customers/cus_123",
  "Instructions": [
    "You are a Stripe API simulator",
    "Customer cus_123 exists with name 'John Doe'"
  ],
  "Timestamp": "2024-01-15T10:30:00Z"
}
```

### Cache Management

```csharp
// Clear all cached responses
simulator.ClearCache();

// Disable caching for a specific test
var simulator = this.CreateAPISimulator("MyAPI", options => 
{
    options.EnableCaching = false;
});
```

## 🔧 Advanced Usage

### Custom Instructions

Provide detailed instructions to get better AI responses:

```csharp
simulator
    .WithInstruction("You are simulating a payment processing API")
    .WithInstruction("All payments should have a unique transaction ID")
    .WithInstruction("Use realistic credit card data (test cards only)")
    .WithInstruction("Return proper HTTP status codes (200 for success, 400 for invalid data)")
    .WithInstruction("Include timestamps in ISO 8601 format")
    .WithInstruction("Payment amounts should be in cents (e.g., 2000 = $20.00)");
```

### Environment Variables

Set up your OpenAI API key using environment variables:

```bash
# Windows
set OPENAI_API_KEY=your-api-key-here

# Linux/macOS
export OPENAI_API_KEY=your-api-key-here
```

```csharp
var simulator = this.CreateAPISimulator("MyAPI", options => 
{
    options.OpenAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
        ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is required");
});
```

## 📦 Releases

Augustus is automatically published to NuGet.org when a new GitHub release is created.

### For Users

Visit [Augustus.AI on NuGet.org](https://www.nuget.org/packages/Augustus.AI) to see the latest versions and release notes.

### For Contributors

See [.github/RELEASE.md](.github/RELEASE.md) for detailed information on:
- How releases are automated
- Version numbering conventions
- How to create a new release

## 🛠️ Building from Source

1. Clone the repository
2. Ensure you have .NET 8.0+ SDK installed (supports .NET 6.0 through 10.0)
3. Run the tests:

```bash
dotnet test
```

4. Build the package:

```bash
dotnet pack
```

## 📝 Best Practices

1. **Use Environment Variables**: Store your OpenAI API key in environment variables, not in code
2. **Enable Caching**: Keep caching enabled for faster test execution and reduced API costs
3. **Specific Instructions**: Provide detailed, specific instructions for better AI responses
4. **Unique Ports**: Use different ports for different test classes to avoid conflicts
5. **Cleanup Resources**: Always call `StopAsync()` in test cleanup methods

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🐛 Support

If you encounter any issues or have questions:

1. Check the [documentation](#documentation)
2. Search existing [issues](https://github.com/yourusername/augustus/issues)
3. Create a new issue if needed

## 🎯 Roadmap

- [ ] Support for more AI providers (Anthropic Claude, Google Gemini)
- [ ] Request/Response validation
- [ ] Performance metrics and analytics
- [ ] Docker support
- [ ] Integration with popular API testing tools

---

**Made with ❤️ for the .NET testing community**