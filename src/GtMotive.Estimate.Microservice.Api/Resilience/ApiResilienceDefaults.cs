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

        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
    }
}
