using System;
using System.Threading.Tasks;
using DataLayer.DTOs.UserReport;
using Microsoft.Extensions.Logging;
using Moq;
using RepositoryLayer.Report;
using ServiceLayer.Report;
using Xunit;

namespace Lumina.Test.Services
{
    public class FindByIdAsyncUnitTest
    {
        private readonly Mock<IReportRepository> _mockReportRepository;
        private readonly Mock<ILogger<ReportService>> _mockLogger;
        private readonly ReportService _service;

        public FindByIdAsyncUnitTest()
        {
            _mockReportRepository = new Mock<IReportRepository>();
            _mockLogger = new Mock<ILogger<ReportService>>();

            _service = new ReportService(
                _mockReportRepository.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task FindByIdAsync_WhenReportExists_ShouldReturnReportAndLogInformation()
        {
            // Arrange
            int id = 1;
            var expectedReport = new UserReportResponse
            {
                ReportId = id,
                Title = "Existing report",
                Content = "Content",
                SendBy = "User1",
                SendAt = DateTime.UtcNow,
                Type = "General"
            };

            _mockReportRepository
                .Setup(r => r.FindByIdAsync(id))
                .ReturnsAsync(expectedReport);

            // Act
            var result = await _service.FindByIdAsync(id);

            // Assert
            Assert.NotNull(result);
            Assert.Same(expectedReport, result);

            _mockReportRepository.Verify(
                r => r.FindByIdAsync(id),
                Times.Once);

            // Initial info log
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Finding report by ID")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            // No warning log when report exists
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task FindByIdAsync_WhenReportDoesNotExist_ShouldReturnNullAndLogWarning()
        {
            // Arrange
            int id = 0; // boundary/invalid id in business sense

            _mockReportRepository
                .Setup(r => r.FindByIdAsync(id))
                .ReturnsAsync((UserReportResponse?)null);

            // Act
            var result = await _service.FindByIdAsync(id);

            // Assert
            Assert.Null(result);

            _mockReportRepository.Verify(
                r => r.FindByIdAsync(id),
                Times.Once);

            // Initial info log
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Finding report by ID")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            // Warning when not found
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Report not found")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task FindByIdAsync_WhenRepositoryThrowsException_ShouldLogErrorAndRethrow()
        {
            // Arrange
            int id = -1; // invalid id to simulate error scenario
            var exception = new InvalidOperationException("Repository error");

            _mockReportRepository
                .Setup(r => r.FindByIdAsync(id))
                .ThrowsAsync(exception);

            // Act
            var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.FindByIdAsync(id));

            // Assert
            Assert.Equal(exception.Message, thrown.Message);

            _mockReportRepository.Verify(
                r => r.FindByIdAsync(id),
                Times.Once);

            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Error finding report by ID")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}


