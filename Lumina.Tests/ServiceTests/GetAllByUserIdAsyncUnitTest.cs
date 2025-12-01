using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataLayer.DTOs.UserReport;
using Microsoft.Extensions.Logging;
using Moq;
using RepositoryLayer.Report;
using ServiceLayer.Report;
using Xunit;

namespace Lumina.Test.Services
{
    public class GetAllByUserIdAsyncUnitTest
    {
        private readonly Mock<IReportRepository> _mockReportRepository;
        private readonly Mock<ILogger<ReportService>> _mockLogger;
        private readonly ReportService _service;

        public GetAllByUserIdAsyncUnitTest()
        {
            _mockReportRepository = new Mock<IReportRepository>();
            _mockLogger = new Mock<ILogger<ReportService>>();

            _service = new ReportService(
                _mockReportRepository.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task GetAllByUserIdAsync_WhenUserIdIsOne_ShouldReturnListAndLogCount()
        {
            // Arrange
            int userId = 1;
            var reports = new List<UserReportResponse>
            {
                new UserReportResponse
                {
                    ReportId = 1,
                    Title = "User 1 Report 1",
                    Content = "Content 1",
                    SendBy = "User1",
                    SendAt = DateTime.UtcNow,
                    Type = "General"
                },
                new UserReportResponse
                {
                    ReportId = 2,
                    Title = "User 1 Report 2",
                    Content = "Content 2",
                    SendBy = "User1",
                    SendAt = DateTime.UtcNow,
                    Type = "General"
                }
            };

            _mockReportRepository
                .Setup(r => r.GetAllByUserIdAsync(userId))
                .ReturnsAsync(reports);

            // Act
            var result = await _service.GetAllByUserIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            _mockReportRepository.Verify(
                r => r.GetAllByUserIdAsync(userId),
                Times.Once);

            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Getting all reports for user")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Found 2 reports for user")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllByUserIdAsync_WhenUserIdIsZero_ShouldReturnEmptyListAndLogZeroCount()
        {
            // Arrange
            int userId = 0;
            var reports = new List<UserReportResponse>();

            _mockReportRepository
                .Setup(r => r.GetAllByUserIdAsync(userId))
                .ReturnsAsync(reports);

            // Act
            var result = await _service.GetAllByUserIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            _mockReportRepository.Verify(
                r => r.GetAllByUserIdAsync(userId),
                Times.Once);

            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Getting all reports for user")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Found 0 reports for user")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllByUserIdAsync_WhenUserIdIsNegativeOne_ShouldLogErrorAndRethrow()
        {
            // Arrange
            int userId = -1;
            var exception = new InvalidOperationException("Repository error");

            _mockReportRepository
                .Setup(r => r.GetAllByUserIdAsync(userId))
                .ThrowsAsync(exception);

            // Act
            var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetAllByUserIdAsync(userId));

            // Assert
            Assert.Equal(exception.Message, thrown.Message);

            _mockReportRepository.Verify(
                r => r.GetAllByUserIdAsync(userId),
                Times.Once);

            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Error getting reports for user")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}


