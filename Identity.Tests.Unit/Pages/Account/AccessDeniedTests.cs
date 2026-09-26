namespace Identity.Tests.Unit.Pages.Account;

using Identity.Pages.Account;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Authorization;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class AccessDeniedTests
{
    [Fact]
    public void AccessDeniedModel_Class_HasAllowAnonymousAttribute()
    {
        // Act
        var hasAttribute = Attribute.IsDefined(typeof(AccessDenied), typeof(AllowAnonymousAttribute));

        // Assert
        Assert.True(hasAttribute);
    }
}
