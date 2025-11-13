using DataLayer.DTOs.Vocabulary;
using DataLayer.Models;
using Moq;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer.UserSpacedRepetition;
using ServiceLayer.Vocabulary;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Lumina.Tests
{
    public class GetUserRepetitionsSpacedRepetitionServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserSpacedRepetitionRepository> _mockUserSpacedRepetitionRepository;
        private readonly SpacedRepetitionService _service;

        public GetUserRepetitionsSpacedRepetitionServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUserSpacedRepetitionRepository = new Mock<IUserSpacedRepetitionRepository>();
            _mockUnitOfWork.Setup(u => u.UserSpacedRepetitions).Returns(_mockUserSpacedRepetitionRepository.Object);
            _service = new SpacedRepetitionService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task GetUserRepetitionsAsync_WithValidUserId_ShouldReturnAllItems()
        {
            // Arrange
            var userId = 1;
            var now = DateTime.UtcNow;
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = 1,
                Name = "Test List"
            };
            var items = new List<UserSpacedRepetition>
            {
                new UserSpacedRepetition
                {
                    UserSpacedRepetitionId = 1,
                    UserId = userId,
                    VocabularyListId = 1,
                    VocabularyList = vocabularyList,
                    LastReviewedAt = now.AddDays(-5),
                    NextReviewAt = now.AddDays(2),
                    ReviewCount = 3,
                    Intervals = 5,
                    Status = "Learning"
                }
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(items);

            // Act
            var result = await _service.GetUserRepetitionsAsync(userId);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Single(resultList);
            Assert.Equal(1, resultList[0].UserSpacedRepetitionId);
            Assert.Equal(userId, resultList[0].UserId);
            Assert.Equal("Test List", resultList[0].VocabularyListName);
            _mockUserSpacedRepetitionRepository.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetUserRepetitionsAsync_WithNullVocabularyList_ShouldUseUnknown()
        {
            // Arrange
            var userId = 1;
            var now = DateTime.UtcNow;
            var items = new List<UserSpacedRepetition>
            {
                new UserSpacedRepetition
                {
                    UserSpacedRepetitionId = 1,
                    UserId = userId,
                    VocabularyListId = 1,
                    VocabularyList = null,
                    LastReviewedAt = now,
                    NextReviewAt = now.AddDays(1),
                    ReviewCount = 1,
                    Intervals = 1,
                    Status = "Learning"
                }
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(items);

            // Act
            var result = await _service.GetUserRepetitionsAsync(userId);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Single(resultList);
            Assert.Equal("Unknown", resultList[0].VocabularyListName);
        }

        [Fact]
        public async Task GetUserRepetitionsAsync_WithDueItem_ShouldSetIsDueToTrue()
        {
            // Arrange
            var userId = 1;
            var now = DateTime.UtcNow;
            var pastDate = now.AddDays(-1);
            var items = new List<UserSpacedRepetition>
            {
                new UserSpacedRepetition
                {
                    UserSpacedRepetitionId = 1,
                    UserId = userId,
                    VocabularyListId = 1,
                    VocabularyList = new VocabularyList { Name = "Test" },
                    LastReviewedAt = now,
                    NextReviewAt = pastDate, // Past date - should be due
                    ReviewCount = 1,
                    Intervals = 1,
                    Status = "Learning"
                }
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(items);

            // Act
            var result = await _service.GetUserRepetitionsAsync(userId);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Single(resultList);
            Assert.True(resultList[0].IsDue);
        }

        [Fact]
        public async Task GetUserRepetitionsAsync_WithFutureNextReviewAt_ShouldSetIsDueToFalse()
        {
            // Arrange
            var userId = 1;
            var now = DateTime.UtcNow;
            var futureDate = now.AddDays(5);
            var items = new List<UserSpacedRepetition>
            {
                new UserSpacedRepetition
                {
                    UserSpacedRepetitionId = 1,
                    UserId = userId,
                    VocabularyListId = 1,
                    VocabularyList = new VocabularyList { Name = "Test" },
                    LastReviewedAt = now,
                    NextReviewAt = futureDate, // Future date - should not be due
                    ReviewCount = 1,
                    Intervals = 1,
                    Status = "Learning"
                }
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(items);

            // Act
            var result = await _service.GetUserRepetitionsAsync(userId);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Single(resultList);
            Assert.False(resultList[0].IsDue);
        }

        [Fact]
        public async Task GetUserRepetitionsAsync_WithNullNextReviewAt_ShouldSetIsDueToFalse()
        {
            // Arrange
            var userId = 1;
            var now = DateTime.UtcNow;
            var items = new List<UserSpacedRepetition>
            {
                new UserSpacedRepetition
                {
                    UserSpacedRepetitionId = 1,
                    UserId = userId,
                    VocabularyListId = 1,
                    VocabularyList = new VocabularyList { Name = "Test" },
                    LastReviewedAt = now,
                    NextReviewAt = null, // Null NextReviewAt
                    ReviewCount = 1,
                    Intervals = 1,
                    Status = "Learning"
                }
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(items);

            // Act
            var result = await _service.GetUserRepetitionsAsync(userId);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Single(resultList);
            Assert.False(resultList[0].IsDue);
            Assert.Equal(0, resultList[0].DaysUntilReview);
        }

        [Fact]
        public async Task GetUserRepetitionsAsync_WithNullValues_ShouldUseDefaults()
        {
            // Arrange
            var userId = 1;
            var now = DateTime.UtcNow;
            var items = new List<UserSpacedRepetition>
            {
                new UserSpacedRepetition
                {
                    UserSpacedRepetitionId = 1,
                    UserId = userId,
                    VocabularyListId = 1,
                    VocabularyList = new VocabularyList { Name = "Test" },
                    LastReviewedAt = now,
                    NextReviewAt = now.AddDays(1),
                    ReviewCount = null,
                    Intervals = null,
                    Status = null
                }
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(items);

            // Act
            var result = await _service.GetUserRepetitionsAsync(userId);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Single(resultList);
            Assert.Equal(0, resultList[0].ReviewCount);
            Assert.Equal(1, resultList[0].Intervals);
            Assert.Equal("New", resultList[0].Status);
        }

        [Fact]
        public async Task GetUserRepetitionsAsync_WithEmptyList_ShouldReturnEmpty()
        {
            // Arrange
            var userId = 1;
            var items = new List<UserSpacedRepetition>();

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(items);

            // Act
            var result = await _service.GetUserRepetitionsAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetUserRepetitionsAsync_WithMultipleItems_ShouldReturnAllItems()
        {
            // Arrange
            var userId = 1;
            var now = DateTime.UtcNow;
            var vocabularyList = new VocabularyList { VocabularyListId = 1, Name = "Test List" };
            var items = new List<UserSpacedRepetition>
            {
                new UserSpacedRepetition
                {
                    UserSpacedRepetitionId = 1,
                    UserId = userId,
                    VocabularyListId = 1,
                    VocabularyList = vocabularyList,
                    LastReviewedAt = now,
                    NextReviewAt = now.AddDays(1),
                    ReviewCount = 1,
                    Intervals = 1,
                    Status = "Learning"
                },
                new UserSpacedRepetition
                {
                    UserSpacedRepetitionId = 2,
                    UserId = userId,
                    VocabularyListId = 2,
                    VocabularyList = vocabularyList,
                    LastReviewedAt = now,
                    NextReviewAt = now.AddDays(-1), // Due
                    ReviewCount = 2,
                    Intervals = 2,
                    Status = "Learning"
                }
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(items);

            // Act
            var result = await _service.GetUserRepetitionsAsync(userId);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Equal(2, resultList.Count);
            Assert.False(resultList[0].IsDue); // Future date
            Assert.True(resultList[1].IsDue); // Past date
        }
    }
}


