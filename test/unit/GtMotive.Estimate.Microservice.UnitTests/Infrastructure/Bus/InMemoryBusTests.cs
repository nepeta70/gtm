using System.Threading.Tasks;
using FluentAssertions;
using GtMotive.Estimate.Microservice.Domain.Interfaces;
using GtMotive.Estimate.Microservice.Infrastructure.Bus;
using Moq;
using Xunit;

namespace GtMotive.Estimate.Microservice.UnitTests.Infrastructure.Bus
{
    public sealed class InMemoryBusTests
    {
        private readonly Mock<IAppLogger<InMemoryBus>> _logger = new();

        [Fact]
        public async Task SendStoresMessageInMemory()
        {
            var bus = new InMemoryBus(_logger.Object);
            var message = new { Id = 1, Text = "hello" };

            await bus.Send(message);

            bus.SentMessages.Should().ContainSingle()
                .Which.Should().BeEquivalentTo(message);
        }

        [Fact]
        public async Task SendNullCompletesWithoutStoring()
        {
            var bus = new InMemoryBus(_logger.Object);

            await bus.Send(null);

            bus.SentMessages.Should().BeEmpty();
        }
    }
}
