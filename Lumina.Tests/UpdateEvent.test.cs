using System;
using System.Security.Claims;
using System.Threading.Tasks;
using DataLayer.DTOs;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Event;
using Xunit;

namespace Lumina.Tests
{
    public class UpdateEventTests
    {
        private readonly Mock<IEventService> _mockEventService;
        private readonly EventController _controller;

        public UpdateEventTests()
        {
            _mockEventService = new Mock<IEventService>();
            _controller = new EventController(_mockEventService.Object);
        }

        [Fact]
        public async Task Update_ValidEventDTO_ReturnsNoContent()
        {
            // Arrange
            int eventId = 123;
            var eventDto = new EventDTO
            {
                EventName = "Updated Event",
                Content = "Updated Content",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(1)
            };
            int userId = 1;

            _mockEventService
                .Setup(s => s.UpdateAsync(eventId, It.IsAny<EventDTO>(), userId))
                .ReturnsAsync(true);

            SetupUserClaims(userId);

            // Act
            var result = await _controller.Update(eventId, eventDto);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockEventService.Verify(s => s.UpdateAsync(eventId, eventDto, userId), Times.Once);
        }

        [Fact]
        public async Task Update_ServiceReturnsFalse_ReturnsNotFound()
        {
            // Arrange
            int eventId = 999;
            var eventDto = new EventDTO
            {
                EventName = "Non-existent Event",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(1)
            };
            int userId = 1;

            _mockEventService
                .Setup(s => s.UpdateAsync(eventId, It.IsAny<EventDTO>(), userId))
                .ReturnsAsync(false);

            SetupUserClaims(userId);

            // Act
            var result = await _controller.Update(eventId, eventDto);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockEventService.Verify(s => s.UpdateAsync(eventId, eventDto, userId), Times.Once);
        }

        [Fact]
        public async Task Update_MissingUserIdClaim_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            int eventId = 123;
            var eventDto = new EventDTO
            {
                EventName = "Test Event",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(1)
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                async () => await _controller.Update(eventId, eventDto)
            );
            Assert.Equal("Missing user id claim", exception.Message);
        }

        [Fact]
        public async Task Update_EmptyUserIdClaim_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            int eventId = 123;
            var eventDto = new EventDTO
            {
                EventName = "Test Event",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(1)
            };

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, string.Empty)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                async () => await _controller.Update(eventId, eventDto)
            );
            Assert.Equal("Missing user id claim", exception.Message);
        }

        [Fact]
        public async Task Update_ServiceThrowsException_PropagatesException()
        {
            // Arrange
            int eventId = 123;
            var eventDto = new EventDTO
            {
                EventName = "Test Event",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(1)
            };
            int userId = 1;

            _mockEventService
                .Setup(s => s.UpdateAsync(eventId, It.IsAny<EventDTO>(), userId))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            SetupUserClaims(userId);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _controller.Update(eventId, eventDto)
            );
        }

        [Fact]
        public async Task Update_ZeroEventId_CallsServiceWithZeroId()
        {
            // Arrange
            int eventId = 0;
            var eventDto = new EventDTO
            {
                EventName = "Test Event",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(1)
            };
            int userId = 1;

            _mockEventService
                .Setup(s => s.UpdateAsync(eventId, It.IsAny<EventDTO>(), userId))
                .ReturnsAsync(false);

            SetupUserClaims(userId);

            // Act
            var result = await _controller.Update(eventId, eventDto);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockEventService.Verify(s => s.UpdateAsync(eventId, eventDto, userId), Times.Once);
        }

        [Fact]
        public async Task Update_NegativeEventId_CallsServiceWithNegativeId()
        {
            // Arrange
            int eventId = -1;
            var eventDto = new EventDTO
            {
                EventName = "Test Event",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(1)
            };
            int userId = 1;

            _mockEventService
                .Setup(s => s.UpdateAsync(eventId, It.IsAny<EventDTO>(), userId))
                .ReturnsAsync(true);

            SetupUserClaims(userId);

            // Act
            var result = await _controller.Update(eventId, eventDto);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockEventService.Verify(s => s.UpdateAsync(eventId, eventDto, userId), Times.Once);
        }

        private void SetupUserClaims(int userId)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };
        }
    }
}
