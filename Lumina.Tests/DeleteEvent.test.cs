using System;
using System.Threading.Tasks;
using lumina.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Event;
using Xunit;

namespace Lumina.Tests
{
    public class DeleteEventTests
    {
        private readonly Mock<IEventService> _mockEventService;
        private readonly EventController _controller;

        public DeleteEventTests()
        {
            _mockEventService = new Mock<IEventService>();
            _controller = new EventController(_mockEventService.Object);
        }

        [Fact]
        public async Task Delete_ExistingEventId_ReturnsNoContent()
        {
            // Arrange
            int eventId = 123;

            _mockEventService
                .Setup(s => s.DeleteAsync(eventId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(eventId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockEventService.Verify(s => s.DeleteAsync(eventId), Times.Once);
        }

        [Fact]
        public async Task Delete_NonExistingEventId_ReturnsNotFound()
        {
            // Arrange
            int eventId = 999;

            _mockEventService
                .Setup(s => s.DeleteAsync(eventId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(eventId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockEventService.Verify(s => s.DeleteAsync(eventId), Times.Once);
        }

        [Fact]
        public async Task Delete_ServiceThrowsException_PropagatesException()
        {
            // Arrange
            int eventId = 123;

            _mockEventService
                .Setup(s => s.DeleteAsync(eventId))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _controller.Delete(eventId)
            );
            _mockEventService.Verify(s => s.DeleteAsync(eventId), Times.Once);
        }

        [Fact]
        public async Task Delete_ZeroEventId_CallsServiceWithZeroId()
        {
            // Arrange
            int eventId = 0;

            _mockEventService
                .Setup(s => s.DeleteAsync(eventId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(eventId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockEventService.Verify(s => s.DeleteAsync(eventId), Times.Once);
        }

        [Fact]
        public async Task Delete_NegativeEventId_CallsServiceWithNegativeId()
        {
            // Arrange
            int eventId = -1;

            _mockEventService
                .Setup(s => s.DeleteAsync(eventId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(eventId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockEventService.Verify(s => s.DeleteAsync(eventId), Times.Once);
        }

        [Fact]
        public async Task Delete_MaxIntEventId_CallsServiceSuccessfully()
        {
            // Arrange
            int eventId = int.MaxValue;

            _mockEventService
                .Setup(s => s.DeleteAsync(eventId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(eventId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockEventService.Verify(s => s.DeleteAsync(eventId), Times.Once);
        }
    }
}
