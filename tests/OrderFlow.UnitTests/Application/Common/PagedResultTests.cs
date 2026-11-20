using OrderFlow.Application.Common.Models;
using Xunit;

namespace OrderFlow.UnitTests.Application.Common;

public sealed class PagedResultTests
{
    [Theory]
    [InlineData(1, 10, 25L, 3, true, false)]
    [InlineData(2, 10, 25L, 3, true, true)]
    [InlineData(3, 10, 25L, 3, false, true)]
    [InlineData(1, 10, 0L, 0, false, false)]
    [InlineData(1, 10, 10L, 1, false, false)]
    public void Paging_IsDerivedFromCountsAndPage(int page, int pageSize, long total, int expectedPages, bool hasNext, bool hasPrevious)
    {
        var result = new PagedResult<int>(Array.Empty<int>(), page, pageSize, total);

        Assert.Equal(expectedPages, result.TotalPages);
        Assert.Equal(hasNext, result.HasNextPage);
        Assert.Equal(hasPrevious, result.HasPreviousPage);
    }

    [Fact]
    public void TotalPages_WithZeroPageSize_IsZero()
    {
        var result = new PagedResult<int>(Array.Empty<int>(), 1, 0, 50);

        Assert.Equal(0, result.TotalPages);
    }
}
