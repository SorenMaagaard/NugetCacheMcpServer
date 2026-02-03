namespace NugetCacheMcpServer.Tests;

public class DocumentationToolTests
{
    [Test]
    public async Task GetMethodDocumentation_ReturnsMethodInfo()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.Documentation.GetMethodDocumentation("testlibrary", "ClassWithMemberRemoval", "MethodToRemove", "1.0.0");

        await Assert.That(result).Contains("MethodToRemove");
    }

    [Test]
    public async Task GetMethodDocumentation_IncludesMethodSignature()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.Documentation.GetMethodDocumentation("testlibrary", "StaticUtilityClass", "ProcessString", "1.0.0");

        // Verify the method signature and parameters are included
        await Assert.That(result).Contains("ProcessString");
        await Assert.That(result).Contains("string input");
    }

    [Test]
    public async Task GetMethodDocumentation_ShowsParameters()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.Documentation.GetMethodDocumentation("testlibrary", "ClassWithMethodSignatureChange", "MethodWithParamChange", "1.0.0");

        await Assert.That(result).Contains("value");
        await Assert.That(result).Contains("int");
    }

    [Test]
    public async Task GetMethodDocumentation_ShowsReturnType()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.Documentation.GetMethodDocumentation("testlibrary", "ClassWithMethodSignatureChange", "MethodWithReturnChange", "1.0.0");

        await Assert.That(result).Contains("int");
    }

    [Test]
    public async Task GetMethodDocumentation_NonExistentMethod_ReturnsNotFound()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.Documentation.GetMethodDocumentation("testlibrary", "ClassToRemove", "NonExistentMethod", "1.0.0");

        await Assert.That(result).Contains("not found");
    }

    [Test]
    public async Task GetMethodDocumentation_NonExistentType_ReturnsNotFound()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.Documentation.GetMethodDocumentation("testlibrary", "NonExistentType", "SomeMethod", "1.0.0");

        await Assert.That(result).Contains("not found");
    }
}
