namespace Identity.Tests.Load;

[CollectionDefinition(Name)]
public sealed class LoadCollection : ICollectionFixture<LoadFixture>
{
    public const string Name = "Load";
}
