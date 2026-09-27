namespace OrderFlow.Api.Contracts;

public sealed record CreateProductRequest(string Sku, string Name, string Category, decimal UnitPrice);
