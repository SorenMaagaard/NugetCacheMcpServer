namespace NugetCacheMcpServer.Tests;

public class VersionCompareToolTests
{
    [Test]
    public async Task ComparePackageVersions_DetectsTypeRemoval()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.VersionCompare.ComparePackageVersions("testlibrary", "1.0.0", "2.0.0");

        // ClassToRemove was removed in V2 - should appear in breaking changes
        await Assert.That(result).Contains("ClassToRemove");
    }

    [Test]
    public async Task ComparePackageVersions_DetectsMemberRemoval()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.VersionCompare.ComparePackageVersions("testlibrary", "1.0.0", "2.0.0");

        // Methods and properties were removed - they should appear in the changes
        await Assert.That(result).Contains("MethodToRemove");
    }

    [Test]
    public async Task ComparePackageVersions_DetectsNewTypes()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.VersionCompare.ComparePackageVersions("testlibrary", "1.0.0", "2.0.0");

        // NewClassInV2 was added
        await Assert.That(result).Contains("NewClassInV2");
    }

    [Test]
    public async Task ComparePackageVersions_HasBreakingChanges()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.VersionCompare.ComparePackageVersions("testlibrary", "1.0.0", "2.0.0");

        // V2 has breaking changes (type removal, member removal, etc.)
        await Assert.That(result).Contains("breakingChanges");
    }

    [Test]
    public async Task ComparePackageVersions_ReturnsVersionInfo()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.VersionCompare.ComparePackageVersions("testlibrary", "1.0.0", "2.0.0");

        await Assert.That(result).Contains("fromVersion");
        await Assert.That(result).Contains("1.0.0");
        await Assert.That(result).Contains("toVersion");
        await Assert.That(result).Contains("2.0.0");
    }

    [Test]
    public async Task ComparePackageVersions_IncludesSummary()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.VersionCompare.ComparePackageVersions("testlibrary", "1.0.0", "2.0.0");

        await Assert.That(result).Contains("summary");
        await Assert.That(result).Contains("totalChanges");
    }

    [Test]
    public async Task ComparePackageVersions_IdenticalVersions_NoChanges()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.VersionCompare.ComparePackageVersions("testlibrary", "1.0.0", "1.0.0");

        await Assert.That(result).Contains("totalChanges");
        // Same version comparison should have 0 total changes
        await Assert.That(result).Contains("\"totalChanges\":0");
    }

    [Test]
    public async Task ComparePackageVersions_NonExistentFromVersion_ReturnsError()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.VersionCompare.ComparePackageVersions("testlibrary", "0.0.1", "2.0.0");

        await Assert.That(result).Contains("not found");
    }

    [Test]
    public async Task ComparePackageVersions_NonExistentToVersion_ReturnsError()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.VersionCompare.ComparePackageVersions("testlibrary", "1.0.0", "9.9.9");

        await Assert.That(result).Contains("not found");
    }

    [Test]
    public async Task ComparePackageVersions_NonExistentPackage_ReturnsError()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.VersionCompare.ComparePackageVersions("nonexistent.package", "1.0.0", "2.0.0");

        await Assert.That(result).Contains("not found");
    }
}
