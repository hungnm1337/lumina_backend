using DataLayer.DTOs.UserReport;
using Microsoft.Extensions.Logging;
using Moq;
using RepositoryLayer.Report;
using ServiceLayer.Report;
using Xunit;

namespace Lumina.Tests.ServiceTests;

public class AddAsyncUnitTest
{
    private readonly Mock<IReportRepository> _mockReportRepository;
    private readonly Mock<ILogger<ReportService>> _mockLogger;
    private readonly ReportService _reportService;

    public AddAsyncUnitTest()
    {
        _mockReportRepository = new Mock<IReportRepository>();
        _mockLogger = new Mock<ILogger<ReportService>>();
        _reportService = new ReportService(_mockReportRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task AddAsync_WithValidReport_ReturnsTrue()
    {
        // Arrange
        var validReport = new UserReportRequest
        {
            Title = "Valid Title",
            Content = "Valid Content",
            SendBy = 1,
            SendAt = DateTime.Now,
            Type = "Bug"
        };

        _mockReportRepository
            .Setup(r => r.AddAsync(It.IsAny<UserReportRequest>()))
            .ReturnsAsync(true);

        // Act
        var result = await _reportService.AddAsync(validReport);

        // Assert
        Assert.True(result);
        _mockReportRepository.Verify(r => r.AddAsync(It.IsAny<UserReportRequest>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_WhenRepositoryReturnsFalse_ReturnsFalse()
    {
        // Arrange
        var validReport = new UserReportRequest
        {
            Title = "Valid Title",
            Content = "Valid Content",
            SendBy = 1,
            SendAt = DateTime.Now,
            Type = "Bug"
        };

        _mockReportRepository
            .Setup(r => r.AddAsync(It.IsAny<UserReportRequest>()))
            .ReturnsAsync(false);

        // Act
        var result = await _reportService.AddAsync(validReport);

        // Assert
        Assert.False(result);
        _mockReportRepository.Verify(r => r.AddAsync(It.IsAny<UserReportRequest>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_WhenRepositoryThrowsException_ThrowsException()
    {
        // Arrange
        var validReport = new UserReportRequest
        {
            Title = "Valid Title",
            Content = "Valid Content",
            SendBy = 1,
            SendAt = DateTime.Now,
            Type = "Bug"
        };

        var expectedException = new Exception("Database error");
        _mockReportRepository
            .Setup(r => r.AddAsync(It.IsAny<UserReportRequest>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            async () => await _reportService.AddAsync(validReport)
        );

        Assert.Equal("Database error", exception.Message);
        _mockReportRepository.Verify(r => r.AddAsync(It.IsAny<UserReportRequest>()), Times.Once);
    }
}
