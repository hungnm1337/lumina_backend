using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using RepositoryLayer.Statistic;
using ServiceLayer.Statistic;
using Xunit;

namespace Lumina.Test.Services
{
    /// <summary>
    /// Unit tests cho StatisticService - Đạt 100% Line Coverage và 100% Branch Coverage
    /// </summary>
    public class StatisticServiceUnitTest
    {
        private readonly Mock<IStatisticRepository> _mockRepository;
        private readonly StatisticService _service;

        public StatisticServiceUnitTest()
        {
            _mockRepository = new Mock<IStatisticRepository>(MockBehavior.Strict);
            _service = new StatisticService(_mockRepository.Object);
        }

        #region GetDashboardStatsAsync Tests

        /// <summary>
        /// Test GetDashboardStatsAsync - Test tất cả các nhánh logic
        /// Coverage: Lines 18-58, all branches including month calculation
        /// </summary>
        [Fact]
        public async Task GetDashboardStatsAsync_ShouldCalculateStatsCorrectly()
        {
            // Arrange
            var currentYear = DateTime.UtcNow.Year;
            var currentMonth = DateTime.UtcNow.Month;
            var lastMonth = currentMonth == 1 ? 12 : currentMonth - 1;
            var lastMonthYear = currentMonth == 1 ? currentYear - 1 : currentYear;

            // Mock current month data
            _mockRepository.Setup(r => r.GetMonthlyRevenueAsync(currentYear, currentMonth))
                .ReturnsAsync(5000m);
            _mockRepository.Setup(r => r.GetNewUsersThisMonthAsync())
                .ReturnsAsync(100);
            _mockRepository.Setup(r => r.GetProUserCountAsync())
                .ReturnsAsync(50);
            _mockRepository.Setup(r => r.GetTotalUsersAsync())
                .ReturnsAsync(200);

            // Mock last month data
            _mockRepository.Setup(r => r.GetMonthlyRevenueAsync(lastMonthYear, lastMonth))
                .ReturnsAsync(4000m);

            // Act
            var result = await _service.GetDashboardStatsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5000m, result.MonthlyRevenue);
            Assert.Equal("+25.0%", result.MonthlyRevenueGrowth); // (5000-4000)/4000 * 100 = 25.0%
            Assert.Equal(100, result.NewUsers);
            Assert.Equal(25m, result.ProConversionRate); // 50/200 * 100 = 25%
            Assert.Equal(94.2m, result.RetentionRate);

