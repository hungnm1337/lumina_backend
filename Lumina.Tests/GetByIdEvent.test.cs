using System;
using System.Threading.Tasks;
using DataLayer.DTOs;
using lumina.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Event;
using Xunit;

namespace Lumina.Tests
{
    public class GetByIdEventTests
    {
        private readonly Mock<IEventService> _mockEventService;
        private readonly EventController _controller;

        public GetByIdEventTests()
        {
            _mockEventService = new Mock<IEventService>();
            _controller = new EventController(_mockEventService.Object);
        }

        [Fact]
        public async Task GetById_ExistingEventId_ReturnsOkWithEventDTO()
        {
            // Arrange
            int eventId = 123;
            var expectedEvent = new EventDTO
            {
                EventId = eventId,
                EventName = "Test Event",
                Content = "Test Content",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(1)
            };

            _mockEventService
                .Setup(s => s.GetByIdAsync(eventId))
                .ReturnsAsync(expectedEvent);

            // Act
            var result = await _controller.GetById(eventId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedEvent = Assert.IsType<EventDTO>(okResult.Value);
            Assert.Equal(expectedEvent.EventId, returnedEvent.EventId);
            Assert.Equal(expectedEvent.EventName, returnedEvent.EventName);
            Assert.Equal(expectedEvent.Content, returnedEvent.Content);
            _mockEventService.Verify(s => s.GetByIdAsync(eventId), Times.Once);
        }

        [Fact]
        public async Task GetById_NonExistingEventId_ReturnsNotFound()
        {
            // Arrange
            int eventId = 999;

            _mockEventService
                .Setup(s => s.GetByIdAsync(eventId))
                .ReturnsAsync((EventDTO)null);

            // Act
            var result = await _controller.GetById(eventId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
            _mockEventService.Verify(s => s.GetByIdAsync(eventId), Times.Once);
        }

        [Fact]
        public async Task GetById_ServiceThrowsException_PropagatesException()
        {
            // Arrange
            int eventId = 123;

            _mockEventService
                .Setup(s => s.GetByIdAsync(eventId))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _controller.GetById(eventId)
            );
            _mockEventService.Verify(s => s.GetByIdAsync(eventId), Times.Once);
        }

        [Fact]
        public async Task GetById_ZeroEventId_ReturnsNotFound()
        {
            // Arrange
            int eventId = 0;

            _mockEventService
                .Setup(s => s.GetByIdAsync(eventId))
                .ReturnsAsync((EventDTO)null);

            // Act
            var result = await _controller.GetById(eventId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
            _mockEventService.Verify(s => s.GetByIdAsync(eventId), Times.Once);
        }

        [Fact]
        public async Task GetById_NegativeEventId_ReturnsNotFound()
        {
            // Arrange
            int eventId = -1;

            _mockEventService
                .Setup(s => s.GetByIdAsync(eventId))
                .ReturnsAsync((EventDTO)null);

            // Act
            var result = await _controller.GetById(eventId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
            _mockEventService.Verify(s => s.GetByIdAsync(eventId), Times.Once);
        }

        [Fact]
        public async Task GetById_MaxIntEventId_ReturnsOkWithEventDTO()
        {
            // Arrange
            int eventId = int.MaxValue;
            var expectedEvent = new EventDTO
            {
                EventId = eventId,
                EventName = "Max ID Event",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(1)
            };

            _mockEventService
                .Setup(s => s.GetByIdAsync(eventId))
                .ReturnsAsync(expectedEvent);

            // Act
            var result = await _controller.GetById(eventId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedEvent = Assert.IsType<EventDTO>(okResult.Value);
            Assert.Equal(eventId, returnedEvent.EventId);
            _mockEventService.Verify(s => s.GetByIdAsync(eventId), Times.Once);
        }
    }
}
