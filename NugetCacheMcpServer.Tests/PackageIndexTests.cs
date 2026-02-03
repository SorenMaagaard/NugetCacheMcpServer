namespace NugetCacheMcpServer.Tests;

public class PackageIndexTests
{
    [Test]
    public async Task GetPackages_ReturnsTestLibrary()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var packages = fixture.PackageIndex.GetPackages().ToList();

        await Assert.That(packages.Count).IsGreaterThanOrEqualTo(2);
        await Assert.That(packages.Any(p => p.PackageId.Equals("testlibrary", StringComparison.OrdinalIgnoreCase))).IsTrue();
    }

    [Test]
    public async Task GetPackages_WithFilter_ReturnsMatchingPackages()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var packages = fixture.PackageIndex.GetPackages("testlibrary").ToList();

        await Assert.That(packages.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(packages.All(p => p.PackageId.Contains("testlibrary", StringComparison.OrdinalIgnoreCase))).IsTrue();
    }

    [Test]
    public async Task GetPackageVersions_ReturnsMultipleVersions()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var versions = fixture.PackageIndex.GetPackageVersions("testlibrary").ToList();

        await Assert.That(versions.Count).IsEqualTo(2);
        await Assert.That(versions).Contains("1.0.0");
        await Assert.That(versions).Contains("2.0.0");
    }

    [Test]
    public async Task GetPackage_WithVersion_ReturnsCorrectPackage()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var package = fixture.PackageIndex.GetPackage("testlibrary", "1.0.0");

        await Assert.That(package).IsNotNull();
        await Assert.That(package!.Version).IsEqualTo("1.0.0");
    }

    [Test]
    public async Task GetPackage_WithoutVersion_ReturnsLatestVersion()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var package = fixture.PackageIndex.GetPackage("testlibrary");

        await Assert.That(package).IsNotNull();
        await Assert.That(package!.Version).IsEqualTo("2.0.0");
    }

    [Test]
    public async Task GetPackage_NonExistent_ReturnsNull()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var package = fixture.PackageIndex.GetPackage("nonexistent.package");

        await Assert.That(package).IsNull();
    }
}