            // Verify all repository calls
            _mockRepository.Verify(r => r.GetMonthlyRevenueAsync(currentYear, currentMonth), Times.Once);
            _mockRepository.Verify(r => r.GetNewUsersThisMonthAsync(), Times.Once);
            _mockRepository.Verify(r => r.GetProUserCountAsync(), Times.Once);
            _mockRepository.Verify(r => r.GetTotalUsersAsync(), Times.Once);
            _mockRepository.Verify(r => r.GetMonthlyRevenueAsync(lastMonthYear, lastMonth), Times.Once);
            _mockRepository.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Test GetDashboardStatsAsync khi lastMonthRevenue = 0 (Division by zero protection)
        /// Coverage: Lines 36-38, Branch: lastMonthRevenue > 0 is FALSE, revenueGrowth = 0
        /// </summary>
        [Fact]
        public async Task GetDashboardStatsAsync_WhenLastMonthRevenueIsZero_ShouldReturnZeroGrowth()
        {
            // Arrange
            var currentYear = DateTime.UtcNow.Year;
            var currentMonth = DateTime.UtcNow.Month;
            var lastMonth = currentMonth == 1 ? 12 : currentMonth - 1;
            var lastMonthYear = currentMonth == 1 ? currentYear - 1 : currentYear;

            _mockRepository.Setup(r => r.GetMonthlyRevenueAsync(currentYear, currentMonth))
                .ReturnsAsync(5000m);
            _mockRepository.Setup(r => r.GetNewUsersThisMonthAsync())
                .ReturnsAsync(50);
            _mockRepository.Setup(r => r.GetProUserCountAsync())
                .ReturnsAsync(25);
            _mockRepository.Setup(r => r.GetTotalUsersAsync())
                .ReturnsAsync(100);
            _mockRepository.Setup(r => r.GetMonthlyRevenueAsync(lastMonthYear, lastMonth))
                .ReturnsAsync(0m); // Last month revenue is zero

            // Act
            var result = await _service.GetDashboardStatsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("+0%", result.MonthlyRevenueGrowth); // Should be 0 when lastMonthRevenue is 0

            _mockRepository.Verify(r => r.GetMonthlyRevenueAsync(currentYear, currentMonth), Times.Once);
            _mockRepository.Verify(r => r.GetNewUsersThisMonthAsync(), Times.Once);
            _mockRepository.Verify(r => r.GetProUserCountAsync(), Times.Once);
            _mockRepository.Verify(r => r.GetTotalUsersAsync(), Times.Once);
            _mockRepository.Verify(r => r.GetMonthlyRevenueAsync(lastMonthYear, lastMonth), Times.Once);
            _mockRepository.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Test GetDashboardStatsAsync khi totalUsers = 0 (Division by zero protection)
        /// Coverage: Lines 40-42, Branch: totalUsers > 0 is FALSE, proConversionRate = 0
        /// </summary>
        [Fact]
        public async Task GetDashboardStatsAsync_WhenTotalUsersIsZero_ShouldReturnZeroConversionRate()
        {
            // Arrange
            var currentYear = DateTime.UtcNow.Year;
            var currentMonth = DateTime.UtcNow.Month;
            var lastMonth = currentMonth == 1 ? 12 : currentMonth - 1;
            var lastMonthYear = currentMonth == 1 ? currentYear - 1 : currentYear;

            _mockRepository.Setup(r => r.GetMonthlyRevenueAsync(currentYear, currentMonth))
                .ReturnsAsync(5000m);
            _mockRepository.Setup(r => r.GetNewUsersThisMonthAsync())
                .ReturnsAsync(50);
            _mockRepository.Setup(r => r.GetProUserCountAsync())
                .ReturnsAsync(10);
            _mockRepository.Setup(r => r.GetTotalUsersAsync())
                .ReturnsAsync(0); // Total users is zero
            _mockRepository.Setup(r => r.GetMonthlyRevenueAsync(lastMonthYear, lastMonth))
                .ReturnsAsync(4000m);

            // Act
            var result = await _service.GetDashboardStatsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0m, result.ProConversionRate); // Should be 0 when totalUsers is 0

            _mockRepository.Verify(r => r.GetMonthlyRevenueAsync(currentYear, currentMonth), Times.Once);
            _mockRepository.Verify(r => r.GetNewUsersThisMonthAsync(), Times.Once);
            _mockRepository.Verify(r => r.GetProUserCountAsync(), Times.Once);
            _mockRepository.Verify(r => r.GetTotalUsersAsync(), Times.Once);
            _mockRepository.Verify(r => r.GetMonthlyRevenueAsync(lastMonthYear, lastMonth), Times.Once);
            _mockRepository.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Test GetDashboardStatsAsync với revenue âm (negative growth)
        /// Coverage: Test case khi revenue giảm
        /// </summary>
        [Fact]
        public async Task GetDashboardStatsAsync_WhenRevenueDecreases_ShouldReturnNegativeGrowth()
        {
            // Arrange
            var currentYear = DateTime.UtcNow.Year;
            var currentMonth = DateTime.UtcNow.Month;
            var lastMonth = currentMonth == 1 ? 12 : currentMonth - 1;
            var lastMonthYear = currentMonth == 1 ? currentYear - 1 : currentYear;

            _mockRepository.Setup(r => r.GetMonthlyRevenueAsync(currentYear, currentMonth))
                .ReturnsAsync(3000m); // Current month lower than last month
            _mockRepository.Setup(r => r.GetNewUsersThisMonthAsync())
                .ReturnsAsync(50);
            _mockRepository.Setup(r => r.GetProUserCountAsync())
                .ReturnsAsync(25);
            _mockRepository.Setup(r => r.GetTotalUsersAsync())
                .ReturnsAsync(100);
            _mockRepository.Setup(r => r.GetMonthlyRevenueAsync(lastMonthYear, lastMonth))
                .ReturnsAsync(4000m);

            // Act
            var result = await _service.GetDashboardStatsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("+-25.0%", result.MonthlyRevenueGrowth); // (3000-4000)/4000 * 100 = -25.0%

            _mockRepository.Verify(r => r.GetMonthlyRevenueAsync(currentYear, currentMonth), Times.Once);
            _mockRepository.Verify(r => r.GetNewUsersThisMonthAsync(), Times.Once);
            _mockRepository.Verify(r => r.GetProUserCountAsync(), Times.Once);
            _mockRepository.Verify(r => r.GetTotalUsersAsync(), Times.Once);
            _mockRepository.Verify(r => r.GetMonthlyRevenueAsync(lastMonthYear, lastMonth), Times.Once);
            _mockRepository.VerifyNoOtherCalls();
        }

        #endregion

        #region GetRevenueChartDataAsync Tests

        /// <summary>
        /// Test GetRevenueChartDataAsync với revenue data hợp lệ
        /// Coverage: Lines 60-70
        /// </summary>
        [Fact]
        public async Task GetRevenueChartDataAsync_WhenValidYear_ShouldReturnChartData()
        {
            // Arrange
            var year = 2024;
            var monthlyData = new List<MonthlyRevenueDTO>
            {
                new MonthlyRevenueDTO { Month = 1, Revenue = 1000m },
                new MonthlyRevenueDTO { Month = 2, Revenue = 1500m },
                new MonthlyRevenueDTO { Month = 3, Revenue = 2000m },
                new MonthlyRevenueDTO { Month = 4, Revenue = 2500m },
                new MonthlyRevenueDTO { Month = 5, Revenue = 3000m },
                new MonthlyRevenueDTO { Month = 6, Revenue = 3500m },
                new MonthlyRevenueDTO { Month = 7, Revenue = 4000m },
                new MonthlyRevenueDTO { Month = 8, Revenue = 4500m },
                new MonthlyRevenueDTO { Month = 9, Revenue = 5000m },
                new MonthlyRevenueDTO { Month = 10, Revenue = 5500m },
                new MonthlyRevenueDTO { Month = 11, Revenue = 6000m },
                new MonthlyRevenueDTO { Month = 12, Revenue = 6500m }
            };

            _mockRepository.Setup(r => r.GetMonthlyRevenueForYearAsync(year))
                .ReturnsAsync(monthlyData);

            // Act
            var result = await _service.GetRevenueChartDataAsync(year);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Labels);
            Assert.Equal(12, result.Labels.Count);
            Assert.Equal("T1", result.Labels[0]);
            Assert.Equal("T12", result.Labels[11]);
            
            Assert.NotNull(result.Data);
            Assert.Equal(12, result.Data.Count);
            Assert.Equal(1000m, result.Data[0]);
            Assert.Equal(6500m, result.Data[11]);
            
            Assert.Equal(45000m, result.Total); // Sum of all revenues

            _mockRepository.Verify(r => r.GetMonthlyRevenueForYearAsync(year), Times.Once);
            _mockRepository.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Test GetRevenueChartDataAsync khi không có dữ liệu
        /// Coverage: Empty list scenario
        /// </summary>
        [Fact]
        public async Task GetRevenueChartDataAsync_WhenNoData_ShouldReturnEmptyChartData()
        {
            // Arrange
            var year = 2023;
            var emptyData = new List<MonthlyRevenueDTO>();

            _mockRepository.Setup(r => r.GetMonthlyRevenueForYearAsync(year))
                .ReturnsAsync(emptyData);

            // Act
            var result = await _service.GetRevenueChartDataAsync(year);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(12, result.Labels.Count); // Labels are fixed
            Assert.Empty(result.Data);
            Assert.Equal(0m, result.Total);

            _mockRepository.Verify(r => r.GetMonthlyRevenueForYearAsync(year), Times.Once);
            _mockRepository.VerifyNoOtherCalls();
        }

        #endregion

        #region GetUserGrowthChartDataAsync Tests

        /// <summary>
        /// Test GetUserGrowthChartDataAsync với dữ liệu hợp lệ
        /// Coverage: Lines 72-84
        /// </summary>
        [Fact]
        public async Task GetUserGrowthChartDataAsync_WhenValidMonths_ShouldReturnGrowthData()
        {
            // Arrange
            var months = 6;
            var growthData = new List<UserGrowthDTO>
            {
                new UserGrowthDTO { Month = 1, FreeUsers = 100, ProUsers = 20 },
                new UserGrowthDTO { Month = 2, FreeUsers = 150, ProUsers = 30 },
                new UserGrowthDTO { Month = 3, FreeUsers = 200, ProUsers = 40 },
                new UserGrowthDTO { Month = 4, FreeUsers = 250, ProUsers = 50 },
                new UserGrowthDTO { Month = 5, FreeUsers = 300, ProUsers = 60 },
                new UserGrowthDTO { Month = 6, FreeUsers = 350, ProUsers = 70 }
            };

            _mockRepository.Setup(r => r.GetUserGrowthForMonthsAsync(months))
                .ReturnsAsync(growthData);

            // Act
            var result = await _service.GetUserGrowthChartDataAsync(months);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Labels);
            Assert.Equal(6, result.Labels.Count);
            Assert.Equal("T1", result.Labels[0]);
            Assert.Equal("T6", result.Labels[5]);
            
            Assert.NotNull(result.FreeUsers);
            Assert.Equal(6, result.FreeUsers.Count);
            Assert.Equal(100, result.FreeUsers[0]);
            Assert.Equal(350, result.FreeUsers[5]);
            
            Assert.NotNull(result.ProUsers);
            Assert.Equal(6, result.ProUsers.Count);
            Assert.Equal(20, result.ProUsers[0]);
            Assert.Equal(70, result.ProUsers[5]);

            _mockRepository.Verify(r => r.GetUserGrowthForMonthsAsync(months), Times.Once);
            _mockRepository.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Test GetUserGrowthChartDataAsync khi không có dữ liệu
        /// Coverage: Empty list scenario
        /// </summary>
        [Fact]
        public async Task GetUserGrowthChartDataAsync_WhenNoData_ShouldReturnEmptyGrowthData()
        {
            // Arrange
            var months = 3;
            var emptyData = new List<UserGrowthDTO>();

            _mockRepository.Setup(r => r.GetUserGrowthForMonthsAsync(months))
                .ReturnsAsync(emptyData);

            // Act
            var result = await _service.GetUserGrowthChartDataAsync(months);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Labels);
            Assert.Empty(result.FreeUsers);
            Assert.Empty(result.ProUsers);

            _mockRepository.Verify(r => r.GetUserGrowthForMonthsAsync(months), Times.Once);
            _mockRepository.VerifyNoOtherCalls();
        }

