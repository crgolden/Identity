namespace Identity.Tests.Pages.Account;
using Infrastructure;

using Identity.Pages.Account;
using Microsoft.AspNetCore.Authorization;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class AccessDeniedModelTests
{
    [Fact]
    public void AccessDeniedModel_Class_HasAllowAnonymousAttribute()
    {
        // Arrange & Act
        var hasAttribute = Attribute.IsDefined(typeof(AccessDeniedModel), typeof(AllowAnonymousAttribute));

        // Assert
        Assert.True(hasAttribute);
    }
}