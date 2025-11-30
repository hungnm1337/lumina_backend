using NotificationHub = ServiceLayer.Hubs.NotificationHub;
using Xunit;
using Moq;
using ServiceLayer.Notification;
using RepositoryLayer.Notification;
using DataLayer.DTOs.Notification;
using DataLayer.Models;
using Microsoft.AspNetCore.SignalR;

using Lumina.Tests.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class NotificationServiceGetAllTests
    {
        private readonly Mock<INotificationRepository> _mockNotificationRepo;
        private readonly Mock<IUserNotificationRepository> _mockUserNotificationRepo;
        private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
        private readonly LuminaSystemContext _context;
        private readonly NotificationService _service;

        public NotificationServiceGetAllTests()
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
        public async Task GetAllAsync_ShouldReturnListFromRepository()
        {
            // Arrange
            var expectedNotifications = new List<NotificationDTO>
            {
                new NotificationDTO
                {
                    NotificationId = 1,
                    Title = "Test Notification 1",
                    Content = "Test Content 1",
                    IsActive = true
                },
                new NotificationDTO
                {
                    NotificationId = 2,
                    Title = "Test Notification 2",
                    Content = "Test Content 2",
                    IsActive = false
                }
            };

            _mockNotificationRepo
                .Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(expectedNotifications);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(expectedNotifications[0].NotificationId, result[0].NotificationId);
            Assert.Equal(expectedNotifications[1].Title, result[1].Title);

            // Verify repository was called exactly once
            _mockNotificationRepo.Verify(
                repo => repo.GetAllAsync(),
                Times.Once
            );
        }
    }
}
