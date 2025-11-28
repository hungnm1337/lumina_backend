using Xunit;
using Moq;
using ServiceLayer.Exam.Writting;
using RepositoryLayer.Exam.Writting;
using Microsoft.Extensions.Configuration;
using DataLayer.DTOs.UserAnswer;
using System;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class SaveWritingAnswerUnitTest
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IWrittingRepository> _mockWrittingRepository;
        private readonly WritingService _service;

        public SaveWritingAnswerUnitTest()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockWrittingRepository = new Mock<IWrittingRepository>();

            // Setup configuration for API key
            _mockConfiguration
                .Setup(c => c["Gemini:ApiKey"])
                .Returns("test-api-key");

            _service = new WritingService(
                _mockConfiguration.Object,
                _mockWrittingRepository.Object
            );
        }

        #region Test Cases for Null DTO

        [Fact]
        public async Task SaveWritingAnswer_WhenRequestIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            WritingAnswerRequestDTO? request = null;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _service.SaveWritingAnswer(request!)
            );

            Assert.Equal("writingAnswerRequestDTO", exception.ParamName);
            Assert.Contains("Request cannot be null", exception.Message);

            // Verify repository is never called
            _mockWrittingRepository.Verify(
                repo => repo.SaveWritingAnswer(It.IsAny<WritingAnswerRequestDTO>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases for Invalid Request Fields

        [Fact]
        public async Task SaveWritingAnswer_WhenAttemptIDIsInvalid_ShouldThrowArgumentException()
        {
            // Arrange
            var request = new WritingAnswerRequestDTO
            {
                AttemptID = 0,
                QuestionId = 1,
                UserAnswerContent = "Test answer",
                FeedbackFromAI = "Test feedback"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.SaveWritingAnswer(request)
            );

            Assert.Equal("AttemptID", exception.ParamName);
            Assert.Contains("Attempt ID must be greater than zero", exception.Message);

            // Verify repository is never called
            _mockWrittingRepository.Verify(
                repo => repo.SaveWritingAnswer(It.IsAny<WritingAnswerRequestDTO>()),
                Times.Never
            );
        }

        [Fact]
        public async Task SaveWritingAnswer_WhenQuestionIdIsInvalid_ShouldThrowArgumentException()
        {
            // Arrange
            var request = new WritingAnswerRequestDTO
            {
                AttemptID = 1,
                QuestionId = 0,
                UserAnswerContent = "Test answer",
                FeedbackFromAI = "Test feedback"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.SaveWritingAnswer(request)
            );

            Assert.Equal("QuestionId", exception.ParamName);
            Assert.Contains("Question ID must be greater than zero", exception.Message);
        }

        [Fact]
        public async Task SaveWritingAnswer_WhenUserAnswerContentIsEmpty_ShouldThrowArgumentException()
        {
            // Arrange
            var request = new WritingAnswerRequestDTO
            {
                AttemptID = 1,
                QuestionId = 1,
                UserAnswerContent = string.Empty,
                FeedbackFromAI = "Test feedback"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.SaveWritingAnswer(request)
            );

            Assert.Equal("UserAnswerContent", exception.ParamName);
            Assert.Contains("User Answer Content cannot be empty", exception.Message);
        }

        [Fact]
        public async Task SaveWritingAnswer_WhenUserAnswerContentIsNull_ShouldThrowArgumentException()
        {
            // Arrange
            var request = new WritingAnswerRequestDTO
            {
                AttemptID = 1,
                QuestionId = 1,
                UserAnswerContent = null!,
                FeedbackFromAI = "Test feedback"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.SaveWritingAnswer(request)
            );

            Assert.Equal("UserAnswerContent", exception.ParamName);
            Assert.Contains("User Answer Content cannot be empty", exception.Message);
        }

        [Fact]
        public async Task SaveWritingAnswer_WhenUserAnswerContentIsWhitespace_ShouldThrowArgumentException()
        {
            // Arrange
            var request = new WritingAnswerRequestDTO
            {
                AttemptID = 1,
                QuestionId = 1,
                UserAnswerContent = "   ",
                FeedbackFromAI = "Test feedback"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.SaveWritingAnswer(request)
            );

            Assert.Equal("UserAnswerContent", exception.ParamName);
            Assert.Contains("User Answer Content cannot be empty", exception.Message);
        }

        #endregion

        #region Test Cases for Valid Request - Success

        [Fact]
        public async Task SaveWritingAnswer_WhenValidAndRepositoryReturnsTrue_ShouldReturnTrue()
        {
            // Arrange
            var request = new WritingAnswerRequestDTO
            {
                AttemptID = 1,
                QuestionId = 1,
                UserAnswerContent = "This is a valid answer",
                FeedbackFromAI = "Good feedback"
            };

            _mockWrittingRepository
                .Setup(repo => repo.SaveWritingAnswer(It.IsAny<WritingAnswerRequestDTO>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.SaveWritingAnswer(request);

            // Assert
            Assert.True(result);

            // Verify repository is called exactly once
            _mockWrittingRepository.Verify(
                repo => repo.SaveWritingAnswer(It.Is<WritingAnswerRequestDTO>(
                    dto => dto.AttemptID == request.AttemptID &&
                           dto.QuestionId == request.QuestionId &&
                           dto.UserAnswerContent == request.UserAnswerContent
                )),
                Times.Once
            );
        }

        [Fact]
        public async Task SaveWritingAnswer_WhenValidAndRepositoryReturnsFalse_ShouldReturnFalse()
        {
            // Arrange
            var request = new WritingAnswerRequestDTO
            {
                AttemptID = 1,
                QuestionId = 1,
                UserAnswerContent = "This is a valid answer",
                FeedbackFromAI = "Good feedback"
            };

            _mockWrittingRepository
                .Setup(repo => repo.SaveWritingAnswer(It.IsAny<WritingAnswerRequestDTO>()))
                .ReturnsAsync(false);

            // Act
            var result = await _service.SaveWritingAnswer(request);

            // Assert
            Assert.False(result);

            // Verify repository is called exactly once
            _mockWrittingRepository.Verify(
                repo => repo.SaveWritingAnswer(It.IsAny<WritingAnswerRequestDTO>()),
                Times.Once
            );
        }

        #endregion

        #region Test Cases for Exception Handling

        [Fact]
        public async Task SaveWritingAnswer_WhenRepositoryThrowsException_ShouldReturnFalse()
        {
            // Arrange
            var request = new WritingAnswerRequestDTO
            {
                AttemptID = 1,
                QuestionId = 1,
                UserAnswerContent = "This is a valid answer",
                FeedbackFromAI = "Good feedback"
            };

            _mockWrittingRepository
                .Setup(repo => repo.SaveWritingAnswer(It.IsAny<WritingAnswerRequestDTO>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _service.SaveWritingAnswer(request);

            // Assert
            Assert.False(result);

            // Verify repository is called exactly once
            _mockWrittingRepository.Verify(
                repo => repo.SaveWritingAnswer(It.IsAny<WritingAnswerRequestDTO>()),
                Times.Once
            );
        }

        [Fact]
        public async Task SaveWritingAnswer_WhenRepositoryThrowsArgumentException_ShouldReturnFalse()
        {
            // Arrange
            var request = new WritingAnswerRequestDTO
            {
                AttemptID = 1,
                QuestionId = 1,
                UserAnswerContent = "This is a valid answer",
                FeedbackFromAI = "Good feedback"
            };

            _mockWrittingRepository
                .Setup(repo => repo.SaveWritingAnswer(It.IsAny<WritingAnswerRequestDTO>()))
                .ThrowsAsync(new ArgumentException("Invalid argument"));

            // Act
            var result = await _service.SaveWritingAnswer(request);

            // Assert
            Assert.False(result);

            // Verify repository is called exactly once
            _mockWrittingRepository.Verify(
                repo => repo.SaveWritingAnswer(It.IsAny<WritingAnswerRequestDTO>()),
                Times.Once
            );
        }

        #endregion
    }
}

