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
    public class EventServiceCreateTests
    {
        private readonly Mock<IEventRepository> _mockEventRepository;
        private readonly EventService _service;

        public EventServiceCreateTests()
        {
            _mockEventRepository = new Mock<IEventRepository>();
            _service = new EventService(_mockEventRepository.Object);
        }

        [Fact]
        public async Task CreateAsync_WhenDtoIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            EventDTO? dto = null;
            int userId = 1;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _service.CreateAsync(dto!, userId)
            );

            // Verify repository is never called
            _mockEventRepository.Verify(
                repo => repo.CreateAsync(It.IsAny<Event>()),
                Times.Never
            );
        }

        [Fact]
        public async Task CreateAsync_WhenEventNameIsEmpty_ShouldThrowArgumentException()
        {
            // Arrange
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
                async () => await _service.CreateAsync(dto, userId)
            );

            // Verify repository is never called
            _mockEventRepository.Verify(
                repo => repo.CreateAsync(It.IsAny<Event>()),
                Times.Never
            );
        }

        [Fact]
        public async Task CreateAsync_WhenEventNameIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
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
                async () => await _service.CreateAsync(dto, userId)
            );

            // Verify repository is never called
            _mockEventRepository.Verify(
                repo => repo.CreateAsync(It.IsAny<Event>()),
                Times.Never
            );
        }

        [Fact]
        public async Task CreateAsync_WhenDtoIsValid_ShouldReturnCreatedId()
        {
            // Arrange
            var dto = new EventDTO
            {
                EventName = "Test Event",
                Content = "Test Content",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1)
            };
            int userId = 1;
            int expectedId = 100;

            _mockEventRepository
                .Setup(repo => repo.CreateAsync(It.Is<Event>(e => 
                    e.EventName == dto.EventName &&
                    e.Content == dto.Content &&
                    e.StartDate == dto.StartDate &&
                    e.EndDate == dto.EndDate &&
                    e.CreateBy == userId &&
                    e.UpdateBy == null &&
                    e.UpdateAt == null)))
                .ReturnsAsync(expectedId);

            // Act
            var result = await _service.CreateAsync(dto, userId);

            // Assert
            Assert.Equal(expectedId, result);

            // Verify repository is called exactly once
            _mockEventRepository.Verify(
                repo => repo.CreateAsync(It.Is<Event>(e => 
                    e.EventName == dto.EventName &&
                    e.Content == dto.Content &&
                    e.StartDate == dto.StartDate &&
                    e.EndDate == dto.EndDate &&
                    e.CreateBy == userId &&
                    e.UpdateBy == null &&
                    e.UpdateAt == null)),
                Times.Once
            );
        }

        [Fact]
        public async Task CreateAsync_WhenContentIsNull_ShouldCreateSuccessfully()
        {
            // Arrange
            var dto = new EventDTO
            {
                EventName = "Test Event",
                Content = null,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1)
            };
            int userId = 1;
            int expectedId = 100;

            _mockEventRepository
                .Setup(repo => repo.CreateAsync(It.Is<Event>(e => 
                    e.EventName == dto.EventName &&
                    e.Content == null &&
                    e.StartDate == dto.StartDate &&
                    e.EndDate == dto.EndDate)))
                .ReturnsAsync(expectedId);

            // Act
            var result = await _service.CreateAsync(dto, userId);

            // Assert
            Assert.Equal(expectedId, result);

            // Verify repository is called exactly once
            _mockEventRepository.Verify(
                repo => repo.CreateAsync(It.Is<Event>(e => 
                    e.EventName == dto.EventName &&
                    e.Content == null)),
                Times.Once
            );
        }
    }
}
