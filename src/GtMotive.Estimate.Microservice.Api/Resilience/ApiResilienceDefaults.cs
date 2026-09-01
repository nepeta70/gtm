using System;

namespace GtMotive.Estimate.Microservice.Api.Resilience
{
    /// <summary>
    /// Internal constants for API resilience policies, such as concurrency limits and timeouts.
    /// NOTE: it should be configured to come from the Configuration.
    /// </summary>
    public static class ApiResilienceDefaults
    {
        public const int EndpointPermitLimit = 20;
        public const int EndpointQueueLimit = 0;
        public const double FailureRatio = 0.5;
        public const int MinimumThroughput = 10;
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan SamplingDuration = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan BreakDuration = TimeSpan.FromSeconds(15);
    }
}
