namespace Sergin.IntegrationTests;

[CollectionDefinition(nameof(IntegrationTestCollection))]
public sealed class IntegrationTestCollection : ICollectionFixture<SerginApiFactory>;
