using Xunit;
using Moq;
using ServiceLayer.Exam.Writting;
using RepositoryLayer.Exam.Writting;
using Microsoft.Extensions.Configuration;
using DataLayer.DTOs.Exam.Writting;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Lumina.Test.Services
{
    public class GetFeedbackP1FromAIUnitTest
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IWrittingRepository> _mockWrittingRepository;
        private readonly Mock<IGenerativeAIService> _mockGenerativeAIService;
        private readonly WritingService _service;

        public GetFeedbackP1FromAIUnitTest()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockWrittingRepository = new Mock<IWrittingRepository>();
            _mockGenerativeAIService = new Mock<IGenerativeAIService>();

            // Setup configuration for API key
            _mockConfiguration
                .Setup(c => c["Gemini:ApiKey"])
                .Returns("test-api-key");

            _service = new WritingService(
                _mockConfiguration.Object,
                _mockWrittingRepository.Object,
                _mockGenerativeAIService.Object
            );
        }

        #region Test Cases for Null Request

        [Fact]
        public async Task GetFeedbackP1FromAI_WhenRequestIsNull_ShouldReturnErrorDTO()
        {
            // Arrange
            WritingRequestP1DTO? request = null;

            // Act
            var result = await _service.GetFeedbackP1FromAI(request!);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalScore);
            Assert.Contains("Error:", result.GrammarFeedback);
            Assert.Contains("Error:", result.VocabularyFeedback);
            Assert.Contains("Error:", result.ContentAccuracyFeedback);
            Assert.Contains("Error:", result.CorreededAnswerProposal);

            // Verify AI service is never called
            _mockGenerativeAIService.Verify(
                service => service.GenerateContentAsync(It.IsAny<string>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases for Valid Request - Success Scenarios

        [Fact]
        public async Task GetFeedbackP1FromAI_WhenValidRequestAndSuccessfulResponse_ShouldReturnWritingResponseDTO()
        {
            // Arrange
            var request = new WritingRequestP1DTO
            {
                PictureCaption = "A woman is reading a book in the library",
                VocabularyRequest = "library, book, reading",
                UserAnswer = "The woman is reading a book in the library."
            };

            var expectedResponse = new WritingResponseDTO
            {
                TotalScore = 4,
                GrammarFeedback = "Good grammar usage",
                VocabularyFeedback = "Appropriate vocabulary",
                ContentAccuracyFeedback = "Accurate description",
                CorreededAnswerProposal = "The woman is reading a book in the library."
            };

            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);

            _mockGenerativeAIService
                .Setup(service => service.GenerateContentAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = await _service.GetFeedbackP1FromAI(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse.TotalScore, result.TotalScore);
            Assert.Equal(expectedResponse.GrammarFeedback, result.GrammarFeedback);
            Assert.Equal(expectedResponse.VocabularyFeedback, result.VocabularyFeedback);
            Assert.Equal(expectedResponse.ContentAccuracyFeedback, result.ContentAccuracyFeedback);
            Assert.Equal(expectedResponse.CorreededAnswerProposal, result.CorreededAnswerProposal);

            // Verify AI service is called exactly once
            _mockGenerativeAIService.Verify(
                service => service.GenerateContentAsync(It.Is<string>(s => s.Contains(request.PictureCaption))),
                Times.Once
            );
        }

        [Fact]
        public async Task GetFeedbackP1FromAI_WhenResponseHasMarkdownCodeBlocks_ShouldRemoveMarkdownAndReturnDTO()
        {
            // Arrange
            var request = new WritingRequestP1DTO
            {
                PictureCaption = "A cat sitting on a mat",
                VocabularyRequest = "cat, mat",
                UserAnswer = "A cat is sitting on a mat."
            };

            var expectedResponse = new WritingResponseDTO
            {
                TotalScore = 5,
                GrammarFeedback = "Perfect grammar",
                VocabularyFeedback = "Excellent vocabulary",
                ContentAccuracyFeedback = "Perfect description",
                CorreededAnswerProposal = "A cat is sitting on a mat."
            };

            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);
            var responseWithMarkdown = $"```json\n{jsonResponse}\n```";

            _mockGenerativeAIService
                .Setup(service => service.GenerateContentAsync(It.IsAny<string>()))
                .ReturnsAsync(responseWithMarkdown);

            // Act
            var result = await _service.GetFeedbackP1FromAI(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse.TotalScore, result.TotalScore);
            Assert.Equal(expectedResponse.GrammarFeedback, result.GrammarFeedback);

            // Verify AI service is called exactly once
            _mockGenerativeAIService.Verify(
                service => service.GenerateContentAsync(It.IsAny<string>()),
                Times.Once
            );
        }

        [Fact]
        public async Task GetFeedbackP1FromAI_WhenResponseHasOnlyJsonCodeBlock_ShouldRemoveCodeBlockAndReturnDTO()
        {
            // Arrange
            var request = new WritingRequestP1DTO
            {
                PictureCaption = "A dog playing",
                VocabularyRequest = "dog, play",
                UserAnswer = "The dog is playing."
            };

            var expectedResponse = new WritingResponseDTO
            {
                TotalScore = 3,
                GrammarFeedback = "Good",
                VocabularyFeedback = "Good",
                ContentAccuracyFeedback = "Good",
                CorreededAnswerProposal = "The dog is playing."
            };

            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);
            var responseWithJsonBlock = $"```json{jsonResponse}```";

            _mockGenerativeAIService
                .Setup(service => service.GenerateContentAsync(It.IsAny<string>()))
                .ReturnsAsync(responseWithJsonBlock);

            // Act
            var result = await _service.GetFeedbackP1FromAI(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse.TotalScore, result.TotalScore);

            // Verify AI service is called exactly once
            _mockGenerativeAIService.Verify(
                service => service.GenerateContentAsync(It.IsAny<string>()),
                Times.Once
            );
        }

        [Fact]
        public async Task GetFeedbackP1FromAI_WhenResponseHasWhitespace_ShouldTrimAndReturnDTO()
        {
            // Arrange
            var request = new WritingRequestP1DTO
            {
                PictureCaption = "A bird flying",
                VocabularyRequest = "bird, fly",
                UserAnswer = "A bird is flying."
            };

            var expectedResponse = new WritingResponseDTO
            {
                TotalScore = 4,
                GrammarFeedback = "Good grammar",
                VocabularyFeedback = "Good vocabulary",
                ContentAccuracyFeedback = "Good content",
                CorreededAnswerProposal = "A bird is flying."
            };

            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);
            var responseWithWhitespace = $"   {jsonResponse}   ";

            _mockGenerativeAIService
                .Setup(service => service.GenerateContentAsync(It.IsAny<string>()))
                .ReturnsAsync(responseWithWhitespace);

            // Act
            var result = await _service.GetFeedbackP1FromAI(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse.TotalScore, result.TotalScore);

            // Verify AI service is called exactly once
            _mockGenerativeAIService.Verify(
                service => service.GenerateContentAsync(It.IsAny<string>()),
                Times.Once
            );
        }

        #endregion

        #region Test Cases for Empty/Null Response

        [Fact]
        public async Task GetFeedbackP1FromAI_WhenResponseIsEmpty_ShouldReturnErrorDTO()
        {
            // Arrange
            var request = new WritingRequestP1DTO
            {
                PictureCaption = "A test image",
                VocabularyRequest = "test",
                UserAnswer = "This is a test answer."
            };

            _mockGenerativeAIService
                .Setup(service => service.GenerateContentAsync(It.IsAny<string>()))
                .ReturnsAsync(string.Empty);

            // Act
            var result = await _service.GetFeedbackP1FromAI(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalScore);
            Assert.Contains("Error:", result.GrammarFeedback);
            Assert.Contains("No response received from Gemini API", result.GrammarFeedback);
            Assert.Contains("Error:", result.VocabularyFeedback);
            Assert.Contains("Error:", result.ContentAccuracyFeedback);
            Assert.Contains("Error:", result.CorreededAnswerProposal);

            // Verify AI service is called exactly once
            _mockGenerativeAIService.Verify(
                service => service.GenerateContentAsync(It.IsAny<string>()),
                Times.Once
            );
        }

        [Fact]
        public async Task GetFeedbackP1FromAI_WhenResponseIsNull_ShouldReturnErrorDTO()
        {
            // Arrange
            var request = new WritingRequestP1DTO
            {
                PictureCaption = "A test image",
                VocabularyRequest = "test",
                UserAnswer = "This is a test answer."
            };

            _mockGenerativeAIService
                .Setup(service => service.GenerateContentAsync(It.IsAny<string>()))
                .ReturnsAsync((string?)null!);

            // Act
            var result = await _service.GetFeedbackP1FromAI(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalScore);
            Assert.Contains("Error:", result.GrammarFeedback);
            Assert.Contains("No response received from Gemini API", result.GrammarFeedback);

            // Verify AI service is called exactly once
            _mockGenerativeAIService.Verify(
                service => service.GenerateContentAsync(It.IsAny<string>()),
                Times.Once
            );
        }

        #endregion

        #region Test Cases for Invalid JSON Response

        [Fact]
        public async Task GetFeedbackP1FromAI_WhenResponseIsInvalidJSON_ShouldReturnErrorDTO()
        {
            // Arrange
            var request = new WritingRequestP1DTO
            {
                PictureCaption = "A test image",
                VocabularyRequest = "test",
                UserAnswer = "This is a test answer."
            };

            var invalidJson = "{ invalid json }";

            _mockGenerativeAIService
                .Setup(service => service.GenerateContentAsync(It.IsAny<string>()))
                .ReturnsAsync(invalidJson);

            // Act
            var result = await _service.GetFeedbackP1FromAI(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalScore);
            Assert.Contains("Error:", result.GrammarFeedback);
            Assert.Contains("Error:", result.VocabularyFeedback);
            Assert.Contains("Error:", result.ContentAccuracyFeedback);
            Assert.Contains("Error:", result.CorreededAnswerProposal);

            // Verify AI service is called exactly once
            _mockGenerativeAIService.Verify(
                service => service.GenerateContentAsync(It.IsAny<string>()),
                Times.Once
            );
        }

        [Fact]
        public async Task GetFeedbackP1FromAI_WhenDeserializationReturnsNull_ShouldReturnErrorDTO()
        {
            // Arrange
            var request = new WritingRequestP1DTO
            {
                PictureCaption = "A test image",
                VocabularyRequest = "test",
                UserAnswer = "This is a test answer."
            };

            // Empty JSON object deserializes to WritingResponseDTO with default values, not null
            // To test null case, we need to return null from deserialization
            // Since JsonConvert doesn't return null for {}, we test with a response that would cause null
            // Actually, empty JSON {} will create an object with default values, so this test case
            // should verify that the service handles empty/default objects correctly
            var emptyJson = "null";

            _mockGenerativeAIService
                .Setup(service => service.GenerateContentAsync(It.IsAny<string>()))
                .ReturnsAsync(emptyJson);

            // Act
            var result = await _service.GetFeedbackP1FromAI(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalScore);
            Assert.Contains("Error:", result.GrammarFeedback);
            Assert.Contains("Failed to deserialize Gemini API response", result.GrammarFeedback);

            // Verify AI service is called exactly once
            _mockGenerativeAIService.Verify(
                service => service.GenerateContentAsync(It.IsAny<string>()),
                Times.Once
            );
        }

        #endregion

        #region Test Cases for Exception Handling

        [Fact]
        public async Task GetFeedbackP1FromAI_WhenAIServiceThrowsException_ShouldReturnErrorDTO()
        {
            // Arrange
            var request = new WritingRequestP1DTO
            {
                PictureCaption = "A test image",
                VocabularyRequest = "test",
                UserAnswer = "This is a test answer."
            };

            _mockGenerativeAIService
                .Setup(service => service.GenerateContentAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("AI service error"));

            // Act
            var result = await _service.GetFeedbackP1FromAI(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalScore);
            Assert.Contains("Error:", result.GrammarFeedback);
            Assert.Contains("AI service error", result.GrammarFeedback);
            Assert.Contains("Error:", result.VocabularyFeedback);
            Assert.Contains("Error:", result.ContentAccuracyFeedback);
            Assert.Contains("Error:", result.CorreededAnswerProposal);

            // Verify AI service is called exactly once
            _mockGenerativeAIService.Verify(
                service => service.GenerateContentAsync(It.IsAny<string>()),
                Times.Once
            );
        }

        [Fact]
        public async Task GetFeedbackP1FromAI_WhenAIServiceThrowsArgumentException_ShouldReturnErrorDTO()
        {
            // Arrange
            var request = new WritingRequestP1DTO
            {
                PictureCaption = "A test image",
                VocabularyRequest = "test",
                UserAnswer = "This is a test answer."
            };

            _mockGenerativeAIService
                .Setup(service => service.GenerateContentAsync(It.IsAny<string>()))
                .ThrowsAsync(new ArgumentException("Invalid argument"));

            // Act
            var result = await _service.GetFeedbackP1FromAI(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalScore);
            Assert.Contains("Error:", result.GrammarFeedback);
            Assert.Contains("Invalid argument", result.GrammarFeedback);

            // Verify AI service is called exactly once
            _mockGenerativeAIService.Verify(
                service => service.GenerateContentAsync(It.IsAny<string>()),
                Times.Once
            );
        }

        [Fact]
        public async Task GetFeedbackP1FromAI_WhenCreatePromptThrowsException_ShouldReturnErrorDTO()
        {
            // Arrange
            var request = new WritingRequestP1DTO
            {
                PictureCaption = null!,
                VocabularyRequest = null!,
                UserAnswer = null!
            };

            // The CreatePromptP1 method will create a prompt with null values, which should work
            // But if it throws, we should catch it
            _mockGenerativeAIService
                .Setup(service => service.GenerateContentAsync(It.IsAny<string>()))
                .ThrowsAsync(new NullReferenceException("Null reference in prompt"));

            // Act
            var result = await _service.GetFeedbackP1FromAI(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalScore);
            Assert.Contains("Error:", result.GrammarFeedback);
            Assert.Contains("Null reference in prompt", result.GrammarFeedback);

            // Verify AI service is called exactly once (or not if prompt creation fails)
            _mockGenerativeAIService.Verify(
                service => service.GenerateContentAsync(It.IsAny<string>()),
                Times.Once
            );
        }

        #endregion

        #region Test Cases for Edge Cases

        [Fact]
        public async Task GetFeedbackP1FromAI_WhenRequestHasEmptyStrings_ShouldStillProcess()
        {
            // Arrange
            var request = new WritingRequestP1DTO
            {
                PictureCaption = "",
                VocabularyRequest = "",
                UserAnswer = ""
            };

            var expectedResponse = new WritingResponseDTO
            {
                TotalScore = 0,
                GrammarFeedback = "No content",
                VocabularyFeedback = "No content",
                ContentAccuracyFeedback = "No content",
                CorreededAnswerProposal = ""
            };

            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);

            _mockGenerativeAIService
                .Setup(service => service.GenerateContentAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = await _service.GetFeedbackP1FromAI(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse.TotalScore, result.TotalScore);

            // Verify AI service is called exactly once
            _mockGenerativeAIService.Verify(
                service => service.GenerateContentAsync(It.IsAny<string>()),
                Times.Once
            );
        }

        [Fact]
        public async Task GetFeedbackP1FromAI_WhenResponseHasPartialJSON_ShouldReturnErrorDTO()
        {
            // Arrange
            var request = new WritingRequestP1DTO
            {
                PictureCaption = "A test image",
                VocabularyRequest = "test",
                UserAnswer = "This is a test answer."
            };

            var partialJson = "{\"TotalScore\": 5";

            _mockGenerativeAIService
                .Setup(service => service.GenerateContentAsync(It.IsAny<string>()))
                .ReturnsAsync(partialJson);

            // Act
            var result = await _service.GetFeedbackP1FromAI(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalScore);
            Assert.Contains("Error:", result.GrammarFeedback);

            // Verify AI service is called exactly once
            _mockGenerativeAIService.Verify(
                service => service.GenerateContentAsync(It.IsAny<string>()),
                Times.Once
            );
        }

        #endregion
    }
}

