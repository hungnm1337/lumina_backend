using FluentAssertions;
using Moq;
using RepositoryLayer.Quota;
using ServiceLayer.Quota;

namespace Lumina.Tests.ServiceTests
{
    public class IncrementQuotaAsyncUnitTest
    {
        private readonly Mock<IQuotaRepository> _mockQuotaRepo;
        private readonly QuotaService _service;

        public IncrementQuotaAsyncUnitTest()
        {
            _mockQuotaRepo = new Mock<IQuotaRepository>();
            _service = new QuotaService(_mockQuotaRepo.Object);
        }

        [Theory]
        [InlineData(1, "reading")]
        [InlineData(2, "listening")]
        [InlineData(3, "speaking")]
        public async Task IncrementQuotaAsync_WithValidInputs_ShouldCheckResetAndIncrementInOrder(int userId, string skill)
        {
            // Arrange
            var callSequence = new List<string>();

            _mockQuotaRepo.Setup(x => x.CheckAndResetQuotaAsync(userId))
                .Callback(() => callSequence.Add("CheckAndReset"))
                .Returns(Task.CompletedTask);

            _mockQuotaRepo.Setup(x => x.IncrementAttemptAsync(userId, skill))
                .Callback(() => callSequence.Add("Increment"))
                .Returns(Task.CompletedTask);

            // Act
            await _service.IncrementQuotaAsync(userId, skill);

            // Assert
            callSequence.Should().HaveCount(2);
            callSequence[0].Should().Be("CheckAndReset");
            callSequence[1].Should().Be("Increment");

            _mockQuotaRepo.Verify(x => x.CheckAndResetQuotaAsync(userId), Times.Once);
            _mockQuotaRepo.Verify(x => x.IncrementAttemptAsync(userId, skill), Times.Once);
        }
    }
}
