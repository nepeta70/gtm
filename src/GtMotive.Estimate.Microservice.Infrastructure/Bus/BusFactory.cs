using System;
using GtMotive.Estimate.Microservice.Domain.Interfaces;
using GtMotive.Estimate.Microservice.Infrastructure.Bus.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GtMotive.Estimate.Microservice.Infrastructure.Bus
{
    /// <summary>
    /// Factory that resolves the appropriate <see cref="IBus"/> implementation
    /// based on the configured bus provider.
    /// </summary>
    public sealed class BusFactory : IBusFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly BusSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="BusFactory"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider used to resolve bus implementations.</param>
        /// <param name="options">The bus settings.</param>
        public BusFactory(IServiceProvider serviceProvider, IOptions<BusSettings> options)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);
            ArgumentNullException.ThrowIfNull(options);

            _serviceProvider = serviceProvider;
            _settings = options.Value;
        }

        /// <inheritdoc />
        public IBus GetClient(Type eventType)
        {
            ArgumentNullException.ThrowIfNull(eventType);

            var provider = _settings.Provider?.ToUpperInvariant();

            return provider switch
            {
                BusNames.InMemory => _serviceProvider.GetRequiredKeyedService<IBus>(BusNames.InMemory),
                BusNames.Azure => _serviceProvider.GetRequiredKeyedService<IBus>(BusNames.Azure),
                _ => _serviceProvider.GetRequiredKeyedService<IBus>(BusNames.NoOp)
            };
        }
    }
}
