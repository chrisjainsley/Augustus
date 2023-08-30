using System.Net;

namespace Augustus.Tests;

public class StripeTests
{
    [Fact]
    public async Task CustomerExists()
    {
        // Given
        var customerId = "cus_9s6XKzkNRiz8i3";
        var simulator = new APISimulator("stripe", new APISimulatorOptions());
        simulator.StartAsync().ConfigureAwait(false);
        simulator.AddInstruction($"customer {customerId} exists");

        // When
        var client = simulator.CreateClient();
        var response = await client.GetAsync($"/v1/customers/{customerId}");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("Hello World");
    }
}