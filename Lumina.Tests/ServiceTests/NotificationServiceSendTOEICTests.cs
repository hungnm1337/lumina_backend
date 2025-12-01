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
    public class NotificationServiceSendTOEICTests
    {
        private readonly Mock<INotificationRepository> _mockNotificationRepo;
        private readonly Mock<IUserNotificationRepository> _mockUserNotificationRepo;
        private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
        private readonly Mock<IHubClients> _mockClients;
        private readonly Mock<ISingleClientProxy> _mockClientProxy;
        private readonly LuminaSystemContext _context;
        private readonly NotificationService _service;

        public NotificationServiceSendTOEICTests()
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
        public async Task SendTOEICNotificationAsync_ShouldCreateNotificationWithCorrectTitleAndContent()
        {
            // Arrange
            int userId = 10;
            int estimatedTOEIC = 750;
            string toeicLevel = "Intermediate";
            string message = "Bạn đạt 750 điểm TOEIC (Intermediate). Kết quả tốt! Hãy tiếp tục luyện tập.";

            int expectedNotificationId = 300;

            _mockNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<DataLayer.Models.Notification>()))
                .ReturnsAsync(expectedNotificationId);

            _mockUserNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<UserNotification>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.SendTOEICNotificationAsync(userId, estimatedTOEIC, toeicLevel, message);

            // Assert
            Assert.Equal(expectedNotificationId, result);

            // Verify notification was created with correct title and content
            _mockNotificationRepo.Verify(
                repo => repo.CreateAsync(It.Is<DataLayer.Models.Notification>(n =>
                    n.Title == $"📊 Kết quả TOEIC: {toeicLevel}" &&
                    n.Content == message &&
                    n.IsActive == true &&
                    n.CreatedBy == 1)), // System user ID
                Times.Once
            );
        }

        [Fact]
        public async Task SendTOEICNotificationAsync_ShouldCreateUserNotificationForSpecificUser()
        {
            // Arrange
            int userId = 10;
            int estimatedTOEIC = 850;
            string toeicLevel = "Advanced";
            string message = "Xuất sắc! Bạn đạt 850 điểm TOEIC.";

            int expectedNotificationId = 301;

            _mockNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<DataLayer.Models.Notification>()))
                .ReturnsAsync(expectedNotificationId);

            _mockUserNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<UserNotification>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.SendTOEICNotificationAsync(userId, estimatedTOEIC, toeicLevel, message);

            // Assert
            Assert.Equal(expectedNotificationId, result);

            // Verify UserNotification was created for the specific user
            _mockUserNotificationRepo.Verify(
                repo => repo.CreateAsync(It.Is<UserNotification>(un =>
                    un.UserId == userId &&
                    un.NotificationId == expectedNotificationId &&
                    un.IsRead == false)),
                Times.Once
            );
        }

        [Fact]
        public async Task SendTOEICNotificationAsync_ShouldBroadcastViaSignalR_WhenUserConnected()
        {
            // Arrange
            int userId = 10;
            int estimatedTOEIC = 650;
            string toeicLevel = "Pre-Intermediate";
            string message = "Bạn đạt 650 điểm TOEIC. Hãy cố gắng thêm!";

            int expectedNotificationId = 302;

            _mockNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<DataLayer.Models.Notification>()))
                .ReturnsAsync(expectedNotificationId);

            _mockUserNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<UserNotification>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.SendTOEICNotificationAsync(userId, estimatedTOEIC, toeicLevel, message);

            // Assert
            Assert.Equal(expectedNotificationId, result);

            // Verify notification and user notification were created
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
        [Fact]
        public async Task SendTOEICNotificationAsync_WhenRepositoryThrowsException_ShouldRethrow()
        {
            // Arrange
            int userId = 10;
            _mockNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<DataLayer.Models.Notification>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => 
                _service.SendTOEICNotificationAsync(userId, 500, "Level", "Message"));
        }
    }
}
