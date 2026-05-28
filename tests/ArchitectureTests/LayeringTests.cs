namespace ArchitectureTests;

using NetArchTest.Rules;
using Xunit;

public class LayeringTests
{
    [Fact]
    public void Domain_must_not_reference_Infrastructure()
    {
        var result = Types.InCurrentDomain()
            .That().ResideInNamespaceContaining(".Domain")
            .ShouldNot().HaveDependencyOn("Infrastructure")
            .GetResult();
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void Public_types_in_Sample_Lib_are_sealed_or_abstract()
    {
        var result = Types.InAssembly(typeof(Sample.Library.Greeter).Assembly)
            .That().ArePublic()
            .Should().BeSealed().Or().BeAbstract()
            .GetResult();
        Assert.True(result.IsSuccessful);
    }
}
