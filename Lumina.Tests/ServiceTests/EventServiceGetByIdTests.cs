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
    public class EventServiceGetByIdTests
    {
        private readonly Mock<IEventRepository> _mockEventRepository;
        private readonly EventService _service;

        public EventServiceGetByIdTests()
        {
            _mockEventRepository = new Mock<IEventRepository>();
            _service = new EventService(_mockEventRepository.Object);
        }

        [Fact]
        public async Task GetByIdAsync_WhenEventIdIsNegative_ShouldReturnNull()
        {
            // Arrange
            int eventId = -1;

            _mockEventRepository
                .Setup(repo => repo.GetByIdAsync(eventId))
                .ReturnsAsync((EventDTO?)null);

            // Act
            var result = await _service.GetByIdAsync(eventId);

            // Assert
            Assert.Null(result);

            // Verify repository is called exactly once
            _mockEventRepository.Verify(
                repo => repo.GetByIdAsync(eventId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetByIdAsync_WhenEventIdIsZero_ShouldReturnNull()
        {
            // Arrange
            int eventId = 0;

            _mockEventRepository
                .Setup(repo => repo.GetByIdAsync(eventId))
                .ReturnsAsync((EventDTO?)null);

            // Act
            var result = await _service.GetByIdAsync(eventId);

            // Assert
            Assert.Null(result);

            // Verify repository is called exactly once
            _mockEventRepository.Verify(
                repo => repo.GetByIdAsync(eventId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetByIdAsync_WhenEventIdIsValid_ShouldReturnEventDto()
        {
            // Arrange
            int eventId = 1;
            var expectedDto = new EventDTO
            {
                EventId = eventId,
                EventName = "Test Event",
                Content = "Test Content",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1),
                CreateAt = DateTime.UtcNow,
                UpdateAt = null,
                CreateBy = 1,
                UpdateBy = null
            };

            _mockEventRepository
                .Setup(repo => repo.GetByIdAsync(eventId))
                .ReturnsAsync(expectedDto);

            // Act
            var result = await _service.GetByIdAsync(eventId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedDto.EventId, result!.EventId);
            Assert.Equal(expectedDto.EventName, result.EventName);
            Assert.Equal(expectedDto.Content, result.Content);

            // Verify repository is called exactly once
            _mockEventRepository.Verify(
                repo => repo.GetByIdAsync(eventId),
                Times.Once
            );
        }
    }
}
