using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using GtMotive.Estimate.Microservice.Infrastructure.Resilience;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using Polly;
using Xunit;

namespace GtMotive.Estimate.Microservice.UnitTests.Infrastructure.Resilience
{
    public sealed class ResiliencePipelineTests
    {
        [Fact]
        public void AddResiliencePipelinesRegistersAllKeyedPipelines()
        {
            using var provider = CreateProvider();

            provider.GetRequiredKeyedService<ResiliencePipeline>(ResiliencePipelineNames.Mongo).Should().NotBeNull();
            provider.GetRequiredKeyedService<ResiliencePipeline>(ResiliencePipelineNames.MongoTransactionCommit).Should().NotBeNull();
            provider.GetRequiredKeyedService<ResiliencePipeline>(ResiliencePipelineNames.ServiceBus).Should().NotBeNull();
        }

        [Fact]
        public async Task MongoPipelineRetriesTransientConnectionErrors()
        {
            using var provider = CreateProvider();
            var pipeline = provider.GetRequiredKeyedService<ResiliencePipeline>(ResiliencePipelineNames.Mongo);

            var attempts = 0;

            var result = await pipeline.ExecuteAsync(_ =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw CreateTransientConnectionException();
                }

                return new ValueTask<int>(attempts);
            });

            result.Should().Be(3);
            attempts.Should().Be(3);
        }

        [Fact]
        public async Task MongoPipelineDoesNotRetryNonTransientMongoErrors()
        {
            using var provider = CreateProvider();
            var pipeline = provider.GetRequiredKeyedService<ResiliencePipeline>(ResiliencePipelineNames.Mongo);

            var attempts = 0;

            Func<Task> act = async () => await pipeline.ExecuteAsync(_ =>
            {
                attempts++;
                throw new MongoException("permanent failure");
            });

            await act.Should().ThrowAsync<MongoException>();
            attempts.Should().Be(1);
        }

        private static ServiceProvider CreateProvider()
        {
            var services = new ServiceCollection();
            services.AddResiliencePipelines();
            return services.BuildServiceProvider();
        }

        private static MongoConnectionException CreateTransientConnectionException()
        {
            var connectionId = new ConnectionId(new ServerId(new ClusterId(1), new DnsEndPoint("localhost", 27017)));
            return new MongoConnectionException(connectionId, "simulated transient failure");
        }
    }
}
