using Microsoft.Extensions.Logging.Abstractions;
using OrderFlow.Application.Common.Behaviors;
using OrderFlow.UnitTests.TestData;
using Xunit;

namespace OrderFlow.UnitTests.Application.Behaviors;

public sealed class LoggingBehaviorTests
{
    private readonly LoggingBehavior<SampleRequest, string> _behavior = new LoggingBehavior<SampleRequest, string>(NullLogger<LoggingBehavior<SampleRequest, string>>.Instance);

    [Fact]
    public async Task Handle_ReturnsResponseFromNext()
    {
        var response = await _behavior.Handle(new SampleRequest("x"), () => Task.FromResult("handled"), CancellationToken.None);

        Assert.Equal("handled", response);
    }

    [Fact]
    public async Task Handle_WhenNextThrows_Rethrows()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _behavior.Handle(new SampleRequest("x"), () => throw new InvalidOperationException("boom"), CancellationToken.None));
    }
}
