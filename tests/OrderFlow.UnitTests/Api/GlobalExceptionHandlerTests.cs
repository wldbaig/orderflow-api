using System.Text.Json;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using OrderFlow.Api.Common;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Domain.Common;
using Xunit;

namespace OrderFlow.UnitTests.Api;

public sealed class GlobalExceptionHandlerTests
{
    private static async Task<(int StatusCode, JsonElement Body)> HandleAsync(Exception exception)
    {
        var context = new DefaultHttpContext();
        var body = new MemoryStream();
        context.Response.Body = body;
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        body.Position = 0;
        using var document = await JsonDocument.ParseAsync(body);
        return (context.Response.StatusCode, document.RootElement.Clone());
    }

    [Fact]
    public async Task ValidationException_Returns400WithFieldErrors()
    {
        var (status, body) = await HandleAsync(new ValidationException(new[] { new ValidationFailure("CustomerName", "Required") }));

        Assert.Equal(StatusCodes.Status400BadRequest, status);
        Assert.False(body.GetProperty("success").GetBoolean());
        Assert.Equal("Required", body.GetProperty("errors").GetProperty("CustomerName")[0].GetString());
    }

    [Fact]
    public async Task NotFoundException_Returns404()
    {
        var (status, body) = await HandleAsync(new NotFoundException("Order", "abc"));

        Assert.Equal(StatusCodes.Status404NotFound, status);
        Assert.Equal("Order with key 'abc' was not found.", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task DomainException_Returns409()
    {
        var (status, body) = await HandleAsync(new DomainException("Order is already cancelled."));

        Assert.Equal(StatusCodes.Status409Conflict, status);
        Assert.Equal("Order is already cancelled.", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task AiUnavailableException_Returns503()
    {
        var (status, _) = await HandleAsync(new AiUnavailableException("AI down"));

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status);
    }

    [Fact]
    public async Task OperationCanceledException_Returns499()
    {
        var (status, _) = await HandleAsync(new OperationCanceledException());

        Assert.Equal(499, status);
    }

    [Fact]
    public async Task UnexpectedException_Returns500WithoutLeakingDetails()
    {
        var (status, body) = await HandleAsync(new InvalidOperationException("connection string Password=secret"));

        Assert.Equal(StatusCodes.Status500InternalServerError, status);
        Assert.Equal("An unexpected error occurred.", body.GetProperty("message").GetString());
    }
}
