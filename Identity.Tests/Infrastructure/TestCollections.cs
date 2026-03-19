namespace Identity.Tests.Infrastructure;

/// <summary>
/// xUnit collection that shares <see cref="PlaywrightFixture"/> across all E2E tests.
/// </summary>
[CollectionDefinition(Name)]
public sealed class E2ECollection : ICollectionFixture<PlaywrightFixture>
{
    public const string Name = "E2E";
}