        #endregion

        #region GetPlanDistributionDataAsync Tests

        /// <summary>
        /// Test GetPlanDistributionDataAsync với dữ liệu hợp lệ
        /// Coverage: Lines 86-96
        /// </summary>
        [Fact]
        public async Task GetPlanDistributionDataAsync_WhenPlansExist_ShouldReturnDistributionData()
        {
            // Arrange
            var planData = new List<PlanDistributionDTO>
            {
                new PlanDistributionDTO { PlanName = "Free", UserCount = 500, Percentage = 50m },
                new PlanDistributionDTO { PlanName = "Basic", UserCount = 300, Percentage = 30m },
                new PlanDistributionDTO { PlanName = "Premium", UserCount = 200, Percentage = 20m }
            };

            _mockRepository.Setup(r => r.GetPlanDistributionAsync())
                .ReturnsAsync(planData);

            // Act
            var result = await _service.GetPlanDistributionDataAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Labels);
            Assert.Equal(3, result.Labels.Count);
            Assert.Equal("Free", result.Labels[0]);
            Assert.Equal("Premium", result.Labels[2]);
            
            Assert.NotNull(result.Data);
            Assert.Equal(3, result.Data.Count);
            Assert.Equal(500, result.Data[0]);
            Assert.Equal(200, result.Data[2]);
            
