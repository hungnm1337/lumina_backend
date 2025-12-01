using Xunit;
using Moq;
using ServiceLayer.Event;
using RepositoryLayer.Event;
using DataLayer.DTOs;
using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class EventServiceGetAllTests
    {
        private readonly Mock<IEventRepository> _mockEventRepository;
        private readonly EventService _service;

        public EventServiceGetAllTests()
        {
            _mockEventRepository = new Mock<IEventRepository>();
            _service = new EventService(_mockEventRepository.Object);
        }

        [Fact]
        public async Task GetAllAsync_WhenAllParametersAreNull_ShouldReturnListFromRepository()
        {
            // Arrange
            DateTime? from = null;
            DateTime? to = null;
            string? keyword = null;

            var expectedResult = new List<EventDTO>
            {
                new EventDTO
                {
                    EventId = 1,
                    EventName = "Event 1",
                    Content = "Content 1",
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(1),
                    CreateAt = DateTime.UtcNow,
                    CreateBy = 1
                }
            };

            _mockEventRepository
                .Setup(repo => repo.GetAllAsync(from, to, keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllAsync(from, to, keyword);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(expectedResult[0].EventId, result[0].EventId);

            // Verify repository is called exactly once
            _mockEventRepository.Verify(
                repo => repo.GetAllAsync(null, null, null),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_WhenAllParametersAreProvided_ShouldReturnFilteredList()
        {
            // Arrange
            DateTime from = DateTime.UtcNow.AddDays(-10);
            DateTime to = DateTime.UtcNow.AddDays(10);
            string keyword = "test";

            var expectedResult = new List<EventDTO>
            {
                new EventDTO
                {
                    EventId = 1,
                    EventName = "Test Event",
                    Content = "Test Content",
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(1),
                    CreateAt = DateTime.UtcNow,
                    CreateBy = 1
                }
            };

            _mockEventRepository
                .Setup(repo => repo.GetAllAsync(from, to, keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllAsync(from, to, keyword);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);

            // Verify repository is called exactly once
            _mockEventRepository.Verify(
                repo => repo.GetAllAsync(from, to, keyword),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllAsync_WhenRepositoryReturnsEmpty_ShouldReturnEmptyList()
        {
            // Arrange
            DateTime? from = null;
            DateTime? to = null;
            string? keyword = null;

            var expectedResult = new List<EventDTO>();

            _mockEventRepository
                .Setup(repo => repo.GetAllAsync(from, to, keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllAsync(from, to, keyword);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            // Verify repository is called exactly once
            _mockEventRepository.Verify(
                repo => repo.GetAllAsync(null, null, null),
                Times.Once
            );
        }
    }
}
