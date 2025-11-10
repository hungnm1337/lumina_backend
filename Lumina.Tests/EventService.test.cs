using Xunit;
using Moq;
using ServiceLayer.Event;
using RepositoryLayer.Event;
using DataLayer.DTOs;
using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumina.Tests
{
    public class EventServiceTests
    {
        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_ShouldCallRepository_WithNoFilters()
        {
            // Arrange
            var mockRepo = new Mock<IEventRepository>();
            mockRepo.Setup(r => r.GetAllAsync(null, null, null))
                .ReturnsAsync(new List<EventDTO>());
            var service = new EventService(mockRepo.Object);

            // Act
            await service.GetAllAsync();

            // Assert
            mockRepo.Verify(r => r.GetAllAsync(null, null, null), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ShouldCallRepository_WithFilters()
        {
            // Arrange
            var mockRepo = new Mock<IEventRepository>();
            var from = new DateTime(2025, 1, 1);
            var to = new DateTime(2025, 12, 31);
            var keyword = "test";
            
            mockRepo.Setup(r => r.GetAllAsync(from, to, keyword))
                .ReturnsAsync(new List<EventDTO>());
            var service = new EventService(mockRepo.Object);

            // Act
            await service.GetAllAsync(from, to, keyword);

            // Assert
            mockRepo.Verify(r => r.GetAllAsync(from, to, keyword), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnEvents_FromRepository()
        {
            // Arrange
            var mockRepo = new Mock<IEventRepository>();
            var expectedEvents = new List<EventDTO>
            {
                new EventDTO { EventId = 1, EventName = "Event 1" },
                new EventDTO { EventId = 2, EventName = "Event 2" }
            };
            mockRepo.Setup(r => r.GetAllAsync(null, null, null))
                .ReturnsAsync(expectedEvents);
            var service = new EventService(mockRepo.Object);

            // Act
            var result = await service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Event 1", result[0].EventName);
        }

        #endregion

        #region GetAllPaginatedAsync Tests

        [Fact]
        public async Task GetAllPaginatedAsync_ShouldCallRepository_WithPagination()
        {
            // Arrange
            var mockRepo = new Mock<IEventRepository>();
            var paginatedResult = new PaginatedResultDTO<EventDTO>
            {
                Items = new List<EventDTO>(),
                Total = 0,
                Page = 1,
                PageSize = 10,
                TotalPages = 0
            };
            mockRepo.Setup(r => r.GetAllPaginatedAsync(null, null, null, 1, 10))
                .ReturnsAsync(paginatedResult);
            var service = new EventService(mockRepo.Object);

            // Act
            await service.GetAllPaginatedAsync(page: 1, pageSize: 10);

            // Assert
            mockRepo.Verify(r => r.GetAllPaginatedAsync(null, null, null, 1, 10), Times.Once);
        }

        [Fact]
        public async Task GetAllPaginatedAsync_ShouldReturnPaginatedResult()
        {
            // Arrange
            var mockRepo = new Mock<IEventRepository>();
            var paginatedResult = new PaginatedResultDTO<EventDTO>
            {
                Items = new List<EventDTO>
                {
                    new EventDTO { EventId = 1, EventName = "Event 1" }
                },
                Total = 15,
                Page = 2,
                PageSize = 10,
                TotalPages = 2,
                HasNext = false,
                HasPrevious = true
            };
            mockRepo.Setup(r => r.GetAllPaginatedAsync(null, null, null, 2, 10))
                .ReturnsAsync(paginatedResult);
            var service = new EventService(mockRepo.Object);

            // Act
            var result = await service.GetAllPaginatedAsync(page: 2, pageSize: 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(15, result.Total);
            Assert.Equal(2, result.Page);
            Assert.False(result.HasNext);
            Assert.True(result.HasPrevious);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ShouldCallRepository_WithCorrectId()
        {
            // Arrange
            var mockRepo = new Mock<IEventRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new EventDTO { EventId = 1, EventName = "Test Event" });
            var service = new EventService(mockRepo.Object);

            // Act
            await service.GetByIdAsync(1);

            // Assert
            mockRepo.Verify(r => r.GetByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnEvent_WhenExists()
        {
            // Arrange
            var mockRepo = new Mock<IEventRepository>();
            var expectedEvent = new EventDTO 
            { 
                EventId = 5, 
                EventName = "Test Event",
                Content = "Test Content"
            };
            mockRepo.Setup(r => r.GetByIdAsync(5))
                .ReturnsAsync(expectedEvent);
            var service = new EventService(mockRepo.Object);

            // Act
            var result = await service.GetByIdAsync(5);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.EventId);
            Assert.Equal("Test Event", result.EventName);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            // Arrange
            var mockRepo = new Mock<IEventRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((EventDTO?)null);
            var service = new EventService(mockRepo.Object);

            // Act
            var result = await service.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_ShouldCallRepository_WithCorrectData()
        {
            // Arrange
            var mockRepo = new Mock<IEventRepository>();
            mockRepo.Setup(r => r.CreateAsync(It.IsAny<DataLayer.Models.Event>()))
                .ReturnsAsync(1);
            var service = new EventService(mockRepo.Object);

            var dto = new EventDTO
            {
                EventName = "New Event",
                Content = "Event Content",
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2025, 6, 5)
            };

            // Act
            await service.CreateAsync(dto, userId: 10);

            // Assert
            mockRepo.Verify(r => r.CreateAsync(It.Is<DataLayer.Models.Event>(e =>
                e.EventName == "New Event" &&
                e.Content == "Event Content" &&
                e.StartDate == new DateTime(2025, 6, 1) &&
                e.EndDate == new DateTime(2025, 6, 5) &&
                e.CreateBy == 10 &&
                e.UpdateBy == null
            )), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldSetCreateAt_ToCurrentTime()
        {
            // Arrange
            var mockRepo = new Mock<IEventRepository>();
            var capturedEntity = (DataLayer.Models.Event?)null;
            mockRepo.Setup(r => r.CreateAsync(It.IsAny<DataLayer.Models.Event>()))
                .Callback<DataLayer.Models.Event>(e => capturedEntity = e)
                .ReturnsAsync(1);
            var service = new EventService(mockRepo.Object);

            var before = DateTime.UtcNow;
            var dto = new EventDTO
            {
                EventName = "Test Event",
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2025, 6, 5)
            };

            // Act
            await service.CreateAsync(dto, userId: 1);
            var after = DateTime.UtcNow;

            // Assert
            Assert.NotNull(capturedEntity);
            Assert.True(capturedEntity.CreateAt >= before && capturedEntity.CreateAt <= after);
        }

        [Fact]
        public async Task CreateAsync_ShouldSetUpdateAt_ToNull()
        {
            // Arrange
            var mockRepo = new Mock<IEventRepository>();
            var capturedEntity = (DataLayer.Models.Event?)null;
            mockRepo.Setup(r => r.CreateAsync(It.IsAny<DataLayer.Models.Event>()))
                .Callback<DataLayer.Models.Event>(e => capturedEntity = e)
                .ReturnsAsync(1);
            var service = new EventService(mockRepo.Object);

            var dto = new EventDTO
            {
                EventName = "Test Event",
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2025, 6, 5)
            };

            // Act
            await service.CreateAsync(dto, userId: 1);

            // Assert
            Assert.NotNull(capturedEntity);
            Assert.Null(capturedEntity.UpdateAt);
            Assert.Null(capturedEntity.UpdateBy);
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnGeneratedId()
        {
            // Arrange
            var mockRepo = new Mock<IEventRepository>();
            mockRepo.Setup(r => r.CreateAsync(It.IsAny<DataLayer.Models.Event>()))
                .ReturnsAsync(42);
            var service = new EventService(mockRepo.Object);

            var dto = new EventDTO
            {
                EventName = "Test Event",
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2025, 6, 5)
            };

            // Act
            var result = await service.CreateAsync(dto, userId: 1);

            // Assert
            Assert.Equal(42, result);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_ShouldReturnFalse_WhenEventNotExists()
        {
            // Arrange
            var mockRepo = new Mock<IEventRepository>();
            mockRepo.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((EventDTO?)null);
            var service = new EventService(mockRepo.Object);

            var dto = new EventDTO
            {
                EventName = "Updated Event",
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2025, 6, 5)
            };

            // Act
            var result = await service.UpdateAsync(999, dto, userId: 1);

            // Assert
            Assert.False(result);
            mockRepo.Verify(r => r.UpdateAsync(It.IsAny<DataLayer.Models.Event>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldCallRepository_WhenEventExists()
        {
            // Arrange
            var mockRepo = new Mock<IEventRepository>();
            var existingEvent = new EventDTO
            {
                EventId = 1,
                EventName = "Old Name",
                CreateAt = new DateTime(2025, 1, 1),
                CreateBy = 5
            };
            mockRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingEvent);
            mockRepo.Setup(r => r.UpdateAsync(It.IsAny<DataLayer.Models.Event>()))
                .ReturnsAsync(true);
            var service = new EventService(mockRepo.Object);

            var dto = new EventDTO
            {
                EventName = "Updated Name",
                Content = "Updated Content",
                StartDate = new DateTime(2025, 7, 1),
                EndDate = new DateTime(2025, 7, 5)
            };

            // Act
            var result = await service.UpdateAsync(1, dto, userId: 10);

            // Assert
            Assert.True(result);
            mockRepo.Verify(r => r.UpdateAsync(It.Is<DataLayer.Models.Event>(e =>
                e.EventId == 1 &&
                e.EventName == "Updated Name" &&
                e.Content == "Updated Content" &&
                e.StartDate == new DateTime(2025, 7, 1) &&
                e.EndDate == new DateTime(2025, 7, 5) &&
                e.UpdateBy == 10
            )), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldPreserveCreateAtAndCreateBy()
        {
            // Arrange
            var mockRepo = new Mock<IEventRepository>();
            var originalCreateAt = new DateTime(2025, 1, 1, 10, 0, 0);
            var existingEvent = new EventDTO
            {
                EventId = 1,
                EventName = "Old Name",
                CreateAt = originalCreateAt,
                CreateBy = 5
            };
            mockRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingEvent);

            var capturedEntity = (DataLayer.Models.Event?)null;
            mockRepo.Setup(r => r.UpdateAsync(It.IsAny<DataLayer.Models.Event>()))
                .Callback<DataLayer.Models.Event>(e => capturedEntity = e)
                .ReturnsAsync(true);
            var service = new EventService(mockRepo.Object);

            var dto = new EventDTO
            {
                EventName = "Updated Name",
                StartDate = new DateTime(2025, 7, 1),
                EndDate = new DateTime(2025, 7, 5)
            };

            // Act
            await service.UpdateAsync(1, dto, userId: 10);

            // Assert
            Assert.NotNull(capturedEntity);
            Assert.Equal(originalCreateAt, capturedEntity.CreateAt);
            Assert.Equal(5, capturedEntity.CreateBy);
        }

        [Fact]
        public async Task UpdateAsync_ShouldSetUpdateAt_ToCurrentTime()
        {
            // Arrange
            var mockRepo = new Mock<IEventRepository>();
            var existingEvent = new EventDTO
            {
                EventId = 1,
                EventName = "Old Name",
                CreateAt = new DateTime(2025, 1, 1),
                CreateBy = 5
            };
            mockRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingEvent);

            var capturedEntity = (DataLayer.Models.Event?)null;
            mockRepo.Setup(r => r.UpdateAsync(It.IsAny<DataLayer.Models.Event>()))
                .Callback<DataLayer.Models.Event>(e => capturedEntity = e)
                .ReturnsAsync(true);
            var service = new EventService(mockRepo.Object);

            var before = DateTime.UtcNow;
            var dto = new EventDTO
            {
                EventName = "Updated Name",
                StartDate = new DateTime(2025, 7, 1),
                EndDate = new DateTime(2025, 7, 5)
            };

            // Act
            await service.UpdateAsync(1, dto, userId: 10);
            var after = DateTime.UtcNow;

            // Assert
            Assert.NotNull(capturedEntity);
            Assert.NotNull(capturedEntity.UpdateAt);
            Assert.True(capturedEntity.UpdateAt >= before && capturedEntity.UpdateAt <= after);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_ShouldCallRepository_WithCorrectId()
        {
            // Arrange
            var mockRepo = new Mock<IEventRepository>();
            mockRepo.Setup(r => r.DeleteAsync(1))
                .ReturnsAsync(true);
            var service = new EventService(mockRepo.Object);

            // Act
            await service.DeleteAsync(1);

            // Assert
            mockRepo.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnTrue_WhenSuccessful()
        {
            // Arrange
            var mockRepo = new Mock<IEventRepository>();
            mockRepo.Setup(r => r.DeleteAsync(1))
                .ReturnsAsync(true);
            var service = new EventService(mockRepo.Object);

            // Act
            var result = await service.DeleteAsync(1);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenFailed()
        {
            // Arrange
            var mockRepo = new Mock<IEventRepository>();
            mockRepo.Setup(r => r.DeleteAsync(999))
                .ReturnsAsync(false);
            var service = new EventService(mockRepo.Object);

            // Act
            var result = await service.DeleteAsync(999);

            // Assert
            Assert.False(result);
        }

        #endregion
    }
}
