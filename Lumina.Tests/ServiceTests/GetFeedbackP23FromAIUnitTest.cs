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
    public class GetFeedbackP23FromAIUnitTest
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IWrittingRepository> _mockWrittingRepository;
        private readonly Mock<IGenerativeAIService> _mockGenerativeAIService;
        private readonly WritingService _service;

        public GetFeedbackP23FromAIUnitTest()
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
        public async Task GetFeedbackP23FromAI_WhenRequestIsNull_ShouldReturnErrorDTO()
        {
            // Arrange
            WritingRequestP23DTO? request = null;

            // Act
            var result = await _service.GetFeedbackP23FromAI(request!);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalScore);
            Assert.Contains("Error:", result.GrammarFeedback);
            Assert.Contains("Error:", result.VocabularyFeedback);
            Assert.Contains("Error:", result.ContentAccuracyFeedback);
            Assert.Contains("Error:", result.CorreededAnswerProposal);

            // Verify AI service is never called (CreatePromptP23 throws before calling AI)
            _mockGenerativeAIService.Verify(
                service => service.GenerateContentAsync(It.IsAny<string>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases for Valid Request - Success Scenarios

        [Fact]
        public async Task GetFeedbackP23FromAI_WhenValidRequestPart2AndSuccessfulResponse_ShouldReturnWritingResponseDTO()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 2,
                Prompt = "Write an email requesting information about a product",
                UserAnswer = "Dear Sir/Madam, I would like to request information about your product."
            };

            var expectedResponse = new WritingResponseDTO
            {
                TotalScore = 4,
                GrammarFeedback = "Good grammar usage",
                VocabularyFeedback = "Appropriate vocabulary",
                ContentAccuracyFeedback = "Accurate content",
                CorreededAnswerProposal = "Dear Sir/Madam,\n\nI would like to request information about your product."
            };

            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);

            _mockGenerativeAIService
                .Setup(service => service.GenerateContentAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = await _service.GetFeedbackP23FromAI(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse.TotalScore, result.TotalScore);
            Assert.Equal(expectedResponse.GrammarFeedback, result.GrammarFeedback);
            Assert.Equal(expectedResponse.VocabularyFeedback, result.VocabularyFeedback);
            Assert.Equal(expectedResponse.ContentAccuracyFeedback, result.ContentAccuracyFeedback);
            Assert.Equal(expectedResponse.CorreededAnswerProposal, result.CorreededAnswerProposal);

            // Verify AI service is called exactly once
            _mockGenerativeAIService.Verify(
                service => service.GenerateContentAsync(It.Is<string>(s => s.Contains(request.Prompt))),
                Times.Once
            );
        }

        [Fact]
        public async Task GetFeedbackP23FromAI_WhenValidRequestPart3AndSuccessfulResponse_ShouldReturnWritingResponseDTO()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 3,
                Prompt = "Do you agree or disagree with the following statement? Technology has made our lives better.",
                UserAnswer = "I agree that technology has made our lives better because it provides convenience and efficiency."
            };

            var expectedResponse = new WritingResponseDTO
            {
                TotalScore = 5,
                GrammarFeedback = "Excellent grammar",
                VocabularyFeedback = "Excellent vocabulary",
                ContentAccuracyFeedback = "Well-structured essay",
                CorreededAnswerProposal = "I agree that technology has made our lives better because it provides convenience and efficiency."
            };

            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);

            _mockGenerativeAIService
                .Setup(service => service.GenerateContentAsync(It.IsAny<string>()))
                .ReturnsAsync(jsonResponse);

            // Act
            var result = await _service.GetFeedbackP23FromAI(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse.TotalScore, result.TotalScore);
            Assert.Equal(expectedResponse.GrammarFeedback, result.GrammarFeedback);
            Assert.Equal(expectedResponse.VocabularyFeedback, result.VocabularyFeedback);
            Assert.Equal(expectedResponse.ContentAccuracyFeedback, result.ContentAccuracyFeedback);
            Assert.Equal(expectedResponse.CorreededAnswerProposal, result.CorreededAnswerProposal);

            // Verify AI service is called exactly once
            _mockGenerativeAIService.Verify(
                service => service.GenerateContentAsync(It.Is<string>(s => s.Contains(request.Prompt))),
                Times.Once
            );
        }

        [Fact]
        public async Task GetFeedbackP23FromAI_WhenResponseHasMarkdownCodeBlocks_ShouldRemoveMarkdownAndReturnDTO()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 2,
                Prompt = "Write an email",
                UserAnswer = "This is a test answer."
            };

            var expectedResponse = new WritingResponseDTO
            {
                TotalScore = 4,
                GrammarFeedback = "Good grammar",
                VocabularyFeedback = "Good vocabulary",
                ContentAccuracyFeedback = "Good content",
                CorreededAnswerProposal = "Corrected answer"
            };

            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);
            var responseWithMarkdown = $"```json\n{jsonResponse}\n```";

            _mockGenerativeAIService
                .Setup(service => service.GenerateContentAsync(It.IsAny<string>()))
                .ReturnsAsync(responseWithMarkdown);

            // Act
            var result = await _service.GetFeedbackP23FromAI(request);

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
        public async Task GetFeedbackP23FromAI_WhenResponseHasOnlyJsonCodeBlock_ShouldRemoveCodeBlockAndReturnDTO()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 3,
                Prompt = "Write an essay",
                UserAnswer = "This is a test answer."
            };

            var expectedResponse = new WritingResponseDTO
            {
                TotalScore = 3,
                GrammarFeedback = "Good",
                VocabularyFeedback = "Good",
                ContentAccuracyFeedback = "Good",
                CorreededAnswerProposal = "Corrected answer"
            };

            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);
            var responseWithJsonBlock = $"```json{jsonResponse}```";

            _mockGenerativeAIService
                .Setup(service => service.GenerateContentAsync(It.IsAny<string>()))
                .ReturnsAsync(responseWithJsonBlock);

            // Act
            var result = await _service.GetFeedbackP23FromAI(request);

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
        public async Task GetFeedbackP23FromAI_WhenResponseHasWhitespace_ShouldTrimAndReturnDTO()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 2,
                Prompt = "Write an email",
                UserAnswer = "This is a test answer."
            };

            var expectedResponse = new WritingResponseDTO
            {
                TotalScore = 4,
                GrammarFeedback = "Good grammar",
                VocabularyFeedback = "Good vocabulary",
                ContentAccuracyFeedback = "Good content",
                CorreededAnswerProposal = "Corrected answer"
            };

            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);
            var responseWithWhitespace = $"   {jsonResponse}   ";

            _mockGenerativeAIService
                .Setup(service => service.GenerateContentAsync(It.IsAny<string>()))
                .ReturnsAsync(responseWithWhitespace);

            // Act
            var result = await _service.GetFeedbackP23FromAI(request);

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

        #region Test Cases for Invalid PartNumber

        [Fact]
        public async Task GetFeedbackP23FromAI_WhenPartNumberIsInvalid_ShouldReturnErrorDTO()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 1, // Invalid: should be 2 or 3
                Prompt = "Write something",
                UserAnswer = "This is a test answer."
            };

            // Act
            var result = await _service.GetFeedbackP23FromAI(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalScore);
            Assert.Contains("Error:", result.GrammarFeedback);
            Assert.Contains("Invalid Part Number", result.GrammarFeedback);

            // Verify AI service is never called (CreatePromptP23 throws before calling AI)
            _mockGenerativeAIService.Verify(
                service => service.GenerateContentAsync(It.IsAny<string>()),
                Times.Never
            );
        }

        [Fact]
        public async Task GetFeedbackP23FromAI_WhenPartNumberIsZero_ShouldReturnErrorDTO()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 0, // Invalid: should be 2 or 3
                Prompt = "Write something",
                UserAnswer = "This is a test answer."
            };

            // Act
            var result = await _service.GetFeedbackP23FromAI(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalScore);
            Assert.Contains("Error:", result.GrammarFeedback);
            Assert.Contains("Invalid Part Number", result.GrammarFeedback);

            // Verify AI service is never called
            _mockGenerativeAIService.Verify(
                service => service.GenerateContentAsync(It.IsAny<string>()),
                Times.Never
            );
        }

        [Fact]
        public async Task GetFeedbackP23FromAI_WhenPartNumberIsGreaterThanThree_ShouldReturnErrorDTO()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 4, // Invalid: should be 2 or 3
                Prompt = "Write something",
                UserAnswer = "This is a test answer."
            };

            // Act
            var result = await _service.GetFeedbackP23FromAI(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalScore);
            Assert.Contains("Error:", result.GrammarFeedback);
            Assert.Contains("Invalid Part Number", result.GrammarFeedback);

            // Verify AI service is never called
            _mockGenerativeAIService.Verify(
                service => service.GenerateContentAsync(It.IsAny<string>()),
                Times.Never
            );
        }

        #endregion

        #region Test Cases for Empty/Null Response

        [Fact]
        public async Task GetFeedbackP23FromAI_WhenResponseIsEmpty_ShouldReturnErrorDTO()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 2,
                Prompt = "Write an email",
                UserAnswer = "This is a test answer."
            };

            _mockGenerativeAIService
                .Setup(service => service.GenerateContentAsync(It.IsAny<string>()))
                .ReturnsAsync(string.Empty);

            // Act
            var result = await _service.GetFeedbackP23FromAI(request);

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
        public async Task GetFeedbackP23FromAI_WhenResponseIsNull_ShouldReturnErrorDTO()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 3,
                Prompt = "Write an essay",
                UserAnswer = "This is a test answer."
            };

            _mockGenerativeAIService
                .Setup(service => service.GenerateContentAsync(It.IsAny<string>()))
                .ReturnsAsync((string?)null!);

            // Act
            var result = await _service.GetFeedbackP23FromAI(request);

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
        public async Task GetFeedbackP23FromAI_WhenResponseIsInvalidJSON_ShouldReturnErrorDTO()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 2,
                Prompt = "Write an email",
                UserAnswer = "This is a test answer."
            };

            var invalidJson = "{ invalid json }";

            _mockGenerativeAIService
                .Setup(service => service.GenerateContentAsync(It.IsAny<string>()))
                .ReturnsAsync(invalidJson);

            // Act
            var result = await _service.GetFeedbackP23FromAI(request);

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
        public async Task GetFeedbackP23FromAI_WhenDeserializationReturnsNull_ShouldReturnErrorDTO()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 3,
                Prompt = "Write an essay",
                UserAnswer = "This is a test answer."
            };

            // Return "null" which deserializes to null
            var emptyJson = "null";

            _mockGenerativeAIService
                .Setup(service => service.GenerateContentAsync(It.IsAny<string>()))
                .ReturnsAsync(emptyJson);

            // Act
            var result = await _service.GetFeedbackP23FromAI(request);

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
        public async Task GetFeedbackP23FromAI_WhenAIServiceThrowsException_ShouldReturnErrorDTO()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 2,
                Prompt = "Write an email",
                UserAnswer = "This is a test answer."
            };

            _mockGenerativeAIService
                .Setup(service => service.GenerateContentAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("AI service error"));

            // Act
            var result = await _service.GetFeedbackP23FromAI(request);

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
        public async Task GetFeedbackP23FromAI_WhenAIServiceThrowsArgumentException_ShouldReturnErrorDTO()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 3,
                Prompt = "Write an essay",
                UserAnswer = "This is a test answer."
            };

            _mockGenerativeAIService
                .Setup(service => service.GenerateContentAsync(It.IsAny<string>()))
                .ThrowsAsync(new ArgumentException("Invalid argument"));

            // Act
            var result = await _service.GetFeedbackP23FromAI(request);

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

        #endregion

        #region Test Cases for Edge Cases

        [Fact]
        public async Task GetFeedbackP23FromAI_WhenRequestHasEmptyStrings_ShouldStillProcess()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 2,
                Prompt = "",
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
            var result = await _service.GetFeedbackP23FromAI(request);

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
        public async Task GetFeedbackP23FromAI_WhenResponseHasPartialJSON_ShouldReturnErrorDTO()
        {
            // Arrange
            var request = new WritingRequestP23DTO
            {
                PartNumber = 2,
                Prompt = "Write an email",
                UserAnswer = "This is a test answer."
            };

            var partialJson = "{\"TotalScore\": 5";

            _mockGenerativeAIService
                .Setup(service => service.GenerateContentAsync(It.IsAny<string>()))
                .ReturnsAsync(partialJson);

            // Act
            var result = await _service.GetFeedbackP23FromAI(request);

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

