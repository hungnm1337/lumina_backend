using Xunit;
using Moq;
using ServiceLayer.UserNote;
using RepositoryLayer.UserNote;
using DataLayer.DTOs.UserNote;
using System;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class GetUserNoteByIDUnitTest
    {
        private readonly Mock<IUserNoteRepository> _mockUserNoteRepository;
        private readonly UserNoteService _service;

        public GetUserNoteByIDUnitTest()
        {
            _mockUserNoteRepository = new Mock<IUserNoteRepository>();

            _service = new UserNoteService(
                _mockUserNoteRepository.Object
            );
        }

        #region Test Cases for Invalid UserNoteId

        [Fact]
        public async Task GetUserNoteByID_WhenUserNoteIdIsNegative_ShouldThrowArgumentException()
        {
            // Arrange
            int userNoteId = -1;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.GetUserNoteByID(userNoteId)
            );

            Assert.Equal("userNoteId", exception.ParamName);
            Assert.Contains("User Note ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockUserNoteRepository.Verify(
                repo => repo.GetUserNoteByID(It.IsAny<int>()),
                Times.Never
            );
        }

        [Fact]
        public async Task GetUserNoteByID_WhenUserNoteIdIsZero_ShouldThrowArgumentException()
        {
            // Arrange
            int userNoteId = 0;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.GetUserNoteByID(userNoteId)
            );

            Assert.Equal("userNoteId", exception.ParamName);
            Assert.Contains("User Note ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockUserNoteRepository.Verify(
                repo => repo.GetUserNoteByID(It.IsAny<int>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases for Valid UserNoteId

        [Fact]
        public async Task GetUserNoteByID_WhenUserNoteIdIsValidAndExists_ShouldReturnUserNoteResponseDTO()
        {
            // Arrange
            int userNoteId = 1;
            var expectedResult = new UserNoteResponseDTO
            {
                NoteId = 1,
                UserId = 1,
                User = "Test User",
                ArticleId = 1,
                Article = "Test Article",
                SectionId = 1,
                Section = "Test Section",
                NoteContent = "Test note content",
                CreateAt = DateTime.UtcNow,
                UpdateAt = null
            };

            _mockUserNoteRepository
                .Setup(repo => repo.GetUserNoteByID(userNoteId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetUserNoteByID(userNoteId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult.NoteId, result.NoteId);
            Assert.Equal(expectedResult.UserId, result.UserId);
            Assert.Equal(expectedResult.NoteContent, result.NoteContent);
            Assert.Equal(expectedResult.Article, result.Article);
            Assert.Equal(expectedResult.Section, result.Section);
            Assert.Null(result.UpdateAt);

            // Verify repository is called exactly once with correct userNoteId
            _mockUserNoteRepository.Verify(
                repo => repo.GetUserNoteByID(userNoteId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetUserNoteByID_WhenUserNoteIdIsValidButNotFound_ShouldReturnNull()
        {
            // Arrange
            int userNoteId = 999;

            _mockUserNoteRepository
                .Setup(repo => repo.GetUserNoteByID(userNoteId))
                .ReturnsAsync((UserNoteResponseDTO?)null);

            // Act
            var result = await _service.GetUserNoteByID(userNoteId);

            // Assert
            Assert.Null(result);

            // Verify repository is called exactly once
            _mockUserNoteRepository.Verify(
                repo => repo.GetUserNoteByID(userNoteId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetUserNoteByID_WhenUserNoteIdIsLarge_ShouldReturnUserNoteResponseDTO()
        {
            // Arrange
            int userNoteId = int.MaxValue;
            var expectedResult = new UserNoteResponseDTO
            {
                NoteId = int.MaxValue,
                UserId = 1,
                User = "Large User Note ID",
                ArticleId = 1,
                Article = "Test Article",
                SectionId = 1,
                Section = "Test Section",
                NoteContent = "Test note content",
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            };

            _mockUserNoteRepository
                .Setup(repo => repo.GetUserNoteByID(userNoteId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.GetUserNoteByID(userNoteId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult.NoteId, result.NoteId);
            Assert.Equal(expectedResult.UserId, result.UserId);
            Assert.NotNull(result.UpdateAt);

            // Verify repository is called exactly once
            _mockUserNoteRepository.Verify(
                repo => repo.GetUserNoteByID(userNoteId),
                Times.Once
            );
        }

        #endregion

        #region Test Cases - Repository Exception Handling

        [Fact]
        public async Task GetUserNoteByID_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            int userNoteId = 1;
            var exceptionMessage = "Database connection error";

            _mockUserNoteRepository
                .Setup(repo => repo.GetUserNoteByID(userNoteId))
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                async () => await _service.GetUserNoteByID(userNoteId)
            );

            Assert.Equal(exceptionMessage, exception.Message);

            // Verify repository is called exactly once
            _mockUserNoteRepository.Verify(
                repo => repo.GetUserNoteByID(userNoteId),
                Times.Once
            );
        }

        #endregion
    }
}

