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
    public class UpdateReportAsyncUnitTest
    {
        private readonly Mock<IReportRepository> _mockReportRepository;
        private readonly Mock<ILogger<ReportService>> _mockLogger;
        private readonly ReportService _service;

        public UpdateReportAsyncUnitTest()
        {
            _mockReportRepository = new Mock<IReportRepository>();
            _mockLogger = new Mock<ILogger<ReportService>>();

            _service = new ReportService(
                _mockReportRepository.Object,
                _mockLogger.Object);
        }

        private static UserReportRequest CreateValidRequest(int? reportId = 1)
        {
            return new UserReportRequest
            {
                ReportId = reportId,
                Title = "Updated Title",
                Content = "Updated Content",
                SendBy = 1,
                SendAt = DateTime.UtcNow,
                Type = "General"
            };
        }

        [Fact]
        public async Task UpdateAsync_WhenRepositoryReturnsTrue_ShouldLogSuccessAndReturnTrue()
        {
            // Arrange
            var request = CreateValidRequest(1);

            _mockReportRepository
                .Setup(r => r.UpdateAsync(request))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateAsync(request);

            // Assert
            Assert.True(result);

            _mockReportRepository.Verify(
                r => r.UpdateAsync(request),
                Times.Once);

            // Initial info log
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Updating report")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            // Success info log
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("updated successfully")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenRepositoryReturnsFalse_ShouldLogWarningAndReturnFalse()
        {
            // Arrange
            var request = CreateValidRequest(1);

            _mockReportRepository
                .Setup(r => r.UpdateAsync(request))
                .ReturnsAsync(false);

            // Act
            var result = await _service.UpdateAsync(request);

            // Assert
            Assert.False(result);

            _mockReportRepository.Verify(
                r => r.UpdateAsync(request),
                Times.Once);

            // Initial info log
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Updating report")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            // Failure warning log
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Failed to update report")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenRepositoryThrowsException_ShouldLogErrorAndRethrow()
        {
            // Arrange
            var request = CreateValidRequest(-1);
            var exception = new InvalidOperationException("Repository error");

            _mockReportRepository
                .Setup(r => r.UpdateAsync(request))
                .ThrowsAsync(exception);

            // Act
            var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(request));

            // Assert
            Assert.Equal(exception.Message, thrown.Message);

            _mockReportRepository.Verify(
                r => r.UpdateAsync(request),
                Times.Once);

            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Error updating report")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}


