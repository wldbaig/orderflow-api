using System.Net;
using System.Text;

namespace OrderFlow.UnitTests.TestData;

public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _content;

    public StubHttpMessageHandler(
        HttpStatusCode statusCode,
        string content)
    {
        _statusCode = statusCode;
        _content = content;
    }

    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_content, Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
