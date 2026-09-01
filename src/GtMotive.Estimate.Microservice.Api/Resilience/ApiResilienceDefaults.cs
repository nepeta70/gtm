using System;

namespace GtMotive.Estimate.Microservice.Api.Resilience
{
    public static class ApiResilienceDefaults
    {
        public const int EndpointPermitLimit = 20;
        public const int EndpointQueueLimit = 0;

        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
    }
}
