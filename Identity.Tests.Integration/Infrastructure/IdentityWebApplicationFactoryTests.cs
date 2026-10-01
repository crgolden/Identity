namespace Identity.Tests.Integration.Infrastructure;

using Identity.Tests.E2E.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

[Trait("Category", "Integration")]
public sealed class IdentityWebApplicationFactoryTests
{
    [Fact]
    public async Task StartingAgainstACatalogWithoutTheTestSuffix_IsRefusedBeforeAnythingIsWritten()
    {
        var productionLikeCatalog = Generated.NewDatabaseName();
        await using var configuredFactory = new IdentityWebApplicationFactory();
        await using var factory = configuredFactory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [$"{nameof(SqlConnectionStringBuilder)}:{nameof(SqlConnectionStringBuilder.InitialCatalog)}"] =
                        productionLikeCatalog,
                })));

        var refusal = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        Assert.Contains(productionLikeCatalog, refusal.Message, StringComparison.Ordinal);
    }
}
