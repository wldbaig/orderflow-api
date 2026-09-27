namespace OrderFlow.Api.Contracts;

/// <summary>Edge DTOs for the Orders endpoints. Mapped to Application commands in the controller.</summary>
public sealed record CreateOrderLineRequest(string ProductSku, string ProductName, int Quantity, decimal UnitPrice);

public sealed record CreateOrderRequest(
    string CustomerName,
    string? CustomerReference,
    string? SpecialInstructions,
    List<CreateOrderLineRequest> Lines);

public sealed record NlSearchRequest(string Phrase, int? Page, int? PageSize);
