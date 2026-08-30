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
            var services = new ServiceCollection();
            services.AddScoped<NoOpBus>();
            services.AddScoped<InMemoryBus>(_ => new InMemoryBus(Mock.Of<IAppLogger<InMemoryBus>>()));
            var sp = services.BuildServiceProvider();

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
            var services = new ServiceCollection();
            services.AddScoped<NoOpBus>();
            services.AddScoped<InMemoryBus>(_ => new InMemoryBus(Mock.Of<IAppLogger<InMemoryBus>>()));
            var sp = services.BuildServiceProvider();

            var factory = new BusFactory(sp, CreateOptions(provider));
            var bus = factory.GetClient(typeof(object));

            bus.Should().BeOfType<InMemoryBus>();
        }

        [Fact]
        public void GetClientWithNullEventTypeThrowsArgumentNullException()
        {
            var services = new ServiceCollection();
            services.AddScoped<NoOpBus>();
            var sp = services.BuildServiceProvider();

            var factory = new BusFactory(sp, CreateOptions("NoOp"));

            Action act = () => factory.GetClient(null);

            act.Should().Throw<ArgumentNullException>();
        }

        private static IOptions<BusSettings> CreateOptions(string provider)
        {
            return Options.Create(new BusSettings { Provider = provider });
        }
    }
}
