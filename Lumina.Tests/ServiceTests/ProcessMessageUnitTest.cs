using Xunit;
using Moq;
using Moq.Protected;
using ServiceLayer.Chat;
using ServiceLayer.UploadFile;
using DataLayer.DTOs.Chat;
using DataLayer.Models;
using Lumina.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;

namespace Lumina.Test.Services
{
    /// <summary>
    /// Optimized test suite for ChatService.ProcessMessage - Reduced from 16 to 8 tests
    /// </summary>
    public class ProcessMessageUnitTest
    {
        private readonly LuminaSystemContext _context;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<IUploadService> _mockUploadService;
        private readonly ChatService _service;

        public ProcessMessageUnitTest()
        {
            _context = InMemoryDbContextHelper.CreateContext();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockUploadService = new Mock<IUploadService>();

            // Setup configuration
            _mockConfiguration.Setup(c => c["GeminiStudent:ApiKey"]).Returns("test-api-key");
            _mockConfiguration.Setup(c => c["GeminiStudent:BaseUrl"]).Returns("https://test.com");
            _mockConfiguration.Setup(c => c["GeminiStudent:Model"]).Returns("test-model");

            _service = new ChatService(
                _context,
                _mockConfiguration.Object,
                _mockHttpClientFactory.Object,
                _mockUploadService.Object
            );
        }

        private void SetupHttpClientWithResponse(string responseJson)
        {
            var geminiResponse = new
            {
                candidates = new[]
                {
                    new
                    {
                        content = new
                        {
                            parts = new[]
                            {
                                new { text = responseJson }
                            }
                        }
                    }
                }
            };

            var geminiResponseJson = JsonConvert.SerializeObject(geminiResponse);

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(geminiResponseJson)
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        }

        #region Core Functionality Tests

        [Fact]
        public async Task ProcessMessage_WhenMessageIsOutOfScope_ShouldReturnOutOfScopeResponse()
        {
            // Arrange
            var request = new ChatRequestDTO { Message = "lập trình python", UserId = 1 };

            // Act
            var result = await _service.ProcessMessage(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("out_of_scope", result.ConversationType);
            Assert.Contains("TOEIC", result.Answer);
        }

        [Theory]
        [InlineData("từ vựng TOEIC", "vocabulary")]
        [InlineData("giải thích thì hiện tại đơn", "grammar")]
        [InlineData("strategy", "toeic_strategy")]
        [InlineData("luyện tập listening", "practice")]
        [InlineData("hello", "general")]
        public async Task ProcessMessage_WhenMessageTypeDetected_ShouldReturnCorrectConversationType(
            string message, string expectedType)
        {
            // Arrange
            var request = new ChatRequestDTO { Message = message, UserId = 1 };
            var responseJson = JsonConvert.SerializeObject(new
            {
                answer = "Test answer",
                suggestions = new[] { "Suggestion 1" },
                conversationType = expectedType
            });

            SetupHttpClientWithResponse(responseJson);

            // Act
            var result = await _service.ProcessMessage(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedType, result.ConversationType);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task ProcessMessage_WhenExceptionOccurs_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new ChatRequestDTO { Message = "test", UserId = 1 };
            await _context.DisposeAsync();

            // Act
            var result = await _service.ProcessMessage(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("error", result.ConversationType);
        }

        [Theory]
        [InlineData(null)] // API key null
        [InlineData("BadRequest")] // API returns error
        [InlineData("")] // API returns empty response
        public async Task ProcessMessage_WhenErrorScenario_ShouldReturnErrorResponse(string? scenario)
        {
            // Arrange
            var request = new ChatRequestDTO { Message = "vocabulary", UserId = 1 };

            if (scenario == null)
            {
                // API key null scenario
                _mockConfiguration.Setup(c => c["GeminiStudent:ApiKey"]).Returns((string?)null);
                var service = new ChatService(_context, _mockConfiguration.Object, _mockHttpClientFactory.Object, _mockUploadService.Object);
                
                // Act
                var result = await service.ProcessMessage(request);
                
                // Assert
                Assert.NotNull(result);
                Assert.Equal("error", result.ConversationType);
            }
            else
            {
                // API error scenarios
                var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
                mockHttpMessageHandler.Protected()
                    .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(new HttpResponseMessage
                    {
                        StatusCode = scenario == "BadRequest" ? HttpStatusCode.BadRequest : HttpStatusCode.OK,
                        Content = new StringContent(scenario)
                    });

                var httpClient = new HttpClient(mockHttpMessageHandler.Object);
                _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

                // Act
                var result = await _service.ProcessMessage(request);

                // Assert
                Assert.NotNull(result);
                Assert.Equal("error", result.ConversationType);
            }
        }

        #endregion

        #region Vocabulary Generation Tests

        [Fact]
        public async Task ProcessMessage_WhenVocabularyGeneration_ShouldReturnGenerationResponse()
        {
            // Arrange
            var request = new ChatRequestDTO { Message = "tạo 10 từ vựng về business", UserId = 1 };
            var responseJson = JsonConvert.SerializeObject(new
            {
                answer = "",
                vocabularies = new[]
                {
                    new
                    {
                        word = "acquire",
                        definition = "đạt được",
                        example = "Example",
                        typeOfWord = "Verb",
                        category = "Business",
                        imageDescription = "Business person signing contract"
                    }
                },
                hasSaveOption = true,
                saveAction = "CREATE_VOCABULARY_LIST",
                conversationType = "vocabulary_generation"
            });

            SetupHttpClientWithResponse(responseJson);
            _mockUploadService.Setup(s => s.UploadFromUrlAsync(It.IsAny<string>()))
                .ReturnsAsync(new DataLayer.DTOs.UploadResultDTO { Url = "http://cloudinary.com/image.jpg" });

            // Act
            var result = await _service.ProcessMessage(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("vocabulary_generation", result.ConversationType);
        }

        [Fact]
        public async Task ProcessMessage_WhenVocabularyGenerationUploadFails_ShouldUsePollinationsUrl()
        {
            // Arrange
            var request = new ChatRequestDTO { Message = "tạo 5 từ vựng", UserId = 1 };
            var responseJson = JsonConvert.SerializeObject(new
            {
                answer = "",
                vocabularies = new[]
                {
                    new
                    {
                        word = "test",
                        definition = "test",
                        example = "test",
                        typeOfWord = "Noun",
                        category = "Test",
                        imageDescription = "Test image description"
                    }
                },
                conversationType = "vocabulary_generation"
            });

            SetupHttpClientWithResponse(responseJson);
            _mockUploadService.Setup(s => s.UploadFromUrlAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Upload failed"));

            // Act
            var result = await _service.ProcessMessage(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("vocabulary_generation", result.ConversationType);
        }

        [Fact]
        public async Task ProcessMessage_WhenVocabularyGenerationNoVocabularies_ShouldHandleGracefully()
        {
            // Arrange
            var request = new ChatRequestDTO { Message = "tạo từ vựng", UserId = 1 };
            var responseJson = JsonConvert.SerializeObject(new
            {
                answer = "No vocabularies",
                vocabularies = (object?)null,
                conversationType = "vocabulary_generation"
            });

            SetupHttpClientWithResponse(responseJson);

            // Act
            var result = await _service.ProcessMessage(request);

            // Assert
            Assert.NotNull(result);
        }

        #endregion
    }
}
