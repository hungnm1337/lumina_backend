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
    public class EventServiceDeleteTests
    {
        private readonly Mock<IEventRepository> _mockEventRepository;
        private readonly EventService _service;

        public EventServiceDeleteTests()
        {
            _mockEventRepository = new Mock<IEventRepository>();
            _service = new EventService(_mockEventRepository.Object);
        }

        [Fact]
        public async Task DeleteAsync_WhenEventIdIsNegative_ShouldCallRepository()
        {
            // Arrange
            int eventId = -1;

            _mockEventRepository
                .Setup(repo => repo.DeleteAsync(eventId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.DeleteAsync(eventId);

            // Assert
            Assert.False(result);

            // Verify repository is called exactly once
            _mockEventRepository.Verify(
                repo => repo.DeleteAsync(eventId),
                Times.Once
            );
        }

        [Fact]
        public async Task DeleteAsync_WhenEventIdIsZero_ShouldCallRepository()
        {
            // Arrange
            int eventId = 0;

            _mockEventRepository
                .Setup(repo => repo.DeleteAsync(eventId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.DeleteAsync(eventId);

            // Assert
            Assert.False(result);

            // Verify repository is called exactly once
            _mockEventRepository.Verify(
                repo => repo.DeleteAsync(eventId),
                Times.Once
            );
        }

        [Fact]
        public async Task DeleteAsync_WhenEventIdIsValid_ShouldReturnTrue()
        {
            // Arrange
            int eventId = 1;

            _mockEventRepository
                .Setup(repo => repo.DeleteAsync(eventId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(eventId);

            // Assert
            Assert.True(result);

            // Verify repository is called exactly once
            _mockEventRepository.Verify(
                repo => repo.DeleteAsync(eventId),
                Times.Once
            );
        }
    }
}
