using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using lumina.Controllers;
using ServiceLayer.Event;
using DataLayer.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumina.Tests
{
    public class GetAllEventTests
    {
        private readonly Mock<IEventService> _mockService;
        private readonly EventController _controller;

        public GetAllEventTests()
        {
            _mockService = new Mock<IEventService>();
            _controller = new EventController(_mockService.Object);
        }

        [Fact]
        public async Task GetAll_AllParametersDefault_ReturnsOkWithPaginatedList()
        {
            // Arrange
            var expectedResult = new PaginatedResultDTO<EventDTO>
            {
                Items = new List<EventDTO>
                {
                    new EventDTO { EventId = 1, EventName = "Event 1" },
                    new EventDTO { EventId = 2, EventName = "Event 2" }
                },
                Total = 2,
                Page = 1,
                PageSize = 10,
                TotalPages = 1
            };

            _mockService.Setup(s => s.GetAllPaginatedAsync(null, null, null, 1, 10))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var paginatedResult = Assert.IsType<PaginatedResultDTO<EventDTO>>(okResult.Value);
            Assert.Equal(2, paginatedResult.Total);
            Assert.Equal(2, paginatedResult.Items.Count);
            _mockService.Verify(s => s.GetAllPaginatedAsync(null, null, null, 1, 10), Times.Once);
        }

        [Fact]
        public async Task GetAll_WithFromDate_ReturnsFilteredEvents()
        {
            // Arrange
            DateTime fromDate = new DateTime(2024, 1, 1);
            var expectedResult = new PaginatedResultDTO<EventDTO>
            {
                Items = new List<EventDTO>
                {
                    new EventDTO { EventId = 1, EventName = "Event 1", StartDate = new DateTime(2024, 2, 1) }
                },
                Total = 1,
                Page = 1,
                PageSize = 10,
                TotalPages = 1
            };

            _mockService.Setup(s => s.GetAllPaginatedAsync(fromDate, null, null, 1, 10))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetAll(fromDate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var paginatedResult = Assert.IsType<PaginatedResultDTO<EventDTO>>(okResult.Value);
            Assert.Equal(1, paginatedResult.Total);
            _mockService.Verify(s => s.GetAllPaginatedAsync(fromDate, null, null, 1, 10), Times.Once);
        }

        [Fact]
        public async Task GetAll_WithToDate_ReturnsFilteredEvents()
        {
            // Arrange
            DateTime toDate = new DateTime(2024, 12, 31);
            var expectedResult = new PaginatedResultDTO<EventDTO>
            {
                Items = new List<EventDTO>
                {
                    new EventDTO { EventId = 1, EventName = "Event 1", EndDate = new DateTime(2024, 6, 1) }
                },
                Total = 1,
                Page = 1,
                PageSize = 10,
                TotalPages = 1
            };

            _mockService.Setup(s => s.GetAllPaginatedAsync(null, toDate, null, 1, 10))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetAll(null, toDate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var paginatedResult = Assert.IsType<PaginatedResultDTO<EventDTO>>(okResult.Value);
            Assert.Equal(1, paginatedResult.Total);
            _mockService.Verify(s => s.GetAllPaginatedAsync(null, toDate, null, 1, 10), Times.Once);
        }

        [Fact]
        public async Task GetAll_WithDateRange_ReturnsFilteredEvents()
        {
            // Arrange
            DateTime fromDate = new DateTime(2024, 1, 1);
            DateTime toDate = new DateTime(2024, 12, 31);
            var expectedResult = new PaginatedResultDTO<EventDTO>
            {
                Items = new List<EventDTO>
                {
                    new EventDTO { EventId = 1, EventName = "Event 1", StartDate = new DateTime(2024, 6, 15) },
                    new EventDTO { EventId = 2, EventName = "Event 2", StartDate = new DateTime(2024, 8, 20) }
                },
                Total = 2,
                Page = 1,
                PageSize = 10,
                TotalPages = 1
            };

            _mockService.Setup(s => s.GetAllPaginatedAsync(fromDate, toDate, null, 1, 10))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetAll(fromDate, toDate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var paginatedResult = Assert.IsType<PaginatedResultDTO<EventDTO>>(okResult.Value);
            Assert.Equal(2, paginatedResult.Total);
            _mockService.Verify(s => s.GetAllPaginatedAsync(fromDate, toDate, null, 1, 10), Times.Once);
        }

        [Fact]
        public async Task GetAll_WithKeyword_ReturnsFilteredEvents()
        {
            // Arrange
            string keyword = "conference";
            var expectedResult = new PaginatedResultDTO<EventDTO>
            {
                Items = new List<EventDTO>
                {
                    new EventDTO { EventId = 1, EventName = "Tech Conference 2024" }
                },
                Total = 1,
                Page = 1,
                PageSize = 10,
                TotalPages = 1
            };

            _mockService.Setup(s => s.GetAllPaginatedAsync(null, null, keyword, 1, 10))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetAll(null, null, keyword);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var paginatedResult = Assert.IsType<PaginatedResultDTO<EventDTO>>(okResult.Value);
            Assert.Equal(1, paginatedResult.Total);
            Assert.Contains("Conference", paginatedResult.Items[0].EventName);
            _mockService.Verify(s => s.GetAllPaginatedAsync(null, null, keyword, 1, 10), Times.Once);
        }

        [Fact]
        public async Task GetAll_WithEmptyKeyword_ReturnsAllEvents()
        {
            // Arrange
            string keyword = string.Empty;
            var expectedResult = new PaginatedResultDTO<EventDTO>
            {
                Items = new List<EventDTO>
                {
                    new EventDTO { EventId = 1, EventName = "Event 1" },
                    new EventDTO { EventId = 2, EventName = "Event 2" }
                },
                Total = 2,
                Page = 1,
                PageSize = 10,
                TotalPages = 1
            };

            _mockService.Setup(s => s.GetAllPaginatedAsync(null, null, keyword, 1, 10))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetAll(null, null, keyword);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var paginatedResult = Assert.IsType<PaginatedResultDTO<EventDTO>>(okResult.Value);
            Assert.Equal(2, paginatedResult.Total);
            _mockService.Verify(s => s.GetAllPaginatedAsync(null, null, keyword, 1, 10), Times.Once);
        }

        [Fact]
        public async Task GetAll_WithCustomPagination_ReturnsCorrectPage()
        {
            // Arrange
            int page = 2;
            int pageSize = 5;
            var expectedResult = new PaginatedResultDTO<EventDTO>
            {
                Items = new List<EventDTO>
                {
                    new EventDTO { EventId = 6, EventName = "Event 6" },
                    new EventDTO { EventId = 7, EventName = "Event 7" }
                },
                Total = 12,
                Page = 2,
                PageSize = 5,
                TotalPages = 3
            };

            _mockService.Setup(s => s.GetAllPaginatedAsync(null, null, null, page, pageSize))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetAll(null, null, null, page, pageSize);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var paginatedResult = Assert.IsType<PaginatedResultDTO<EventDTO>>(okResult.Value);
            Assert.Equal(2, paginatedResult.Page);
            Assert.Equal(5, paginatedResult.PageSize);
            Assert.Equal(3, paginatedResult.TotalPages);
            _mockService.Verify(s => s.GetAllPaginatedAsync(null, null, null, page, pageSize), Times.Once);
        }

        [Fact]
        public async Task GetAll_WithPageZero_CallsService()
        {
            // Arrange
            int page = 0;
            var expectedResult = new PaginatedResultDTO<EventDTO>
            {
                Items = new List<EventDTO>(),
                Total = 0,
                Page = 0,
                PageSize = 10,
                TotalPages = 0
            };

            _mockService.Setup(s => s.GetAllPaginatedAsync(null, null, null, page, 10))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetAll(null, null, null, page);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            _mockService.Verify(s => s.GetAllPaginatedAsync(null, null, null, page, 10), Times.Once);
        }

        [Fact]
        public async Task GetAll_WithPageSizeZero_CallsService()
        {
            // Arrange
            int pageSize = 0;
            var expectedResult = new PaginatedResultDTO<EventDTO>
            {
                Items = new List<EventDTO>(),
                Total = 10,
                Page = 1,
                PageSize = 0,
                TotalPages = 0
            };

            _mockService.Setup(s => s.GetAllPaginatedAsync(null, null, null, 1, pageSize))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetAll(null, null, null, 1, pageSize);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            _mockService.Verify(s => s.GetAllPaginatedAsync(null, null, null, 1, pageSize), Times.Once);
        }

        [Fact]
        public async Task GetAll_EmptyResult_ReturnsOkWithEmptyList()
        {
            // Arrange
            var expectedResult = new PaginatedResultDTO<EventDTO>
            {
                Items = new List<EventDTO>(),
                Total = 0,
                Page = 1,
                PageSize = 10,
                TotalPages = 0
            };

            _mockService.Setup(s => s.GetAllPaginatedAsync(null, null, null, 1, 10))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var paginatedResult = Assert.IsType<PaginatedResultDTO<EventDTO>>(okResult.Value);
            Assert.Equal(0, paginatedResult.Total);
            Assert.Empty(paginatedResult.Items);
            _mockService.Verify(s => s.GetAllPaginatedAsync(null, null, null, 1, 10), Times.Once);
        }

        [Fact]
        public async Task GetAll_AllParametersCombined_ReturnsFilteredPaginatedEvents()
        {
            // Arrange
            DateTime fromDate = new DateTime(2024, 1, 1);
            DateTime toDate = new DateTime(2024, 12, 31);
            string keyword = "workshop";
            int page = 2;
            int pageSize = 5;

            var expectedResult = new PaginatedResultDTO<EventDTO>
            {
                Items = new List<EventDTO>
                {
                    new EventDTO { EventId = 6, EventName = "Workshop 6" }
                },
                Total = 6,
                Page = 2,
                PageSize = 5,
                TotalPages = 2
            };

            _mockService.Setup(s => s.GetAllPaginatedAsync(fromDate, toDate, keyword, page, pageSize))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetAll(fromDate, toDate, keyword, page, pageSize);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var paginatedResult = Assert.IsType<PaginatedResultDTO<EventDTO>>(okResult.Value);
            Assert.Single(paginatedResult.Items);
            Assert.Equal(2, paginatedResult.Page);
            _mockService.Verify(s => s.GetAllPaginatedAsync(fromDate, toDate, keyword, page, pageSize), Times.Once);
        }

        [Fact]
        public async Task GetAll_ServiceThrowsException_PropagatesException()
        {
            // Arrange
            _mockService.Setup(s => s.GetAllPaginatedAsync(null, null, null, 1, 10))
                .ThrowsAsync(new System.Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<System.Exception>(() => _controller.GetAll());
            _mockService.Verify(s => s.GetAllPaginatedAsync(null, null, null, 1, 10), Times.Once);
        }
    }
}
