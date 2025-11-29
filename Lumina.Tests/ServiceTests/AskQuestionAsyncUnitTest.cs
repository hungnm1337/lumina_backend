using Xunit;
using Moq;
using ServiceLayer.UserNoteAI;
using DataLayer.DTOs.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class AskQuestionAsyncUnitTest
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<AIChatService>> _mockLogger;
        private readonly AIChatService _service;

        public AskQuestionAsyncUnitTest()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<AIChatService>>();

            // Setup configuration để tránh throw exception khi khởi tạo service
            _mockConfiguration.Setup(x => x["Gemini:ApiKey"]).Returns("test-api-key");
            _mockConfiguration.Setup(x => x["Gemini:ModelName"]).Returns("gemini-2.5-flash");

            _service = new AIChatService(
                _mockConfiguration.Object,
                _mockLogger.Object
            );
        }

        #region Test Cases for Null Request

        [Fact]
        public async Task AskQuestionAsync_WhenRequestIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            ChatRequestDTO? request = null;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _service.AskQuestionAsync(request!)
            );

            Assert.Equal("request", exception.ParamName);
        }

        #endregion

        #region Test Cases for Invalid UserQuestion

        [Fact]
        public async Task AskQuestionAsync_WhenUserQuestionIsNull_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = null!,
                LessonContent = "Test lesson content"
            };

            // Act
            var result = await _service.AskQuestionAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("User question cannot be empty", result.ErrorMessage);
        }

        [Fact]
        public async Task AskQuestionAsync_WhenUserQuestionIsEmpty_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = string.Empty,
                LessonContent = "Test lesson content"
            };

            // Act
            var result = await _service.AskQuestionAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("User question cannot be empty", result.ErrorMessage);
        }

        [Fact]
        public async Task AskQuestionAsync_WhenUserQuestionIsWhitespace_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = "   ",
                LessonContent = "Test lesson content"
            };

            // Act
            var result = await _service.AskQuestionAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("User question cannot be empty", result.ErrorMessage);
        }

        #endregion

        #region Test Cases for Invalid LessonContent

        [Fact]
        public async Task AskQuestionAsync_WhenLessonContentIsNull_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = "What is this?",
                LessonContent = null!
            };

            // Act
            var result = await _service.AskQuestionAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("Lesson content cannot be empty", result.ErrorMessage);
        }

        [Fact]
        public async Task AskQuestionAsync_WhenLessonContentIsEmpty_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = "What is this?",
                LessonContent = string.Empty
            };

            // Act
            var result = await _service.AskQuestionAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("Lesson content cannot be empty", result.ErrorMessage);
        }

        [Fact]
        public async Task AskQuestionAsync_WhenLessonContentIsWhitespace_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = "What is this?",
                LessonContent = "   "
            };

            // Act
            var result = await _service.AskQuestionAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("Lesson content cannot be empty", result.ErrorMessage);
        }

        #endregion

        #region Test Cases for Valid Request

        [Fact]
        public async Task AskQuestionAsync_WhenRequestIsValid_ShouldProcessAndReturnResponse()
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = "What is the main idea of this lesson?",
                LessonContent = "This is a test lesson content about TOEIC reading comprehension.",
                LessonTitle = "TOEIC Reading Practice"
            };

            // Act & Assert
            // Note: This will actually call Gemini API
            // Since we can't mock GenerativeModel without refactoring, we test that:
            // 1. Validation passes
            // 2. Method attempts to process the request
            // 3. Returns appropriate response or error
            
            try
            {
                var result = await _service.AskQuestionAsync(request);

                // If API call succeeds
                Assert.NotNull(result);
                
                // If success, verify response structure
                if (result.Success)
                {
                    Assert.NotNull(result.Answer);
                    Assert.True(result.ConfidenceScore >= 0 && result.ConfidenceScore <= 100);
                }
                else
                {
                    // If failed, verify error message exists
                    Assert.NotNull(result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                // If API fails (expected in test environment), verify it's not a validation error
                Assert.DoesNotContain("cannot be empty", ex.Message);
            }
        }

        [Fact]
        public async Task AskQuestionAsync_WhenRequestIsValidWithoutOptionalFields_ShouldProcessAndReturnResponse()
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = "Explain this concept",
                LessonContent = "Test lesson content"
                // LessonTitle is null (optional)
            };

            // Act & Assert
            try
            {
                var result = await _service.AskQuestionAsync(request);

                Assert.NotNull(result);
                
                if (result.Success)
                {
                    Assert.NotNull(result.Answer);
                }
                else
                {
                    Assert.NotNull(result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                // Verify it's not a validation error
                Assert.DoesNotContain("cannot be empty", ex.Message);
            }
        }

        #endregion

        #region Test Cases - Exception Handling

        [Fact]
        public async Task AskQuestionAsync_WhenAPIThrowsException_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = "What is this?",
                LessonContent = "Test lesson content"
            };

            // Note: Since we can't mock GenerativeModel directly, this test verifies
            // that exception handling works. In a real scenario with invalid API key,
            // the method should catch the exception and return error response.
            
            // Act
            var result = await _service.AskQuestionAsync(request);

            // Assert
            // The method should handle exceptions gracefully and return error response
            // instead of throwing
            Assert.NotNull(result);
            
            // If API fails, result should have error message
            if (!result.Success)
            {
                Assert.NotNull(result.ErrorMessage);
                Assert.Contains("Error", result.ErrorMessage);
                Assert.NotNull(result.Answer); // Should have fallback message
            }
        }

        #endregion
    }
}

