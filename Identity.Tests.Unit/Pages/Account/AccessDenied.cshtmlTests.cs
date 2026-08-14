namespace Identity.Tests.Unit.Pages.Account;

using Identity.Pages.Account;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class AccessDeniedModelTests
{
    [Fact]
    public void AccessDeniedModel_Class_HasAllowAnonymousAttribute()
    {
        // Act
        var hasAttribute = Attribute.IsDefined(typeof(AccessDeniedModel), typeof(AllowAnonymousAttribute));

        // Assert
        Assert.True(hasAttribute);
    }
}