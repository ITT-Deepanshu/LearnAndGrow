namespace PRM.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<PrmWebApplicationFactory>
{
    public const string Name = "PRM Integration Tests";
}
