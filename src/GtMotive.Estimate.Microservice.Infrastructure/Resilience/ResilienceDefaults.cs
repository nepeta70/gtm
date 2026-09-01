using System;

namespace GtMotive.Estimate.Microservice.Infrastructure.Resilience
{
    /// <summary>
    /// Default tuning values for the outbound resilience pipelines.
    /// NOTE: in a production-grade solution these values should come from configuration
    /// (for example a "Resilience" section bound through IOptions) so they can be tuned
    /// per environment without a redeploy. They are kept as constants here for simplicity.
    /// </summary>
    public static class ResilienceDefaults
    {
        public const int MongoMaxRetryAttempts = 3;
        public const int CommitMaxRetryAttempts = 3;
        public const int ServiceBusMaxRetryAttempts = 3;
        public const double ServiceBusCircuitFailureRatio = 0.5;
        public const int ServiceBusCircuitMinimumThroughput = 5;

        public static readonly TimeSpan MongoRetryDelay = TimeSpan.FromMilliseconds(200);
        public static readonly TimeSpan MongoTimeout = TimeSpan.FromSeconds(10);
        public static readonly TimeSpan CommitRetryDelay = TimeSpan.FromMilliseconds(150);
        public static readonly TimeSpan ServiceBusRetryDelay = TimeSpan.FromMilliseconds(250);
        public static readonly TimeSpan ServiceBusTimeout = TimeSpan.FromSeconds(15);
        public static readonly TimeSpan ServiceBusCircuitSamplingDuration = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan ServiceBusCircuitBreakDuration = TimeSpan.FromSeconds(15);
    }
}
