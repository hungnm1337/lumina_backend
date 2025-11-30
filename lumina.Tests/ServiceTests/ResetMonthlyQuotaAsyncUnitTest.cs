using Moq;
using RepositoryLayer.Quota;
using ServiceLayer.Quota;

namespace Lumina.Tests.ServiceTests
{
    public class ResetMonthlyQuotaAsyncUnitTest
    {
        private readonly Mock<IQuotaRepository> _mockQuotaRepo;
        private readonly QuotaService _service;

        public ResetMonthlyQuotaAsyncUnitTest()
        {
            _mockQuotaRepo = new Mock<IQuotaRepository>();
            _service = new QuotaService(_mockQuotaRepo.Object);
        }

        [Fact]
        public async Task ResetMonthlyQuotaAsync_ShouldCallResetAllQuotasAsync()
        {
            // Arrange
            _mockQuotaRepo.Setup(x => x.ResetAllQuotasAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _service.ResetMonthlyQuotaAsync();

            // Assert
            _mockQuotaRepo.Verify(x => x.ResetAllQuotasAsync(), Times.Once);
        }
    }
}
