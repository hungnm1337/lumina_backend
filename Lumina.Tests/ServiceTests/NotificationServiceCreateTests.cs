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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class NotificationServiceCreateTests
    {
        private readonly Mock<INotificationRepository> _mockNotificationRepo;
        private readonly Mock<IUserNotificationRepository> _mockUserNotificationRepo;
        private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
        private readonly Mock<IHubClients> _mockClients;
        private readonly Mock<ISingleClientProxy> _mockClientProxy;
        private readonly LuminaSystemContext _context;
        private readonly NotificationService _service;

        public NotificationServiceCreateTests()
        {
            _mockNotificationRepo = new Mock<INotificationRepository>();
            _mockUserNotificationRepo = new Mock<IUserNotificationRepository>();
            _mockHubContext = new Mock<IHubContext<NotificationHub>>();
            _mockClients = new Mock<IHubClients>();
            _mockClientProxy = new Mock<ISingleClientProxy>();
            _context = InMemoryDbContextHelper.CreateContext();

            // Setup HubContext to return mock clients
            _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);
            _mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);
            _mockClients.Setup(c => c.Client(It.IsAny<string>())).Returns(_mockClientProxy.Object);

            _service = new NotificationService(
                _mockNotificationRepo.Object,
                _mockUserNotificationRepo.Object,
                _mockHubContext.Object,
                _context
            );
        }

        [Fact]
        public async Task CreateAsync_WithUserIds_ShouldCreateNotificationForSpecificUsers()
        {
            // Arrange
            var dto = new CreateNotificationDTO
            {
                Title = "Test Notification",
                Content = "Test Content",
                IsActive = true,
                UserIds = new List<int> { 1, 2, 3 }
            };
            int createdBy = 10;
            int expectedNotificationId = 100;

            var userIds = new List<int> { 1, 2, 3 };

            _mockNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<DataLayer.Models.Notification>()))
                .ReturnsAsync(expectedNotificationId);

            _mockNotificationRepo
                .Setup(repo => repo.GetUserIdsByUserIdsAsync(dto.UserIds))
                .ReturnsAsync(userIds);

            _mockUserNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<UserNotification>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateAsync(dto, createdBy);

            // Assert
            Assert.Equal(expectedNotificationId, result);

            // Verify notification was created
            _mockNotificationRepo.Verify(
                repo => repo.CreateAsync(It.Is<DataLayer.Models.Notification>(n =>
                    n.Title == dto.Title &&
                    n.Content == dto.Content &&
                    n.IsActive == dto.IsActive &&
                    n.CreatedBy == createdBy)),
                Times.Once
            );

            // Verify GetUserIdsByUserIdsAsync was called (not GetUserIdsByRoleIdsAsync or GetAllUserIdsAsync)
            _mockNotificationRepo.Verify(
                repo => repo.GetUserIdsByUserIdsAsync(dto.UserIds),
                Times.Once
            );

            _mockNotificationRepo.Verify(
                repo => repo.GetUserIdsByRoleIdsAsync(It.IsAny<List<int>>()),
                Times.Never
            );

            _mockNotificationRepo.Verify(
                repo => repo.GetAllUserIdsAsync(),
                Times.Never
            );

            // Verify UserNotification was created for each user
            _mockUserNotificationRepo.Verify(
                repo => repo.CreateAsync(It.IsAny<UserNotification>()),
                Times.Exactly(userIds.Count)
            );
        }

        [Fact]
        public async Task CreateAsync_WithRoleIds_ShouldCreateNotificationForUsersInRoles()
        {
            // Arrange
            var dto = new CreateNotificationDTO
            {
                Title = "Role Notification",
                Content = "Content for roles",
                IsActive = true,
                RoleIds = new List<int> { 1, 2 }
            };
            int createdBy = 10;
            int expectedNotificationId = 101;

            var userIds = new List<int> { 5, 6, 7, 8 };

            _mockNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<DataLayer.Models.Notification>()))
                .ReturnsAsync(expectedNotificationId);

            _mockNotificationRepo
                .Setup(repo => repo.GetUserIdsByRoleIdsAsync(dto.RoleIds))
                .ReturnsAsync(userIds);

            _mockUserNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<UserNotification>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateAsync(dto, createdBy);

            // Assert
            Assert.Equal(expectedNotificationId, result);

            // Verify GetUserIdsByRoleIdsAsync was called (not GetUserIdsByUserIdsAsync or GetAllUserIdsAsync)
            _mockNotificationRepo.Verify(
                repo => repo.GetUserIdsByRoleIdsAsync(dto.RoleIds),
                Times.Once
            );

            _mockNotificationRepo.Verify(
                repo => repo.GetUserIdsByUserIdsAsync(It.IsAny<List<int>>()),
                Times.Never
            );

            _mockNotificationRepo.Verify(
                repo => repo.GetAllUserIdsAsync(),
                Times.Never
            );

            // Verify UserNotification was created for each user
            _mockUserNotificationRepo.Verify(
                repo => repo.CreateAsync(It.IsAny<UserNotification>()),
                Times.Exactly(userIds.Count)
            );
        }

        [Fact]
        public async Task CreateAsync_WithoutUserIdsOrRoleIds_ShouldCreateNotificationForAllUsers()
        {
            // Arrange
            var dto = new CreateNotificationDTO
            {
                Title = "Broadcast Notification",
                Content = "Content for all",
                IsActive = true
                // No UserIds or RoleIds
            };
            int createdBy = 10;
            int expectedNotificationId = 102;

            var allUserIds = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            _mockNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<DataLayer.Models.Notification>()))
                .ReturnsAsync(expectedNotificationId);

            _mockNotificationRepo
                .Setup(repo => repo.GetAllUserIdsAsync())
                .ReturnsAsync(allUserIds);

            _mockUserNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<UserNotification>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateAsync(dto, createdBy);

            // Assert
            Assert.Equal(expectedNotificationId, result);

            // Verify GetAllUserIdsAsync was called (not GetUserIdsByUserIdsAsync or GetUserIdsByRoleIdsAsync)
            _mockNotificationRepo.Verify(
                repo => repo.GetAllUserIdsAsync(),
                Times.Once
            );

            _mockNotificationRepo.Verify(
                repo => repo.GetUserIdsByUserIdsAsync(It.IsAny<List<int>>()),
                Times.Never
            );

            _mockNotificationRepo.Verify(
                repo => repo.GetUserIdsByRoleIdsAsync(It.IsAny<List<int>>()),
                Times.Never
            );

            // Verify UserNotification was created for all users
            _mockUserNotificationRepo.Verify(
                repo => repo.CreateAsync(It.IsAny<UserNotification>()),
                Times.Exactly(allUserIds.Count)
            );
        }

        [Fact]
        public async Task CreateAsync_ShouldBroadcastToAllUsersGroup_WhenNoUserIdsOrRoleIds()
        {
            // Arrange
            var dto = new CreateNotificationDTO
            {
                Title = "Broadcast",
                Content = "To all users",
                IsActive = true
            };
            int createdBy = 10;

            _mockNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<DataLayer.Models.Notification>()))
                .ReturnsAsync(100);

            _mockNotificationRepo
                .Setup(repo => repo.GetAllUserIdsAsync())
                .ReturnsAsync(new List<int> { 1, 2 });

            _mockUserNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<UserNotification>()))
                .ReturnsAsync(1);

            // Act
            await _service.CreateAsync(dto, createdBy);

            // Assert - Verify broadcast to "AllUsers" group
            _mockClients.Verify(
                c => c.Group("AllUsers"),
                Times.Once
            );

            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "ReceiveNotification",
                    It.IsAny<object[]>(),
                    It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task CreateAsync_ShouldBroadcastToSpecificUsers_WhenUserIdsProvided()
        {
            // Arrange
            var dto = new CreateNotificationDTO
            {
                Title = "Specific Users",
                Content = "Content",
                IsActive = true,
                UserIds = new List<int> { 1, 2 }
            };
            int createdBy = 10;

            _mockNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<DataLayer.Models.Notification>()))
                .ReturnsAsync(100);

            _mockNotificationRepo
                .Setup(repo => repo.GetUserIdsByUserIdsAsync(dto.UserIds))
                .ReturnsAsync(new List<int> { 1, 2 });

            _mockUserNotificationRepo
                .Setup(repo => repo.CreateAsync(It.IsAny<UserNotification>()))
                .ReturnsAsync(1);

            // Act
            await _service.CreateAsync(dto, createdBy);

            // Assert - Verify NOT broadcasting to "AllUsers" group
            _mockClients.Verify(
                c => c.Group("AllUsers"),
                Times.Never
            );

            // Note: We can't easily verify individual client calls without knowing connection IDs
            // The important part is that it doesn't broadcast to AllUsers group
        }
    }
}
