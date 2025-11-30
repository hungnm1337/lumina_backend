using Xunit;
using Moq;
using ServiceLayer.Vocabulary;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer.UserSpacedRepetition;
using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class GetUserRepetitionsAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserSpacedRepetitionRepository> _mockRepetitionRepository;
        private readonly SpacedRepetitionService _service;

        public GetUserRepetitionsAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockRepetitionRepository = new Mock<IUserSpacedRepetitionRepository>();

            _mockUnitOfWork.Setup(u => u.UserSpacedRepetitions).Returns(_mockRepetitionRepository.Object);

            _service = new SpacedRepetitionService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task GetUserRepetitionsAsync_WhenItemsExist_ShouldReturnAllItems()
        {
            // Arrange
            var items = new List<UserSpacedRepetition>
            {
                new UserSpacedRepetition
                {
                    UserSpacedRepetitionId = 1,
                    UserId = 1,
                    VocabularyId = 1,
                    VocabularyListId = 1,
                    NextReviewAt = DateTime.UtcNow.AddDays(-1),
                    ReviewCount = 1,
                    Intervals = 1,
                    VocabularyList = new VocabularyList { Name = "Test List" }
                },
                new UserSpacedRepetition
                {
                    UserSpacedRepetitionId = 2,
                    UserId = 1,
                    VocabularyId = 2,
                    VocabularyListId = 1,
                    NextReviewAt = null,
                    ReviewCount = 0,
                    Intervals = 1,
                    VocabularyList = new VocabularyList { Name = "Test List" }
                }
            };

            _mockRepetitionRepository
                .Setup(repo => repo.GetByUserIdAsync(1))
                .ReturnsAsync(items);

            // Act
            var result = await _service.GetUserRepetitionsAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetUserRepetitionsAsync_WhenNoItems_ShouldReturnEmpty()
        {
            // Arrange
            _mockRepetitionRepository
                .Setup(repo => repo.GetByUserIdAsync(1))
                .ReturnsAsync(new List<UserSpacedRepetition>());

            // Act
            var result = await _service.GetUserRepetitionsAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}

