namespace NugetCacheMcpServer.Tests;

public class PackageInfoToolTests
{
    [Test]
    public async Task GetPackageInfo_ReturnsPackageMetadata()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.PackageInfo.GetPackageInfo("testlibrary", "1.0.0");

        await Assert.That(result).Contains("\"packageId\"");
        await Assert.That(result).Contains("\"version\":\"1.0.0\"");
        await Assert.That(result).Contains("\"description\"");
    }

    [Test]
    public async Task GetPackageInfo_IncludesFrameworks()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.PackageInfo.GetPackageInfo("testlibrary", "1.0.0");

        await Assert.That(result).Contains("net10.0");
    }

    [Test]
    public async Task GetPackageInfo_IncludesCachedVersions()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.PackageInfo.GetPackageInfo("testlibrary");

        await Assert.That(result).Contains("cachedVersions");
        await Assert.That(result).Contains("1.0.0");
        await Assert.That(result).Contains("2.0.0");
    }

    [Test]
    public async Task GetPackageInfo_MetaPackage_IncludesWarning()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.PackageInfo.GetPackageInfo("testlibrary.metapackage", "1.0.0");

        await Assert.That(result).Contains("isMetaPackage");
        await Assert.That(result).Contains("warning");
        await Assert.That(result).Contains("META-PACKAGE");
    }

    [Test]
    public async Task GetPackageInfo_NonExistentPackage_ReturnsNotFoundMessage()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.PackageInfo.GetPackageInfo("nonexistent.package");

        await Assert.That(result).Contains("not found");
        await Assert.That(result).Contains("dotnet restore");
    }
}
