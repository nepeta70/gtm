using Xunit;

namespace GtMotive.Estimate.Microservice.InfrastructureTests.Infrastructure
{
    [CollectionDefinition(TestCollections.JwtTestServer)]
    public class JwtTestServerCollectionFixture : ICollectionFixture<JwtTestServerFixture>
    {
    }
}
