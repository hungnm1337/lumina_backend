using Xunit;
using Moq;
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ServiceLayer.PictureCaptioning;

namespace Lumina.Test.Services
{
    public class GetCaptionFromImageUrlUnitTest
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly ImageCaptioningService _service;

        public GetCaptionFromImageUrlUnitTest()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("http://localhost:5000")
            };

            _service = new ImageCaptioningService(_httpClient);
        }

        #region Test Cases for Invalid ImageUrl

        [Fact]
        public async Task GetCaptionFromImageUrl_WhenImageUrlIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            string? imageUrl = null;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _service.GetCaptionFromImageUrl(imageUrl!)
            );

            Assert.Equal("imageUrl", exception.ParamName);

            // Verify HttpClient is never called
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async Task GetCaptionFromImageUrl_WhenImageUrlIsEmpty_ShouldThrowArgumentException()
        {
            // Arrange
            string imageUrl = string.Empty;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.GetCaptionFromImageUrl(imageUrl)
            );

            Assert.Equal("imageUrl", exception.ParamName);
            Assert.Contains("Image URL cannot be null or empty", exception.Message);

            // Verify HttpClient is never called
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async Task GetCaptionFromImageUrl_WhenImageUrlIsWhitespace_ShouldThrowArgumentException()
        {
            // Arrange
            string imageUrl = "   ";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.GetCaptionFromImageUrl(imageUrl)
            );

            Assert.Equal("imageUrl", exception.ParamName);
            Assert.Contains("Image URL cannot be null or empty", exception.Message);

            // Verify HttpClient is never called
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        #endregion

        #region Test Cases for Valid ImageUrl

        [Fact]
        public async Task GetCaptionFromImageUrl_WhenImageUrlIsValid_ShouldReturnCaption()
        {
            // Arrange
            string imageUrl = "https://example.com/image.jpg";
            var expectedCaption = "A beautiful sunset over the ocean";
            var responseObject = new { caption = expectedCaption };
            var responseJson = JsonConvert.SerializeObject(responseObject);

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri != null &&
                        req.RequestUri.AbsolutePath == "/caption"),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
                });

            // Act
            var result = await _service.GetCaptionFromImageUrl(imageUrl);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedCaption, result);

            // Verify HttpClient is called exactly once
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri != null &&
                    req.RequestUri.AbsolutePath == "/caption"),
                ItExpr.IsAny<CancellationToken>()
            );
        }
        #endregion       
    }
}

