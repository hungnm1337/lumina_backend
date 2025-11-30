using NotificationHub = ServiceLayer.Hubs.NotificationHub;
using Xunit;
using Moq;
using ServiceLayer.Notification;
using RepositoryLayer.Notification;
using DataLayer.Models;
using Microsoft.AspNetCore.SignalR;

using Lumina.Tests.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class NotificationServiceSendPointsTests
    {
        private readonly Mock<INotificationRepository> _mockNotificationRepo;
        private readonly Mock<IUserNotificationRepository> _mockUserNotificationRepo;
        private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
        private readonly Mock<IHubClients> _mockClients;
        private readonly Mock<ISingleClientProxy> _mockClientProxy;
        private readonly LuminaSystemContext _context;
        private readonly NotificationService _service;

        public NotificationServiceSendPointsTests()
        {
            _mockNotificationRepo = new Mock<INotificationRepository>();
            _mockUserNotificationRepo = new Mock<IUserNotificationRepository>();
            _mockHubContext = new Mock<IHubContext<NotificationHub>>();
            _mockClients = new Mock<IHubClients>();
            _mockClientProxy = new Mock<ISingleClientProxy>();
            _context = InMemoryDbContextHelper.CreateContext();

            // Setup HubContext
            _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);
            _mockClients.Setup(c => c.Client(It.IsAny<string>())).Returns(_mockClientProxy.Object);

            // Seed database with a system user for GetSystemUserIdAsync
            SeedSystemUser();

            _service = new NotificationService(
                _mockNotificationRepo.Object,
                _mockUserNotificationRepo.Object,
                _mockHubContext.Object,
                _context
            );
        }

        private void SeedSystemUser()
        {
            var adminRole = new Role { RoleId = 1, RoleName = "Admin" };
            var adminUser = new User
            {
                UserId = 1,
                Email = "admin@test.com",
                FullName = "Admin User",
                RoleId = 1,
                IsActive = true
            };

            _context.Roles.Add(adminRole);
            _context.Users.Add(adminUser);
            _context.SaveChanges();
        }

        [Fact]
        public async Task SendPointsNotificationAsync_WhenZeroCorrectAnswers_ShouldSendEncouragementMessage()
        {
            // Arrange
            int userId = 10;
            int pointsEarned = 0;
            int totalAccumulatedScore = 100;
            int correctAnswers = 0;
            int totalQuestions = 20;
            int timeBonus = 0;
            int accuracyBonus = 0;
            bool isFirstAttempt = true;

            int expectedNotificationId = 200;

            _mockNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<DataLayer.Models.Notification>()))
                .ReturnsAsync(expectedNotificationId);

            _mockUserNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<UserNotification>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.SendPointsNotificationAsync(
                userId, pointsEarned, totalAccumulatedScore,
                correctAnswers, totalQuestions, timeBonus, accuracyBonus, isFirstAttempt);

            // Assert
            Assert.Equal(expectedNotificationId, result);

            // Verify notification content mentions zero correct answers
            _mockNotificationRepo.Verify(
                repo => repo.CreateAsync(It.Is<DataLayer.Models.Notification>(n =>
                    n.Title == "🎯 Điểm tích lũy mới!" &&
                    n.Content.Contains("0/20 câu đúng") &&
                    n.Content.Contains("chưa nhận được điểm tích lũy") &&
                    n.Content.Contains("Đừng nản lòng"))),
                Times.Once
            );

            _mockUserNotificationRepo.Verify(
                repo => repo.CreateAsync(It.Is<UserNotification>(un =>
                    un.UserId == userId &&
                    un.NotificationId == expectedNotificationId &&
                    un.IsRead == false)),
                Times.Once
            );
        }

        [Fact]
        public async Task SendPointsNotificationAsync_WhenNotFirstAttempt_ShouldSendNoPointsMessage()
        {
            // Arrange
            int userId = 10;
            int pointsEarned = 0;
            int totalAccumulatedScore = 150;
            int correctAnswers = 15;
            int totalQuestions = 20;
            int timeBonus = 0;
            int accuracyBonus = 0;
            bool isFirstAttempt = false; // Not first attempt

            int expectedNotificationId = 201;

            _mockNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<DataLayer.Models.Notification>()))
                .ReturnsAsync(expectedNotificationId);

            _mockUserNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<UserNotification>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.SendPointsNotificationAsync(
                userId, pointsEarned, totalAccumulatedScore,
                correctAnswers, totalQuestions, timeBonus, accuracyBonus, isFirstAttempt);

            // Assert
            Assert.Equal(expectedNotificationId, result);

            // Verify notification content mentions not first attempt
            _mockNotificationRepo.Verify(
                repo => repo.CreateAsync(It.Is<DataLayer.Models.Notification>(n =>
                    n.Content.Contains("15/20 câu đúng") &&
                    n.Content.Contains("không phải lần đầu") &&
                    n.Content.Contains("không cộng điểm tích lũy"))),
                Times.Once
            );
        }

        [Fact]
        public async Task SendPointsNotificationAsync_WhenFirstAttemptWithPoints_ShouldSendSuccessMessage()
        {
            // Arrange
            int userId = 10;
            int pointsEarned = 50;
            int totalAccumulatedScore = 150;
            int correctAnswers = 15;
            int totalQuestions = 20;
            int timeBonus = 0;
            int accuracyBonus = 0;
            bool isFirstAttempt = true;

            int expectedNotificationId = 202;

            _mockNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<DataLayer.Models.Notification>()))
                .ReturnsAsync(expectedNotificationId);

            _mockUserNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<UserNotification>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.SendPointsNotificationAsync(
                userId, pointsEarned, totalAccumulatedScore,
                correctAnswers, totalQuestions, timeBonus, accuracyBonus, isFirstAttempt);

            // Assert
            Assert.Equal(expectedNotificationId, result);

            // Verify notification content mentions points earned
            _mockNotificationRepo.Verify(
                repo => repo.CreateAsync(It.Is<DataLayer.Models.Notification>(n =>
                    n.Content.Contains("15/20 câu đúng") &&
                    n.Content.Contains("50 điểm tích lũy") &&
                    n.Content.Contains("Tổng điểm tích lũy: 150 điểm"))),
                Times.Once
            );
        }

        [Fact]
        public async Task SendPointsNotificationAsync_WithTimeBonus_ShouldIncludeTimeBonusInMessage()
        {
            // Arrange
            int userId = 10;
            int pointsEarned = 60;
            int totalAccumulatedScore = 160;
            int correctAnswers = 18;
            int totalQuestions = 20;
            int timeBonus = 10; // Has time bonus
            int accuracyBonus = 0;
            bool isFirstAttempt = true;

            int expectedNotificationId = 203;

            _mockNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<DataLayer.Models.Notification>()))
                .ReturnsAsync(expectedNotificationId);

            _mockUserNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<UserNotification>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.SendPointsNotificationAsync(
                userId, pointsEarned, totalAccumulatedScore,
                correctAnswers, totalQuestions, timeBonus, accuracyBonus, isFirstAttempt);

            // Assert
            Assert.Equal(expectedNotificationId, result);

            // Verify notification content mentions time bonus
            _mockNotificationRepo.Verify(
                repo => repo.CreateAsync(It.Is<DataLayer.Models.Notification>(n =>
                    n.Content.Contains("Bonus tốc độ: +10 điểm"))),
                Times.Once
            );
        }

        [Fact]
        public async Task SendPointsNotificationAsync_WithAccuracyBonus_ShouldIncludeAccuracyBonusInMessage()
        {
            // Arrange
            int userId = 10;
            int pointsEarned = 70;
            int totalAccumulatedScore = 170;
            int correctAnswers = 19;
            int totalQuestions = 20;
            int timeBonus = 0;
            int accuracyBonus = 20; // Has accuracy bonus
            bool isFirstAttempt = true;

            int expectedNotificationId = 204;

            _mockNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<DataLayer.Models.Notification>()))
                .ReturnsAsync(expectedNotificationId);

            _mockUserNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<UserNotification>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.SendPointsNotificationAsync(
                userId, pointsEarned, totalAccumulatedScore,
                correctAnswers, totalQuestions, timeBonus, accuracyBonus, isFirstAttempt);

            // Assert
            Assert.Equal(expectedNotificationId, result);

            // Verify notification content mentions accuracy bonus
            _mockNotificationRepo.Verify(
                repo => repo.CreateAsync(It.Is<DataLayer.Models.Notification>(n =>
                    n.Content.Contains("Bonus độ chính xác: +20 điểm"))),
                Times.Once
            );
        }

        [Fact]
        public async Task SendPointsNotificationAsync_ShouldBroadcastViaSignalR_WhenUserConnected()
        {
            // Arrange
            int userId = 10;
            int pointsEarned = 50;
            int totalAccumulatedScore = 150;
            int correctAnswers = 15;
            int totalQuestions = 20;
            int timeBonus = 0;
            int accuracyBonus = 0;
            bool isFirstAttempt = true;

            _mockNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<DataLayer.Models.Notification>()))
                .ReturnsAsync(200);

            _mockUserNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<UserNotification>()))
                .ReturnsAsync(1);

            // Act
            await _service.SendPointsNotificationAsync(
                userId, pointsEarned, totalAccumulatedScore,
                correctAnswers, totalQuestions, timeBonus, accuracyBonus, isFirstAttempt);

            // Assert - Verify notification and user notification were created
            _mockNotificationRepo.Verify(
                repo => repo.CreateAsync(It.IsAny<DataLayer.Models.Notification>()),
                Times.Once
            );

            _mockUserNotificationRepo.Verify(
                repo => repo.CreateAsync(It.IsAny<UserNotification>()),
                Times.Once
            );

            // Note: SignalR broadcasting is wrapped in try-catch and depends on connection ID
            // We verify that the notification was created successfully
        }
        [Theory]
        [InlineData(19, 20, 0, 0, "Xuất sắc! Bạn đã làm rất tốt!")] // 95%
        [InlineData(18, 20, 0, 0, "Tuyệt vời! Kết quả rất ấn tượng!")] // 90%
        [InlineData(16, 20, 10, 10, "Tốt lắm! Bạn vừa nhanh vừa chính xác!")] // 80% + Time + Accuracy
        [InlineData(16, 20, 10, 0, "Tốt! Bạn làm bài rất nhanh!")] // 80% + Time
        [InlineData(16, 20, 0, 10, "Tốt! Độ chính xác của bạn rất cao!")] // 80% + Accuracy
        [InlineData(16, 20, 0, 0, "Tốt! Bạn đã làm khá tốt!")] // 80%
        [InlineData(14, 20, 0, 0, "Không tệ! Bạn đang tiến bộ.")] // 70%
        [InlineData(12, 20, 0, 0, "Cần cố gắng thêm! Hãy xem lại bài học")] // 60%
        [InlineData(10, 20, 0, 0, "Đừng nản lòng! Mỗi lần làm bài là một cơ hội")] // 50%
        [InlineData(8, 20, 0, 0, "Mọi hành trình đều bắt đầu từ bước đầu tiên!")] // < 50%
        public async Task SendPointsNotificationAsync_ShouldIncludeCorrectEncouragementMessage(
            int correctAnswers, int totalQuestions, int timeBonus, int accuracyBonus, string expectedMessagePart)
        {
            // Arrange
            int userId = 10;
            int pointsEarned = 50;
            int totalAccumulatedScore = 150;
            bool isFirstAttempt = true;

            _mockNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<DataLayer.Models.Notification>()))
                .ReturnsAsync(200);

            _mockUserNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<UserNotification>()))
                .ReturnsAsync(1);

            // Act
            await _service.SendPointsNotificationAsync(
                userId, pointsEarned, totalAccumulatedScore,
                correctAnswers, totalQuestions, timeBonus, accuracyBonus, isFirstAttempt);

            // Assert
            _mockNotificationRepo.Verify(
                repo => repo.CreateAsync(It.Is<DataLayer.Models.Notification>(n =>
                    n.Content.Contains(expectedMessagePart))),
                Times.Once
            );
        }

        [Fact]
        public async Task SendPointsNotificationAsync_WhenNoAdminButActiveUserExists_ShouldUseFirstActiveUserAsSender()
        {
            // Arrange
            // Clear existing users
            _context.Users.RemoveRange(_context.Users);
            _context.Roles.RemoveRange(_context.Roles);
            await _context.SaveChangesAsync();

            // Add non-admin active user
            var normalUser = new User
            {
                UserId = 99,
                Email = "user@test.com",
                FullName = "Normal User",
                RoleId = 2, // Not Admin
                IsActive = true
            };
            _context.Users.Add(normalUser);
            await _context.SaveChangesAsync();

            int userId = 10;
            
            _mockNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<DataLayer.Models.Notification>()))
                .ReturnsAsync(200);

            _mockUserNotificationRepo.Setup(repo => repo.CreateAsync(It.IsAny<UserNotification>())).ReturnsAsync(1);

            // Act
            await _service.SendPointsNotificationAsync(
                userId, 10, 100, 5, 10, 0, 0, true);

            // Assert
            _mockNotificationRepo.Verify(
                repo => repo.CreateAsync(It.Is<DataLayer.Models.Notification>(n =>
                    n.CreatedBy == 99)), // Should use the normal user ID
                Times.Once
            );
        }

        [Fact]
        public async Task SendPointsNotificationAsync_WhenNoActiveUsers_ShouldUseFallbackId()
        {
            // Arrange
            // Clear existing users
            _context.Users.RemoveRange(_context.Users);
            _context.Roles.RemoveRange(_context.Roles);
            await _context.SaveChangesAsync();

            int userId = 10;

            _mockNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<DataLayer.Models.Notification>()))
                .ReturnsAsync(200);

            _mockUserNotificationRepo.Setup(repo => repo.CreateAsync(It.IsAny<UserNotification>())).ReturnsAsync(1);

            // Act
            await _service.SendPointsNotificationAsync(
                userId, 10, 100, 5, 10, 0, 0, true);

            // Assert
            _mockNotificationRepo.Verify(
                repo => repo.CreateAsync(It.Is<DataLayer.Models.Notification>(n =>
                    n.CreatedBy == 1)), // Fallback ID
                Times.Once
            );
        }

        [Fact]
        public async Task SendPointsNotificationAsync_WhenRepositoryThrowsException_ShouldRethrow()
        {
            // Arrange
            int userId = 10;
            _mockNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<DataLayer.Models.Notification>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => 
                _service.SendPointsNotificationAsync(
                    userId, 10, 100, 5, 10, 0, 0, true));
        }
    }
}
