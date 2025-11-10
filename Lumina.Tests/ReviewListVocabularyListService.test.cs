using DataLayer.Models;
using Moq;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.Vocabulary;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Lumina.Tests
{
    public class ReviewListVocabularyListServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IVocabularyListRepository> _mockVocabularyListRepository;
        private readonly VocabularyListService _service;

        public ReviewListVocabularyListServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockVocabularyListRepository = new Mock<IVocabularyListRepository>();
            _mockUnitOfWork.Setup(u => u.VocabularyLists).Returns(_mockVocabularyListRepository.Object);
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
            _service = new VocabularyListService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task ReviewListAsync_WithApproval_ShouldReturnTrue()
        {
            // Arrange
            var listId = 1;
            var managerUserId = 2;
            var isApproved = true;
            var comment = "Good list";
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                Name = "Pending List",
                Status = "Pending",
                IsPublic = false,
                MakeBy = 1,
                CreateAt = DateTime.UtcNow,
                IsDeleted = false,
                RejectionReason = "Previous reason"
            };

            _mockVocabularyListRepository
                .Setup(r => r.FindByIdAsync(listId))
                .ReturnsAsync(vocabularyList);
            _mockVocabularyListRepository
                .Setup(r => r.UpdateAsync(It.IsAny<VocabularyList>()))
                .ReturnsAsync((VocabularyList l) => l);

            // Act
            var result = await _service.ReviewListAsync(listId, isApproved, comment, managerUserId);

            // Assert
            Assert.True(result);
            Assert.Equal("Published", vocabularyList.Status);
            Assert.True(vocabularyList.IsPublic);
            Assert.Null(vocabularyList.RejectionReason);
            Assert.Equal(managerUserId, vocabularyList.UpdatedBy);
            Assert.NotNull(vocabularyList.UpdateAt);
            _mockVocabularyListRepository.Verify(r => r.FindByIdAsync(listId), Times.Once);
            _mockVocabularyListRepository.Verify(r => r.UpdateAsync(It.IsAny<VocabularyList>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task ReviewListAsync_WithRejection_ShouldReturnTrue()
        {
            // Arrange
            var listId = 1;
            var managerUserId = 2;
            var isApproved = false;
            var comment = "Needs improvement";
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                Name = "Pending List",
                Status = "Pending",
                IsPublic = false,
                MakeBy = 1,
                CreateAt = DateTime.UtcNow,
                IsDeleted = false,
                RejectionReason = null
            };

            _mockVocabularyListRepository
                .Setup(r => r.FindByIdAsync(listId))
                .ReturnsAsync(vocabularyList);
            _mockVocabularyListRepository
                .Setup(r => r.UpdateAsync(It.IsAny<VocabularyList>()))
                .ReturnsAsync((VocabularyList l) => l);

            // Act
            var result = await _service.ReviewListAsync(listId, isApproved, comment, managerUserId);

            // Assert
            Assert.True(result);
            Assert.Equal("Rejected", vocabularyList.Status);
            Assert.False(vocabularyList.IsPublic);
            Assert.Equal(comment, vocabularyList.RejectionReason);
            Assert.Equal(managerUserId, vocabularyList.UpdatedBy);
            Assert.NotNull(vocabularyList.UpdateAt);
        }

        [Fact]
        public async Task ReviewListAsync_WithNullComment_ShouldSetRejectionReasonToNull()
        {
            // Arrange
            var listId = 1;
            var managerUserId = 2;
            var isApproved = false;
            string? comment = null;
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                Name = "Pending List",
                Status = "Pending",
                IsPublic = false,
                RejectionReason = "Previous reason"
            };

            _mockVocabularyListRepository
                .Setup(r => r.FindByIdAsync(listId))
                .ReturnsAsync(vocabularyList);
            _mockVocabularyListRepository
                .Setup(r => r.UpdateAsync(It.IsAny<VocabularyList>()))
                .ReturnsAsync((VocabularyList l) => l);

            // Act
            var result = await _service.ReviewListAsync(listId, isApproved, comment, managerUserId);

            // Assert
            Assert.True(result);
            Assert.Equal("Rejected", vocabularyList.Status);
            Assert.Null(vocabularyList.RejectionReason);
        }

        [Fact]
        public async Task ReviewListAsync_WithNonPendingStatus_ShouldReturnFalse()
        {
            // Arrange
            var listId = 1;
            var managerUserId = 2;
            var isApproved = true;
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                Name = "Draft List",
                Status = "Draft" // Not Pending
            };

            _mockVocabularyListRepository
                .Setup(r => r.FindByIdAsync(listId))
                .ReturnsAsync(vocabularyList);

            // Act
            var result = await _service.ReviewListAsync(listId, isApproved, null, managerUserId);

            // Assert
            Assert.False(result);
            Assert.Equal("Draft", vocabularyList.Status);
            _mockVocabularyListRepository.Verify(r => r.FindByIdAsync(listId), Times.Once);
            _mockVocabularyListRepository.Verify(r => r.UpdateAsync(It.IsAny<VocabularyList>()), Times.Never);
        }

        [Fact]
        public async Task ReviewListAsync_WithNullVocabularyList_ShouldReturnFalse()
        {
            // Arrange
            var listId = 999;
            var managerUserId = 2;
            var isApproved = true;

            _mockVocabularyListRepository
                .Setup(r => r.FindByIdAsync(listId))
                .ReturnsAsync((VocabularyList?)null);

            // Act
            var result = await _service.ReviewListAsync(listId, isApproved, null, managerUserId);

            // Assert
            Assert.False(result);
            _mockVocabularyListRepository.Verify(r => r.FindByIdAsync(listId), Times.Once);
            _mockVocabularyListRepository.Verify(r => r.UpdateAsync(It.IsAny<VocabularyList>()), Times.Never);
        }

        [Fact]
        public async Task ReviewListAsync_WithApproval_ShouldClearRejectionReason()
        {
            // Arrange
            var listId = 1;
            var managerUserId = 2;
            var isApproved = true;
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                Name = "Pending List",
                Status = "Pending",
                IsPublic = false,
                RejectionReason = "Previous rejection reason"
            };

            _mockVocabularyListRepository
                .Setup(r => r.FindByIdAsync(listId))
                .ReturnsAsync(vocabularyList);
            _mockVocabularyListRepository
                .Setup(r => r.UpdateAsync(It.IsAny<VocabularyList>()))
                .ReturnsAsync((VocabularyList l) => l);

            // Act
            var result = await _service.ReviewListAsync(listId, isApproved, null, managerUserId);

            // Assert
            Assert.True(result);
            Assert.Null(vocabularyList.RejectionReason);
        }

        [Fact]
        public async Task ReviewListAsync_ShouldSetUpdatedByAndUpdateAt()
        {
            // Arrange
            var listId = 1;
            var managerUserId = 2;
            var isApproved = true;
            var vocabularyList = new VocabularyList
            {
                VocabularyListId = listId,
                Name = "Pending List",
                Status = "Pending",
                IsPublic = false,
                UpdatedBy = null,
                UpdateAt = null
            };

            _mockVocabularyListRepository
                .Setup(r => r.FindByIdAsync(listId))
                .ReturnsAsync(vocabularyList);
            _mockVocabularyListRepository
                .Setup(r => r.UpdateAsync(It.IsAny<VocabularyList>()))
                .ReturnsAsync((VocabularyList l) => l);

            // Act
            var result = await _service.ReviewListAsync(listId, isApproved, null, managerUserId);

            // Assert
            Assert.True(result);
            Assert.Equal(managerUserId, vocabularyList.UpdatedBy);
            Assert.NotNull(vocabularyList.UpdateAt);
        }
    }
}

