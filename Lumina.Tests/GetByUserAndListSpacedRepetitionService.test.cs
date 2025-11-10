using DataLayer.DTOs.Vocabulary;
using DataLayer.Models;
using Moq;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer.UserSpacedRepetition;
using ServiceLayer.Vocabulary;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Lumina.Tests
{
    public class GetByUserAndListSpacedRepetitionServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserSpacedRepetitionRepository> _mockUserSpacedRepetitionRepository;
        private readonly SpacedRepetitionService _service;

        public GetByUserAndListSpacedRepetitionServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUserSpacedRepetitionRepository = new Mock<IUserSpacedRepetitionRepository>();
            _mockUnitOfWork.Setup(u => u.UserSpacedRepetitions).Returns(_mockUserSpacedRepetitionRepository.Object);
            _service = new SpacedRepetitionService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task GetByUserAndListAsync_WithValidIds_ShouldReturnSpacedRepetitionDTO()
        {
            // Arrange
            var userId = 1;
            var vocabularyListId = 1;
            var now = DateTime.UtcNow;
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = vocabularyListId,
                Name = "Test List"
            };
            var item = new UserSpacedRepetition
            {
                UserSpacedRepetitionId = 1,
                UserId = userId,
                VocabularyListId = vocabularyListId,
                VocabularyList = vocabularyList,
                LastReviewedAt = now.AddDays(-5),
                NextReviewAt = now.AddDays(2),
                ReviewCount = 3,
                Intervals = 5,
                Status = "Learning"
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByUserAndListAsync(userId, vocabularyListId))
                .ReturnsAsync(item);

            // Act
            var result = await _service.GetByUserAndListAsync(userId, vocabularyListId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.UserSpacedRepetitionId);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(vocabularyListId, result.VocabularyListId);
            Assert.Equal("Test List", result.VocabularyListName);
            Assert.Equal(3, result.ReviewCount);
            Assert.Equal(5, result.Intervals);
            Assert.Equal("Learning", result.Status);
            _mockUserSpacedRepetitionRepository.Verify(r => r.GetByUserAndListAsync(userId, vocabularyListId), Times.Once);
        }

        [Fact]
        public async Task GetByUserAndListAsync_WithNullItem_ShouldReturnNull()
        {
            // Arrange
            var userId = 1;
            var vocabularyListId = 999;

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByUserAndListAsync(userId, vocabularyListId))
                .ReturnsAsync((UserSpacedRepetition?)null);

            // Act
            var result = await _service.GetByUserAndListAsync(userId, vocabularyListId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByUserAndListAsync_WithNullVocabularyList_ShouldUseUnknown()
        {
            // Arrange
            var userId = 1;
            var vocabularyListId = 1;
            var now = DateTime.UtcNow;
            var item = new UserSpacedRepetition
            {
                UserSpacedRepetitionId = 1,
                UserId = userId,
                VocabularyListId = vocabularyListId,
                VocabularyList = null, // Null VocabularyList
                LastReviewedAt = now,
                NextReviewAt = now.AddDays(1),
                ReviewCount = 1,
                Intervals = 1,
                Status = "Learning"
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByUserAndListAsync(userId, vocabularyListId))
                .ReturnsAsync(item);

            // Act
            var result = await _service.GetByUserAndListAsync(userId, vocabularyListId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Unknown", result.VocabularyListName);
        }

        [Fact]
        public async Task GetByUserAndListAsync_WithDueItem_ShouldSetIsDueToTrue()
        {
            // Arrange
            var userId = 1;
            var vocabularyListId = 1;
            var now = DateTime.UtcNow;
            var pastDate = now.AddDays(-1);
            var item = new UserSpacedRepetition
            {
                UserSpacedRepetitionId = 1,
                UserId = userId,
                VocabularyListId = vocabularyListId,
                VocabularyList = new VocabularyList { Name = "Test" },
                LastReviewedAt = now,
                NextReviewAt = pastDate, // Past date - should be due
                ReviewCount = 1,
                Intervals = 1,
                Status = "Learning"
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByUserAndListAsync(userId, vocabularyListId))
                .ReturnsAsync(item);

            // Act
            var result = await _service.GetByUserAndListAsync(userId, vocabularyListId);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsDue);
        }

        [Fact]
        public async Task GetByUserAndListAsync_WithFutureNextReviewAt_ShouldSetIsDueToFalse()
        {
            // Arrange
            var userId = 1;
            var vocabularyListId = 1;
            var now = DateTime.UtcNow;
            var futureDate = now.AddDays(5);
            var item = new UserSpacedRepetition
            {
                UserSpacedRepetitionId = 1,
                UserId = userId,
                VocabularyListId = vocabularyListId,
                VocabularyList = new VocabularyList { Name = "Test" },
                LastReviewedAt = now,
                NextReviewAt = futureDate, // Future date - should not be due
                ReviewCount = 1,
                Intervals = 1,
                Status = "Learning"
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByUserAndListAsync(userId, vocabularyListId))
                .ReturnsAsync(item);

            // Act
            var result = await _service.GetByUserAndListAsync(userId, vocabularyListId);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsDue);
        }

        [Fact]
        public async Task GetByUserAndListAsync_WithNullNextReviewAt_ShouldSetIsDueToFalse()
        {
            // Arrange
            var userId = 1;
            var vocabularyListId = 1;
            var now = DateTime.UtcNow;
            var item = new UserSpacedRepetition
            {
                UserSpacedRepetitionId = 1,
                UserId = userId,
                VocabularyListId = vocabularyListId,
                VocabularyList = new VocabularyList { Name = "Test" },
                LastReviewedAt = now,
                NextReviewAt = null, // Null NextReviewAt
                ReviewCount = 1,
                Intervals = 1,
                Status = "Learning"
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByUserAndListAsync(userId, vocabularyListId))
                .ReturnsAsync(item);

            // Act
            var result = await _service.GetByUserAndListAsync(userId, vocabularyListId);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsDue);
            Assert.Equal(0, result.DaysUntilReview);
        }

        [Fact]
        public async Task GetByUserAndListAsync_WithNullValues_ShouldUseDefaults()
        {
            // Arrange
            var userId = 1;
            var vocabularyListId = 1;
            var now = DateTime.UtcNow;
            var item = new UserSpacedRepetition
            {
                UserSpacedRepetitionId = 1,
                UserId = userId,
                VocabularyListId = vocabularyListId,
                VocabularyList = new VocabularyList { Name = "Test" },
                LastReviewedAt = now,
                NextReviewAt = now.AddDays(1),
                ReviewCount = null,
                Intervals = null,
                Status = null
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByUserAndListAsync(userId, vocabularyListId))
                .ReturnsAsync(item);

            // Act
            var result = await _service.GetByUserAndListAsync(userId, vocabularyListId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.ReviewCount);
            Assert.Equal(1, result.Intervals);
            Assert.Equal("New", result.Status);
        }
    }
}

