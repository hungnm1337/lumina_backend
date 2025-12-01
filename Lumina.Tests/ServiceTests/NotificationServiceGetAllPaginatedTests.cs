using NotificationHub = ServiceLayer.Hubs.NotificationHub;
using Xunit;
using Moq;
using ServiceLayer.Notification;
using RepositoryLayer.Notification;
using DataLayer.DTOs;
using DataLayer.DTOs.Notification;
using DataLayer.Models;
using Microsoft.AspNetCore.SignalR;

using Lumina.Tests.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class NotificationServiceGetAllPaginatedTests
    {
        private readonly Mock<INotificationRepository> _mockNotificationRepo;
        private readonly Mock<IUserNotificationRepository> _mockUserNotificationRepo;
        private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
        private readonly LuminaSystemContext _context;
        private readonly NotificationService _service;

        public NotificationServiceGetAllPaginatedTests()
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
        public async Task GetAllPaginatedAsync_WithDefaultParameters_ShouldReturnPaginatedResult()
        {
            // Arrange
            var expectedResult = new PaginatedResultDTO<NotificationDTO>
            {
                Items = new List<NotificationDTO>
                {
                    new NotificationDTO { NotificationId = 1, Title = "Test 1", Content = "Content 1", IsActive = true }
                },
                Total = 1,
                Page = 1,
                PageSize = 10,
                TotalPages = 1,
                HasNext = false,
                HasPrevious = false
            };

            _mockNotificationRepo
                .Setup(repo => repo.GetAllPaginatedAsync(1, 10))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllPaginatedAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Total);
            Assert.Equal(1, result.Page);
            Assert.Equal(10, result.PageSize);

            _mockNotificationRepo.Verify(
                repo => repo.GetAllPaginatedAsync(1, 10),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllPaginatedAsync_WithCustomParameters_ShouldReturnPaginatedResult()
        {
            // Arrange
            int page = 2;
            int pageSize = 5;

            var expectedResult = new PaginatedResultDTO<NotificationDTO>
            {
                Items = new List<NotificationDTO>(),
                Total = 10,
                Page = 2,
                PageSize = 5,
                TotalPages = 2,
                HasNext = false,
                HasPrevious = true
            };

            _mockNotificationRepo
                .Setup(repo => repo.GetAllPaginatedAsync(page, pageSize))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllPaginatedAsync(page, pageSize);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(page, result.Page);
            Assert.Equal(pageSize, result.PageSize);
            Assert.True(result.HasPrevious);
            Assert.False(result.HasNext);

            _mockNotificationRepo.Verify(
                repo => repo.GetAllPaginatedAsync(page, pageSize),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllPaginatedAsync_ShouldPassParametersToRepository()
        {
            // Arrange
            int page = 3;
            int pageSize = 20;

            _mockNotificationRepo
                .Setup(repo => repo.GetAllPaginatedAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new PaginatedResultDTO<NotificationDTO>
                {
                    Items = new List<NotificationDTO>(),
                    Total = 0,
                    Page = page,
                    PageSize = pageSize
                });

            // Act
            await _service.GetAllPaginatedAsync(page, pageSize);

            // Assert
            _mockNotificationRepo.Verify(
                repo => repo.GetAllPaginatedAsync(page, pageSize),
                Times.Once
            );
        }
    }
}
