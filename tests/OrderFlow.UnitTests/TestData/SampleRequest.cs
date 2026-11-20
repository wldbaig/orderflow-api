using MediatR;

namespace OrderFlow.UnitTests.TestData;

public sealed record SampleRequest(string Name) : IRequest<string>;
