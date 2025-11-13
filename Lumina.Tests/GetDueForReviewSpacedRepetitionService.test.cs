using DataLayer.DTOs.Vocabulary;
using DataLayer.Models;
using Moq;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer.UserSpacedRepetition;
using ServiceLayer.Vocabulary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Lumina.Tests
{
    public class GetDueForReviewSpacedRepetitionServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserSpacedRepetitionRepository> _mockUserSpacedRepetitionRepository;
        private readonly SpacedRepetitionService _service;

        public GetDueForReviewSpacedRepetitionServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUserSpacedRepetitionRepository = new Mock<IUserSpacedRepetitionRepository>();
            _mockUnitOfWork.Setup(u => u.UserSpacedRepetitions).Returns(_mockUserSpacedRepetitionRepository.Object);
            _service = new SpacedRepetitionService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task GetDueForReviewAsync_WithValidUserId_ShouldReturnDueItems()
        {
            // Arrange
            var userId = 1;
            var now = DateTime.UtcNow;
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = 1,
                Name = "Test List"
            };
            var dueItems = new List<UserSpacedRepetition>
            {
                new UserSpacedRepetition
                {
                    UserSpacedRepetitionId = 1,
                    UserId = userId,
                    VocabularyListId = 1,
                    VocabularyList = vocabularyList,
                    LastReviewedAt = now.AddDays(-2),
                    NextReviewAt = now.AddDays(-1),
                    ReviewCount = 1,
                    Intervals = 1,
                    Status = "Learning"
                }
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetDueForReviewAsync(userId))
                .ReturnsAsync(dueItems);

            // Act
            var result = await _service.GetDueForReviewAsync(userId);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Single(resultList);
            Assert.Equal(1, resultList[0].UserSpacedRepetitionId);
            Assert.Equal(userId, resultList[0].UserId);
            Assert.Equal(1, resultList[0].VocabularyListId);
            Assert.Equal("Test List", resultList[0].VocabularyListName);
            Assert.True(resultList[0].IsDue);
            _mockUserSpacedRepetitionRepository.Verify(r => r.GetDueForReviewAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetDueForReviewAsync_WithNullVocabularyList_ShouldUseUnknown()
        {
            // Arrange
            var userId = 1;
            var now = DateTime.UtcNow;
            var dueItems = new List<UserSpacedRepetition>
            {
                new UserSpacedRepetition
                {
                    UserSpacedRepetitionId = 1,
                    UserId = userId,
                    VocabularyListId = 1,
                    VocabularyList = null, // Null vocabulary list
                    LastReviewedAt = now.AddDays(-2),
                    NextReviewAt = now.AddDays(-1),
                    ReviewCount = 1,
                    Intervals = 1,
                    Status = "Learning"
                }
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetDueForReviewAsync(userId))
                .ReturnsAsync(dueItems);

            // Act
            var result = await _service.GetDueForReviewAsync(userId);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Single(resultList);
            Assert.Equal("Unknown", resultList[0].VocabularyListName);
        }

        [Fact]
        public async Task GetDueForReviewAsync_WithNullNextReviewAt_ShouldSetDaysUntilReviewToZero()
        {
            // Arrange
            var userId = 1;
            var dueItems = new List<UserSpacedRepetition>
            {
                new UserSpacedRepetition
                {
                    UserSpacedRepetitionId = 1,
                    UserId = userId,
                    VocabularyListId = 1,
                    VocabularyList = new VocabularyList { Name = "Test" },
                    LastReviewedAt = DateTime.UtcNow,
                    NextReviewAt = null, // Null NextReviewAt
                    ReviewCount = 1,
                    Intervals = 1,
                    Status = "Learning"
                }
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetDueForReviewAsync(userId))
                .ReturnsAsync(dueItems);

            // Act
            var result = await _service.GetDueForReviewAsync(userId);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Single(resultList);
            Assert.Equal(0, resultList[0].DaysUntilReview);
        }

        [Fact]
        public async Task GetDueForReviewAsync_WithNullReviewCount_ShouldUseZero()
        {
            // Arrange
            var userId = 1;
            var now = DateTime.UtcNow;
            var dueItems = new List<UserSpacedRepetition>
            {
                new UserSpacedRepetition
                {
                    UserSpacedRepetitionId = 1,
                    UserId = userId,
                    VocabularyListId = 1,
                    VocabularyList = new VocabularyList { Name = "Test" },
                    LastReviewedAt = now,
                    NextReviewAt = now.AddDays(1),
                    ReviewCount = null, // Null ReviewCount
                    Intervals = 1,
                    Status = "Learning"
                }
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetDueForReviewAsync(userId))
                .ReturnsAsync(dueItems);

            // Act
            var result = await _service.GetDueForReviewAsync(userId);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Single(resultList);
            Assert.Equal(0, resultList[0].ReviewCount);
        }

        [Fact]
        public async Task GetDueForReviewAsync_WithNullIntervals_ShouldUseOne()
        {
            // Arrange
            var userId = 1;
            var now = DateTime.UtcNow;
            var dueItems = new List<UserSpacedRepetition>
            {
                new UserSpacedRepetition
                {
                    UserSpacedRepetitionId = 1,
                    UserId = userId,
                    VocabularyListId = 1,
                    VocabularyList = new VocabularyList { Name = "Test" },
                    LastReviewedAt = now,
                    NextReviewAt = now.AddDays(1),
                    ReviewCount = 1,
                    Intervals = null, // Null Intervals
                    Status = "Learning"
                }
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetDueForReviewAsync(userId))
                .ReturnsAsync(dueItems);

            // Act
            var result = await _service.GetDueForReviewAsync(userId);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Single(resultList);
            Assert.Equal(1, resultList[0].Intervals);
        }

        [Fact]
        public async Task GetDueForReviewAsync_WithNullStatus_ShouldUseNew()
        {
            // Arrange
            var userId = 1;
            var now = DateTime.UtcNow;
            var dueItems = new List<UserSpacedRepetition>
            {
                new UserSpacedRepetition
                {
                    UserSpacedRepetitionId = 1,
                    UserId = userId,
                    VocabularyListId = 1,
                    VocabularyList = new VocabularyList { Name = "Test" },
                    LastReviewedAt = now,
                    NextReviewAt = now.AddDays(1),
                    ReviewCount = 1,
                    Intervals = 1,
                    Status = null // Null Status
                }
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetDueForReviewAsync(userId))
                .ReturnsAsync(dueItems);

            // Act
            var result = await _service.GetDueForReviewAsync(userId);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Single(resultList);
            Assert.Equal("New", resultList[0].Status);
        }

        [Fact]
        public async Task GetDueForReviewAsync_WithEmptyList_ShouldReturnEmpty()
        {
            // Arrange
            var userId = 1;
            var dueItems = new List<UserSpacedRepetition>();

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetDueForReviewAsync(userId))
                .ReturnsAsync(dueItems);

            // Act
            var result = await _service.GetDueForReviewAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetDueForReviewAsync_WithMultipleItems_ShouldReturnAllItems()
        {
            // Arrange
            var userId = 1;
            var now = DateTime.UtcNow;
            var vocabularyList = new VocabularyList { VocabularyListId = 1, Name = "Test List" };
            var dueItems = new List<UserSpacedRepetition>
            {
                new UserSpacedRepetition
                {
                    UserSpacedRepetitionId = 1,
                    UserId = userId,
                    VocabularyListId = 1,
                    VocabularyList = vocabularyList,
                    LastReviewedAt = now.AddDays(-2),
                    NextReviewAt = now.AddDays(-1),
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
                    LastReviewedAt = now.AddDays(-3),
                    NextReviewAt = now.AddDays(-2),
                    ReviewCount = 2,
                    Intervals = 1,
                    Status = "Learning"
                }
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetDueForReviewAsync(userId))
                .ReturnsAsync(dueItems);

            // Act
            var result = await _service.GetDueForReviewAsync(userId);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Equal(2, resultList.Count);
            Assert.All(resultList, item => Assert.True(item.IsDue));
        }

        [Fact]
        public async Task GetDueForReviewAsync_WithPastNextReviewAt_ShouldCalculateDaysUntilReview()
        {
            // Arrange
            var userId = 1;
            var now = DateTime.UtcNow;
            var pastDate = now.AddDays(-5);
            var dueItems = new List<UserSpacedRepetition>
            {
                new UserSpacedRepetition
                {
                    UserSpacedRepetitionId = 1,
                    UserId = userId,
                    VocabularyListId = 1,
                    VocabularyList = new VocabularyList { Name = "Test" },
                    LastReviewedAt = now,
                    NextReviewAt = pastDate,
                    ReviewCount = 1,
                    Intervals = 1,
                    Status = "Learning"
                }
            };

            _mockUserSpacedRepetitionRepository
                .Setup(r => r.GetDueForReviewAsync(userId))
                .ReturnsAsync(dueItems);

            // Act
            var result = await _service.GetDueForReviewAsync(userId);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Single(resultList);
            // DaysUntilReview should be 0 (max of 0 and negative days)
            Assert.Equal(0, resultList[0].DaysUntilReview);
        }
    }
}


