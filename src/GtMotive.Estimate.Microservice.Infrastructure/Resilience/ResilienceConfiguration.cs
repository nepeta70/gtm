using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace GtMotive.Estimate.Microservice.Infrastructure.Resilience
{
    /// <summary>
    /// Registers the outbound resilience pipelines (Polly v8) as keyed singletons.
    /// </summary>
    public static class ResilienceConfiguration
    {
        /// <summary>
        /// Adds the MongoDB CRUD, MongoDB transaction commit and Azure Service Bus pipelines.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddResiliencePipelines(this IServiceCollection services)
        {
            // CRUD operations: connection drops, primary elections, wait-queue saturation.
            // Deliberately does NOT retry TransientTransactionError: inside a transaction
            // that label means "abort and retry the whole transaction", not the statement.
            services.AddKeyedSingleton(ResiliencePipelineNames.Mongo, (_, _) =>
                new ResiliencePipelineBuilder()
                    .AddRetry(new RetryStrategyOptions
                    {
                        MaxRetryAttempts = ResilienceDefaults.MongoMaxRetryAttempts,
                        BackoffType = DelayBackoffType.Exponential,
                        Delay = ResilienceDefaults.MongoRetryDelay,
                        UseJitter = true,
                        ShouldHandle = new PredicateBuilder()
                            .Handle<MongoConnectionException>()
                            .Handle<MongoWaitQueueFullException>()
                            .Handle<System.TimeoutException>()
                    })
                    .AddTimeout(ResilienceDefaults.MongoTimeout)
                    .Build());

            // Transaction commit: UnknownTransactionCommitResult means the commit MAY have
            // succeeded server-side. MongoDB guidance is to retry the commit operation.
            services.AddKeyedSingleton(ResiliencePipelineNames.MongoTransactionCommit, (_, _) =>
                new ResiliencePipelineBuilder()
                    .AddRetry(new RetryStrategyOptions
                    {
                        MaxRetryAttempts = ResilienceDefaults.CommitMaxRetryAttempts,
                        BackoffType = DelayBackoffType.Exponential,
                        Delay = ResilienceDefaults.CommitRetryDelay,
                        UseJitter = true,
                        ShouldHandle = new PredicateBuilder()
                            .Handle<MongoConnectionException>()
                            .Handle<MongoException>(ex => ex.HasErrorLabel("UnknownTransactionCommitResult"))
                    })
                    .Build());

            // Azure Service Bus: transient send failures plus a circuit breaker so a
            // prolonged broker outage fails fast instead of piling up HTTP requests.
            services.AddKeyedSingleton(ResiliencePipelineNames.ServiceBus, (_, _) =>
                new ResiliencePipelineBuilder()
                    .AddRetry(new RetryStrategyOptions
                    {
                        MaxRetryAttempts = ResilienceDefaults.ServiceBusMaxRetryAttempts,
                        BackoffType = DelayBackoffType.Exponential,
                        Delay = ResilienceDefaults.ServiceBusRetryDelay,
                        UseJitter = true,
                        ShouldHandle = new PredicateBuilder()
                            .Handle<ServiceBusException>(ex => ex.IsTransient)
                            .Handle<System.TimeoutException>()
                    })
                    .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                    {
                        FailureRatio = ResilienceDefaults.ServiceBusCircuitFailureRatio,
                        SamplingDuration = ResilienceDefaults.ServiceBusCircuitSamplingDuration,
                        MinimumThroughput = ResilienceDefaults.ServiceBusCircuitMinimumThroughput,
                        BreakDuration = ResilienceDefaults.ServiceBusCircuitBreakDuration
                    })
                    .AddTimeout(ResilienceDefaults.ServiceBusTimeout)
                    .Build());

            return services;
        }
    }
}
