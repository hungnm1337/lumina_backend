using Xunit;
using Moq;
using ServiceLayer.Vocabulary;
using RepositoryLayer.UnitOfWork;
using RepositoryLayer;
using DataLayer.Models;
using System;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    /// <summary>
    /// Test cases for VocabularyListService.RequestApprovalAsync method
    /// Following AAA (Arrange-Act-Assert) pattern with Moq verification
    /// </summary>
    public class RequestApprovalAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IVocabularyListRepository> _mockVocabularyListRepository;
        private readonly VocabularyListService _service;

        public RequestApprovalAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockVocabularyListRepository = new Mock<IVocabularyListRepository>();
            _mockUnitOfWork.Setup(u => u.VocabularyLists).Returns(_mockVocabularyListRepository.Object);
            _service = new VocabularyListService(_mockUnitOfWork.Object);
        }

        #region ID Invalid Tests

        [Fact]
        public async Task RequestApprovalAsync_WhenListIdNotFound_ShouldReturnFalse()
        {
            // Arrange
            int listId = 999;
            int staffUserId = 1;
            _mockVocabularyListRepository.Setup(repo => repo.FindByIdAsync(listId)).ReturnsAsync((VocabularyList?)null);

            // Act
            var result = await _service.RequestApprovalAsync(listId, staffUserId);

            // Assert
            Assert.False(result);
            _mockVocabularyListRepository.Verify(repo => repo.FindByIdAsync(listId), Times.Once);
            _mockVocabularyListRepository.Verify(repo => repo.UpdateAsync(It.IsAny<VocabularyList>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-999)]
        public async Task RequestApprovalAsync_WhenListIdIsInvalidBoundary_ShouldReturnFalse(int invalidListId)
        {
            // Arrange
            _mockVocabularyListRepository.Setup(repo => repo.FindByIdAsync(invalidListId)).ReturnsAsync((VocabularyList?)null);

            // Act
            var result = await _service.RequestApprovalAsync(invalidListId, 1);

            // Assert
            Assert.False(result);
            _mockVocabularyListRepository.Verify(repo => repo.UpdateAsync(It.IsAny<VocabularyList>()), Times.Never);
        }

        #endregion

        #region Status Validation Tests

        [Theory]
        [InlineData("Published")]
        [InlineData("Pending")]
        [InlineData("SomeOtherStatus")]
        public async Task RequestApprovalAsync_WhenStatusIsNotDraftOrRejected_ShouldReturnFalse(string invalidStatus)
        {
            // Arrange
            var list = new VocabularyList { VocabularyListId = 1, Status = invalidStatus, MakeBy = 1, CreateAt = DateTime.UtcNow };
            _mockVocabularyListRepository.Setup(repo => repo.FindByIdAsync(1)).ReturnsAsync(list);

            // Act
            var result = await _service.RequestApprovalAsync(1, 1);

            // Assert
            Assert.False(result);
            _mockVocabularyListRepository.Verify(repo => repo.UpdateAsync(It.IsAny<VocabularyList>()), Times.Never);
        }

        #endregion

        #region Valid Success Tests

        [Theory]
        [InlineData("Draft")]
        [InlineData("Rejected")]
        public async Task RequestApprovalAsync_WhenStatusIsValid_ShouldUpdateToPendingAndReturnTrue(string validStatus)
        {
            // Arrange
            int listId = 1;
            int staffUserId = 5;
            var list = new VocabularyList
            {
                VocabularyListId = listId,
                Name = "Test List",
                Status = validStatus,
                IsPublic = false,
                MakeBy = 1,
                CreateAt = DateTime.UtcNow
            };

            _mockVocabularyListRepository.Setup(repo => repo.FindByIdAsync(listId)).ReturnsAsync(list);
            _mockVocabularyListRepository.Setup(repo => repo.UpdateAsync(It.IsAny<VocabularyList>())).ReturnsAsync((VocabularyList v) => v);
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(0);

            // Act
            var result = await _service.RequestApprovalAsync(listId, staffUserId);

            // Assert
            Assert.True(result);
            Assert.Equal("Pending", list.Status);
            Assert.False(list.IsPublic);
            Assert.Equal(staffUserId, list.UpdatedBy);
            Assert.NotEqual(default(DateTime), list.UpdateAt);

            // Verify repository calls
            _mockVocabularyListRepository.Verify(repo => repo.FindByIdAsync(listId), Times.Once);
            _mockVocabularyListRepository.Verify(
                repo => repo.UpdateAsync(It.Is<VocabularyList>(v =>
                    v.Status == "Pending" &&
                    v.IsPublic == false &&
                    v.UpdatedBy == staffUserId)),
                Times.Once);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task RequestApprovalAsync_WhenCalled_ShouldSetUpdateAtToCurrentTime()
        {
            // Arrange
            var beforeTime = DateTime.UtcNow;
            var list = new VocabularyList { VocabularyListId = 1, Status = "Draft", MakeBy = 1, CreateAt = DateTime.UtcNow.AddDays(-1) };
            _mockVocabularyListRepository.Setup(repo => repo.FindByIdAsync(1)).ReturnsAsync(list);
            _mockVocabularyListRepository.Setup(repo => repo.UpdateAsync(It.IsAny<VocabularyList>())).ReturnsAsync((VocabularyList v) => v);
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(0);

            // Act
            await _service.RequestApprovalAsync(1, 1);
            var afterTime = DateTime.UtcNow;

            // Assert
            Assert.True(list.UpdateAt >= beforeTime && list.UpdateAt <= afterTime);
        }

        [Fact]
        public async Task RequestApprovalAsync_WhenValidRequest_ShouldSetIsPublicToFalse()
        {
            // Arrange
            var list = new VocabularyList { VocabularyListId = 1, Status = "Draft", IsPublic = true, MakeBy = 1, CreateAt = DateTime.UtcNow };
            _mockVocabularyListRepository.Setup(repo => repo.FindByIdAsync(1)).ReturnsAsync(list);
            _mockVocabularyListRepository.Setup(repo => repo.UpdateAsync(It.IsAny<VocabularyList>())).ReturnsAsync((VocabularyList v) => v);
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(0);

            // Act
            await _service.RequestApprovalAsync(1, 1);

            // Assert
            Assert.False(list.IsPublic);
        }

        [Fact]
        public async Task RequestApprovalAsync_WhenRejectedListResubmitted_ShouldUpdateCorrectly()
        {
            // Arrange - List bị reject trước đó và được gửi lại
            var list = new VocabularyList
            {
                VocabularyListId = 1,
                Name = "Rejected List",
                Status = "Rejected",
                IsPublic = false,
                MakeBy = 1,
                CreateAt = DateTime.UtcNow,
                RejectionReason = "Previous rejection"
            };

            _mockVocabularyListRepository.Setup(repo => repo.FindByIdAsync(1)).ReturnsAsync(list);
            _mockVocabularyListRepository.Setup(repo => repo.UpdateAsync(It.IsAny<VocabularyList>())).ReturnsAsync((VocabularyList v) => v);
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(0);

            // Act
            var result = await _service.RequestApprovalAsync(1, 3);

            // Assert
            Assert.True(result);
            Assert.Equal("Pending", list.Status);
            Assert.Equal(3, list.UpdatedBy);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task RequestApprovalAsync_WhenStaffUserIdIsZero_ShouldStillProcess()
        {
            // Arrange
            var list = new VocabularyList { VocabularyListId = 1, Status = "Draft", MakeBy = 1, CreateAt = DateTime.UtcNow };
            _mockVocabularyListRepository.Setup(repo => repo.FindByIdAsync(1)).ReturnsAsync(list);
            _mockVocabularyListRepository.Setup(repo => repo.UpdateAsync(It.IsAny<VocabularyList>())).ReturnsAsync((VocabularyList v) => v);
            _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(0);

            // Act
            var result = await _service.RequestApprovalAsync(1, 0);

            // Assert
            Assert.True(result);
            Assert.Equal(0, list.UpdatedBy);
        }

        #endregion
    }
}
