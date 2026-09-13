namespace Identity.Tests.Unit.Pages.Account.Manage;

using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using Identity.Pages.Account.Manage;
using Infrastructure;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ConsentPageModelBaseTests
{
    private const char ScopeParameterSeparator = ':';

    [Fact]
    public void CreateScopeViewModel_ApiScope_NoParsedParameter_MapsFieldsCorrectly()
    {
        // Arrange
        var scopeName = TestValues.NewApiScopeName();
        var scopeDisplayName = TestValues.NewDisplayName();
        var scopeDescription = TestValues.NewDescription();
        var parsed = new ParsedScopeValue(scopeName);
        var apiScope = new ApiScope(scopeName, scopeDisplayName)
        {
            Description = scopeDescription,
            Emphasize = true,
            Required = false,
        };

        // Act
        var vm = TestableBase.CallCreateScopeViewModel(parsed, apiScope, false);

        // Assert
        Assert.Equal(scopeName, vm.Name);
        Assert.Equal(scopeName, vm.Value);
        Assert.Equal(scopeDisplayName, vm.DisplayName);
        Assert.Equal(scopeDescription, vm.Description);
        Assert.True(vm.Emphasize);
        Assert.False(vm.Required);
        Assert.False(vm.Checked);
    }

    [Fact]
    public void CreateScopeViewModel_ApiScope_WithParsedParameter_AppendsToDisplayName()
    {
        // Arrange
        var scopeName = TestValues.NewApiScopeName();
        var scopeParameter = TestValues.NewPropertyValue();
        var parsed = new ParsedScopeValue(scopeName + ScopeParameterSeparator + scopeParameter)
        {
            ParsedName = scopeName,
            ParsedParameter = scopeParameter,
        };
        var apiScope = new ApiScope(scopeName, TestValues.NewDisplayName());

        // Act
        var vm = TestableBase.CallCreateScopeViewModel(parsed, apiScope, true);

        // Assert
        Assert.EndsWith(ScopeParameterSeparator + scopeParameter, vm.DisplayName, StringComparison.Ordinal);
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
        // Arrange
        var resourceName = TestValues.NewApiResourceName();
        var resourceDisplayName = TestValues.NewDisplayName();

        // Act
        var resource = new ConsentPageModelBase.ResourceViewModel
        {
            Name = resourceName,
            DisplayName = resourceDisplayName,
        };

        // Assert
        Assert.Equal(resourceName, resource.Name);
        Assert.Equal(resourceDisplayName, resource.DisplayName);
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