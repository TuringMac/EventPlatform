using Xunit;

namespace EventPlatform.IntegrationTests;

[CollectionDefinition("PostgreSql")]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
}
