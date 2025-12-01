using Xunit;
using Moq;
using ServiceLayer.Event;
using RepositoryLayer.Event;
using DataLayer.DTOs;
using DataLayer.Models;
using System;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class EventServiceUpdateTests
    {
        private readonly Mock<IEventRepository> _mockEventRepository;
        private readonly EventService _service;

        public EventServiceUpdateTests()
        {
            _mockEventRepository = new Mock<IEventRepository>();
            _service = new EventService(_mockEventRepository.Object);
        }

        [Fact]
        public async Task UpdateAsync_WhenEventIdIsNegative_ShouldReturnFalse()
        {
            // Arrange
            int eventId = -1;
            var dto = new EventDTO
            {
                EventName = "Test Event",
                Content = "Test Content",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1)
            };
            int userId = 1;

            // Act
            var result = await _service.UpdateAsync(eventId, dto, userId);

            // Assert
            Assert.False(result);

            // Verify GetByIdAsync is called with negative ID
            _mockEventRepository.Verify(
                repo => repo.GetByIdAsync(eventId),
                Times.Once
            );

            // Verify UpdateAsync is never called
            _mockEventRepository.Verify(
                repo => repo.UpdateAsync(It.IsAny<Event>()),
                Times.Never
            );
        }

        [Fact]
        public async Task UpdateAsync_WhenEventIdIsZero_ShouldReturnFalse()
        {
            // Arrange
            int eventId = 0;
            var dto = new EventDTO
            {
                EventName = "Test Event",
                Content = "Test Content",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1)
            };
            int userId = 1;

            _mockEventRepository
                .Setup(repo => repo.GetByIdAsync(eventId))
                .ReturnsAsync((EventDTO?)null);

            // Act
            var result = await _service.UpdateAsync(eventId, dto, userId);

            // Assert
            Assert.False(result);

            // Verify GetByIdAsync is called exactly once
            _mockEventRepository.Verify(
                repo => repo.GetByIdAsync(eventId),
                Times.Once
            );

            // Verify UpdateAsync is never called
            _mockEventRepository.Verify(
                repo => repo.UpdateAsync(It.IsAny<Event>()),
                Times.Never
            );
        }

        [Fact]
        public async Task UpdateAsync_WhenEventNotFound_ShouldReturnFalse()
        {
            // Arrange
            int eventId = 999;
            var dto = new EventDTO
            {
                EventName = "Test Event",
                Content = "Test Content",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1)
            };
            int userId = 1;

            _mockEventRepository
                .Setup(repo => repo.GetByIdAsync(eventId))
                .ReturnsAsync((EventDTO?)null);

            // Act
            var result = await _service.UpdateAsync(eventId, dto, userId);

            // Assert
            Assert.False(result);

            // Verify GetByIdAsync is called exactly once
            _mockEventRepository.Verify(
                repo => repo.GetByIdAsync(eventId),
                Times.Once
            );

            // Verify UpdateAsync is never called
            _mockEventRepository.Verify(
                repo => repo.UpdateAsync(It.IsAny<Event>()),
                Times.Never
            );
        }

        [Fact]
        public async Task UpdateAsync_WhenDtoIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            int eventId = 1;
            EventDTO? dto = null;
            int userId = 1;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _service.UpdateAsync(eventId, dto!, userId)
            );

            // Verify GetByIdAsync is never called
            _mockEventRepository.Verify(
                repo => repo.GetByIdAsync(It.IsAny<int>()),
                Times.Never
            );

            // Verify UpdateAsync is never called
            _mockEventRepository.Verify(
                repo => repo.UpdateAsync(It.IsAny<Event>()),
                Times.Never
            );
        }

        [Fact]
        public async Task UpdateAsync_WhenEventNameIsEmpty_ShouldThrowArgumentException()
        {
            // Arrange
            int eventId = 1;
            var dto = new EventDTO
            {
                EventName = string.Empty,
                Content = "Test Content",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1)
            };
            int userId = 1;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.UpdateAsync(eventId, dto, userId)
            );

            // Verify GetByIdAsync is never called
            _mockEventRepository.Verify(
                repo => repo.GetByIdAsync(It.IsAny<int>()),
                Times.Never
            );

            // Verify UpdateAsync is never called
            _mockEventRepository.Verify(
                repo => repo.UpdateAsync(It.IsAny<Event>()),
                Times.Never
            );
        }

        [Fact]
        public async Task UpdateAsync_WhenEventNameIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            int eventId = 1;
            var dto = new EventDTO
            {
                EventName = null!,
                Content = "Test Content",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1)
            };
            int userId = 1;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _service.UpdateAsync(eventId, dto, userId)
            );

            // Verify GetByIdAsync is never called
            _mockEventRepository.Verify(
                repo => repo.GetByIdAsync(It.IsAny<int>()),
                Times.Never
            );

            // Verify UpdateAsync is never called
            _mockEventRepository.Verify(
                repo => repo.UpdateAsync(It.IsAny<Event>()),
                Times.Never
            );
        }

        [Fact]
        public async Task UpdateAsync_WhenEventExistsAndDtoIsValid_ShouldReturnTrue()
        {
            // Arrange
            int eventId = 1;
            var existingDto = new EventDTO
            {
                EventId = eventId,
                EventName = "Existing Event",
                Content = "Existing Content",
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(-5),
                CreateAt = DateTime.UtcNow.AddDays(-10),
                UpdateAt = null,
                CreateBy = 2,
                UpdateBy = null
            };

            var updateDto = new EventDTO
            {
                EventName = "Updated Event",
                Content = "Updated Content",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1)
            };
            int userId = 1;

            _mockEventRepository
                .Setup(repo => repo.GetByIdAsync(eventId))
                .ReturnsAsync(existingDto);

            _mockEventRepository
                .Setup(repo => repo.UpdateAsync(It.Is<Event>(e => 
                    e.EventId == eventId &&
                    e.EventName == updateDto.EventName &&
                    e.Content == updateDto.Content &&
                    e.StartDate == updateDto.StartDate &&
                    e.EndDate == updateDto.EndDate &&
                    e.CreateAt == existingDto.CreateAt &&
                    e.CreateBy == existingDto.CreateBy &&
                    e.UpdateBy == userId &&
                    e.UpdateAt.HasValue)))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateAsync(eventId, updateDto, userId);

            // Assert
            Assert.True(result);

            // Verify GetByIdAsync is called exactly once
            _mockEventRepository.Verify(
                repo => repo.GetByIdAsync(eventId),
                Times.Once
            );

            // Verify UpdateAsync is called exactly once with correct entity
            _mockEventRepository.Verify(
                repo => repo.UpdateAsync(It.Is<Event>(e => 
                    e.EventId == eventId &&
                    e.EventName == updateDto.EventName &&
                    e.Content == updateDto.Content &&
                    e.StartDate == updateDto.StartDate &&
                    e.EndDate == updateDto.EndDate &&
                    e.CreateAt == existingDto.CreateAt &&
                    e.CreateBy == existingDto.CreateBy &&
                    e.UpdateBy == userId &&
                    e.UpdateAt.HasValue)),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateAsync_WhenContentIsNull_ShouldUpdateSuccessfully()
        {
            // Arrange
            int eventId = 1;
            var existingDto = new EventDTO
            {
                EventId = eventId,
                EventName = "Existing Event",
                Content = "Existing Content",
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(-5),
                CreateAt = DateTime.UtcNow.AddDays(-10),
                UpdateAt = null,
                CreateBy = 2,
                UpdateBy = null
            };

            var updateDto = new EventDTO
            {
                EventName = "Updated Event",
                Content = null,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1)
            };
            int userId = 1;

            _mockEventRepository
                .Setup(repo => repo.GetByIdAsync(eventId))
                .ReturnsAsync(existingDto);

            _mockEventRepository
                .Setup(repo => repo.UpdateAsync(It.Is<Event>(e => 
                    e.EventId == eventId &&
                    e.EventName == updateDto.EventName &&
                    e.Content == null)))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateAsync(eventId, updateDto, userId);

            // Assert
            Assert.True(result);

            // Verify UpdateAsync is called with null Content
            _mockEventRepository.Verify(
                repo => repo.UpdateAsync(It.Is<Event>(e => 
                    e.EventId == eventId &&
                    e.EventName == updateDto.EventName &&
                    e.Content == null)),
                Times.Once
            );
        }
    }
}
