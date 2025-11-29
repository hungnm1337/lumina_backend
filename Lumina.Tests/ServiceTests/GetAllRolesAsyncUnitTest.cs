using Xunit;
using Moq;
using DataLayer.DTOs.Role;
using DataLayer.Models;
using ServiceLayer.Role;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Lumina.Test.Services
{
    public class GetAllRolesAsyncUnitTest
    {
        private readonly Mock<IRoleRepository> _mockRoleRepository;
        private readonly RoleService _service;

        public GetAllRolesAsyncUnitTest()
        {
            _mockRoleRepository = new Mock<IRoleRepository>();
            _service = new RoleService(_mockRoleRepository.Object);
        }

        /// <summary>
        /// Test GetAllRolesAsync khi có nhiều roles trong database
        /// Coverage: Line 20-25 (repository call, mapping, và return)
        /// </summary>
        [Fact]
        public async Task GetAllRolesAsync_WhenRolesExist_ShouldReturnMappedRoleDtos()
        {
            // Arrange
            var roles = new List<Role>
            {
                new Role { RoleId = 1, RoleName = "Admin" },
                new Role { RoleId = 2, RoleName = "Student" },
                new Role { RoleId = 3, RoleName = "Teacher" }
            };

            _mockRoleRepository
                .Setup(r => r.GetAllRolesAsync())
                .ReturnsAsync(roles);

            // Act
            var result = await _service.GetAllRolesAsync();

            // Assert
            Assert.NotNull(result);
            var roleDtos = result.ToList();
            Assert.Equal(3, roleDtos.Count);

            // Verify mapping
            Assert.Equal(1, roleDtos[0].Id);
            Assert.Equal("Admin", roleDtos[0].Name);
            Assert.Equal(2, roleDtos[1].Id);
            Assert.Equal("Student", roleDtos[1].Name);
            Assert.Equal(3, roleDtos[2].Id);
            Assert.Equal("Teacher", roleDtos[2].Name);

            // Verify repository được gọi đúng 1 lần
            _mockRoleRepository.Verify(r => r.GetAllRolesAsync(), Times.Once);
        }

        /// <summary>
        /// Test GetAllRolesAsync khi database rỗng (không có roles)
        /// Coverage: Line 20-25 (empty collection handling)
        /// </summary>
        [Fact]
        public async Task GetAllRolesAsync_WhenNoRolesExist_ShouldReturnEmptyCollection()
        {
            // Arrange
            var emptyRoles = new List<Role>();

            _mockRoleRepository
                .Setup(r => r.GetAllRolesAsync())
                .ReturnsAsync(emptyRoles);

            // Act
            var result = await _service.GetAllRolesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            // Verify repository được gọi đúng 1 lần
            _mockRoleRepository.Verify(r => r.GetAllRolesAsync(), Times.Once);
        }

        /// <summary>
        /// Test GetAllRolesAsync với 1 role duy nhất
        /// Coverage: Line 20-25 (single item mapping)
        /// </summary>
        [Fact]
        public async Task GetAllRolesAsync_WhenSingleRoleExists_ShouldReturnSingleRoleDto()
        {
            // Arrange
            var roles = new List<Role>
            {
                new Role { RoleId = 1, RoleName = "SuperAdmin" }
            };

            _mockRoleRepository
                .Setup(r => r.GetAllRolesAsync())
                .ReturnsAsync(roles);

            // Act
            var result = await _service.GetAllRolesAsync();

            // Assert
            Assert.NotNull(result);
            var roleDtos = result.ToList();
            Assert.Single(roleDtos);
            Assert.Equal(1, roleDtos[0].Id);
            Assert.Equal("SuperAdmin", roleDtos[0].Name);

            // Verify repository được gọi đúng 1 lần
            _mockRoleRepository.Verify(r => r.GetAllRolesAsync(), Times.Once);
        }
    }
}
