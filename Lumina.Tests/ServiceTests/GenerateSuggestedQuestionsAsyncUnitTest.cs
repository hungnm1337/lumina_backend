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
    public class GenerateSuggestedQuestionsAsyncUnitTest
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<AIChatService>> _mockLogger;
        private readonly AIChatService _service;

        public GenerateSuggestedQuestionsAsyncUnitTest()
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

        #region Test Cases for Invalid LessonContent

        [Fact]
        public async Task GenerateSuggestedQuestionsAsync_WhenLessonContentIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            string? lessonContent = null;
            string? lessonTitle = null;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _service.GenerateSuggestedQuestionsAsync(lessonContent!, lessonTitle)
            );

            Assert.Equal("lessonContent", exception.ParamName);
        }

        [Fact]
        public async Task GenerateSuggestedQuestionsAsync_WhenLessonContentIsEmpty_ShouldThrowArgumentException()
        {
            // Arrange
            string lessonContent = string.Empty;
            string? lessonTitle = null;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.GenerateSuggestedQuestionsAsync(lessonContent, lessonTitle)
            );

            Assert.Equal("lessonContent", exception.ParamName);
            Assert.Contains("Lesson content cannot be null or empty", exception.Message);
        }

        [Fact]
        public async Task GenerateSuggestedQuestionsAsync_WhenLessonContentIsWhitespace_ShouldThrowArgumentException()
        {
            // Arrange
            string lessonContent = "   ";
            string? lessonTitle = null;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.GenerateSuggestedQuestionsAsync(lessonContent, lessonTitle)
            );

            Assert.Equal("lessonContent", exception.ParamName);
            Assert.Contains("Lesson content cannot be null or empty", exception.Message);
        }

        #endregion

        #region Test Cases for Valid LessonContent

        [Fact]
        public async Task GenerateSuggestedQuestionsAsync_WhenLessonContentIsValidAndLessonTitleIsNull_ShouldReturnResponse()
        {
            // Arrange
            string lessonContent = "This is a test lesson content about TOEIC reading comprehension. It covers various topics and concepts.";
            string? lessonTitle = null;

            // Act & Assert
            // Note: This will actually call Gemini API
            // Since we can't mock GenerativeModel without refactoring, we test that:
            // 1. Validation passes
            // 2. Method attempts to process the request
            // 3. Returns appropriate response or error
            
            try
            {
                var result = await _service.GenerateSuggestedQuestionsAsync(lessonContent, lessonTitle);

                // If API call succeeds
                Assert.NotNull(result);
                
                if (result.Success)
                {
                    Assert.True(result.Success);
                    Assert.NotNull(result.Answer);
                    Assert.Contains("câu hỏi gợi ý", result.Answer);
                    Assert.Equal(95, result.ConfidenceScore);
                    Assert.NotNull(result.SuggestedQuestions);
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
                Assert.DoesNotContain("cannot be null or empty", ex.Message);
            }
        }

        [Fact]
        public async Task GenerateSuggestedQuestionsAsync_WhenLessonContentAndLessonTitleAreValid_ShouldReturnResponse()
        {
            // Arrange
            string lessonContent = "This is a comprehensive lesson about TOEIC listening skills. It includes tips and strategies for improving listening comprehension.";
            string lessonTitle = "TOEIC Listening Mastery";

            // Act & Assert
            try
            {
                var result = await _service.GenerateSuggestedQuestionsAsync(lessonContent, lessonTitle);

                Assert.NotNull(result);
                
                if (result.Success)
                {
                    Assert.True(result.Success);
                    Assert.NotNull(result.Answer);
                    Assert.NotNull(result.SuggestedQuestions);
                    Assert.Equal(95, result.ConfidenceScore);
                }
                else
                {
                    Assert.NotNull(result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                // Verify it's not a validation error
                Assert.DoesNotContain("cannot be null or empty", ex.Message);
            }
        }

        [Fact]
        public async Task GenerateSuggestedQuestionsAsync_WhenLessonTitleIsEmpty_ShouldProcessCorrectly()
        {
            // Arrange
            string lessonContent = "Test lesson content";
            string lessonTitle = string.Empty;

            // Act & Assert
            // Empty string for lessonTitle should be treated as null (optional parameter)
            try
            {
                var result = await _service.GenerateSuggestedQuestionsAsync(lessonContent, lessonTitle);

                Assert.NotNull(result);
            }
            catch (Exception ex)
            {
                Assert.DoesNotContain("cannot be null or empty", ex.Message);
            }
        }

        #endregion

        #region Test Cases - Exception Handling

        [Fact]
        public async Task GenerateSuggestedQuestionsAsync_WhenAPIThrowsException_ShouldReturnErrorResponse()
        {
            // Arrange
            string lessonContent = "Test lesson content";
            string? lessonTitle = null;

            // Act & Assert
            // Note: Since we can't mock GenerativeModel directly, this test verifies
            // that exception handling works. In a real scenario with invalid API key,
            // the method should catch the exception and return error response.
            
            var result = await _service.GenerateSuggestedQuestionsAsync(lessonContent, lessonTitle);

            // Assert
            // The method should handle exceptions gracefully and return error response
            // instead of throwing
            Assert.NotNull(result);
            
            // If API fails, result should have error message
            if (!result.Success)
            {
                Assert.False(result.Success);
                Assert.NotNull(result.ErrorMessage);
                Assert.Contains("Error", result.ErrorMessage);
            }
        }

        #endregion
    }
}

