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
    public class CreateEventTests
    {
        private readonly Mock<IEventService> _mockEventService;
        private readonly EventController _controller;

        public CreateEventTests()
        {
            _mockEventService = new Mock<IEventService>();
            _controller = new EventController(_mockEventService.Object);
        }

        [Fact]
        public async Task Create_ValidEventDTO_ReturnsCreatedAtActionWithEventId()
        {
            // Arrange
            var eventDto = new EventDTO
            {
                EventName = "Test Event",
                Content = "Test Content",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(1)
            };
            int expectedEventId = 123;
            int userId = 1;

            _mockEventService
                .Setup(s => s.CreateAsync(It.IsAny<EventDTO>(), It.IsAny<int>()))
                .ReturnsAsync(expectedEventId);

            SetupUserClaims(userId);

            // Act
            var result = await _controller.Create(eventDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(EventController.GetById), createdAtActionResult.ActionName);
            Assert.Equal(expectedEventId, createdAtActionResult.Value);
            Assert.Equal(expectedEventId, createdAtActionResult.RouteValues["eventId"]);
            _mockEventService.Verify(s => s.CreateAsync(eventDto, userId), Times.Once);
        }

        [Fact]
        public async Task Create_ServiceReturnsZero_ReturnsCreatedAtActionWithZero()
        {
            // Arrange
            var eventDto = new EventDTO
            {
                EventName = "Test Event",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(1)
            };
            int expectedEventId = 0;
            int userId = 5;

            _mockEventService
                .Setup(s => s.CreateAsync(It.IsAny<EventDTO>(), It.IsAny<int>()))
                .ReturnsAsync(expectedEventId);

            SetupUserClaims(userId);

            // Act
            var result = await _controller.Create(eventDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(expectedEventId, createdAtActionResult.Value);
        }

        [Fact]
        public async Task Create_MissingUserIdClaim_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var eventDto = new EventDTO
            {
                EventName = "Test Event",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(1)
            };

            // Setup controller without user claims
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                async () => await _controller.Create(eventDto)
            );
            Assert.Equal("Missing user id claim", exception.Message);
        }

        [Fact]
        public async Task Create_EmptyUserIdClaim_ThrowsUnauthorizedAccessException()
        {
            // Arrange
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
                async () => await _controller.Create(eventDto)
            );
            Assert.Equal("Missing user id claim", exception.Message);
        }

        [Fact]
        public async Task Create_MinimalEventDTO_ReturnsCreatedAtActionWithEventId()
        {
            // Arrange
            var eventDto = new EventDTO
            {
                EventName = "Minimal Event",
                StartDate = DateTime.MinValue,
                EndDate = DateTime.MaxValue
            };
            int expectedEventId = 999;
            int userId = 100;

            _mockEventService
                .Setup(s => s.CreateAsync(It.IsAny<EventDTO>(), It.IsAny<int>()))
                .ReturnsAsync(expectedEventId);

            SetupUserClaims(userId);

            // Act
            var result = await _controller.Create(eventDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(expectedEventId, createdAtActionResult.Value);
            _mockEventService.Verify(s => s.CreateAsync(eventDto, userId), Times.Once);
        }

        [Fact]
        public async Task Create_NegativeUserId_CallsServiceWithNegativeUserId()
        {
            // Arrange
            var eventDto = new EventDTO
            {
                EventName = "Test Event",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(1)
            };
            int expectedEventId = 50;
            int userId = -1;

            _mockEventService
                .Setup(s => s.CreateAsync(It.IsAny<EventDTO>(), It.IsAny<int>()))
                .ReturnsAsync(expectedEventId);

            SetupUserClaims(userId);

            // Act
            var result = await _controller.Create(eventDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(expectedEventId, createdAtActionResult.Value);
            _mockEventService.Verify(s => s.CreateAsync(eventDto, userId), Times.Once);
        }

        [Fact]
        public async Task Create_ServiceThrowsException_PropagatesException()
        {
            // Arrange
            var eventDto = new EventDTO
            {
                EventName = "Test Event",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(1)
            };
            int userId = 1;

            _mockEventService
                .Setup(s => s.CreateAsync(It.IsAny<EventDTO>(), It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            SetupUserClaims(userId);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _controller.Create(eventDto)
            );
        }

        [Fact]
        public async Task Create_ServiceReturnsNegativeId_ReturnsCreatedAtActionWithNegativeId()
        {
            // Arrange
            var eventDto = new EventDTO
            {
                EventName = "Test Event",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(1)
            };
            int expectedEventId = -100;
            int userId = 1;

            _mockEventService
                .Setup(s => s.CreateAsync(It.IsAny<EventDTO>(), It.IsAny<int>()))
                .ReturnsAsync(expectedEventId);

            SetupUserClaims(userId);

            // Act
            var result = await _controller.Create(eventDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(expectedEventId, createdAtActionResult.Value);
            Assert.Equal(expectedEventId, createdAtActionResult.RouteValues["eventId"]);
            _mockEventService.Verify(s => s.CreateAsync(eventDto, userId), Times.Once);
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
