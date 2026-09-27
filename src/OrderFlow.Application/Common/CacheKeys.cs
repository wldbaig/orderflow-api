namespace OrderFlow.Application.Common;

/// <summary>Central registry of cache keys so reads and their invalidation stay in sync.</summary>
public static class CacheKeys
{
    public const string ProductCatalog = "reference:product-catalog";
}
