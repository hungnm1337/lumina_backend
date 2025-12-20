using DataLayer.DTOs.UserReport;
using Microsoft.Extensions.Logging;
using Moq;
using RepositoryLayer.Report;
using ServiceLayer.Report;
using Xunit;

namespace Lumina.Tests.ServiceTests;

public class UpdateAsyncUnitTest
{
    private readonly Mock<IReportRepository> _mockReportRepository;
    private readonly Mock<ILogger<ReportService>> _mockLogger;
    private readonly ReportService _reportService;

    public UpdateAsyncUnitTest()
    {
        _mockReportRepository = new Mock<IReportRepository>();
        _mockLogger = new Mock<ILogger<ReportService>>();
        _reportService = new ReportService(_mockReportRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task UpdateAsync_WithValidReport_ReturnsTrue()
    {
        // Arrange
        var validReport = new UserReportRequest
        {
            ReportId = 1,
            Title = "Updated Report",
            Content = "Updated Content",
            SendBy = 1,
            SendAt = DateTime.Now,
            Type = "Bug"
        };

        _mockReportRepository
            .Setup(r => r.UpdateAsync(It.IsAny<UserReportRequest>()))
            .ReturnsAsync(true);

        // Act
        var result = await _reportService.UpdateAsync(validReport);

        // Assert
        Assert.True(result);
        _mockReportRepository.Verify(r => r.UpdateAsync(It.IsAny<UserReportRequest>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenRepositoryReturnsFalse_ReturnsFalse()
    {
        // Arrange
        var validReport = new UserReportRequest
        {
            ReportId = 999,
            Title = "Non-existent Report",
            Content = "Content",
            SendBy = 1,
            SendAt = DateTime.Now,
            Type = "Bug"
        };

        _mockReportRepository
            .Setup(r => r.UpdateAsync(It.IsAny<UserReportRequest>()))
            .ReturnsAsync(false);

        // Act
        var result = await _reportService.UpdateAsync(validReport);

        // Assert
        Assert.False(result);
        _mockReportRepository.Verify(r => r.UpdateAsync(It.IsAny<UserReportRequest>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenRepositoryThrowsException_ThrowsException()
    {
        // Arrange
        var validReport = new UserReportRequest
        {
            ReportId = 1,
            Title = "Report Title",
            Content = "Content",
            SendBy = 1,
            SendAt = DateTime.Now,
            Type = "Bug"
        };

        var expectedException = new Exception("Database error");
        _mockReportRepository
            .Setup(r => r.UpdateAsync(It.IsAny<UserReportRequest>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            async () => await _reportService.UpdateAsync(validReport)
        );

        Assert.Equal("Database error", exception.Message);
        _mockReportRepository.Verify(r => r.UpdateAsync(It.IsAny<UserReportRequest>()), Times.Once);
    }
}
