using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataLayer.DTOs.Packages;
using DataLayer.Models;
using Moq;
using ServiceLayer.Packages;
using Xunit;

public class PackageServiceTests
{
    private readonly Mock<IPackageRepository> _packageRepositoryMock;
    private readonly PackageService _service;

    public PackageServiceTests()
    {
        _packageRepositoryMock = new Mock<IPackageRepository>(MockBehavior.Strict);
        _service = new PackageService(_packageRepositoryMock.Object);
    }

    [Fact]
    public async Task GetActivePackagesAsync_RepositoryReturnsPackages_ReturnsSameList()
    {
        // Arrange
        var expectedPackages = new List<Package>
        {
            new Package { PackageId = 1, PackageName = "Basic" },
            new Package { PackageId = 2, PackageName = "Premium" }
        };

        _packageRepositoryMock
            .Setup(r => r.GetActivePackagesAsync())
            .ReturnsAsync(expectedPackages);

        // Act
        var result = await _service.GetActivePackagesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Same(expectedPackages, result);
        Assert.Equal(2, result.Count);

        _packageRepositoryMock.Verify(r => r.GetActivePackagesAsync(), Times.Once);
        _packageRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetActivePackagesAsync_RepositoryThrows_PropagatesException()
    {
        // Arrange
        _packageRepositoryMock
            .Setup(r => r.GetActivePackagesAsync())
            .ThrowsAsync(new InvalidOperationException("Repo error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetActivePackagesAsync());

        _packageRepositoryMock.Verify(r => r.GetActivePackagesAsync(), Times.Once);
        _packageRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsPackage()
    {
        // Arrange
        var package = new Package { PackageId = 10, PackageName = "Standard" };

        _packageRepositoryMock
            .Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(package);

        // Act
        var result = await _service.GetByIdAsync(10);

        // Assert
        Assert.NotNull(result);
        Assert.Same(package, result);
        Assert.Equal(10, result.PackageId);

        _packageRepositoryMock.Verify(r => r.GetByIdAsync(10), Times.Once);
        _packageRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryReturnsNull_ReturnsNull()
    {
        // Arrange
        _packageRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Package?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);

        _packageRepositoryMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _packageRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_PropagatesException()
    {
        // Arrange
        _packageRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ThrowsAsync(new ArgumentException("Invalid id"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetByIdAsync(-1));

        _packageRepositoryMock.Verify(r => r.GetByIdAsync(-1), Times.Once);
        _packageRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddPackageAsync_ValidPackage_CallsRepositoryOnce()
    {
        // Arrange
        var package = new Package { PackageId = 1, PackageName = "Basic" };

        _packageRepositoryMock
            .Setup(r => r.AddPackageAsync(package))
            .Returns(Task.CompletedTask);

        // Act
        await _service.AddPackageAsync(package);

        // Assert
        _packageRepositoryMock.Verify(r => r.AddPackageAsync(package), Times.Once);
        _packageRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddPackageAsync_NullPackage_RepositoryThrows_PropagatesException()
    {
        // Arrange
        _packageRepositoryMock
            .Setup(r => r.AddPackageAsync(null!))
            .ThrowsAsync(new ArgumentNullException("pkg"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AddPackageAsync(null!));

        _packageRepositoryMock.Verify(r => r.AddPackageAsync(null!), Times.Once);
        _packageRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdatePackageAsync_ValidPackage_CallsRepositoryOnce()
    {
        // Arrange
        var package = new Package { PackageId = 5, PackageName = "Updated" };

        _packageRepositoryMock
            .Setup(r => r.UpdateAsync(package))
            .Returns(Task.CompletedTask);

        // Act
        await _service.UpdatePackageAsync(package);

        // Assert
        _packageRepositoryMock.Verify(r => r.UpdateAsync(package), Times.Once);
        _packageRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdatePackageAsync_NullPackage_RepositoryThrows_PropagatesException()
    {
        // Arrange
        _packageRepositoryMock
            .Setup(r => r.UpdateAsync(null!))
            .ThrowsAsync(new ArgumentNullException("pkg"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdatePackageAsync(null!));

        _packageRepositoryMock.Verify(r => r.UpdateAsync(null!), Times.Once);
        _packageRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TogglePackageStatusAsync_ValidId_CallsRepositoryOnce()
    {
        // Arrange
        const int packageId = 3;

        _packageRepositoryMock
            .Setup(r => r.ToggleActiveStatusAsync(packageId))
            .Returns(Task.CompletedTask);

        // Act
        await _service.TogglePackageStatusAsync(packageId);

        // Assert
        _packageRepositoryMock.Verify(r => r.ToggleActiveStatusAsync(packageId), Times.Once);
        _packageRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TogglePackageStatusAsync_InvalidId_RepositoryThrows_PropagatesException()
    {
        // Arrange
        const int invalidId = -1;

        _packageRepositoryMock
            .Setup(r => r.ToggleActiveStatusAsync(invalidId))
            .ThrowsAsync(new ArgumentOutOfRangeException(nameof(invalidId)));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.TogglePackageStatusAsync(invalidId));

        _packageRepositoryMock.Verify(r => r.ToggleActiveStatusAsync(invalidId), Times.Once);
        _packageRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeletePackageAsync_ValidId_CallsRepositoryOnce()
    {
        // Arrange
        const int packageId = 4;

        _packageRepositoryMock
            .Setup(r => r.DeleteAsync(packageId))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeletePackageAsync(packageId);

        // Assert
        _packageRepositoryMock.Verify(r => r.DeleteAsync(packageId), Times.Once);
        _packageRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeletePackageAsync_InvalidId_RepositoryThrows_PropagatesException()
    {
        // Arrange
        const int invalidId = 0;

        _packageRepositoryMock
            .Setup(r => r.DeleteAsync(invalidId))
            .ThrowsAsync(new ArgumentOutOfRangeException(nameof(invalidId)));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.DeletePackageAsync(invalidId));

        _packageRepositoryMock.Verify(r => r.DeleteAsync(invalidId), Times.Once);
        _packageRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetUserActivePackageAsync_ValidUserId_ReturnsInfo()
    {
        // Arrange
        _packageRepositoryMock
            .Setup(r => r.GetUserActivePackageAsync(7))
            .Returns(Task.FromResult<UserActivePackageInfo?>(new UserActivePackageInfo()));

        // Act
        var result = await _service.GetUserActivePackageAsync(7);

        // Assert
        Assert.NotNull(result);

        _packageRepositoryMock.Verify(r => r.GetUserActivePackageAsync(7), Times.Once);
        _packageRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetUserActivePackageAsync_RepositoryReturnsNull_ReturnsNull()
    {
        // Arrange
        _packageRepositoryMock
            .Setup(r => r.GetUserActivePackageAsync(It.IsAny<int>()))
            .Returns(Task.FromResult<UserActivePackageInfo?>(null));

        // Act
        var result = await _service.GetUserActivePackageAsync(123);

        // Assert
        Assert.Null(result);

        _packageRepositoryMock.Verify(r => r.GetUserActivePackageAsync(123), Times.Once);
        _packageRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetUserActivePackageAsync_RepositoryThrows_PropagatesException()
    {
        // Arrange
        _packageRepositoryMock
            .Setup(r => r.GetUserActivePackageAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Unexpected"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.GetUserActivePackageAsync(-10));

        _packageRepositoryMock.Verify(r => r.GetUserActivePackageAsync(-10), Times.Once);
        _packageRepositoryMock.VerifyNoOtherCalls();
    }
}


