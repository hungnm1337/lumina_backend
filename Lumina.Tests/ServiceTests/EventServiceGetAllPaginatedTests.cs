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
    public class EventServiceGetAllPaginatedTests
    {
        private readonly Mock<IEventRepository> _mockEventRepository;
        private readonly EventService _service;

        public EventServiceGetAllPaginatedTests()
        {
            _mockEventRepository = new Mock<IEventRepository>();
            _service = new EventService(_mockEventRepository.Object);
        }

        [Fact]
        public async Task GetAllPaginatedAsync_WhenAllParametersAreDefault_ShouldReturnPaginatedResult()
        {
            // Arrange
            DateTime? from = null;
            DateTime? to = null;
            string? keyword = null;
            int page = 1;
            int pageSize = 10;

            var expectedResult = new PaginatedResultDTO<EventDTO>
            {
                Items = new List<EventDTO>
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
                },
                Total = 1,
                Page = 1,
                PageSize = 10,
                TotalPages = 1,
                HasNext = false,
                HasPrevious = false
            };

            _mockEventRepository
                .Setup(repo => repo.GetAllPaginatedAsync(from, to, keyword, page, pageSize))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllPaginatedAsync(from, to, keyword, page, pageSize);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(expectedResult.Total, result.Total);

            // Verify repository is called exactly once
            _mockEventRepository.Verify(
                repo => repo.GetAllPaginatedAsync(null, null, null, 1, 10),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllPaginatedAsync_WhenAllParametersAreProvided_ShouldReturnFilteredPaginatedResult()
        {
            // Arrange
            DateTime from = DateTime.UtcNow.AddDays(-10);
            DateTime to = DateTime.UtcNow.AddDays(10);
            string keyword = "test";
            int page = 2;
            int pageSize = 5;

            var expectedResult = new PaginatedResultDTO<EventDTO>
            {
                Items = new List<EventDTO>(),
                Total = 0,
                Page = 2,
                PageSize = 5,
                TotalPages = 0,
                HasNext = false,
                HasPrevious = true
            };

            _mockEventRepository
                .Setup(repo => repo.GetAllPaginatedAsync(from, to, keyword, page, pageSize))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllPaginatedAsync(from, to, keyword, page, pageSize);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult.Page, result.Page);
            Assert.Equal(expectedResult.PageSize, result.PageSize);

            // Verify repository is called exactly once
            _mockEventRepository.Verify(
                repo => repo.GetAllPaginatedAsync(from, to, keyword, page, pageSize),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllPaginatedAsync_WhenCalledWithNoArguments_ShouldUseDefaultValues()
        {
            // Arrange
            var expectedResult = new PaginatedResultDTO<EventDTO>
            {
                Items = new List<EventDTO>(),
                Total = 0,
                Page = 1,
                PageSize = 10,
                TotalPages = 0,
                HasNext = false,
                HasPrevious = false
            };

            _mockEventRepository
                .Setup(repo => repo.GetAllPaginatedAsync(null, null, null, 1, 10))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetAllPaginatedAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Page);
            Assert.Equal(10, result.PageSize);

            // Verify repository is called exactly once with default values
            _mockEventRepository.Verify(
                repo => repo.GetAllPaginatedAsync(null, null, null, 1, 10),
                Times.Once
            );
        }
    }
}
