using Xunit;

namespace GtMotive.Estimate.Microservice.InfrastructureTests.Infrastructure
{
    [Collection(TestCollections.JwtTestServer)]
    public abstract class JwtInfrastructureTestBase(JwtTestServerFixture fixture)
    {
        protected JwtTestServerFixture Fixture { get; } = fixture;
    }
}
