using System.Text;
using Microsoft.AspNetCore.Http;
using Augustus;

namespace Augustus.Tests;

public class HttpRequestExtensionsTests
{
    [Fact]
    public async Task ToCurlCommandAsync_GetRequest_IncludesMethodUrlAndHeaders_WithoutBody()
    {
        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Method = HttpMethods.Get;
        request.Scheme = "https";
        request.Host = new HostString("api.example.com");
        request.Path = "/v1/resources";
        request.QueryString = new QueryString("?page=1");
        request.Headers.Append("X-Trace-Id", "abc123");

        var curl = await request.ToCurlCommandAsync();

        curl.Should().Contain("curl -X GET");
        curl.Should().Contain("-H \"X-Trace-Id: abc123\"");
        curl.Should().Contain("\"https://api.example.com/v1/resources?page=1\"");
        curl.Should().NotContain(" -d '");
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    public async Task ToCurlCommandAsync_WriteMethods_IncludeEscapedBody(string method)
    {
        const string body = "{\"name\":\"O'Reilly\"}";
        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Method = method;
        request.Scheme = "https";
        request.Host = new HostString("api.example.com");
        request.Path = "/authors";
        request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));

        var curl = await request.ToCurlCommandAsync();

        curl.Should().Contain($"curl -X {method}");
        curl.Should().Contain("-d '{\"name\":\"O'\\''Reilly\"}'");
    }

    [Fact]
    public async Task ToCurlCommandAsync_HeaderWithQuotesAndBackslashes_EscapesHeaderValue()
    {
        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Method = HttpMethods.Get;
        request.Scheme = "https";
        request.Host = new HostString("api.example.com");
        request.Path = "/headers";
        request.Headers.Append("X-Complex", "value \"quoted\" and \\path\\segment");

        var curl = await request.ToCurlCommandAsync();

        curl.Should().Contain("-H \"X-Complex: value \\\"quoted\\\" and \\\\path\\\\segment\"");
    }

    [Fact]
    public async Task ToCurlCommandAsync_SeekableBodyStream_ResetsStreamPosition()
    {
        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Method = HttpMethods.Post;
        request.Scheme = "http";
        request.Host = new HostString("localhost");
        request.Path = "/items";

        var stream = new MemoryStream(Encoding.UTF8.GetBytes("{\"id\":1}"));
        stream.Position = 3;
        request.Body = stream;

        var curl = await request.ToCurlCommandAsync();

        request.Body.CanSeek.Should().BeTrue();
        request.Body.Position.Should().Be(0);
        curl.Should().Contain("-d '{\"id\":1}'");
    }

    [Fact]
    public async Task ToCurlCommandAsync_NonSeekableBodyStream_DoesNotThrow_AndReturnsCommand()
    {
        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Method = HttpMethods.Post;
        request.Scheme = "https";
        request.Host = new HostString("api.example.com");
        request.Path = "/non-seekable";
        request.Body = new NonSeekableReadStream("{\"name\":\"Ada\"}");

        var act = async () => await request.ToCurlCommandAsync();

        var curl = await act.Should().NotThrowAsync();
        curl.Subject.Should().Contain("curl -X POST");
        curl.Subject.Should().Contain("-d '{\"name\":\"Ada\"}'");
        curl.Subject.Should().Contain("\"https://api.example.com/non-seekable\"");
    }

    [Fact]
    public async Task ToCurlCommandAsync_UrlWithQueryAndHostScheme_IsIncludedExactlyOnce()
    {
        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Method = HttpMethods.Get;
        request.Scheme = "https";
        request.Host = new HostString("example.org:8443");
        request.Path = "/search";
        request.QueryString = new QueryString("?q=augustus&lang=en");

        var curl = await request.ToCurlCommandAsync();
        const string expectedUrl = "https://example.org:8443/search?q=augustus&lang=en";

        curl.Should().Contain($"\"{expectedUrl}\"");
        CountOccurrences(curl, expectedUrl).Should().Be(1);
        CountOccurrences(curl, "https://").Should().Be(1);
    }

    private static int CountOccurrences(string value, string segment)
    {
        var count = 0;
        var index = 0;

        while ((index = value.IndexOf(segment, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += segment.Length;
        }

        return count;
    }

    private sealed class NonSeekableReadStream : Stream
    {
        private readonly MemoryStream _inner;

        public NonSeekableReadStream(string content)
        {
            _inner = new MemoryStream(Encoding.UTF8.GetBytes(content));
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => _inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => _inner.ReadAsync(buffer, cancellationToken);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _inner.ReadAsync(buffer, offset, count, cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
