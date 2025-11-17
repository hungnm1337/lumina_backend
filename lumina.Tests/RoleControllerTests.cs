using DataLayer.DTOs.Role;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Lumina.Tests
{
    public class RoleControllerTests
    {
        private readonly Mock<IRoleService> _mockRoleService;
        private readonly RoleController _controller;

        public RoleControllerTests()
        {
            _mockRoleService = new Mock<IRoleService>();
            _controller = new RoleController(_mockRoleService.Object);
        }

        #region GetAllRoles - Happy Path Tests

        [Fact]
        public async Task GetAllRoles_WithValidRoles_ReturnsOkWithRolesList()
        {
            // Arrange
            var expectedRoles = new List<RoleDto>
            {
                new RoleDto { Id = 2, Name = "Manager" },
                new RoleDto { Id = 3, Name = "Staff" },
                new RoleDto { Id = 4, Name = "User" }
            };
            _mockRoleService.Setup(s => s.GetAllRolesAsync())
                .ReturnsAsync(expectedRoles);

            // Act
            var result = await _controller.GetAllRoles();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var returnedRoles = Assert.IsType<List<RoleDto>>(okResult.Value);
            Assert.Equal(3, returnedRoles.Count);
            Assert.Equal("Manager", returnedRoles[0].Name);
            Assert.Equal("Staff", returnedRoles[1].Name);
            Assert.Equal("User", returnedRoles[2].Name);
            _mockRoleService.Verify(s => s.GetAllRolesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllRoles_WithSingleRole_ReturnsOkWithOneRole()
        {
            // Arrange
            var expectedRoles = new List<RoleDto>
            {
                new RoleDto { Id = 2, Name = "Manager" }
            };
            _mockRoleService.Setup(s => s.GetAllRolesAsync())
                .ReturnsAsync(expectedRoles);

            // Act
            var result = await _controller.GetAllRoles();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var returnedRoles = Assert.IsType<List<RoleDto>>(okResult.Value);
            Assert.Single(returnedRoles);
            Assert.Equal(2, returnedRoles[0].Id);
            _mockRoleService.Verify(s => s.GetAllRolesAsync(), Times.Once);
        }

        #endregion

        #region GetAllRoles - Boundary Cases Tests

        [Fact]
        public async Task GetAllRoles_WithEmptyRolesList_ReturnsOkWithEmptyList()
        {
            // Arrange
            var emptyRoles = new List<RoleDto>();
            _mockRoleService.Setup(s => s.GetAllRolesAsync())
                .ReturnsAsync(emptyRoles);

            // Act
            var result = await _controller.GetAllRoles();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            var returnedRoles = Assert.IsType<List<RoleDto>>(okResult.Value);
            Assert.Empty(returnedRoles);
            _mockRoleService.Verify(s => s.GetAllRolesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllRoles_WithMultipleRoles_VerifyCorrectOrder()
        {
            // Arrange
            var expectedRoles = new List<RoleDto>
            {
                new RoleDto { Id = 2, Name = "Manager" },
                new RoleDto { Id = 3, Name = "Staff" },
                new RoleDto { Id = 4, Name = "User" },
                new RoleDto { Id = 5, Name = "Premium" }
            };
            _mockRoleService.Setup(s => s.GetAllRolesAsync())
                .ReturnsAsync(expectedRoles);

            // Act
            var result = await _controller.GetAllRoles();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedRoles = Assert.IsType<List<RoleDto>>(okResult.Value);
            Assert.Equal(4, returnedRoles.Count);
            // Verify order
            for (int i = 0; i < returnedRoles.Count; i++)
            {
                Assert.Equal(expectedRoles[i].Id, returnedRoles[i].Id);
                Assert.Equal(expectedRoles[i].Name, returnedRoles[i].Name);
            }
        }

        #endregion

        #region GetAllRoles - Exception Handling Tests

        [Fact]
        public async Task GetAllRoles_WhenServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockRoleService.Setup(s => s.GetAllRolesAsync())
                .ThrowsAsync(new Exception("Database connection error"));

            // Act
            var result = await _controller.GetAllRoles();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            var errorValue = Assert.IsType<string>(statusCodeResult.Value);
            Assert.Equal("Lỗi máy chủ nội bộ", errorValue);
            _mockRoleService.Verify(s => s.GetAllRolesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllRoles_WhenServiceThrowsNullReferenceException_ReturnsInternalServerError()
        {
            // Arrange
            _mockRoleService.Setup(s => s.GetAllRolesAsync())
                .ThrowsAsync(new NullReferenceException("Null reference in service"));

            // Act
            var result = await _controller.GetAllRoles();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            var errorValue = Assert.IsType<string>(statusCodeResult.Value);
            Assert.Equal("Lỗi máy chủ nội bộ", errorValue);
        }

        [Fact]
        public async Task GetAllRoles_WhenServiceThrowsInvalidOperationException_ReturnsInternalServerError()
        {
            // Arrange
            _mockRoleService.Setup(s => s.GetAllRolesAsync())
                .ThrowsAsync(new InvalidOperationException("Invalid operation in service"));

            // Act
            var result = await _controller.GetAllRoles();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            var errorValue = Assert.IsType<string>(statusCodeResult.Value);
            Assert.Equal("Lỗi máy chủ nội bộ", errorValue);
        }

        #endregion

        #region GetAllRoles - Service Invocation Tests

        [Fact]
        public async Task GetAllRoles_ShouldCallServiceExactlyOnce()
        {
            // Arrange
            var roles = new List<RoleDto>
            {
                new RoleDto { Id = 2, Name = "Manager" }
            };
            _mockRoleService.Setup(s => s.GetAllRolesAsync())
                .ReturnsAsync(roles);

            // Act
            await _controller.GetAllRoles();

            // Assert
            _mockRoleService.Verify(s => s.GetAllRolesAsync(), Times.Exactly(1));
        }

        [Fact]
        public async Task GetAllRoles_ShouldNotCallServiceMultipleTimes()
        {
            // Arrange
            var roles = new List<RoleDto>();
            _mockRoleService.Setup(s => s.GetAllRolesAsync())
                .ReturnsAsync(roles);

            // Act
            var result = await _controller.GetAllRoles();

            // Assert
            _mockRoleService.Verify(s => s.GetAllRolesAsync(), Times.Once);
            // Verify service is not called more than once
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult);
        }

        #endregion

        #region GetAllRoles - Response Format Tests

        [Fact]
        public async Task GetAllRoles_ResponseShouldContainCorrectProperties()
        {
            // Arrange
            var expectedRoles = new List<RoleDto>
            {
                new RoleDto { Id = 2, Name = "Manager" },
                new RoleDto { Id = 3, Name = "Staff" }
            };
            _mockRoleService.Setup(s => s.GetAllRolesAsync())
                .ReturnsAsync(expectedRoles);

            // Act
            var result = await _controller.GetAllRoles();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedRoles = Assert.IsType<List<RoleDto>>(okResult.Value);

            // Verify each role has required properties
            foreach (var role in returnedRoles)
            {
                Assert.NotNull(role);
                Assert.True(role.Id > 0, "Role ID must be positive");
                Assert.NotNull(role.Name);
                Assert.NotEmpty(role.Name);
            }
        }

        [Fact]
        public async Task GetAllRoles_ReturnedRolesAreIEnumerable()
        {
            // Arrange
            var expectedRoles = new List<RoleDto>
            {
                new RoleDto { Id = 2, Name = "Manager" }
            };
            _mockRoleService.Setup(s => s.GetAllRolesAsync())
                .ReturnsAsync(expectedRoles);

            // Act
            var result = await _controller.GetAllRoles();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedValue = okResult.Value;
            Assert.NotNull(returnedValue);

            // Check if it's enumerable
            var enumerableValue = returnedValue as System.Collections.IEnumerable;
            Assert.NotNull(enumerableValue);
        }

        #endregion
    }
}