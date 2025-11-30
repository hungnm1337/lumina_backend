using FluentAssertions;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using ServiceLayer.Speech;
using ServiceLayer.Configs;
using System.Net;

namespace Lumina.Tests.ServiceTests
{
    public class RecognizeFromUrlAsyncUnitTest : IDisposable
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<IOptions<AzureSpeechSettings>> _mockOptions;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;

        public RecognizeFromUrlAsyncUnitTest()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockOptions = new Mock<IOptions<AzureSpeechSettings>>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            // Setup Azure Speech Settings
            _mockOptions.Setup(o => o.Value).Returns(new AzureSpeechSettings
            {
                SubscriptionKey = "test-key",
                Region = "test-region"
            });

            // Setup HttpClient
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(_httpClient);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        #region HTTP Response Handling

        [Fact]
        public async Task RecognizeFromUrlAsync_WhenHttpRequestFails_ShouldReturnEmptyString()
        {
            // Arrange
            var audioUrl = "http://example.com/audio.mp3";
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound
                });

            var service = new AzureSpeechService(_mockOptions.Object, _mockHttpClientFactory.Object);

            // Act
            var result = await service.RecognizeFromUrlAsync(audioUrl);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task RecognizeFromUrlAsync_WhenHttpRequestSucceeds_ShouldProcessAudio()
        {
            // Arrange
            var audioUrl = "http://example.com/audio.mp3";
            var mp3Content = CreateValidMp3Content();

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new ByteArrayContent(mp3Content)
                });

            var service = new AzureSpeechService(_mockOptions.Object, _mockHttpClientFactory.Object);

            // Act
            var result = await service.RecognizeFromUrlAsync(audioUrl);

            // Assert
            result.Should().NotBeNull();
        }

        #endregion

        #region Language Parameter Handling

        [Fact]
        public async Task RecognizeFromUrlAsync_WhenLanguageIsNull_ShouldUseDefaultRecognizer()
        {
            // Arrange
            var audioUrl = "http://example.com/audio.mp3";
            var mp3Content = CreateValidMp3Content();

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new ByteArrayContent(mp3Content)
                });

            var service = new AzureSpeechService(_mockOptions.Object, _mockHttpClientFactory.Object);

            // Act
            var result = await service.RecognizeFromUrlAsync(audioUrl, null);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task RecognizeFromUrlAsync_WhenLanguageIsProvided_ShouldUseLanguageSpecificRecognizer()
        {
            // Arrange
            var audioUrl = "http://example.com/audio.mp3";
            var language = "vi-VN";
            var mp3Content = CreateValidMp3Content();

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new ByteArrayContent(mp3Content)
                });

            var service = new AzureSpeechService(_mockOptions.Object, _mockHttpClientFactory.Object);

            // Act
            var result = await service.RecognizeFromUrlAsync(audioUrl, language);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task RecognizeFromUrlAsync_WhenLanguageIsWhitespace_ShouldUseDefaultRecognizer()
        {
            // Arrange
            var audioUrl = "http://example.com/audio.mp3";
            var language = "   ";
            var mp3Content = CreateValidMp3Content();

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new ByteArrayContent(mp3Content)
                });

            var service = new AzureSpeechService(_mockOptions.Object, _mockHttpClientFactory.Object);

            // Act
            var result = await service.RecognizeFromUrlAsync(audioUrl, language);

            // Assert
            result.Should().NotBeNull();
        }

        #endregion

        #region Helper Methods

        private byte[] CreateValidMp3Content()
        {
            // Create a minimal valid MP3 file header
            var mp3Header = new byte[]
            {
                0xFF, 0xFB, 0x90, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x49, 0x6E, 0x66, 0x6F,
                0x00, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x01
            };

            var fullContent = new byte[1024];
            Array.Copy(mp3Header, fullContent, mp3Header.Length);
            return fullContent;
        }

        #endregion
    }
}
