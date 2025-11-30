using Xunit;
using Moq;
using ServiceLayer.Leaderboard;
using RepositoryLayer.Leaderboard;
using ServiceLayer.Notification;
using DataLayer.DTOs.Leaderboard;
using DataLayer.Models;
using Lumina.Tests.Helpers;
using System;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class LeaderboardServiceCreateTests
    {
        private readonly Mock<ILeaderboardRepository> _mockRepository;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly LuminaSystemContext _context;
        private readonly LeaderboardService _service;

        public LeaderboardServiceCreateTests()
        {
            _mockRepository = new Mock<ILeaderboardRepository>();
            _mockNotificationService = new Mock<INotificationService>();
            _context = InMemoryDbContextHelper.CreateContext();
            _service = new LeaderboardService(_mockRepository.Object, _context, _mockNotificationService.Object);
        }

        [Fact]
        public async Task CreateAsync_WhenDtoIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            CreateLeaderboardDTO? dto = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _service.CreateAsync(dto!)
            );

            // Verify repository is never called
            _mockRepository.Verify(
                repo => repo.CreateAsync(It.IsAny<Leaderboard>()),
                Times.Never
            );
        }

        [Fact]
        public async Task CreateAsync_WhenSeasonNumberIsZero_ShouldThrowArgumentException()
        {
            // Arrange
            var dto = new CreateLeaderboardDTO
            {
                SeasonNumber = 0,
                SeasonName = "Test Season",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.CreateAsync(dto)
            );

            Assert.Contains("SeasonNumber must be positive", exception.Message);

            // Verify repository is never called
            _mockRepository.Verify(
                repo => repo.CreateAsync(It.IsAny<Leaderboard>()),
                Times.Never
            );
        }

        [Fact]
        public async Task CreateAsync_WhenSeasonNumberIsNegative_ShouldThrowArgumentException()
        {
            // Arrange
            var dto = new CreateLeaderboardDTO
            {
                SeasonNumber = -1,
                SeasonName = "Test Season",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.CreateAsync(dto)
            );

            Assert.Contains("SeasonNumber must be positive", exception.Message);

            // Verify repository is never called
            _mockRepository.Verify(
                repo => repo.CreateAsync(It.IsAny<Leaderboard>()),
                Times.Never
            );
        }

        [Fact]
        public async Task CreateAsync_WhenEndDateIsBeforeStartDate_ShouldThrowArgumentException()
        {
            // Arrange
            var dto = new CreateLeaderboardDTO
            {
                SeasonNumber = 1,
                SeasonName = "Test Season",
                StartDate = DateTime.UtcNow.AddDays(30),
                EndDate = DateTime.UtcNow, // EndDate before StartDate
                IsActive = true
            };

            _mockRepository
                .Setup(repo => repo.ExistsSeasonNumberAsync(1, null))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.CreateAsync(dto)
            );

            Assert.Contains("EndDate must be after StartDate", exception.Message);

            // Verify repository is never called
            _mockRepository.Verify(
                repo => repo.CreateAsync(It.IsAny<Leaderboard>()),
                Times.Never
            );
        }

        [Fact]
        public async Task CreateAsync_WhenSeasonNumberAlreadyExists_ShouldThrowArgumentException()
        {
            // Arrange
            var dto = new CreateLeaderboardDTO
            {
                SeasonNumber = 1,
                SeasonName = "Test Season",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };

            _mockRepository
                .Setup(repo => repo.ExistsSeasonNumberAsync(1, null))
                .ReturnsAsync(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.CreateAsync(dto)
            );

            Assert.Contains("SeasonNumber already exists", exception.Message);

            // Verify repository is never called
            _mockRepository.Verify(
                repo => repo.CreateAsync(It.IsAny<Leaderboard>()),
                Times.Never
            );
        }

        [Fact]
        public async Task CreateAsync_WhenDateRangeOverlaps_ShouldThrowArgumentException()
        {
            // Arrange
            var dto = new CreateLeaderboardDTO
            {
                SeasonNumber = 1,
                SeasonName = "Test Season",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };

            _mockRepository
                .Setup(repo => repo.ExistsSeasonNumberAsync(1, null))
                .ReturnsAsync(false);

            _mockRepository
                .Setup(repo => repo.ExistsDateOverlapAsync(dto.StartDate, dto.EndDate, null))
                .ReturnsAsync(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.CreateAsync(dto)
            );

            Assert.Contains("Date range overlaps", exception.Message);

            // Verify repository is never called
            _mockRepository.Verify(
                repo => repo.CreateAsync(It.IsAny<Leaderboard>()),
                Times.Never
            );
        }

        [Fact]
        public async Task CreateAsync_WhenDtoIsValid_ShouldReturnCreatedId()
        {
            // Arrange
            var dto = new CreateLeaderboardDTO
            {
                SeasonNumber = 1,
                SeasonName = "Test Season",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };

            int expectedId = 100;

            _mockRepository
                .Setup(repo => repo.ExistsSeasonNumberAsync(1, null))
                .ReturnsAsync(false);

            _mockRepository
                .Setup(repo => repo.ExistsDateOverlapAsync(dto.StartDate, dto.EndDate, null))
                .ReturnsAsync(false);

            _mockRepository
                .Setup(repo => repo.CreateAsync(It.Is<Leaderboard>(e => 
                    e.SeasonNumber == dto.SeasonNumber &&
                    e.SeasonName == dto.SeasonName &&
                    e.StartDate == dto.StartDate &&
                    e.EndDate == dto.EndDate &&
                    e.IsActive == dto.IsActive &&
                    e.UpdateAt == null)))
                .ReturnsAsync(expectedId);

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.Equal(expectedId, result);

            // Verify repository is called exactly once
            _mockRepository.Verify(
                repo => repo.ExistsSeasonNumberAsync(1, null),
                Times.Once
            );

            _mockRepository.Verify(
                repo => repo.ExistsDateOverlapAsync(dto.StartDate, dto.EndDate, null),
                Times.Once
            );

            _mockRepository.Verify(
                repo => repo.CreateAsync(It.Is<Leaderboard>(e => 
                    e.SeasonNumber == dto.SeasonNumber &&
                    e.SeasonName == dto.SeasonName &&
                    e.StartDate == dto.StartDate &&
                    e.EndDate == dto.EndDate &&
                    e.IsActive == dto.IsActive &&
                    e.UpdateAt == null)),
                Times.Once
            );
        }

        [Fact]
        public async Task CreateAsync_WhenDatesAreNull_ShouldCreateSuccessfully()
        {
            // Arrange
            var dto = new CreateLeaderboardDTO
            {
                SeasonNumber = 1,
                SeasonName = "Test Season",
                StartDate = null,
                EndDate = null,
                IsActive = true
            };

            int expectedId = 100;

            _mockRepository
                .Setup(repo => repo.ExistsSeasonNumberAsync(1, null))
                .ReturnsAsync(false);

            _mockRepository
                .Setup(repo => repo.ExistsDateOverlapAsync(null, null, null))
                .ReturnsAsync(false);

            _mockRepository
                .Setup(repo => repo.CreateAsync(It.Is<Leaderboard>(e => 
                    e.StartDate == null &&
                    e.EndDate == null)))
                .ReturnsAsync(expectedId);

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.Equal(expectedId, result);

            // Verify repository is called
            _mockRepository.Verify(
                repo => repo.CreateAsync(It.Is<Leaderboard>(e => 
                    e.StartDate == null &&
                    e.EndDate == null)),
                Times.Once
            );
        }
    }
}
