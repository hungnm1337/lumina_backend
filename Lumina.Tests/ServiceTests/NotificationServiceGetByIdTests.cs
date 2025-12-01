using NotificationHub = ServiceLayer.Hubs.NotificationHub;
using Xunit;
using Moq;
using ServiceLayer.Notification;
using RepositoryLayer.Notification;
using DataLayer.DTOs.Notification;
using DataLayer.Models;
using Microsoft.AspNetCore.SignalR;

using Lumina.Tests.Helpers;
using System;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class NotificationServiceGetByIdTests
    {
        private readonly Mock<INotificationRepository> _mockNotificationRepo;
        private readonly Mock<IUserNotificationRepository> _mockUserNotificationRepo;
        private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
        private readonly LuminaSystemContext _context;
        private readonly NotificationService _service;

        public NotificationServiceGetByIdTests()
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
        public async Task GetByIdAsync_WhenNotificationExists_ShouldReturnNotification()
        {
            // Arrange
            int notificationId = 1;
            var expectedNotification = new NotificationDTO
            {
                NotificationId = notificationId,
                Title = "Test Notification",
                Content = "Test Content",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockNotificationRepo
                .Setup(repo => repo.GetByIdAsync(notificationId))
                .ReturnsAsync(expectedNotification);

            // Act
            var result = await _service.GetByIdAsync(notificationId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(notificationId, result!.NotificationId);
            Assert.Equal(expectedNotification.Title, result.Title);
            Assert.Equal(expectedNotification.Content, result.Content);

            _mockNotificationRepo.Verify(
                repo => repo.GetByIdAsync(notificationId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetByIdAsync_WhenNotificationNotFound_ShouldReturnNull()
        {
            // Arrange
            int notificationId = 999;

            _mockNotificationRepo
                .Setup(repo => repo.GetByIdAsync(notificationId))
                .ReturnsAsync((NotificationDTO?)null);

            // Act
            var result = await _service.GetByIdAsync(notificationId);

            // Assert
            Assert.Null(result);

            _mockNotificationRepo.Verify(
                repo => repo.GetByIdAsync(notificationId),
                Times.Once
            );
        }
    }
}
