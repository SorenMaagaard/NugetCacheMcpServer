namespace NugetCacheMcpServer.Tests;

public class CacheExplorerToolTests
{
    [Test]
    public async Task ListCachedPackages_ReturnsJsonWithPackages()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.CacheExplorer.ListCachedPackages();

        await Assert.That(result).Contains("testlibrary");
        await Assert.That(result).Contains("totalCount");
    }

    [Test]
    public async Task ListCachedPackages_WithFilter_ReturnsFilteredResults()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.CacheExplorer.ListCachedPackages(filter: "testlibrary");

        await Assert.That(result).Contains("testlibrary");
    }

    [Test]
    public async Task ListCachedPackages_WithPagination_RespectsPageSize()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.CacheExplorer.ListCachedPackages(pageSize: 1);

        await Assert.That(result).Contains("returnedCount");
    }
}
