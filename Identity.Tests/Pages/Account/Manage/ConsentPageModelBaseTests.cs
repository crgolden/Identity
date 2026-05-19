#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account.Manage;
using Infrastructure;

using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using Identity.Pages.Account.Manage;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ConsentPageModelBaseTests
{
    [Fact]
    public void CreateScopeViewModel_ApiScope_NoParsedParameter_MapsFieldsCorrectly()
    {
        // Arrange
        var parsed = new ParsedScopeValue("api1");
        var apiScope = new ApiScope("api1", "My API")
        {
            Description = "desc",
            Emphasize = true,
            Required = false,
        };

        // Act
        var vm = TestableBase.CallCreateScopeViewModel(parsed, apiScope, false);

        // Assert
        Assert.Equal("api1", vm.Name);
        Assert.Equal("api1", vm.Value);
        Assert.Equal("My API", vm.DisplayName);
        Assert.Equal("desc", vm.Description);
        Assert.True(vm.Emphasize);
        Assert.False(vm.Required);
        Assert.False(vm.Checked);
    }

    [Fact]
    public void CreateScopeViewModel_ApiScope_WithParsedParameter_AppendsToDisplayName()
    {
        // Arrange
        var parsed = new ParsedScopeValue("api1:tenant1")
        {
            ParsedName = "api1",
            ParsedParameter = "tenant1",
        };
        var apiScope = new ApiScope("api1", "My API");

        // Act
        var vm = TestableBase.CallCreateScopeViewModel(parsed, apiScope, true);

        // Assert
        Assert.EndsWith(":tenant1", vm.DisplayName);
        Assert.True(vm.Checked);
    }

    [Fact]
    public void CreateOfflineAccessScope_ReturnsCorrectViewModel()
    {
        // Act
        var vm = TestableBase.CallCreateOfflineAccessScope(true);

        // Assert
        Assert.Equal(Duende.IdentityServer.IdentityServerConstants.StandardScopes.OfflineAccess, vm.Value);
        Assert.True(vm.Emphasize);
        Assert.True(vm.Checked);
    }

    [Fact]
    public void ResourceViewModel_PropertiesRoundTrip()
    {
        // Arrange & Act
        var resource = new ConsentPageModelBase.ResourceViewModel
        {
            Name = "res1",
            DisplayName = "Resource One",
        };

        // Assert
        Assert.Equal("res1", resource.Name);
        Assert.Equal("Resource One", resource.DisplayName);
    }

    private sealed class TestableBase : ConsentPageModelBase
    {
        public static ScopeViewModel CallCreateScopeViewModel(
            ParsedScopeValue parsedScopeValue,
            ApiScope apiScope,
            bool check) =>
            CreateScopeViewModel(parsedScopeValue, apiScope, check);

        public static ScopeViewModel CallCreateOfflineAccessScope(bool check) =>
            CreateOfflineAccessScope(check);
    }
}