            Assert.NotNull(result.Percentages);
            Assert.Equal(3, result.Percentages.Count);
            Assert.Equal(50m, result.Percentages[0]);
            Assert.Equal(20m, result.Percentages[2]);

            _mockRepository.Verify(r => r.GetPlanDistributionAsync(), Times.Once);
            _mockRepository.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Test GetPlanDistributionDataAsync khi không có dữ liệu
        /// Coverage: Empty list scenario
        /// </summary>
        [Fact]
        public async Task GetPlanDistributionDataAsync_WhenNoPlans_ShouldReturnEmptyDistributionData()
        {
            // Arrange
            var emptyData = new List<PlanDistributionDTO>();

            _mockRepository.Setup(r => r.GetPlanDistributionAsync())
                .ReturnsAsync(emptyData);

            // Act
            var result = await _service.GetPlanDistributionDataAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Labels);
            Assert.Empty(result.Data);
            Assert.Empty(result.Percentages);

            _mockRepository.Verify(r => r.GetPlanDistributionAsync(), Times.Once);
            _mockRepository.VerifyNoOtherCalls();
        }

        #endregion

        #region GetDailyAnalyticsAsync Tests

        /// <summary>
        /// Test GetDailyAnalyticsAsync với dữ liệu hợp lệ
        /// Coverage: Lines 98-101
        /// </summary>
        [Fact]
        public async Task GetDailyAnalyticsAsync_WhenValidDays_ShouldReturnAnalyticsData()
        {
            // Arrange
            var days = 7;
            var analyticsData = new List<DailyAnalyticsDTO>
            {
                new DailyAnalyticsDTO 
                { 
                    Date = DateTime.UtcNow.AddDays(-6), 
                    Revenue = 100m, 
                    NewUsers = 10, 
                    ProConversions = 2,
                    ConversionRate = 20m,
                    TrendPercentage = 5m
                },
                new DailyAnalyticsDTO 
                { 
                    Date = DateTime.UtcNow.AddDays(-5), 
                    Revenue = 150m, 
                    NewUsers = 15, 
                    ProConversions = 3,
                    ConversionRate = 20m,
                    TrendPercentage = 10m
                },
                new DailyAnalyticsDTO 
                { 
                    Date = DateTime.UtcNow, 
                    Revenue = 200m, 
                    NewUsers = 20, 
                    ProConversions = 5,
                    ConversionRate = 25m,
                    TrendPercentage = 15m
                }
            };

            _mockRepository.Setup(r => r.GetDailyAnalyticsAsync(days))
                .ReturnsAsync(analyticsData);

            // Act
            var result = await _service.GetDailyAnalyticsAsync(days);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal(100m, result[0].Revenue);
            Assert.Equal(200m, result[2].Revenue);
            Assert.Equal(10, result[0].NewUsers);
            Assert.Equal(20, result[2].NewUsers);

            _mockRepository.Verify(r => r.GetDailyAnalyticsAsync(days), Times.Once);
            _mockRepository.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Test GetDailyAnalyticsAsync khi không có dữ liệu
        /// Coverage: Empty list scenario
        /// </summary>
        [Fact]
        public async Task GetDailyAnalyticsAsync_WhenNoData_ShouldReturnEmptyList()
        {
            // Arrange
            var days = 30;
            var emptyData = new List<DailyAnalyticsDTO>();

            _mockRepository.Setup(r => r.GetDailyAnalyticsAsync(days))
                .ReturnsAsync(emptyData);

            // Act
            var result = await _service.GetDailyAnalyticsAsync(days);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            _mockRepository.Verify(r => r.GetDailyAnalyticsAsync(days), Times.Once);
            _mockRepository.VerifyNoOtherCalls();
        }

        #endregion
    }
}
