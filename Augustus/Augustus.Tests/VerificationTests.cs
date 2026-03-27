using System.Net.Http;
using System.Text;
using Augustus;
using Augustus.Extensions;

namespace Augustus.Tests;

public class VerificationTests
{
    [Fact]
    public async Task Verify_WasCalledOnce_ShouldPassWhenCalledExactlyOnce()
    {
        var simulator = this.CreateAPISimulator(o => o.Port = 0)
            .ForGet("/api/test")
            .WithResponse(new { ok = true })
            .Add();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();
            await client.GetAsync("/api/test");

            simulator.Verify(r => r.Path == "/api/test")
                .WasCalledOnce();
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task Verify_WasCalledOnce_ShouldThrowWhenCalledMultipleTimes()
    {
        var simulator = this.CreateAPISimulator(o => o.Port = 0)
            .ForGet("/api/test")
            .WithResponse(new { ok = true })
            .Add();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();
            await client.GetAsync("/api/test");
            await client.GetAsync("/api/test");

            var act = () => simulator.Verify(r => r.Path == "/api/test")
                .WasCalledOnce();

            act.Should().Throw<VerificationException>();
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task Verify_WasCalledTimes_ShouldPassWhenCountMatches()
    {
        var simulator = this.CreateAPISimulator(o => o.Port = 0)
            .ForPost("/api/items")
            .WithResponse(new { id = 1 })
            .Add();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();
            await client.PostAsync("/api/items", new StringContent("{}", Encoding.UTF8, "application/json"));
            await client.PostAsync("/api/items", new StringContent("{}", Encoding.UTF8, "application/json"));
            await client.PostAsync("/api/items", new StringContent("{}", Encoding.UTF8, "application/json"));

            simulator.Verify(r => r.Path == "/api/items")
                .WasCalledTimes(3);
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task Verify_WasCalledTimes_ShouldThrowWhenCountDoesNotMatch()
    {
        var simulator = this.CreateAPISimulator(o => o.Port = 0)
            .ForGet("/api/test")
            .WithResponse(new { ok = true })
            .Add();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();
            await client.GetAsync("/api/test");

            var act = () => simulator.Verify(r => r.Path == "/api/test")
                .WasCalledTimes(5);

            act.Should().Throw<VerificationException>()
                .WithMessage("*Expected exactly 5*but found 1*");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task Verify_WasNeverCalled_ShouldPassWhenNotCalled()
    {
        var simulator = this.CreateAPISimulator(o => o.Port = 0)
            .ForGet("/api/test")
            .WithResponse(new { ok = true })
            .Add();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();
            await client.GetAsync("/api/test");

            simulator.Verify(r => r.Path == "/api/other")
                .WasNeverCalled();
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task Verify_WasNeverCalled_ShouldThrowWhenCalled()
    {
        var simulator = this.CreateAPISimulator(o => o.Port = 0)
            .ForGet("/api/test")
            .WithResponse(new { ok = true })
            .Add();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();
            await client.GetAsync("/api/test");

            var act = () => simulator.Verify(r => r.Path == "/api/test")
                .WasNeverCalled();

            act.Should().Throw<VerificationException>()
                .WithMessage("*Expected no matching*but found 1*");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task Verify_WasCalledAtLeast_ShouldPassWhenCountMeetsMinimum()
    {
        var simulator = this.CreateAPISimulator(o => o.Port = 0)
            .ForGet("/api/test")
            .WithResponse(new { ok = true })
            .Add();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();
            await client.GetAsync("/api/test");
            await client.GetAsync("/api/test");
            await client.GetAsync("/api/test");

            simulator.Verify(r => r.Path == "/api/test")
                .WasCalledAtLeast(2);
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task Verify_WasCalledAtLeast_ShouldThrowWhenBelowMinimum()
    {
        var simulator = this.CreateAPISimulator(o => o.Port = 0)
            .ForGet("/api/test")
            .WithResponse(new { ok = true })
            .Add();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();
            await client.GetAsync("/api/test");

            var act = () => simulator.Verify(r => r.Path == "/api/test")
                .WasCalledAtLeast(5);

            act.Should().Throw<VerificationException>()
                .WithMessage("*at least 5*but found 1*");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task Verify_WasCalledAtMost_ShouldPassWhenCountWithinMaximum()
    {
        var simulator = this.CreateAPISimulator(o => o.Port = 0)
            .ForGet("/api/test")
            .WithResponse(new { ok = true })
            .Add();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();
            await client.GetAsync("/api/test");

            simulator.Verify(r => r.Path == "/api/test")
                .WasCalledAtMost(5);
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task Verify_WasCalledAtMost_ShouldThrowWhenExceedsMaximum()
    {
        var simulator = this.CreateAPISimulator(o => o.Port = 0)
            .ForGet("/api/test")
            .WithResponse(new { ok = true })
            .Add();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();
            await client.GetAsync("/api/test");
            await client.GetAsync("/api/test");
            await client.GetAsync("/api/test");

            var act = () => simulator.Verify(r => r.Path == "/api/test")
                .WasCalledAtMost(1);

            act.Should().Throw<VerificationException>()
                .WithMessage("*at most 1*but found 3*");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task Verify_WithMethodPredicate_ShouldFilterByMethod()
    {
        var simulator = this.CreateAPISimulator(o => o.Port = 0)
            .ForGet("/api/items")
            .WithResponse(new { items = new[] { 1 } })
            .Add()
            .ForPost("/api/items")
            .WithResponse(new { id = 1 })
            .Add();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();
            await client.GetAsync("/api/items");
            await client.PostAsync("/api/items", new StringContent("{}", Encoding.UTF8, "application/json"));

            simulator.Verify(r => r.Method == HttpMethod.Get && r.Path == "/api/items")
                .WasCalledOnce();

            simulator.Verify(r => r.Method == HttpMethod.Post && r.Path == "/api/items")
                .WasCalledOnce();
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task ReceivedRequests_ShouldReturnAllRequestsInOrder()
    {
        var simulator = this.CreateAPISimulator(o => o.Port = 0)
            .ForGet("/api/first")
            .WithResponse(new { n = 1 })
            .Add()
            .ForGet("/api/second")
            .WithResponse(new { n = 2 })
            .Add();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();
            await client.GetAsync("/api/first");
            await client.GetAsync("/api/second");

            var requests = simulator.ReceivedRequests;
            requests.Should().HaveCount(2);
            requests[0].Path.Should().Be("/api/first");
            requests[1].Path.Should().Be("/api/second");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task ReceivedRequests_ShouldCapturePostBody()
    {
        var simulator = this.CreateAPISimulator(o => o.Port = 0)
            .ForPost("/api/data")
            .WithResponse(new { ok = true })
            .Add();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();
            var content = new StringContent("{\"name\":\"Alice\"}", Encoding.UTF8, "application/json");
            await client.PostAsync("/api/data", content);

            var requests = simulator.ReceivedRequests;
            requests.Should().HaveCount(1);
            requests[0].Body.Should().Contain("Alice");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task ReceivedRequests_ShouldCaptureHeaders()
    {
        var simulator = this.CreateAPISimulator(o => o.Port = 0)
            .ForGet("/api/test")
            .WithResponse(new { ok = true })
            .Add();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();
            client.DefaultRequestHeaders.Add("X-Custom-Header", "test-value");
            await client.GetAsync("/api/test");

            var requests = simulator.ReceivedRequests;
            requests.Should().HaveCount(1);
            requests[0].Headers.Should().ContainKey("X-Custom-Header");
            requests[0].Headers["X-Custom-Header"].Should().Be("test-value");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task ReceivedRequests_ShouldRecord404Requests()
    {
        var simulator = this.CreateAPISimulator(o => o.Port = 0);
        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();
            await client.GetAsync("/api/nonexistent");

            var requests = simulator.ReceivedRequests;
            requests.Should().HaveCount(1);
            requests[0].Path.Should().Be("/api/nonexistent");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task ClearRecordedRequests_ShouldResetList()
    {
        var simulator = this.CreateAPISimulator(o => o.Port = 0)
            .ForGet("/api/test")
            .WithResponse(new { ok = true })
            .Add();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();
            await client.GetAsync("/api/test");
            simulator.ReceivedRequests.Should().HaveCount(1);

            simulator.ClearRecordedRequests();
            simulator.ReceivedRequests.Should().BeEmpty();
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public void Verify_WithNullPredicate_ShouldThrowArgumentNullException()
    {
        var simulator = this.CreateAPISimulator(o => o.Port = 0);

        var act = () => simulator.Verify(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ReceivedRequests_WhenNoRequestsMade_ShouldBeEmpty()
    {
        var simulator = this.CreateAPISimulator(o => o.Port = 0);

        simulator.ReceivedRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task VerificationException_ShouldIncludeDescriptiveMessage()
    {
        var simulator = this.CreateAPISimulator(o => o.Port = 0)
            .ForGet("/api/test")
            .WithResponse(new { ok = true })
            .Add();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();
            await client.GetAsync("/api/test");

            var act = () => simulator.Verify(r => r.Path == "/api/other")
                .WasCalledOnce();

            act.Should().Throw<VerificationException>()
                .WithMessage("*Expected exactly 1*but found 0*")
                .WithMessage("*Recorded requests*total*")
                .WithMessage("*GET /api/test*");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task Verify_ResponseStrategy_ShouldStillWork_AfterRecording()
    {
        // Ensures EnableBuffering + body read doesn't break response generation
        var simulator = this.CreateAPISimulator(o => o.Port = 0)
            .ForPost("/api/echo")
            .WithResponse(new { received = true })
            .Add();

        await simulator.StartAsync();
        try
        {
            var client = simulator.CreateClient();
            var content = new StringContent("{\"data\":\"test\"}", Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/echo", content);

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("received");

            var requests = simulator.ReceivedRequests;
            requests.Should().HaveCount(1);
            requests[0].Body.Should().Contain("test");
        }
        finally
        {
            await simulator.StopAsync();
        }
    }
}
