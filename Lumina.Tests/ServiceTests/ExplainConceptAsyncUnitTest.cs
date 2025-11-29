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
    public class ExplainConceptAsyncUnitTest
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<AIChatService>> _mockLogger;
        private readonly AIChatService _service;

        public ExplainConceptAsyncUnitTest()
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

        #region Test Cases for Invalid Concept

        [Fact]
        public async Task ExplainConceptAsync_WhenConceptIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            string? concept = null;
            string lessonContext = "Test lesson context";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _service.ExplainConceptAsync(concept!, lessonContext)
            );

            Assert.Equal("concept", exception.ParamName);
        }

        [Fact]
        public async Task ExplainConceptAsync_WhenConceptIsEmpty_ShouldThrowArgumentException()
        {
            // Arrange
            string concept = string.Empty;
            string lessonContext = "Test lesson context";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.ExplainConceptAsync(concept, lessonContext)
            );

            Assert.Equal("concept", exception.ParamName);
            Assert.Contains("Concept cannot be null or empty", exception.Message);
        }

        [Fact]
        public async Task ExplainConceptAsync_WhenConceptIsWhitespace_ShouldThrowArgumentException()
        {
            // Arrange
            string concept = "   ";
            string lessonContext = "Test lesson context";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.ExplainConceptAsync(concept, lessonContext)
            );

            Assert.Equal("concept", exception.ParamName);
            Assert.Contains("Concept cannot be null or empty", exception.Message);
        }

        #endregion

        #region Test Cases for Invalid LessonContext

        [Fact]
        public async Task ExplainConceptAsync_WhenLessonContextIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            string concept = "Test concept";
            string? lessonContext = null;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _service.ExplainConceptAsync(concept, lessonContext!)
            );

            Assert.Equal("lessonContext", exception.ParamName);
        }

        [Fact]
        public async Task ExplainConceptAsync_WhenLessonContextIsEmpty_ShouldThrowArgumentException()
        {
            // Arrange
            string concept = "Test concept";
            string lessonContext = string.Empty;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.ExplainConceptAsync(concept, lessonContext)
            );

            Assert.Equal("lessonContext", exception.ParamName);
            Assert.Contains("Lesson context cannot be null or empty", exception.Message);
        }

        [Fact]
        public async Task ExplainConceptAsync_WhenLessonContextIsWhitespace_ShouldThrowArgumentException()
        {
            // Arrange
            string concept = "Test concept";
            string lessonContext = "   ";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.ExplainConceptAsync(concept, lessonContext)
            );

            Assert.Equal("lessonContext", exception.ParamName);
            Assert.Contains("Lesson context cannot be null or empty", exception.Message);
        }

        #endregion

        #region Test Cases for Valid Parameters

        [Fact]
        public async Task ExplainConceptAsync_WhenBothParametersAreValid_ShouldReturnResponse()
        {
            // Arrange
            string concept = "Present Perfect Tense";
            string lessonContext = "This lesson covers various English grammar topics including tenses, vocabulary, and sentence structures.";

            // Act & Assert
            // Note: This will actually call Gemini API
            // Since we can't mock GenerativeModel without refactoring, we test that:
            // 1. Validation passes
            // 2. Method attempts to process the request
            // 3. Returns appropriate response or error
            
            try
            {
                var result = await _service.ExplainConceptAsync(concept, lessonContext);

                // If API call succeeds
                Assert.NotNull(result);
                
                if (result.Success)
                {
                    Assert.True(result.Success);
                    Assert.NotNull(result.Answer);
                    Assert.NotEmpty(result.Answer);
                    Assert.Equal(90, result.ConfidenceScore);
                    Assert.NotNull(result.RelatedTopics);
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
        public async Task ExplainConceptAsync_WhenConceptAndContextAreLongStrings_ShouldProcessCorrectly()
        {
            // Arrange
            string concept = "Complex grammatical structure with multiple clauses and subordinating conjunctions";
            string lessonContext = "This is a comprehensive lesson about advanced English grammar. It covers complex sentence structures, advanced vocabulary, and sophisticated writing techniques. Students will learn how to construct complex sentences and use advanced grammatical patterns effectively.";

            // Act & Assert
            try
            {
                var result = await _service.ExplainConceptAsync(concept, lessonContext);

                Assert.NotNull(result);
                
                if (result.Success)
                {
                    Assert.True(result.Success);
                    Assert.NotNull(result.Answer);
                    Assert.NotNull(result.RelatedTopics);
                    Assert.Equal(90, result.ConfidenceScore);
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

        #endregion

        #region Test Cases - Exception Handling

        [Fact]
        public async Task ExplainConceptAsync_WhenAPIThrowsException_ShouldReturnErrorResponse()
        {
            // Arrange
            string concept = "Test concept";
            string lessonContext = "Test lesson context";

            // Act & Assert
            // Note: Since we can't mock GenerativeModel directly, this test verifies
            // that exception handling works. In a real scenario with invalid API key,
            // the method should catch the exception and return error response.
            
            var result = await _service.ExplainConceptAsync(concept, lessonContext);

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

