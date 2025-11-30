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
    public class NotificationServiceUpdateTests
    {
        private readonly Mock<INotificationRepository> _mockNotificationRepo;
        private readonly Mock<IUserNotificationRepository> _mockUserNotificationRepo;
        private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
        private readonly LuminaSystemContext _context;
        private readonly NotificationService _service;

        public NotificationServiceUpdateTests()
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
        public async Task UpdateAsync_WhenNotificationNotFound_ShouldReturnFalse()
        {
            // Arrange
            int notificationId = 999;
            var dto = new UpdateNotificationDTO
            {
                Title = "Updated Title",
                Content = "Updated Content",
                IsActive = false
            };

            _mockNotificationRepo
                .Setup(repo => repo.GetByIdAsync(notificationId))
                .ReturnsAsync((NotificationDTO?)null);

            // Act
            var result = await _service.UpdateAsync(notificationId, dto);

            // Assert
            Assert.False(result);

            _mockNotificationRepo.Verify(
                repo => repo.GetByIdAsync(notificationId),
                Times.Once
            );

            _mockNotificationRepo.Verify(
                repo => repo.UpdateAsync(It.IsAny<DataLayer.Models.Notification>()),
                Times.Never
            );
        }

        [Fact]
        public async Task UpdateAsync_WhenNotificationExists_ShouldUpdateAndReturnTrue()
        {
            // Arrange
            int notificationId = 1;
            var existingNotification = new NotificationDTO
            {
                NotificationId = notificationId,
                Title = "Original Title",
                Content = "Original Content",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var updateDto = new UpdateNotificationDTO
            {
                Title = "Updated Title",
                Content = "Updated Content",
                IsActive = false
            };

            _mockNotificationRepo
                .Setup(repo => repo.GetByIdAsync(notificationId))
                .ReturnsAsync(existingNotification);

            _mockNotificationRepo
                .Setup(repo => repo.UpdateAsync(It.IsAny<DataLayer.Models.Notification>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateAsync(notificationId, updateDto);

            // Assert
            Assert.True(result);

            _mockNotificationRepo.Verify(
                repo => repo.GetByIdAsync(notificationId),
                Times.Once
            );

            _mockNotificationRepo.Verify(
                repo => repo.UpdateAsync(It.Is<DataLayer.Models.Notification>(n =>
                    n.NotificationId == notificationId &&
                    n.Title == updateDto.Title &&
                    n.Content == updateDto.Content &&
                    n.IsActive == updateDto.IsActive &&
                    n.CreatedAt == existingNotification.CreatedAt)),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateAsync_WithNullTitle_ShouldKeepExistingTitle()
        {
            // Arrange
            int notificationId = 1;
            var existingNotification = new NotificationDTO
            {
                NotificationId = notificationId,
                Title = "Original Title",
                Content = "Original Content",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var updateDto = new UpdateNotificationDTO
            {
                Title = null, // Null title should keep existing
                Content = "Updated Content",
                IsActive = false
            };

            _mockNotificationRepo
                .Setup(repo => repo.GetByIdAsync(notificationId))
                .ReturnsAsync(existingNotification);

            _mockNotificationRepo
                .Setup(repo => repo.UpdateAsync(It.IsAny<DataLayer.Models.Notification>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateAsync(notificationId, updateDto);

            // Assert
            Assert.True(result);

            _mockNotificationRepo.Verify(
                repo => repo.UpdateAsync(It.Is<DataLayer.Models.Notification>(n =>
                    n.Title == existingNotification.Title)), // Should keep original title
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateAsync_WithNullContent_ShouldKeepExistingContent()
        {
            // Arrange
            int notificationId = 1;
            var existingNotification = new NotificationDTO
            {
                NotificationId = notificationId,
                Title = "Original Title",
                Content = "Original Content",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var updateDto = new UpdateNotificationDTO
            {
                Title = "Updated Title",
                Content = null, // Null content should keep existing
                IsActive = false
            };

            _mockNotificationRepo
                .Setup(repo => repo.GetByIdAsync(notificationId))
                .ReturnsAsync(existingNotification);

            _mockNotificationRepo
                .Setup(repo => repo.UpdateAsync(It.IsAny<DataLayer.Models.Notification>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateAsync(notificationId, updateDto);

            // Assert
            Assert.True(result);

            _mockNotificationRepo.Verify(
                repo => repo.UpdateAsync(It.Is<DataLayer.Models.Notification>(n =>
                    n.Content == existingNotification.Content)), // Should keep original content
                Times.Once
            );
        }
    }
}
