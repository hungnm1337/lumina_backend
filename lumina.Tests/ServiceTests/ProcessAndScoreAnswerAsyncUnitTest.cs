using DataLayer.DTOs;
using DataLayer.DTOs.Exam.Speaking;
using DataLayer.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.Exam.Speaking;
using ServiceLayer.Speech;
using ServiceLayer.UploadFile;

namespace Lumina.Tests.ServiceTests
{
    public class ProcessAndScoreAnswerAsyncUnitTest
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUploadService> _mockUploadService;
        private readonly Mock<IAzureSpeechService> _mockAzureSpeechService;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IScoringWeightService> _mockScoringWeightService;
        private readonly SpeakingScoringService _service;

        public ProcessAndScoreAnswerAsyncUnitTest()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUploadService = new Mock<IUploadService>();
            _mockAzureSpeechService = new Mock<IAzureSpeechService>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockScoringWeightService = new Mock<IScoringWeightService>();

            _mockConfiguration.Setup(c => c["CloudinarySettings:CloudName"]).Returns("test-cloud");
            _mockConfiguration.Setup(c => c["ServiceUrls:NlpService"]).Returns((string)null);

            _service = new SpeakingScoringService(
                _mockUnitOfWork.Object,
                _mockUploadService.Object,
                _mockAzureSpeechService.Object,
                _mockHttpClientFactory.Object,
                _mockConfiguration.Object,
                _mockScoringWeightService.Object
            );
        }

        [Fact]
        public async Task ProcessAndScoreAnswerAsync_WhenQuestionNotFound_ShouldThrowException()
        {
            // Arrange
            var mockFile = CreateMockFile();
            _mockUploadService.Setup(x => x.UploadFileAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(new UploadResultDTO { PublicId = "test" });
            _mockUnitOfWork.Setup(x => x.Questions.GetAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Question, bool>>>(),
                It.IsAny<string>()))
                .ReturnsAsync((Question)null);

            // Act
            Func<Task> act = async () => await _service.ProcessAndScoreAnswerAsync(mockFile, 999, 1);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Question with ID 999 or its sample answer not found.");
        }

        [Fact]
        public async Task ProcessAndScoreAnswerAsync_WhenSampleAnswerIsNull_ShouldThrowException()
        {
            // Arrange
            var mockFile = CreateMockFile();
            var question = new Question { QuestionId = 1, SampleAnswer = null };
            
            _mockUploadService.Setup(x => x.UploadFileAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(new UploadResultDTO { PublicId = "test" });
            _mockUnitOfWork.Setup(x => x.Questions.GetAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Question, bool>>>(),
                It.IsAny<string>()))
                .ReturnsAsync(question);

            // Act
            Func<Task> act = async () => await _service.ProcessAndScoreAnswerAsync(mockFile, 1, 1);

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task ProcessAndScoreAnswerAsync_WhenTranscriptIsEmpty_ShouldReturnZeroScores()
        {
            // Arrange
            var mockFile = CreateMockFile();
            var question = new Question 
            { 
                QuestionId = 1, 
                SampleAnswer = "Sample",
                PartId = 1
            };
            var azureResult = new SpeechAnalysisDTO { Transcript = "." };

            _mockUploadService.Setup(x => x.UploadFileAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(new UploadResultDTO { PublicId = "test" });
            _mockUnitOfWork.Setup(x => x.Questions.GetAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Question, bool>>>(),
                It.IsAny<string>()))
                .ReturnsAsync(question);
            
            // Mock will be called 3 times by RetryAzureRecognitionAsync
            _mockAzureSpeechService.SetupSequence(x => x.AnalyzePronunciationFromUrlAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(azureResult)
                .ReturnsAsync(azureResult)
                .ReturnsAsync(azureResult);
            
            _mockUnitOfWork.Setup(x => x.UserAnswersSpeaking.AddAsync(It.IsAny<UserAnswerSpeaking>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.ProcessAndScoreAnswerAsync(mockFile, 1, 1);

            // Assert
            result.Transcript.Should().Contain("di");
            result.OverallScore.Should().Be(0);
        }

        [Fact]
        public async Task ProcessAndScoreAnswerAsync_WithValidTranscript_ShouldReturnScores()
        {
            // Arrange
            var mockFile = CreateMockFile();
            var question = new Question 
            { 
                QuestionId = 1, 
                SampleAnswer = "Sample",
                PartId = 1
            };
            var azureResult = new SpeechAnalysisDTO 
            { 
                Transcript = "Test transcript",
                PronunciationScore = 85,
                AccuracyScore = 90,
                FluencyScore = 88
            };
            var weights = new ScoringWeights
            {
                PronunciationWeight = 0.3f,
                AccuracyWeight = 0.2f,
                FluencyWeight = 0.2f,
                GrammarWeight = 0.1f,
                VocabularyWeight = 0.1f,
                ContentWeight = 0.1f
            };

            _mockUploadService.Setup(x => x.UploadFileAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(new UploadResultDTO { PublicId = "test" });
            _mockUnitOfWork.Setup(x => x.Questions.GetAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Question, bool>>>(),
                It.IsAny<string>()))
                .ReturnsAsync(question);
            
            // Azure will return valid transcript on first call, so retry exits immediately
            _mockAzureSpeechService.Setup(x => x.AnalyzePronunciationFromUrlAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(azureResult);
            
            // Setup GetWeightsForPart - partCode will be empty string because question.Part is null
            _mockScoringWeightService.Setup(x => x.GetWeightsForPart(It.IsAny<string>()))
                .Returns(weights);
            
            // Setup CalculateOverallScore - parameters should be double, not float
            _mockScoringWeightService.Setup(x => x.CalculateOverallScore(
                It.IsAny<ScoringWeights>(),
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<double>()))
                .Returns(85f);
            
            _mockUnitOfWork.Setup(x => x.UserAnswersSpeaking.AddAsync(It.IsAny<UserAnswerSpeaking>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.ProcessAndScoreAnswerAsync(mockFile, 1, 1);

            // Assert
            result.Transcript.Should().Be("Test transcript");
            result.OverallScore.Should().Be(85.0);
            result.PronunciationScore.Should().Be(85);
        }

        private IFormFile CreateMockFile()
        {
            var mock = new Mock<IFormFile>();
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write("test");
            writer.Flush();
            ms.Position = 0;
            mock.Setup(f => f.OpenReadStream()).Returns(ms);
            mock.Setup(f => f.FileName).Returns("test.mp3");
            mock.Setup(f => f.Length).Returns(ms.Length);
            return mock.Object;
        }
    }
}
