using DataLayer.Models;
using lumina.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using RepositoryLayer.Statistic;
using ServiceLayer.Statistic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Lumina.Tests
{
    public class StatisticControllerTests : IDisposable
    {
        private readonly LuminaSystemContext _context;
        private readonly Mock<IStatisticService> _mockStatisticService;
        private readonly StatisticController _controller;

        public StatisticControllerTests()
        {
            var options = new DbContextOptionsBuilder<LuminaSystemContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new LuminaSystemContext(options);

            _mockStatisticService = new Mock<IStatisticService>();
            _controller = new StatisticController(_context, _mockStatisticService.Object);
        }

        #region Helper Methods for Reflection

        private static object? GetPropertyValue(object obj, string propertyName)
        {
            if (obj == null) return null;
            return obj.GetType().GetProperty(propertyName)?.GetValue(obj);
        }

        private static T? GetPropertyValue<T>(object obj, string propertyName)
        {
            var value = GetPropertyValue(obj, propertyName);
            if (value == null) return default;
            return (T)Convert.ChangeType(value, typeof(T));
        }

        #endregion

        #region GetDashboardStatsBasic Tests (3 tests)

        [Fact]
        public async Task GetDashboardStatsBasic_WithData_ReturnsOkWithCalculatedStats()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var today = now.Date;
            var firstOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            _context.Users.AddRange(
                new User { UserId = 1, RoleId = 4, FullName = "User 1", Email = "user1@test.com" },
                new User { UserId = 2, RoleId = 4, FullName = "User 2", Email = "user2@test.com" }
            );

            _context.Accounts.AddRange(
                new Account { AccountId = 1, UserId = 1, Username = "user1", CreateAt = today },
                new Account { AccountId = 2, UserId = 2, Username = "user2", CreateAt = firstOfMonth.AddDays(-5) }
            );

            _context.Packages.Add(new Package { PackageId = 1, PackageName = "Pro Monthly", DurationInDays = 30, Price = 100000, IsActive = true });

            var payment = new Payment
            {
                PaymentId = 1,
                UserId = 1,
                PackageId = 1,
                Amount = 100000,
                PaymentGatewayTransactionId = "TXN001",
                Status = "Success",
                CreatedAt = firstOfMonth.AddDays(5)
            };
            _context.Payments.Add(payment);

            _context.Subscriptions.Add(new Subscription
            {
                SubscriptionId = 1,
                UserId = 1,
                PackageId = 1,
                PaymentId = 1,
                Status = "Active",
                StartTime = now.AddDays(-5),
                EndTime = now.AddDays(10)
            });

            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetDashboardStatsBasic();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = okResult.Value;
            Assert.NotNull(data);
            Assert.Equal(2, GetPropertyValue<int>(data!, "totalUsers"));
            Assert.Equal(1, GetPropertyValue<int>(data!, "registeredToday"));
            Assert.True(GetPropertyValue<decimal>(data!, "monthlyRevenue") > 0);
            Assert.Equal(1, GetPropertyValue<int>(data!, "proUserCount"));
        }

        [Fact]
        public async Task GetDashboardStatsBasic_EmptyDatabase_ReturnsZeroStats()
        {
            // Act
            var result = await _controller.GetDashboardStatsBasic();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = okResult.Value;
            Assert.NotNull(data);
            Assert.Equal(0, GetPropertyValue<int>(data!, "totalUsers"));
            Assert.Equal(0, GetPropertyValue<int>(data!, "registeredToday"));
            Assert.Equal(0m, GetPropertyValue<decimal>(data!, "monthlyRevenue"));
            Assert.Equal(0, GetPropertyValue<int>(data!, "proUserCount"));
            Assert.Equal(0.0, GetPropertyValue<double>(data!, "proPercent"));
        }

        [Fact]
        public async Task GetDashboardStatsBasic_WithNullPackageDuration_HandlesGracefully()
        {
            // Arrange
            _context.Users.Add(new User { UserId = 1, RoleId = 4, FullName = "User Test", Email = "test@test.com" });
            _context.Accounts.Add(new Account { AccountId = 1, UserId = 1, Username = "testuser", CreateAt = DateTime.UtcNow });
            _context.Packages.Add(new Package { PackageId = 1, PackageName = "Test Package", DurationInDays = null, Price = 100000, IsActive = true });
            
            var payment = new Payment
            {
                PaymentId = 1,
                UserId = 1,
                PackageId = 1,
                Amount = 100000,
                PaymentGatewayTransactionId = "TXN002",
                Status = "Success",
                CreatedAt = DateTime.UtcNow
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetDashboardStatsBasic();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        #endregion

        #region GetFullDashboardStats Tests (2 tests)

        [Fact]
        public async Task GetFullDashboardStats_WithMultiplePackages_ReturnsDetailedStats()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var firstOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            _context.Packages.AddRange(
                new Package { PackageId = 1, PackageName = "Pro 1M", DurationInDays = 30, Price = 100000, IsActive = true },
                new Package { PackageId = 2, PackageName = "Pro 3M", DurationInDays = 90, Price = 250000, IsActive = true },
                new Package { PackageId = 3, PackageName = "Pro 12M", DurationInDays = 365, Price = 800000, IsActive = true }
            );

            _context.Users.Add(new User { UserId = 1, RoleId = 4, FullName = "Test User", Email = "test@test.com" });
            _context.Accounts.Add(new Account { AccountId = 1, UserId = 1, Username = "testuser", CreateAt = firstOfMonth.AddDays(5) });

            var payment1 = new Payment { PaymentId = 1, UserId = 1, PackageId = 1, Amount = 100000, PaymentGatewayTransactionId = "TXN003", Status = "Success", CreatedAt = firstOfMonth };
            var payment2 = new Payment { PaymentId = 2, UserId = 1, PackageId = 2, Amount = 250000, PaymentGatewayTransactionId = "TXN004", Status = "Success", CreatedAt = firstOfMonth };
            
            _context.Payments.AddRange(payment1, payment2);

            _context.Subscriptions.AddRange(
                new Subscription { SubscriptionId = 1, UserId = 1, PackageId = 1, PaymentId = 1, Status = "Active", StartTime = firstOfMonth, EndTime = now.AddDays(10) },
                new Subscription { SubscriptionId = 2, UserId = 1, PackageId = 2, PaymentId = 2, Status = "Active", StartTime = firstOfMonth, EndTime = now.AddDays(60) }
            );

            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetFullDashboardStats();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = okResult.Value;
            Assert.NotNull(data);
            Assert.True(GetPropertyValue<int>(data!, "TotalProUsers") >= 0);
            Assert.True(GetPropertyValue<decimal>(data!, "TotalRevenue") >= 0);
            Assert.NotNull(GetPropertyValue(data!, "PackageStats"));
        }

        [Fact]
        public async Task GetFullDashboardStats_NoActiveSubscriptions_ReturnsZeroProUsers()
        {
            // Arrange
            _context.Packages.Add(new Package { PackageId = 1, PackageName = "Pro", DurationInDays = 30, Price = 100000, IsActive = true });
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetFullDashboardStats();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = okResult.Value;
            Assert.NotNull(data);
            Assert.Equal(0, GetPropertyValue<int>(data!, "TotalProUsers"));
        }

        #endregion

        #region GetUserProSummary Tests (3 tests)

        [Fact]
        public async Task GetUserProSummary_ValidUser_ReturnsCalculatedSummary()
        {
            // Arrange
            int userId = 1;
            var now = DateTime.UtcNow;

            var payment1 = new Payment { PaymentId = 1, UserId = userId, PackageId = 1, Amount = 100000, PaymentGatewayTransactionId = "TXN005", Status = "Success", CreatedAt = now };
            var payment2 = new Payment { PaymentId = 2, UserId = userId, PackageId = 2, Amount = 250000, PaymentGatewayTransactionId = "TXN006", Status = "Success", CreatedAt = now };
            
            _context.Payments.AddRange(payment1, payment2);

            _context.Subscriptions.AddRange(
                new Subscription
                {
                    SubscriptionId = 1,
                    UserId = userId,
                    PackageId = 1,
                    PaymentId = 1,
                    Status = "Active",
                    StartTime = now.AddDays(-10),
                    EndTime = now.AddDays(20)
                },
                new Subscription
                {
                    SubscriptionId = 2,
                    UserId = userId,
                    PackageId = 2,
                    PaymentId = 2,
                    Status = "Active",
                    StartTime = now.AddDays(-30),
                    EndTime = now.AddDays(60)
                }
            );

            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetUserProSummary(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = okResult.Value;
            Assert.NotNull(data);
            Assert.Equal(350000m, GetPropertyValue<decimal>(data!, "totalMoney"));
            Assert.Equal(2, GetPropertyValue<int>(data!, "totalPackages"));
            Assert.True(GetPropertyValue<int>(data!, "totalDays") > 0);
            Assert.True(GetPropertyValue<int>(data!, "remainDays") >= 0);
        }

        [Fact]
        public async Task GetUserProSummary_UserNotFound_ReturnsZeroValues()
        {
            // Act
            var result = await _controller.GetUserProSummary(999);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = okResult.Value;
            Assert.NotNull(data);
            Assert.Equal(0m, GetPropertyValue<decimal>(data!, "totalMoney"));
            Assert.Equal(0, GetPropertyValue<int>(data!, "totalPackages"));
            Assert.Equal(0, GetPropertyValue<int>(data!, "totalDays"));
            Assert.Equal(0, GetPropertyValue<int>(data!, "remainDays"));
        }

        [Fact]
        public async Task GetUserProSummary_ExpiredSubscriptions_CalculatesCorrectRemainDays()
        {
            // Arrange
            int userId = 1;
            var now = DateTime.UtcNow;

            var payment = new Payment { PaymentId = 1, UserId = userId, PackageId = 1, Amount = 100000, PaymentGatewayTransactionId = "TXN007", Status = "Success", CreatedAt = now.AddDays(-60) };
            _context.Payments.Add(payment);

            _context.Subscriptions.Add(new Subscription
            {
                SubscriptionId = 1,
                UserId = userId,
                PackageId = 1,
                PaymentId = 1,
                Status = "Expired",
                StartTime = now.AddDays(-60),
                EndTime = now.AddDays(-1)
            });

            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetUserProSummary(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = okResult.Value;
            Assert.NotNull(data);
            Assert.Equal(0, GetPropertyValue<int>(data!, "remainDays"));
        }

        #endregion

        #region GetStaffDashboardStats Tests (2 tests)

        [Fact]
        public async Task GetStaffDashboardStats_WithData_ReturnsStatsAndRecentActivities()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var firstOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            _context.Users.Add(new User { UserId = 1, RoleId = 2, FullName = "Staff User", Email = "staff@test.com" });

            _context.Articles.AddRange(
                new Article { ArticleId = 1, Title = "Article 1", Summary = "Summary 1", CategoryId = 1, CreatedBy = 1, CreatedAt = firstOfMonth.AddDays(5), IsPublished = true },
                new Article { ArticleId = 2, Title = "Article 2", Summary = "Summary 2", CategoryId = 1, CreatedBy = 1, CreatedAt = firstOfMonth.AddDays(10), IsPublished = false }
            );

            _context.Exams.AddRange(
                new Exam 
                { 
                    ExamId = 1, 
                    Name = "Exam 1", 
                    Description = "Test 1", 
                    ExamType = "TOEIC",
                    ExamSetKey = "SET001",
                    CreatedBy = 1,
                    CreatedAt = firstOfMonth.AddDays(3), 
                    IsActive = true 
                },
                new Exam 
                { 
                    ExamId = 2, 
                    Name = "Exam 2", 
                    Description = "Test 2", 
                    ExamType = "TOEIC",
                    ExamSetKey = "SET002",
                    CreatedBy = 1,
                    CreatedAt = firstOfMonth.AddDays(7), 
                    IsActive = false 
                }
            );

            _context.Vocabularies.AddRange(
                new Vocabulary { VocabularyId = 1, VocabularyListId = 1, Word = "Hello", Definition = "Greeting", TypeOfWord = "Interjection" },
                new Vocabulary { VocabularyId = 2, VocabularyListId = 1, Word = "World", Definition = "Earth", TypeOfWord = "Noun" }
            );

            _context.ExamParts.Add(new ExamPart { PartId = 1, ExamId = 1, PartCode = "P1", Title = "Part 1", OrderIndex = 1, MaxQuestions = 10 });
            _context.Questions.Add(new Question { QuestionId = 1, PartId = 1, QuestionType = "MultipleChoice", StemText = "Test?", ScoreWeight = 1, Time = 60, QuestionNumber = 1 });
            _context.ExamAttempts.Add(new ExamAttempt 
            { 
                AttemptID = 1, 
                ExamID = 1, 
                UserID = 1,
                StartTime = now,
                Status = "Completed"
            });

            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetStaffDashboardStats();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = okResult.Value;
            Assert.NotNull(data);
            var stats = GetPropertyValue(data!, "stats");
            
            Assert.NotNull(stats);
            Assert.NotNull(GetPropertyValue(data!, "recentActivities"));
            Assert.NotNull(GetPropertyValue(data!, "metrics"));
            Assert.Equal(2, GetPropertyValue<int>(stats!, "totalArticles"));
            Assert.Equal(2, GetPropertyValue<int>(stats!, "totalTests"));
            Assert.Equal(2, GetPropertyValue<int>(stats!, "totalVocabulary"));
        }

        [Fact]
        public async Task GetStaffDashboardStats_EmptyDatabase_ReturnsZeroStats()
        {
            // Act
            var result = await _controller.GetStaffDashboardStats();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = okResult.Value;
            Assert.NotNull(data);
            var stats = GetPropertyValue(data!, "stats");
            
            Assert.Equal(0, GetPropertyValue<int>(stats!, "totalArticles"));
            Assert.Equal(0, GetPropertyValue<int>(stats!, "totalQuestions"));
            Assert.Equal(0, GetPropertyValue<int>(stats!, "totalTests"));
        }

        #endregion

        #region Service Layer Tests (10 tests)

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

            _mockStatisticService.Setup(s => s.GetDashboardStatsAsync()).ReturnsAsync(expectedStats);

            // Act
            var result = await _controller.GetDashboardStats();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var body = okResult.Value;
            Assert.NotNull(body);
            Assert.True(GetPropertyValue<bool>(body!, "success"));
            Assert.NotNull(GetPropertyValue(body!, "data"));
            _mockStatisticService.Verify(s => s.GetDashboardStatsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetDashboardStats_ServiceThrowsException_Returns500()
        {
            // Arrange
            _mockStatisticService.Setup(s => s.GetDashboardStatsAsync())
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _controller.GetDashboardStats();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var body = statusCodeResult.Value;
            Assert.NotNull(body);
            Assert.False(GetPropertyValue<bool>(body!, "success"));
            Assert.Contains("Lỗi khi lấy thống kê dashboard", GetPropertyValue<string>(body!, "message"));
            Assert.Equal("Database connection failed", GetPropertyValue<string>(body!, "error"));
        }

        [Fact]
        public async Task GetRevenueChart_WithYear_ReturnsOkWithData()
        {
            // Arrange
            var expectedData = new RevenueChartDTO
            {
                Labels = new List<string> { "T1", "T2", "T3" },
                Data = new List<decimal> { 1000000, 2000000, 3000000 },
                Total = 6000000
            };

            _mockStatisticService.Setup(s => s.GetRevenueChartDataAsync(2024)).ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetRevenueChart(2024);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var body = okResult.Value;
            Assert.NotNull(body);
            Assert.True(GetPropertyValue<bool>(body!, "success"));
            Assert.NotNull(GetPropertyValue(body!, "data"));
        }

        [Fact]
        public async Task GetRevenueChart_WithZeroYear_UsesCurrentYear()
        {
            // Arrange
            var currentYear = DateTime.UtcNow.Year;
            _mockStatisticService.Setup(s => s.GetRevenueChartDataAsync(currentYear))
                .ReturnsAsync(new RevenueChartDTO());

            // Act
            var result = await _controller.GetRevenueChart(0);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockStatisticService.Verify(s => s.GetRevenueChartDataAsync(currentYear), Times.Once);
        }

        [Fact]
        public async Task GetRevenueChart_ServiceThrows_Returns500()
        {
            // Arrange
            _mockStatisticService.Setup(s => s.GetRevenueChartDataAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Revenue chart error"));

            // Act
            var result = await _controller.GetRevenueChart(2024);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var body = statusCodeResult.Value;
            Assert.NotNull(body);
            Assert.False(GetPropertyValue<bool>(body!, "success"));
            Assert.Equal("Revenue chart error", GetPropertyValue<string>(body!, "error"));
        }

        [Fact]
        public async Task GetUserGrowthChart_WithMonths_ReturnsOkWithData()
        {
            // Arrange
            var expectedData = new UserGrowthChartDTO
            {
                Labels = new List<string> { "T1", "T2" },
                FreeUsers = new List<int> { 100, 150 },
                ProUsers = new List<int> { 20, 30 }
            };

            _mockStatisticService.Setup(s => s.GetUserGrowthChartDataAsync(6)).ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetUserGrowthChart(6);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var body = okResult.Value;
            Assert.NotNull(body);
            Assert.True(GetPropertyValue<bool>(body!, "success"));
        }

        [Fact]
        public async Task GetUserGrowthChart_ServiceThrows_Returns500()
        {
            // Arrange
            _mockStatisticService.Setup(s => s.GetUserGrowthChartDataAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("User growth error"));

            // Act
            var result = await _controller.GetUserGrowthChart(3);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task GetPlanDistribution_Success_ReturnsOkWithData()
        {
            // Arrange
            var expectedData = new PlanDistributionChartDTO
            {
                Labels = new List<string> { "Free", "Pro 1M" },
                Data = new List<int> { 70, 30 },
                Percentages = new List<decimal> { 70, 30 }
            };

            _mockStatisticService.Setup(s => s.GetPlanDistributionDataAsync()).ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetPlanDistribution();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var body = okResult.Value;
            Assert.NotNull(body);
            Assert.True(GetPropertyValue<bool>(body!, "success"));
        }

        [Fact]
        public async Task GetPlanDistribution_ServiceThrows_Returns500()
        {
            // Arrange
            _mockStatisticService.Setup(s => s.GetPlanDistributionDataAsync())
                .ThrowsAsync(new Exception("Plan distribution error"));

            // Act
            var result = await _controller.GetPlanDistribution();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task GetDailyAnalytics_WithDays_ReturnsOkWithData()
        {
            // Arrange
            var expectedData = new List<DailyAnalyticsDTO>
            {
                new DailyAnalyticsDTO { Date = DateTime.UtcNow.AddDays(-1), NewUsers = 5, Revenue = 100000 }
            };

            _mockStatisticService.Setup(s => s.GetDailyAnalyticsAsync(7)).ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetDailyAnalytics(7);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var body = okResult.Value;
            Assert.NotNull(body);
            Assert.True(GetPropertyValue<bool>(body!, "success"));
        }

        [Fact]
        public async Task GetDailyAnalytics_ServiceThrows_Returns500()
        {
            // Arrange
            _mockStatisticService.Setup(s => s.GetDailyAnalyticsAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Daily analytics error"));

            // Act
            var result = await _controller.GetDailyAnalytics(5);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        #endregion

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}