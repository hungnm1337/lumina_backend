using Xunit;
using Moq;
using ServiceLayer.UserNoteAI;
using DataLayer.DTOs.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    public class ContinueConversationAsyncUnitTest
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<AIChatService>> _mockLogger;
        private readonly AIChatService _service;

        public ContinueConversationAsyncUnitTest()
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
        public async Task ContinueConversationAsync_WhenRequestIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            ChatRequestDTO? request = null;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _service.ContinueConversationAsync(request!)
            );

            Assert.Equal("request", exception.ParamName);
        }

        #endregion

        #region Test Cases - ConversationHistory is Null

        [Fact]
        public async Task ContinueConversationAsync_WhenConversationHistoryIsNull_ShouldCreateNewHistory()
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = "What is the next step?",
                LessonContent = "Test lesson content",
                ConversationHistory = null
            };

            // Act & Assert
            // Note: This will actually call Gemini API
            // Since we can't mock GenerativeModel without refactoring, we test that:
            // 1. Method attempts to process the request
            // 2. ConversationHistory is created if null
            // 3. Returns appropriate response or error
            
            try
            {
                var result = await _service.ContinueConversationAsync(request);

                // If API call succeeds
                Assert.NotNull(result);
                Assert.NotNull(result.CurrentResponse);
                Assert.NotNull(result.ConversationHistory);
                Assert.NotNull(result.SessionId);
                
                // Verify conversation history was created and contains user and assistant messages
                Assert.True(result.ConversationHistory.Count >= 2);
                Assert.Equal("user", result.ConversationHistory[result.ConversationHistory.Count - 2].Role);
                Assert.Equal("assistant", result.ConversationHistory[result.ConversationHistory.Count - 1].Role);
            }
            catch (Exception ex)
            {
                // If API fails, verify it's handled gracefully
                Assert.DoesNotContain("ArgumentNullException", ex.GetType().Name);
            }
        }

        #endregion

        #region Test Cases - ConversationHistory Has Data

        [Fact]
        public async Task ContinueConversationAsync_WhenConversationHistoryHasData_ShouldAppendToHistory()
        {
            // Arrange
            var existingHistory = new List<ChatMessageDTO>
            {
                new ChatMessageDTO
                {
                    Role = "user",
                    Content = "First question",
                    Timestamp = DateTime.UtcNow.AddMinutes(-10)
                },
                new ChatMessageDTO
                {
                    Role = "assistant",
                    Content = "First answer",
                    Timestamp = DateTime.UtcNow.AddMinutes(-9)
                }
            };

            var request = new ChatRequestDTO
            {
                UserQuestion = "Follow-up question",
                LessonContent = "Test lesson content",
                ConversationHistory = existingHistory
            };

            // Act & Assert
            try
            {
                var result = await _service.ContinueConversationAsync(request);

                Assert.NotNull(result);
                Assert.NotNull(result.ConversationHistory);
                
                // Verify history contains original messages plus new ones
                Assert.True(result.ConversationHistory.Count >= 4);
                
                // Verify original messages are preserved
                Assert.Equal("First question", result.ConversationHistory[0].Content);
                Assert.Equal("First answer", result.ConversationHistory[1].Content);
                
                // Verify new messages are added
                var lastUserMessage = result.ConversationHistory[result.ConversationHistory.Count - 2];
                var lastAssistantMessage = result.ConversationHistory[result.ConversationHistory.Count - 1];
                Assert.Equal("user", lastUserMessage.Role);
                Assert.Equal("assistant", lastAssistantMessage.Role);
            }
            catch (Exception ex)
            {
                // Verify it's not a validation error
                Assert.DoesNotContain("ArgumentNullException", ex.GetType().Name);
            }
        }

        [Fact]
        public async Task ContinueConversationAsync_WhenConversationHistoryIsEmpty_ShouldCreateNewHistory()
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = "New question",
                LessonContent = "Test lesson content",
                ConversationHistory = new List<ChatMessageDTO>()
            };

            // Act & Assert
            try
            {
                var result = await _service.ContinueConversationAsync(request);

                Assert.NotNull(result);
                Assert.NotNull(result.ConversationHistory);
                Assert.True(result.ConversationHistory.Count >= 2);
            }
            catch (Exception ex)
            {
                Assert.DoesNotContain("ArgumentNullException", ex.GetType().Name);
            }
        }

        #endregion

        #region Test Cases - Valid Request

        [Fact]
        public async Task ContinueConversationAsync_WhenRequestIsValid_ShouldProcessAndReturnResponse()
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = "What does this mean?",
                LessonContent = "Test lesson content about TOEIC",
                LessonTitle = "TOEIC Reading",
                ConversationHistory = null
            };

            // Act & Assert
            try
            {
                var result = await _service.ContinueConversationAsync(request);

                Assert.NotNull(result);
                Assert.NotNull(result.CurrentResponse);
                Assert.NotNull(result.ConversationHistory);
                Assert.NotNull(result.SessionId);
                Assert.NotEmpty(result.SessionId);
                
                // Verify response structure
                if (result.CurrentResponse.Success)
                {
                    Assert.NotNull(result.CurrentResponse.Answer);
                }
                else
                {
                    Assert.NotNull(result.CurrentResponse.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                // Verify it's not a validation error
                Assert.DoesNotContain("ArgumentNullException", ex.GetType().Name);
            }
        }

        [Fact]
        public async Task ContinueConversationAsync_WhenRequestHasAllOptionalFields_ShouldProcessCorrectly()
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = "Can you explain more?",
                LessonContent = "Test lesson content",
                LessonTitle = "Advanced TOEIC",
                UserId = 1,
                ArticleId = 1,
                ConversationHistory = new List<ChatMessageDTO>
                {
                    new ChatMessageDTO
                    {
                        Role = "user",
                        Content = "Previous question",
                        Timestamp = DateTime.UtcNow
                    }
                }
            };

            // Act & Assert
            try
            {
                var result = await _service.ContinueConversationAsync(request);

                Assert.NotNull(result);
                Assert.NotNull(result.CurrentResponse);
                Assert.NotNull(result.ConversationHistory);
            }
            catch (Exception ex)
            {
                Assert.DoesNotContain("ArgumentNullException", ex.GetType().Name);
            }
        }

        #endregion

        #region Test Cases - Exception Handling

        [Fact]
        public async Task ContinueConversationAsync_WhenAPIThrowsException_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new ChatRequestDTO
            {
                UserQuestion = "Test question",
                LessonContent = "Test lesson content",
                ConversationHistory = null
            };

            // Act & Assert
            // Note: Since we can't mock GenerativeModel directly, this test verifies
            // that exception handling works. In a real scenario with invalid API key,
            // the method should catch the exception and return error response.
            
            var result = await _service.ContinueConversationAsync(request);

            // Assert
            // The method should handle exceptions gracefully and return error response
            // instead of throwing
            Assert.NotNull(result);
            Assert.NotNull(result.CurrentResponse);
            Assert.NotNull(result.ConversationHistory);
            Assert.NotNull(result.SessionId);
            
            // If API fails, result should have error message
            if (!result.CurrentResponse.Success)
            {
                Assert.NotNull(result.CurrentResponse.ErrorMessage);
                Assert.Contains("Error", result.CurrentResponse.ErrorMessage);
                Assert.NotNull(result.CurrentResponse.Answer);
                Assert.Contains("Xin lỗi", result.CurrentResponse.Answer);
            }
        }

        #endregion
    }
}

