using lumina.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Statistic;
using System;
using System.Threading.Tasks;
using Xunit;
using DataLayer.Models;
using RepositoryLayer.Statistic;

namespace Lumina.Tests
{
    public class GetDashboardStatsTest
    {
        private readonly Mock<LuminaSystemContext> _mockContext;
        private readonly Mock<IStatisticService> _mockStatisticService;
        private readonly StatisticController _controller;

        public GetDashboardStatsTest()
        {
            _mockContext = new Mock<LuminaSystemContext>();
            _mockStatisticService = new Mock<IStatisticService>();
            _controller = new StatisticController(_mockContext.Object, _mockStatisticService.Object);
        }

        #region GetDashboardStats Tests

        [Fact]
        public async Task GetDashboardStats_Success_ReturnsOkWithData()
        {
            // Arrange
            var expectedStats = new DashboardStatsDTO
            {
                MonthlyRevenue = 10000000,
                MonthlyRevenueGrowth = "+15.5%",
                NewUsers = 150,
                NewUsersGrowth = "+12.5%",
                ProConversionRate = 25.5m,
                ProConversionGrowth = "+2.1%",
                RetentionRate = 94.2m,
                RetentionGrowth = "+1.8%"
            };

            _mockStatisticService.Setup(s => s.GetDashboardStatsAsync())
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _controller.GetDashboardStats();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            
            var response = okResult.Value;
            var successProperty = response.GetType().GetProperty("success");
            Assert.True((bool)successProperty.GetValue(response));
            
            // Verify service được gọi đúng 1 lần
            _mockStatisticService.Verify(s => s.GetDashboardStatsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetDashboardStats_ServiceThrowsException_Returns500()
        {
            // Arrange
            _mockStatisticService.Setup(s => s.GetDashboardStatsAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetDashboardStats();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            
            var response = statusCodeResult.Value;
            var successProperty = response.GetType().GetProperty("success");
            Assert.False((bool)successProperty.GetValue(response));
        }

        #endregion
    }
}