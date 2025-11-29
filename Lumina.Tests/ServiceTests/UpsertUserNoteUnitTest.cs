using Xunit;
using Moq;
using ServiceLayer.UserNote;
using RepositoryLayer.UserNote;
using DataLayer.DTOs.UserNote;
using System;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class UpsertUserNoteUnitTest
    {
        private readonly Mock<IUserNoteRepository> _mockUserNoteRepository;
        private readonly UserNoteService _service;

        public UpsertUserNoteUnitTest()
        {
            _mockUserNoteRepository = new Mock<IUserNoteRepository>();

            _service = new UserNoteService(
                _mockUserNoteRepository.Object
            );
        }

        #region Test Cases for Null DTO

        [Fact]
        public async Task UpsertUserNote_WhenDTOIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            UserNoteRequestDTO? userNoteRequestDTO = null;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _service.UpsertUserNote(userNoteRequestDTO!)
            );

            Assert.Equal("userNoteRequestDTO", exception.ParamName);

            // Verify repository is never called
            _mockUserNoteRepository.Verify(
                repo => repo.CheckUserNoteExist(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases for Invalid DTO Properties

        [Fact]
        public async Task UpsertUserNote_WhenUserIdIsInvalid_ShouldThrowArgumentException()
        {
            // Arrange
            var userNoteRequestDTO = new UserNoteRequestDTO
            {
                UserId = -1,
                ArticleId = 1,
                SectionId = 1,
                NoteContent = "Test content"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.UpsertUserNote(userNoteRequestDTO)
            );

            Assert.Equal("userNoteRequestDTO", exception.ParamName);
            Assert.Contains("User ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockUserNoteRepository.Verify(
                repo => repo.CheckUserNoteExist(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never
            );
        }

        [Fact]
        public async Task UpsertUserNote_WhenArticleIdIsInvalid_ShouldThrowArgumentException()
        {
            // Arrange
            var userNoteRequestDTO = new UserNoteRequestDTO
            {
                UserId = 1,
                ArticleId = 0,
                SectionId = 1,
                NoteContent = "Test content"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.UpsertUserNote(userNoteRequestDTO)
            );

            Assert.Equal("userNoteRequestDTO", exception.ParamName);
            Assert.Contains("Article ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockUserNoteRepository.Verify(
                repo => repo.CheckUserNoteExist(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never
            );
        }

        [Fact]
        public async Task UpsertUserNote_WhenSectionIdIsInvalid_ShouldThrowArgumentException()
        {
            // Arrange
            var userNoteRequestDTO = new UserNoteRequestDTO
            {
                UserId = 1,
                ArticleId = 1,
                SectionId = -1,
                NoteContent = "Test content"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.UpsertUserNote(userNoteRequestDTO)
            );

            Assert.Equal("userNoteRequestDTO", exception.ParamName);
            Assert.Contains("Section ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockUserNoteRepository.Verify(
                repo => repo.CheckUserNoteExist(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases - UserNote Exists (Update Path)

        [Fact]
        public async Task UpsertUserNote_WhenUserNoteExists_ShouldCallUpdateUserNoteAndReturnTrue()
        {
            // Arrange
            var userNoteRequestDTO = new UserNoteRequestDTO
            {
                UserId = 1,
                ArticleId = 1,
                SectionId = 1,
                NoteContent = "Updated content"
            };

            _mockUserNoteRepository
                .Setup(repo => repo.CheckUserNoteExist(userNoteRequestDTO.UserId, userNoteRequestDTO.ArticleId, userNoteRequestDTO.SectionId))
                .ReturnsAsync(true);

            _mockUserNoteRepository
                .Setup(repo => repo.UpdateUserNote(userNoteRequestDTO))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpsertUserNote(userNoteRequestDTO);

            // Assert
            Assert.True(result);

            // Verify CheckUserNoteExist is called exactly once
            _mockUserNoteRepository.Verify(
                repo => repo.CheckUserNoteExist(userNoteRequestDTO.UserId, userNoteRequestDTO.ArticleId, userNoteRequestDTO.SectionId),
                Times.Once
            );

            // Verify UpdateUserNote is called exactly once
            _mockUserNoteRepository.Verify(
                repo => repo.UpdateUserNote(userNoteRequestDTO),
                Times.Once
            );

            // Verify AddNewUserNote is never called
            _mockUserNoteRepository.Verify(
                repo => repo.AddNewUserNote(It.IsAny<UserNoteRequestDTO>()),
                Times.Never
            );
        }

        [Fact]
        public async Task UpsertUserNote_WhenUserNoteExistsAndUpdateReturnsFalse_ShouldReturnFalse()
        {
            // Arrange
            var userNoteRequestDTO = new UserNoteRequestDTO
            {
                UserId = 1,
                ArticleId = 1,
                SectionId = 1,
                NoteContent = "Updated content"
            };

            _mockUserNoteRepository
                .Setup(repo => repo.CheckUserNoteExist(userNoteRequestDTO.UserId, userNoteRequestDTO.ArticleId, userNoteRequestDTO.SectionId))
                .ReturnsAsync(true);

            _mockUserNoteRepository
                .Setup(repo => repo.UpdateUserNote(userNoteRequestDTO))
                .ReturnsAsync(false);

            // Act
            var result = await _service.UpsertUserNote(userNoteRequestDTO);

            // Assert
            Assert.False(result);

            // Verify UpdateUserNote is called exactly once
            _mockUserNoteRepository.Verify(
                repo => repo.UpdateUserNote(userNoteRequestDTO),
                Times.Once
            );

            // Verify AddNewUserNote is never called
            _mockUserNoteRepository.Verify(
                repo => repo.AddNewUserNote(It.IsAny<UserNoteRequestDTO>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases - UserNote Does Not Exist (Add Path)

        [Fact]
        public async Task UpsertUserNote_WhenUserNoteDoesNotExist_ShouldCallAddNewUserNoteAndReturnTrue()
        {
            // Arrange
            var userNoteRequestDTO = new UserNoteRequestDTO
            {
                UserId = 1,
                ArticleId = 1,
                SectionId = 1,
                NoteContent = "New content"
            };

            _mockUserNoteRepository
                .Setup(repo => repo.CheckUserNoteExist(userNoteRequestDTO.UserId, userNoteRequestDTO.ArticleId, userNoteRequestDTO.SectionId))
                .ReturnsAsync(false);

            _mockUserNoteRepository
                .Setup(repo => repo.AddNewUserNote(userNoteRequestDTO))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpsertUserNote(userNoteRequestDTO);

            // Assert
            Assert.True(result);

            // Verify CheckUserNoteExist is called exactly once
            _mockUserNoteRepository.Verify(
                repo => repo.CheckUserNoteExist(userNoteRequestDTO.UserId, userNoteRequestDTO.ArticleId, userNoteRequestDTO.SectionId),
                Times.Once
            );

            // Verify AddNewUserNote is called exactly once
            _mockUserNoteRepository.Verify(
                repo => repo.AddNewUserNote(userNoteRequestDTO),
                Times.Once
            );

            // Verify UpdateUserNote is never called
            _mockUserNoteRepository.Verify(
                repo => repo.UpdateUserNote(It.IsAny<UserNoteRequestDTO>()),
                Times.Never
            );
        }

        [Fact]
        public async Task UpsertUserNote_WhenUserNoteDoesNotExistAndAddReturnsFalse_ShouldReturnFalse()
        {
            // Arrange
            var userNoteRequestDTO = new UserNoteRequestDTO
            {
                UserId = 1,
                ArticleId = 1,
                SectionId = 1,
                NoteContent = "New content"
            };

            _mockUserNoteRepository
                .Setup(repo => repo.CheckUserNoteExist(userNoteRequestDTO.UserId, userNoteRequestDTO.ArticleId, userNoteRequestDTO.SectionId))
                .ReturnsAsync(false);

            _mockUserNoteRepository
                .Setup(repo => repo.AddNewUserNote(userNoteRequestDTO))
                .ReturnsAsync(false);

            // Act
            var result = await _service.UpsertUserNote(userNoteRequestDTO);

            // Assert
            Assert.False(result);

            // Verify AddNewUserNote is called exactly once
            _mockUserNoteRepository.Verify(
                repo => repo.AddNewUserNote(userNoteRequestDTO),
                Times.Once
            );

            // Verify UpdateUserNote is never called
            _mockUserNoteRepository.Verify(
                repo => repo.UpdateUserNote(It.IsAny<UserNoteRequestDTO>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases - Repository Exception Handling

        [Fact]
        public async Task UpsertUserNote_WhenCheckUserNoteExistThrowsException_ShouldPropagateException()
        {
            // Arrange
            var userNoteRequestDTO = new UserNoteRequestDTO
            {
                UserId = 1,
                ArticleId = 1,
                SectionId = 1,
                NoteContent = "Test content"
            };

            var exceptionMessage = "Database connection error";

            _mockUserNoteRepository
                .Setup(repo => repo.CheckUserNoteExist(userNoteRequestDTO.UserId, userNoteRequestDTO.ArticleId, userNoteRequestDTO.SectionId))
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                async () => await _service.UpsertUserNote(userNoteRequestDTO)
            );

            Assert.Equal(exceptionMessage, exception.Message);

            // Verify CheckUserNoteExist is called exactly once
            _mockUserNoteRepository.Verify(
                repo => repo.CheckUserNoteExist(userNoteRequestDTO.UserId, userNoteRequestDTO.ArticleId, userNoteRequestDTO.SectionId),
                Times.Once
            );

            // Verify UpdateUserNote and AddNewUserNote are never called
            _mockUserNoteRepository.Verify(
                repo => repo.UpdateUserNote(It.IsAny<UserNoteRequestDTO>()),
                Times.Never
            );

            _mockUserNoteRepository.Verify(
                repo => repo.AddNewUserNote(It.IsAny<UserNoteRequestDTO>()),
                Times.Never
            );
        }

        [Fact]
        public async Task UpsertUserNote_WhenUpdateUserNoteThrowsException_ShouldPropagateException()
        {
            // Arrange
            var userNoteRequestDTO = new UserNoteRequestDTO
            {
                UserId = 1,
                ArticleId = 1,
                SectionId = 1,
                NoteContent = "Updated content"
            };

            var exceptionMessage = "Update failed";

            _mockUserNoteRepository
                .Setup(repo => repo.CheckUserNoteExist(userNoteRequestDTO.UserId, userNoteRequestDTO.ArticleId, userNoteRequestDTO.SectionId))
                .ReturnsAsync(true);

            _mockUserNoteRepository
                .Setup(repo => repo.UpdateUserNote(userNoteRequestDTO))
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                async () => await _service.UpsertUserNote(userNoteRequestDTO)
            );

            Assert.Equal(exceptionMessage, exception.Message);

            // Verify UpdateUserNote is called exactly once
            _mockUserNoteRepository.Verify(
                repo => repo.UpdateUserNote(userNoteRequestDTO),
                Times.Once
            );
        }

        [Fact]
        public async Task UpsertUserNote_WhenAddNewUserNoteThrowsException_ShouldPropagateException()
        {
            // Arrange
            var userNoteRequestDTO = new UserNoteRequestDTO
            {
                UserId = 1,
                ArticleId = 1,
                SectionId = 1,
                NoteContent = "New content"
            };

            var exceptionMessage = "Add failed";

            _mockUserNoteRepository
                .Setup(repo => repo.CheckUserNoteExist(userNoteRequestDTO.UserId, userNoteRequestDTO.ArticleId, userNoteRequestDTO.SectionId))
                .ReturnsAsync(false);

            _mockUserNoteRepository
                .Setup(repo => repo.AddNewUserNote(userNoteRequestDTO))
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                async () => await _service.UpsertUserNote(userNoteRequestDTO)
            );

            Assert.Equal(exceptionMessage, exception.Message);

            // Verify AddNewUserNote is called exactly once
            _mockUserNoteRepository.Verify(
                repo => repo.AddNewUserNote(userNoteRequestDTO),
                Times.Once
            );
        }

        #endregion
    }
}

