namespace GtMotive.Estimate.Microservice.Infrastructure.Resilience
{
    /// <summary>
    /// Keys used to register and resolve the outbound resilience pipelines from DI.
    /// </summary>
    public static class ResiliencePipelineNames
    {
        public const string Mongo = nameof(Mongo);
        public const string MongoTransactionCommit = nameof(MongoTransactionCommit);
        public const string ServiceBus = nameof(ServiceBus);
    }
}
