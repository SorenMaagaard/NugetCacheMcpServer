namespace NugetCacheMcpServer.Tests;

public class TypeExplorerToolTests
{
    [Test]
    public async Task ListTypes_ReturnsAllPublicTypes()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.TypeExplorer.ListTypes("testlibrary", "1.0.0");

        await Assert.That(result).Contains("totalCount");
        await Assert.That(result).Contains("ClassToRemove");
        await Assert.That(result).Contains("IInterface1");
    }

    [Test]
    public async Task ListTypes_WithNamespaceFilter_FiltersResults()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.TypeExplorer.ListTypes("testlibrary", "1.0.0", namespaceFilter: "TestLibrary");

        await Assert.That(result).Contains("TestLibrary");
    }

    [Test]
    public async Task ListTypes_WithTypeKindFilter_FiltersResults()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.TypeExplorer.ListTypes("testlibrary", "1.0.0", typeKind: "interface");

        await Assert.That(result).Contains("IInterface1");
        await Assert.That(result).Contains("IInterface2");
        await Assert.That(result).DoesNotContain("ClassToRemove");
    }

    [Test]
    public async Task ListTypes_WithWildcardFilter_MatchesPattern()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.TypeExplorer.ListTypes("testlibrary", "1.0.0", filter: "*Interface*");

        await Assert.That(result).Contains("IInterface1");
        await Assert.That(result).Contains("IInterface2");
        await Assert.That(result).DoesNotContain("ClassToRemove");
    }

    [Test]
    public async Task ListTypes_WildcardFilter_EndsWithPattern()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.TypeExplorer.ListTypes("testlibrary", "1.0.0", filter: "*Removal");

        await Assert.That(result).Contains("ClassWithMemberRemoval");
        await Assert.That(result).Contains("ClassWithInterfaceRemoval");
        await Assert.That(result).DoesNotContain("ClassToRemove");
    }

    [Test]
    public async Task ListTypes_WildcardFilter_StartsWithPattern()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        // Pattern matches against full name (namespace.type), so use *Base* to find BaseClass types
        var result = fixture.TypeExplorer.ListTypes("testlibrary", "1.0.0", filter: "*Base*");

        await Assert.That(result).Contains("BaseClass1");
        await Assert.That(result).Contains("BaseClass2");
        await Assert.That(result).DoesNotContain("ClassToRemove");
    }

    [Test]
    public async Task ListTypes_IncludesIsStaticFlag()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        // Pattern matches full name including namespace
        var result = fixture.TypeExplorer.ListTypes("testlibrary", "1.0.0", filter: "*StaticUtilityClass");

        await Assert.That(result).Contains("StaticUtilityClass");
        await Assert.That(result).Contains("isStatic");
    }

    [Test]
    public async Task ListTypes_IncludesIsAbstractFlag()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        // Pattern matches full name including namespace
        var result = fixture.TypeExplorer.ListTypes("testlibrary", "1.0.0", filter: "*AbstractService");

        await Assert.That(result).Contains("AbstractService");
        await Assert.That(result).Contains("isAbstract");
    }

    [Test]
    public async Task GetTypeDefinition_ReturnsCompleteDefinition()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.TypeExplorer.GetTypeDefinition("testlibrary", "ClassWithMemberRemoval", "1.0.0");

        await Assert.That(result).Contains("\"fullName\"");
        await Assert.That(result).Contains("MethodToRemove");
        await Assert.That(result).Contains("PropertyToRemove");
        await Assert.That(result).Contains("MethodToKeep");
    }

    [Test]
    public async Task GetTypeDefinition_Interface_ShowsMethods()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.TypeExplorer.GetTypeDefinition("testlibrary", "IInterface1", "1.0.0");

        await Assert.That(result).Contains("\"kind\":\"interface\"");
        await Assert.That(result).Contains("InterfaceMethod1");
    }

    [Test]
    public async Task GetTypeDefinition_Enum_ShowsMembers()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.TypeExplorer.GetTypeDefinition("testlibrary", "EnumWithValueRemoval", "1.0.0");

        await Assert.That(result).Contains("\"kind\":\"enum\"");
        await Assert.That(result).Contains("Value1");
        await Assert.That(result).Contains("Value2");
        await Assert.That(result).Contains("Value3");
    }

    [Test]
    public async Task GetTypeDefinition_GenericClass_ShowsTypeParameters()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.TypeExplorer.GetTypeDefinition("testlibrary", "GenericClass", "1.0.0");

        await Assert.That(result).Contains("genericParameters");
        await Assert.That(result).Contains("\"T\"");
    }

    [Test]
    public async Task GetTypeDefinition_ClassWithBaseType_ShowsInheritance()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.TypeExplorer.GetTypeDefinition("testlibrary", "ClassWithBaseChange", "1.0.0");

        await Assert.That(result).Contains("baseType");
        await Assert.That(result).Contains("BaseClass1");
    }

    [Test]
    public async Task GetTypeDefinition_ClassWithInterfaces_ShowsInterfaces()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.TypeExplorer.GetTypeDefinition("testlibrary", "ClassWithInterfaceRemoval", "1.0.0");

        await Assert.That(result).Contains("interfaces");
        await Assert.That(result).Contains("IInterface1");
        await Assert.That(result).Contains("IInterface2");
    }

    [Test]
    public async Task GetTypeDefinition_IncludesXmlDocumentation()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.TypeExplorer.GetTypeDefinition("testlibrary", "ClassToRemove", "1.0.0");

        // Method-level XML docs should be included (method DoSomething has a summary)
        await Assert.That(result).Contains("summary");
        await Assert.That(result).Contains("A method that will disappear");
    }

    [Test]
    public async Task GetTypeDefinition_NonExistentType_ReturnsNotFoundMessage()
    {
        var fixture = await TestFixture.GetInstanceAsync();
        var result = fixture.TypeExplorer.GetTypeDefinition("testlibrary", "NonExistentType", "1.0.0");

        await Assert.That(result).Contains("not found");
        await Assert.That(result).Contains("list_types");
    }
}
