using Augustus;
using Augustus.Extensions;

namespace Augustus.Tests;

public class CoreTests
{
    [Fact]
    public async Task APISimulator_WithStaticResponse_ShouldReturnConfiguredJson()
    {
        // Arrange
        var simulator = this.CreateAPISimulator()
            .ForGet("/api/test")
            .WithResponse(new { message = "Hello World" })
            .Add();

        await simulator.StartAsync();

        try
        {
            var client = simulator.CreateClient();

            // Act
            var response = await client.GetStringAsync("/api/test");

            // Assert
            response.Should().Contain("Hello World");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task APISimulator_DynamicRouteAddition_ShouldWorkAfterStart()
    {
        // Arrange
        var simulator = this.CreateAPISimulator();
        await simulator.StartAsync();

        try
        {
            // Add route after starting
            simulator.ForGet("/api/dynamic")
                .WithResponse(new { dynamic = true })
                .Add();

            var client = simulator.CreateClient();

            // Act
            var response = await client.GetStringAsync("/api/dynamic");

            // Assert
            response.Should().Contain("\"dynamic\":true");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public void APISimulator_RemoveRoute_ShouldRemoveConfiguredRoute()
    {
        // Arrange
        var simulator = this.CreateAPISimulator()
            .ForGet("/api/test")
            .WithResponse(new { message = "test" })
            .Add();

        // Act
        var removed = simulator.RemoveRoute("/api/test", "GET");

        // Assert
        removed.Should().BeTrue();
    }

    [Fact]
    public async Task APISimulator_ClearRoutes_ShouldRemoveAllRoutes()
    {
        // Arrange
        var simulator = this.CreateAPISimulator()
            .ForGet("/api/test1")
            .WithResponse(new { message = "test1" })
            .Add()
            .ForGet("/api/test2")
            .WithResponse(new { message = "test2" })
            .Add();

        await simulator.StartAsync();

        try
        {
            var client = simulator.CreateClient();

            // Verify routes work before clearing
            var response1 = await client.GetStringAsync("/api/test1");
            response1.Should().Contain("test1");

            // Act - clear all routes
            simulator.ClearRoutes();

            // Assert - verify routes return 404 after clearing
            var response2 = await client.GetAsync("/api/test1");
            response2.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task APISimulator_WithJsonFile_ShouldLoadFromFile()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "{\"message\":\"from file\"}");

        try
        {
            var simulator = this.CreateAPISimulator()
                .ForGet("/api/file-test")
                .WithJsonFile(tempFile)
                .Add();

            await simulator.StartAsync();

            try
            {
                var client = simulator.CreateClient();

                // Act
                var response = await client.GetStringAsync("/api/file-test");

                // Assert
                response.Should().Contain("from file");
            }
            finally
            {
                await simulator.StopAsync();
            }
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
