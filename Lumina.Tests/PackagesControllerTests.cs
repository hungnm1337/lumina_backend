using DataLayer.DTOs.Packages;
using DataLayer.Models;
using lumina.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Lumina.Tests
{
    public class PackagesControllerTests
    {
        private readonly Mock<IPackageService> _mockPackageService;
        private readonly PackagesController _controller;

        public PackagesControllerTests()
        {
            _mockPackageService = new Mock<IPackageService>();
            _controller = new PackagesController(_mockPackageService.Object);
        }

        #region GetActiveProPackages Tests (1 test case)

        [Fact]
        public async Task GetActiveProPackages_ValidRequest_ReturnsOkWithPackageList()
        {
            // Arrange
            var packages = new List<Package>
            {
                new Package { PackageId = 1, PackageName = "Pro Monthly", Price = 99000, DurationInDays = 30, IsActive = true },
                new Package { PackageId = 2, PackageName = "Pro Yearly", Price = 990000, DurationInDays = 365, IsActive = true }
            };

            _mockPackageService.Setup(s => s.GetActivePackagesAsync())
                .ReturnsAsync(packages);

            // Act
            var result = await _controller.GetActiveProPackages();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedPackages = Assert.IsAssignableFrom<List<Package>>(okResult.Value);
            Assert.Equal(2, returnedPackages.Count);
            _mockPackageService.Verify(s => s.GetActivePackagesAsync(), Times.Once);
        }

        #endregion

        #region GetPackage Tests (2 test cases)

        [Fact]
        public async Task GetPackage_ValidId_ReturnsOkWithPackage()
        {
            // Arrange
            var package = new Package 
            { 
                PackageId = 1, 
                PackageName = "Pro Monthly", 
                Price = 99000, 
                DurationInDays = 30, 
                IsActive = true 
            };

            _mockPackageService.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(package);

            // Act
            var result = await _controller.GetPackage(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedPackage = Assert.IsType<Package>(okResult.Value);
            Assert.Equal(1, returnedPackage.PackageId);
            Assert.Equal("Pro Monthly", returnedPackage.PackageName);
        }

        [Fact]
        public async Task GetPackage_InvalidId_ReturnsNotFound()
        {
            // Arrange
            _mockPackageService.Setup(s => s.GetByIdAsync(999))
                .ReturnsAsync((Package)null);

            // Act
            var result = await _controller.GetPackage(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region UpdatePackage Tests (3 test cases)

        [Fact]
        public async Task UpdatePackage_ValidRequest_ReturnsNoContent()
        {
            // Arrange
            var existingPackage = new Package 
            { 
                PackageId = 1, 
                PackageName = "Old Name", 
                Price = 50000, 
                DurationInDays = 30, 
                IsActive = true 
            };

            var updatedPackage = new Package 
            { 
                PackageId = 1, 
                PackageName = "New Name", 
                Price = 99000, 
                DurationInDays = 60, 
                IsActive = false 
            };

            _mockPackageService.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(existingPackage);
            _mockPackageService.Setup(s => s.UpdatePackageAsync(It.IsAny<Package>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdatePackage(1, updatedPackage);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal("New Name", existingPackage.PackageName);
            Assert.Equal(99000, existingPackage.Price);
            Assert.Equal(60, existingPackage.DurationInDays);
            Assert.False(existingPackage.IsActive);
            _mockPackageService.Verify(s => s.UpdatePackageAsync(existingPackage), Times.Once);
        }

        [Fact]
        public async Task UpdatePackage_IdMismatch_ReturnsBadRequest()
        {
            // Arrange
            var updatedPackage = new Package { PackageId = 2, PackageName = "Test" };

            // Act
            var result = await _controller.UpdatePackage(1, updatedPackage);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Package ID mismatch", badRequestResult.Value);
        }

        [Fact]
        public async Task UpdatePackage_PackageNotFound_ReturnsNotFound()
        {
            // Arrange
            var updatedPackage = new Package { PackageId = 999, PackageName = "Test" };
            _mockPackageService.Setup(s => s.GetByIdAsync(999))
                .ReturnsAsync((Package)null);

            // Act
            var result = await _controller.UpdatePackage(999, updatedPackage);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region ToggleStatus Tests (1 test case)

        [Fact]
        public async Task ToggleStatus_ValidId_ReturnsNoContent()
        {
            // Arrange
            _mockPackageService.Setup(s => s.TogglePackageStatusAsync(1))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.ToggleStatus(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockPackageService.Verify(s => s.TogglePackageStatusAsync(1), Times.Once);
        }

        #endregion

        #region GetUserActivePackage Tests (2 test cases)

        [Fact]
        public async Task GetUserActivePackage_ValidUserId_ReturnsOkWithPackageInfo()
        {
            // Arrange
            var packageInfo = new UserActivePackageInfo
            {
                Package = new Package 
                { 
                    PackageId = 1, 
                    PackageName = "Pro Monthly", 
                    Price = 99000, 
                    IsActive = true 
                },
                StartTime = DateTime.UtcNow.AddDays(-5),
                EndTime = DateTime.UtcNow.AddDays(25)
            };

            _mockPackageService.Setup(s => s.GetUserActivePackageAsync(1))
                .ReturnsAsync(packageInfo); 

            // Act
            var result = await _controller.GetUserActivePackage(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedInfo = Assert.IsType<UserActivePackageInfo>(okResult.Value);
            Assert.Equal(1, returnedInfo.Package.PackageId);
            Assert.NotNull(returnedInfo.StartTime);
            Assert.NotNull(returnedInfo.EndTime);
        }

        [Fact]
        public async Task GetUserActivePackage_NoActivePackage_ReturnsNotFound()
        {
            // Arrange
            _mockPackageService.Setup(s => s.GetUserActivePackageAsync(999))
                .ReturnsAsync((UserActivePackageInfo)null);

            // Act
            var result = await _controller.GetUserActivePackage(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User has no active package.", notFoundResult.Value);
        }

        #endregion
    }
}