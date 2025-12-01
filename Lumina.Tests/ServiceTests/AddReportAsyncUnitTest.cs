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
    public class AddReportAsyncUnitTest
    {
        private readonly Mock<IReportRepository> _mockReportRepository;
        private readonly Mock<ILogger<ReportService>> _mockLogger;
        private readonly ReportService _service;

        public AddReportAsyncUnitTest()
        {
            _mockReportRepository = new Mock<IReportRepository>();
            _mockLogger = new Mock<ILogger<ReportService>>();

            _service = new ReportService(
                _mockReportRepository.Object,
                _mockLogger.Object);
        }

        private static UserReportRequest CreateValidRequest()
        {
            return new UserReportRequest
            {
                ReportId = null,
                Title = "Test Report",
                Content = "Some content",
                SendBy = 1,
                SendAt = DateTime.UtcNow,
                Type = "General"
            };
        }

        [Fact]
        public async Task AddAsync_WhenRepositoryReturnsTrue_ShouldLogSuccessAndReturnTrue()
        {
            // Arrange
            var request = CreateValidRequest();

            _mockReportRepository
                .Setup(r => r.AddAsync(request))
                .ReturnsAsync(true);

            // Act
            var result = await _service.AddAsync(request);

            // Assert
            Assert.True(result);

            _mockReportRepository.Verify(
                r => r.AddAsync(request),
                Times.Once);

            // Initial info log
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Creating report")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            // Success info log
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Report created successfully")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task AddAsync_WhenRepositoryReturnsFalse_ShouldLogWarningAndReturnFalse()
        {
            // Arrange
            var request = CreateValidRequest();

            _mockReportRepository
                .Setup(r => r.AddAsync(request))
                .ReturnsAsync(false);

            // Act
            var result = await _service.AddAsync(request);

            // Assert
            Assert.False(result);

            _mockReportRepository.Verify(
                r => r.AddAsync(request),
                Times.Once);

            // Initial info log
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Creating report")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            // Failure warning log
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Failed to create report")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task AddAsync_WhenRepositoryThrowsException_ShouldLogErrorAndRethrow()
        {
            // Arrange
            var request = CreateValidRequest();
            var exception = new InvalidOperationException("Repository error");

            _mockReportRepository
                .Setup(r => r.AddAsync(request))
                .ThrowsAsync(exception);

            // Act
            var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AddAsync(request));

            // Assert
            Assert.Equal(exception.Message, thrown.Message);

            _mockReportRepository.Verify(
                r => r.AddAsync(request),
                Times.Once);

            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Error creating report")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}


