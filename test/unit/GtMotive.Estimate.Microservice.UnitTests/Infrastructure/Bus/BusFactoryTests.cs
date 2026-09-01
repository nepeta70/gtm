using System;
using FluentAssertions;
using GtMotive.Estimate.Microservice.Domain.Interfaces;
using GtMotive.Estimate.Microservice.Infrastructure.Bus;
using GtMotive.Estimate.Microservice.Infrastructure.Bus.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GtMotive.Estimate.Microservice.UnitTests.Infrastructure.Bus
{
    public sealed class BusFactoryTests
    {
        [Theory]
        [InlineData("NoOp")]
        [InlineData("noop")]
        [InlineData("")]
        [InlineData(null)]
        public void GetClientWithNoOpProviderReturnsNoOpBus(string provider)
        {
            var sp = CreateServices().BuildServiceProvider();

            var factory = new BusFactory(sp, CreateOptions(provider));
            var bus = factory.GetClient(typeof(object));

            bus.Should().BeOfType<NoOpBus>();
        }

        [Theory]
        [InlineData("InMemory")]
        [InlineData("INMEMORY")]
        [InlineData("inmemory")]
        public void GetClientWithInMemoryProviderReturnsInMemoryBus(string provider)
        {
            var sp = CreateServices().BuildServiceProvider();

            var factory = new BusFactory(sp, CreateOptions(provider));
            var bus = factory.GetClient(typeof(object));

            bus.Should().BeOfType<InMemoryBus>();
        }

        [Fact]
        public void GetClientWithNullEventTypeThrowsArgumentNullException()
        {
            var sp = CreateServices().BuildServiceProvider();
            var factory = new BusFactory(sp, CreateOptions("NoOp"));

            Action act = () => factory.GetClient(null);

            act.Should().Throw<ArgumentNullException>();
        }

        private static ServiceCollection CreateServices()
        {
            var services = new ServiceCollection();

            // Register the logger dependency required by InMemoryBus
            services.AddSingleton(Mock.Of<IAppLogger<InMemoryBus>>());

            // Register buses as keyed services exactly like the production InfrastructureConfiguration
            services.AddKeyedScoped<IBus, NoOpBus>(BusNames.NoOp);
            services.AddKeyedScoped<IBus, InMemoryBus>(BusNames.InMemory);

            return services;
        }

        private static IOptions<BusSettings> CreateOptions(string provider)
        {
            return Options.Create(new BusSettings { Provider = provider });
        }
    }
}
