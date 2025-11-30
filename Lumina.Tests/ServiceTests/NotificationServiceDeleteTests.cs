using NotificationHub = ServiceLayer.Hubs.NotificationHub;
using Xunit;
using Moq;
using ServiceLayer.Notification;
using RepositoryLayer.Notification;
using DataLayer.Models;
using Microsoft.AspNetCore.SignalR;

using Lumina.Tests.Helpers;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class NotificationServiceDeleteTests
    {
        private readonly Mock<INotificationRepository> _mockNotificationRepo;
        private readonly Mock<IUserNotificationRepository> _mockUserNotificationRepo;
        private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
        private readonly LuminaSystemContext _context;
        private readonly NotificationService _service;

        public NotificationServiceDeleteTests()
        {
            _mockNotificationRepo = new Mock<INotificationRepository>();
            _mockUserNotificationRepo = new Mock<IUserNotificationRepository>();
            _mockHubContext = new Mock<IHubContext<NotificationHub>>();
            _context = InMemoryDbContextHelper.CreateContext();
            _service = new NotificationService(
                _mockNotificationRepo.Object,
                _mockUserNotificationRepo.Object,
                _mockHubContext.Object,
                _context
            );
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteUserNotificationsFirst_ThenNotification()
        {
            // Arrange
            int notificationId = 1;

            _mockUserNotificationRepo
                .Setup(repo => repo.DeleteByNotificationIdAsync(notificationId))
                .ReturnsAsync(true);

            _mockNotificationRepo
                .Setup(repo => repo.DeleteAsync(notificationId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(notificationId);

            // Assert
            Assert.True(result);

            // Verify UserNotifications are deleted first
            _mockUserNotificationRepo.Verify(
                repo => repo.DeleteByNotificationIdAsync(notificationId),
                Times.Once
            );

            // Then Notification is deleted
            _mockNotificationRepo.Verify(
                repo => repo.DeleteAsync(notificationId),
                Times.Once
            );
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnResultFromRepository()
        {
            // Arrange
            int notificationId = 999;

            _mockUserNotificationRepo
                .Setup(repo => repo.DeleteByNotificationIdAsync(notificationId))
                .ReturnsAsync(true);

            _mockNotificationRepo
                .Setup(repo => repo.DeleteAsync(notificationId))
                .ReturnsAsync(false); // Notification not found

            // Act
            var result = await _service.DeleteAsync(notificationId);

            // Assert
            Assert.False(result);

            _mockUserNotificationRepo.Verify(
                repo => repo.DeleteByNotificationIdAsync(notificationId),
                Times.Once
            );

            _mockNotificationRepo.Verify(
                repo => repo.DeleteAsync(notificationId),
                Times.Once
            );
        }
    }
}
