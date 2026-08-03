using Sergin.SharedKernel.IntegrationTests;

namespace Sergin.IntegrationTests;

[CollectionDefinition(nameof(IntegrationTestCollection))]
public sealed class IntegrationTestCollection : ICollectionFixture<SerginWebApiFactory<Program>>;
