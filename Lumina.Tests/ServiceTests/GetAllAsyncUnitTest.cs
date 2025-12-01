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
    public class GetAllAsyncUnitTest
    {
        private readonly Mock<IReportRepository> _mockReportRepository;
        private readonly Mock<ILogger<ReportService>> _mockLogger;
        private readonly ReportService _service;

        public GetAllAsyncUnitTest()
        {
            _mockReportRepository = new Mock<IReportRepository>();
            _mockLogger = new Mock<ILogger<ReportService>>();

            _service = new ReportService(
                _mockReportRepository.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task GetAllAsync_WhenReportsExist_ShouldReturnListAndLogCount()
        {
            // Arrange
            int roleId = 1;
            var reports = new List<UserReportResponse>
            {
                new UserReportResponse
                {
                    ReportId = 1,
                    Title = "Report 1",
                    Content = "Content 1",
                    SendBy = "User1",
                    SendAt = DateTime.UtcNow,
                    Type = "General"
                },
                new UserReportResponse
                {
                    ReportId = 2,
                    Title = "Report 2",
                    Content = "Content 2",
                    SendBy = "User2",
                    SendAt = DateTime.UtcNow,
                    Type = "General"
                }
            };

            _mockReportRepository
                .Setup(r => r.GetAllAsync(roleId))
                .ReturnsAsync(reports);

            // Act
            var result = await _service.GetAllAsync(roleId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            _mockReportRepository.Verify(
                r => r.GetAllAsync(roleId),
                Times.Once);

            // Initial info log
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Getting all reports for role")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            // Info log with count
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Found 2 reports for role")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WhenNoReportsExist_ShouldReturnEmptyListAndLogZeroCount()
        {
            // Arrange
            int roleId = 2;
            var reports = new List<UserReportResponse>();

            _mockReportRepository
                .Setup(r => r.GetAllAsync(roleId))
                .ReturnsAsync(reports);

            // Act
            var result = await _service.GetAllAsync(roleId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            _mockReportRepository.Verify(
                r => r.GetAllAsync(roleId),
                Times.Once);

            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Getting all reports for role")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Found 0 reports for role")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WhenNoReportsExist_WithRoleIdZero_ShouldReturnEmptyListAndLogZeroCount()
        {
            // Arrange
            int roleId = 0;
            var reports = new List<UserReportResponse>();

            _mockReportRepository
                .Setup(r => r.GetAllAsync(roleId))
                .ReturnsAsync(reports);

            // Act
            var result = await _service.GetAllAsync(roleId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            _mockReportRepository.Verify(
                r => r.GetAllAsync(roleId),
                Times.Once);

            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Getting all reports for role")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Found 0 reports for role")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WhenReportsExist_WithRoleIdThree_ShouldReturnListAndLogCount()
        {
            // Arrange
            int roleId = 3;
            var reports = new List<UserReportResponse>
            {
                new UserReportResponse
                {
                    ReportId = 10,
                    Title = "Role 3 Report 1",
                    Content = "Content 1",
                    SendBy = "User1",
                    SendAt = DateTime.UtcNow,
                    Type = "General"
                }
            };

            _mockReportRepository
                .Setup(r => r.GetAllAsync(roleId))
                .ReturnsAsync(reports);

            // Act
            var result = await _service.GetAllAsync(roleId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);

            _mockReportRepository.Verify(
                r => r.GetAllAsync(roleId),
                Times.Once);

            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Getting all reports for role")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Found 1 reports for role")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WhenNoReportsExist_WithRoleIdFour_ShouldReturnEmptyListAndLogZeroCount()
        {
            // Arrange
            int roleId = 4;
            var reports = new List<UserReportResponse>();

            _mockReportRepository
                .Setup(r => r.GetAllAsync(roleId))
                .ReturnsAsync(reports);

            // Act
            var result = await _service.GetAllAsync(roleId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            _mockReportRepository.Verify(
                r => r.GetAllAsync(roleId),
                Times.Once);

            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Getting all reports for role")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Found 0 reports for role")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WhenRepositoryThrowsException_ShouldLogErrorAndRethrow()
        {
            // Arrange
            int roleId = -1;
            var exception = new InvalidOperationException("Repository error");

            _mockReportRepository
                .Setup(r => r.GetAllAsync(roleId))
                .ThrowsAsync(exception);

            // Act
            var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetAllAsync(roleId));

            // Assert
            Assert.Equal(exception.Message, thrown.Message);

            _mockReportRepository.Verify(
                r => r.GetAllAsync(roleId),
                Times.Once);

            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Error getting reports for role")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}


