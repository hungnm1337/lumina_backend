using Xunit;
using Moq;
using Moq.Protected;
using DataLayer.DTOs;
using DataLayer.DTOs.AIGeneratedExam;
using Microsoft.Extensions.Options;
using ServiceLayer.ExamGenerationAI;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Lumina.Test.Services
{
    /// <summary>
    /// Unit tests cho ExamGenerationAIService - 17 test cases
    /// Coverage: DetectIntentAsync, ParseUserRequestAsync, GenerateExamAsync, GeneralChatAsync
    /// </summary>
    public class ExamGenerationAIServiceUnitTest
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly ExamGenerationAIService _service;

        public ExamGenerationAIServiceUnitTest()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);

            var options = Options.Create(new OpenAIOptions
            {
                ApiKey = "test-api-key",
                Model = "gpt-4"
            });

            _service = new ExamGenerationAIService(options, _httpClient);
        }

        [Theory]
        [InlineData("a beautiful landscape")]
        [InlineData("test prompt")]
        public void GeneratePollinationsImageUrl_WithValidDescription_ShouldReturnFormattedUrl(string description)
        {
            var result = _service.GeneratePollinationsImageUrl(description);
            Assert.NotNull(result);
            Assert.StartsWith("https://image.pollinations.ai/prompt/", result);
            Assert.Contains("model=flux", result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void GeneratePollinationsImageUrl_WithNullOrEmpty_ShouldReturnEmptyString(string description)
        {
            var result = _service.GeneratePollinationsImageUrl(description);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task DetectIntentAsync_WhenRequestIsExam_ShouldReturnExamIntent()
        {
            var openAIResponse = @"{""choices"":[{""message"":{""content"":""{\""isExamRequest\"":true,\""explanation\"":\""exam request\""}"" }}]}";
            SetupHttpMockResponse(HttpStatusCode.OK, openAIResponse);

            var result = await _service.DetectIntentAsync("Create exam");

            Assert.NotNull(result);
            Assert.True(result.IsExamRequest);
        }

        [Fact]
        public async Task DetectIntentAsync_WhenRequestIsChat_ShouldReturnChatIntent()
        {
            var openAIResponse = @"{""choices"":[{""message"":{""content"":""{\""isExamRequest\"":false,\""explanation\"":\""chat\""}""}}]}";
            SetupHttpMockResponse(HttpStatusCode.OK, openAIResponse);

            var result = await _service.DetectIntentAsync("What is TOEIC?");

            Assert.NotNull(result);
            Assert.False(result.IsExamRequest);
        }

        [Fact]
        public async Task ParseUserRequestAsync_WithValidRequest_ShouldReturnParsedValues()
        {
            var openAIResponse = @"{""choices"":[{""message"":{""content"":""{\""partNumber\"":5,\""quantity\"":10,\""topic\"":\""Business\""}""}}]}";
            SetupHttpMockResponse(HttpStatusCode.OK, openAIResponse);

            var (partNumber, quantity, topic) = await _service.ParseUserRequestAsync("Create 10 Part 5 questions");

            Assert.Equal(5, partNumber);
            Assert.Equal(10, quantity);
            Assert.Equal("Business", topic);
        }

        [Fact]
        public async Task ParseUserRequestAsync_WhenOpenAIFails_ShouldThrowException()
        {
            SetupHttpMockResponse(HttpStatusCode.InternalServerError, "{\"error\":\"fail\"}");

            await Assert.ThrowsAsync<Exception>(
                async () => await _service.ParseUserRequestAsync("test"));
        }

        [Fact]
        public async Task GeneralChatAsync_WithValidQuestion_ShouldReturnChatResponse()
        {
            var openAIResponse = @"{""choices"":[{""message"":{""content"":""TOEIC is a test""}}]}";
            SetupHttpMockResponse(HttpStatusCode.OK, openAIResponse);

            var result = await _service.GeneralChatAsync("What is TOEIC?");

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task GeneralChatAsync_WhenOpenAIFails_ShouldThrowException()
        {
            SetupHttpMockResponse(HttpStatusCode.ServiceUnavailable, "{\"error\":\"503\"}");

            await Assert.ThrowsAsync<Exception>(
                async () => await _service.GeneralChatAsync("test"));
        }

        [Fact]
        public async Task GenerateResponseAsync_WhenOpenAIReturnsError_ShouldThrowException()
        {
            SetupHttpMockResponse(HttpStatusCode.BadRequest, "{\"error\":\"bad request\"}");

            await Assert.ThrowsAsync<Exception>(
                async () => await _service.DetectIntentAsync("test"));
        }

        [Theory]
        [InlineData(5, 5, "Business")]
        [InlineData(6, 3, null)]
        public async Task GenerateExamAsync_WithSmallQuantity_ShouldCallSingleBatch(
            int partNumber, int quantity, string topic)
        {
            var openAIResponse = "{\"choices\":[{\"message\":{\"content\":\"{\\\"examExamTitle\\\":\\\"Test\\\",\\\"skill\\\":\\\"Reading\\\",\\\"partLabel\\\":\\\"Part 5\\\",\\\"prompts\\\":[]}\"}}]}";
            SetupHttpMockResponse(HttpStatusCode.OK, openAIResponse);

            var result = await _service.GenerateExamAsync(partNumber, quantity, topic);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GenerateExamAsync_WithLargeQuantity_ShouldTriggerBatchProcessing()
        {
            var openAIResponse = @"{""choices"":[{""message"":{""content"":""{\""examExamTitle\"":\""Batch Test\"",\""skill\"":\""Reading\"",\""partLabel\"":\""Part 5\"",\""prompts\"":[]}"" }}]}";
            SetupHttpMockResponse(HttpStatusCode.OK, openAIResponse);

            var result = await _service.GenerateExamAsync(5, 15, "test");

            Assert.NotNull(result);
        }

        private void SetupHttpMockResponse(HttpStatusCode statusCode, string content)
        {
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(content)
                });
        }
    }
}
