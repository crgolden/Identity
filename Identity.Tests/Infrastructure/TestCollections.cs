namespace Identity.Tests.Infrastructure;

/// <summary>
/// xUnit collection that shares <see cref="PlaywrightFixture"/> across all E2E tests.
/// </summary>
[CollectionDefinition(Name)]
public sealed class E2ECollection : ICollectionFixture<PlaywrightFixture>
{
    public const string Name = "E2E";
}

/// <summary>
/// xUnit collection for all unit tests. Provides an explicit collection name so that
/// Stryker.NET's MTP coverage capture does not encounter null collection names.
/// </summary>
[CollectionDefinition(Name)]
public sealed class UnitCollection
{
    /// <summary>The collection name used in <c>[Collection]</c> attributes.</summary>
    public const string Name = "Unit";

    private UnitCollection()
    {
    }
}