using Xunit;
using Moq;
using Moq.Protected;
using ServiceLayer.PictureCaptioning;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
                Times.Never,
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
                Times.Never,
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
                Times.Never,
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
                Times.Once,
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri != null &&
                    req.RequestUri.AbsolutePath == "/caption"),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async Task GetCaptionFromImageUrl_WhenResponseObjectIsNull_ShouldReturnError()
        {
            // Arrange
            string imageUrl = "https://example.com/image.jpg";
            var responseJson = "{}"; // Empty JSON object

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
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
            Assert.Contains("Error: Caption field not found or null in API response", result);

            // Verify HttpClient is called exactly once
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once,
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async Task GetCaptionFromImageUrl_WhenCaptionIsNull_ShouldReturnError()
        {
            // Arrange
            string imageUrl = "https://example.com/image.jpg";
            var responseObject = new { caption = (string?)null };
            var responseJson = JsonConvert.SerializeObject(responseObject);

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
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
            Assert.Contains("Error: Caption field not found or null in API response", result);
        }

        [Fact]
        public async Task GetCaptionFromImageUrl_WhenCaptionIsEmpty_ShouldReturnError()
        {
            // Arrange
            string imageUrl = "https://example.com/image.jpg";
            var responseObject = new { caption = string.Empty };
            var responseJson = JsonConvert.SerializeObject(responseObject);

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
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
            Assert.Contains("Error: Caption field not found or null in API response", result);
        }

        #endregion

        #region Test Cases - HTTP Exception Handling

        [Fact]
        public async Task GetCaptionFromImageUrl_WhenHttpRequestExceptionOccurs_ShouldReturnError()
        {
            // Arrange
            string imageUrl = "https://example.com/image.jpg";
            var exceptionMessage = "Connection refused";

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException(exceptionMessage));

            // Act
            var result = await _service.GetCaptionFromImageUrl(imageUrl);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Error: Could not get caption from AI service", result);
            Assert.Contains(exceptionMessage, result);
        }

        [Fact]
        public async Task GetCaptionFromImageUrl_WhenHttpStatusCodeIsNotSuccess_ShouldReturnError()
        {
            // Arrange
            string imageUrl = "https://example.com/image.jpg";

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent("Internal Server Error")
                });

            // Act
            var result = await _service.GetCaptionFromImageUrl(imageUrl);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Error", result);
        }

        [Fact]
        public async Task GetCaptionFromImageUrl_WhenUnexpectedExceptionOccurs_ShouldReturnError()
        {
            // Arrange
            string imageUrl = "https://example.com/image.jpg";
            var exceptionMessage = "Unexpected error";

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new InvalidOperationException(exceptionMessage));

            // Act
            var result = await _service.GetCaptionFromImageUrl(imageUrl);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Error: An unexpected error occurred", result);
            Assert.Contains(exceptionMessage, result);
        }

        #endregion
    }
}

