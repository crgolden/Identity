namespace Identity.Tests.Unit.Pages.Account.Manage;

using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using Identity.Pages.Account.Manage;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.Extensions.Options;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ConsentPageModelBaseTests
{
    private const char ScopeParameterSeparator = ':';

    [Fact]
    public void CreateScopeViewModel_ApiScope_NoParsedParameter_MapsFieldsCorrectly()
    {
        // Arrange
        var scopeName = Generated.NewApiScopeName();
        var scopeDisplayName = Generated.NewDisplayName();
        var scopeDescription = Generated.NewDescription();
        var parsed = new ParsedScopeValue(scopeName);
        var apiScope = new ApiScope(scopeName, scopeDisplayName)
        {
            Description = scopeDescription,
            Emphasize = true,
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
        var scopeName = Generated.NewApiScopeName();
        var scopeParameter = Generated.NewPropertyValue();
        var parsed = new ParsedScopeValue(scopeName + ScopeParameterSeparator + scopeParameter)
        {
            ParsedName = scopeName,
            ParsedParameter = scopeParameter,
        };
        var apiScope = new ApiScope(scopeName, Generated.NewDisplayName());

        // Act
        var vm = TestableBase.CallCreateScopeViewModel(parsed, apiScope, true);

        // Assert
        Assert.EndsWith(ScopeParameterSeparator + scopeParameter, vm.DisplayName, StringComparison.Ordinal);
        Assert.True(vm.Checked);
    }

    [Fact]
    public void CreateOfflineAccessScope_ReturnsCorrectViewModel()
    {
        // Arrange
        var consentOptions = new ConsentOptions(true, Generated.NewDisplayName(), Generated.NewDescription());
        var model = new TestableBase(Options.Create(consentOptions));

        // Act
        var vm = model.CallCreateOfflineAccessScope(true);

        // Assert
        Assert.Equal(Duende.IdentityServer.IdentityServerConstants.StandardScopes.OfflineAccess, vm.Value);
        Assert.Equal(consentOptions.OfflineAccessDisplayName, vm.DisplayName);
        Assert.Equal(consentOptions.OfflineAccessDescription, vm.Description);
        Assert.True(vm.Emphasize);
        Assert.True(vm.Checked);
    }

    [Fact]
    public void ResourceViewModel_PropertiesRoundTrip()
    {
        // Arrange
        var resourceName = Generated.NewApiResourceName();
        var resourceDisplayName = Generated.NewDisplayName();

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

    private sealed class TestableBase(IOptions<ConsentOptions> consentOptions) : ConsentPageModelBase(consentOptions)
    {
        public static ScopeViewModel CallCreateScopeViewModel(
            ParsedScopeValue parsedScopeValue,
            ApiScope apiScope,
            bool check) =>
            CreateScopeViewModel(parsedScopeValue, apiScope, check);

        public ScopeViewModel CallCreateOfflineAccessScope(bool check) =>
            CreateOfflineAccessScope(check);
    }
}
