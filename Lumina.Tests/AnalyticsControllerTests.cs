using DataLayer.DTOs.Analytics;
using lumina.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ServiceLayer.Analytics;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Lumina.Tests
{
    public class AnalyticsControllerTests
    {
        private readonly Mock<IAnalyticsService> _mockAnalyticsService;
        private readonly AnalyticsController _controller;

        public AnalyticsControllerTests()
        {
            _mockAnalyticsService = new Mock<IAnalyticsService>();
            _controller = new AnalyticsController(_mockAnalyticsService.Object);
            SetupControllerContext();
        }

        #region Helper Methods

        private void SetupControllerContext()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        private static object GetPropertyValue(object obj, string propertyName)
        {
            return obj?.GetType().GetProperty(propertyName)?.GetValue(obj, null)!;
        }

        #endregion

        #region GetKeyMetrics Tests

        [Fact]
        public async Task GetKeyMetrics_Success_ReturnsOkWithData()
        {
            // Arrange
            var expectedData = new KeyMetricsDTO
            {
                TotalUsers = 15000,
                NewUsers = 5000,
                Sessions = 8500,
                PageViews = 25000,
                AvgSessionDuration = 180.5,
                BounceRate = 35.2
            };

            _mockAnalyticsService.Setup(s => s.GetKeyMetricsAsync())
                .ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetKeyMetrics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            
            var body = okResult.Value;
            Assert.True((bool)GetPropertyValue(body!, "success"));
            Assert.NotNull(GetPropertyValue(body!, "data"));
            
            _mockAnalyticsService.Verify(s => s.GetKeyMetricsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetKeyMetrics_ServiceThrowsException_Returns500()
        {
            // Arrange
            _mockAnalyticsService.Setup(s => s.GetKeyMetricsAsync())
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.GetKeyMetrics();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            
            var body = statusCodeResult.Value;
            Assert.False((bool)GetPropertyValue(body!, "success"));
            Assert.Equal("Service error", GetPropertyValue(body!, "message"));
        }

        [Fact]
        public async Task GetKeyMetrics_ServiceThrowsInvalidOperationException_Returns500()
        {
            // Arrange
            _mockAnalyticsService.Setup(s => s.GetKeyMetricsAsync())
                .ThrowsAsync(new InvalidOperationException("Configuration not found"));

            // Act
            var result = await _controller.GetKeyMetrics();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            
            var body = statusCodeResult.Value;
            Assert.False((bool)GetPropertyValue(body!, "success"));
            Assert.Contains("Configuration not found", GetPropertyValue(body!, "message")?.ToString());
        }

        [Fact]
        public async Task GetKeyMetrics_ServiceReturnsEmptyData_ReturnsOkWithEmptyObject()
        {
            // Arrange
            _mockAnalyticsService.Setup(s => s.GetKeyMetricsAsync())
                .ReturnsAsync(new KeyMetricsDTO());

            // Act
            var result = await _controller.GetKeyMetrics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            
            var body = okResult.Value;
            Assert.True((bool)GetPropertyValue(body!, "success"));
        }

        #endregion

        #region GetRealtimeUsers Tests

        [Fact]
        public async Task GetRealtimeUsers_Success_ReturnsOkWithActiveUsers()
        {
            // Arrange
            var expectedData = new RealtimeUsersDTO { ActiveUsers = 245 };

            _mockAnalyticsService.Setup(s => s.GetRealtimeUsersAsync())
                .ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetRealtimeUsers();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            
            var body = okResult.Value;
            Assert.True((bool)GetPropertyValue(body!, "success"));
            Assert.NotNull(GetPropertyValue(body!, "data"));
        }

        [Fact]
        public async Task GetRealtimeUsers_ServiceThrowsException_Returns500()
        {
            // Arrange
            _mockAnalyticsService.Setup(s => s.GetRealtimeUsersAsync())
                .ThrowsAsync(new Exception("Network error"));

            // Act
            var result = await _controller.GetRealtimeUsers();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            
            var body = statusCodeResult.Value;
            Assert.False((bool)GetPropertyValue(body!, "success"));
            Assert.Equal("Network error", GetPropertyValue(body!, "message"));
        }

        [Fact]
        public async Task GetRealtimeUsers_ServiceReturnsZeroUsers_ReturnsOkWithZero()
        {
            // Arrange
            _mockAnalyticsService.Setup(s => s.GetRealtimeUsersAsync())
                .ReturnsAsync(new RealtimeUsersDTO { ActiveUsers = 0 });

            // Act
            var result = await _controller.GetRealtimeUsers();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            
            var body = okResult.Value;
            Assert.True((bool)GetPropertyValue(body!, "success"));
        }

        #endregion

        #region GetTopPages Tests

        [Fact]
        public async Task GetTopPages_Success_ReturnsListOfPages()
        {
            // Arrange
            var expectedData = new List<TopPageDTO>
            {
                new TopPageDTO { Title = "Home", Path = "/", Views = 5000, Users = 2000, AvgDuration = 120.5 },
                new TopPageDTO { Title = "About", Path = "/about", Views = 3000, Users = 1500, AvgDuration = 90.2 }
            };

            _mockAnalyticsService.Setup(s => s.GetTopPagesAsync())
                .ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetTopPages();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            
            var body = okResult.Value;
            Assert.True((bool)GetPropertyValue(body!, "success"));
            Assert.NotNull(GetPropertyValue(body!, "data"));
        }

        [Fact]
        public async Task GetTopPages_ServiceThrowsException_Returns500()
        {
            // Arrange
            _mockAnalyticsService.Setup(s => s.GetTopPagesAsync())
                .ThrowsAsync(new Exception("API error"));

            // Act
            var result = await _controller.GetTopPages();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            
            var body = statusCodeResult.Value;
            Assert.False((bool)GetPropertyValue(body!, "success"));
            Assert.Equal("API error", GetPropertyValue(body!, "message"));
        }

        [Fact]
        public async Task GetTopPages_EmptyList_ReturnsOkWithEmptyData()
        {
            // Arrange
            _mockAnalyticsService.Setup(s => s.GetTopPagesAsync())
                .ReturnsAsync(new List<TopPageDTO>());

            // Act
            var result = await _controller.GetTopPages();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            
            var body = okResult.Value;
            Assert.True((bool)GetPropertyValue(body!, "success"));
        }

        [Fact]
        public async Task GetTopPages_ServiceReturnsNull_ReturnsOkWithNull()
        {
            // Arrange
            _mockAnalyticsService.Setup(s => s.GetTopPagesAsync())
                .ReturnsAsync((List<TopPageDTO>)null!);

            // Act
            var result = await _controller.GetTopPages();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        #endregion

        #region GetTrafficSources Tests

        [Fact]
        public async Task GetTrafficSources_Success_ReturnsListOfSources()
        {
            // Arrange
            var expectedData = new List<TrafficSourceDTO>
            {
                new TrafficSourceDTO { Source = "google", Medium = "organic", Sessions = 5000, Users = 3000 },
                new TrafficSourceDTO { Source = "direct", Medium = "none", Sessions = 3000, Users = 2000 }
            };

            _mockAnalyticsService.Setup(s => s.GetTrafficSourcesAsync())
                .ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetTrafficSources();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            
            var body = okResult.Value;
            Assert.True((bool)GetPropertyValue(body!, "success"));
            Assert.NotNull(GetPropertyValue(body!, "data"));
        }

        [Fact]
        public async Task GetTrafficSources_ServiceThrowsException_Returns500()
        {
            // Arrange
            _mockAnalyticsService.Setup(s => s.GetTrafficSourcesAsync())
                .ThrowsAsync(new Exception("Connection timeout"));

            // Act
            var result = await _controller.GetTrafficSources();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            
            var body = statusCodeResult.Value;
            Assert.False((bool)GetPropertyValue(body!, "success"));
            Assert.Equal("Connection timeout", GetPropertyValue(body!, "message"));
        }

        [Fact]
        public async Task GetTrafficSources_ServiceReturnsEmptyList_ReturnsOkWithEmptyArray()
        {
            // Arrange
            _mockAnalyticsService.Setup(s => s.GetTrafficSourcesAsync())
                .ReturnsAsync(new List<TrafficSourceDTO>());

            // Act
            var result = await _controller.GetTrafficSources();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        #endregion

        #region GetDeviceStats Tests

        [Fact]
        public async Task GetDeviceStats_Success_ReturnsDeviceDistribution()
        {
            // Arrange
            var expectedData = new List<DeviceStatsDTO>
            {
                new DeviceStatsDTO { Device = "mobile", Users = 10000, Sessions = 15000, PageViews = 30000 },
                new DeviceStatsDTO { Device = "desktop", Users = 5000, Sessions = 7000, PageViews = 15000 }
            };

            _mockAnalyticsService.Setup(s => s.GetDeviceStatsAsync())
                .ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetDeviceStats();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            
            var body = okResult.Value;
            Assert.True((bool)GetPropertyValue(body!, "success"));
            Assert.NotNull(GetPropertyValue(body!, "data"));
        }

        [Fact]
        public async Task GetDeviceStats_ServiceThrowsException_Returns500()
        {
            // Arrange
            _mockAnalyticsService.Setup(s => s.GetDeviceStatsAsync())
                .ThrowsAsync(new Exception("Service unavailable"));

            // Act
            var result = await _controller.GetDeviceStats();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            
            var body = statusCodeResult.Value;
            Assert.False((bool)GetPropertyValue(body!, "success"));
            Assert.Equal("Service unavailable", GetPropertyValue(body!, "message"));
        }

        [Fact]
        public async Task GetDeviceStats_ServiceReturnsEmptyList_ReturnsOkWithEmptyArray()
        {
            // Arrange
            _mockAnalyticsService.Setup(s => s.GetDeviceStatsAsync())
                .ReturnsAsync(new List<DeviceStatsDTO>());

            // Act
            var result = await _controller.GetDeviceStats();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        #endregion

        #region GetCountryStats Tests

        [Fact]
        public async Task GetCountryStats_Success_ReturnsCountryList()
        {
            // Arrange
            var expectedData = new List<CountryStatsDTO>
            {
                new CountryStatsDTO { Country = "Vietnam", City = "Ho Chi Minh City", Users = 20000, Sessions = 30000 },
                new CountryStatsDTO { Country = "United States", City = "New York", Users = 5000, Sessions = 7000 }
            };

            _mockAnalyticsService.Setup(s => s.GetCountryStatsAsync())
                .ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetCountryStats();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            
            var body = okResult.Value;
            Assert.True((bool)GetPropertyValue(body!, "success"));
            Assert.NotNull(GetPropertyValue(body!, "data"));
        }

        [Fact]
        public async Task GetCountryStats_ServiceThrowsException_Returns500()
        {
            // Arrange
            _mockAnalyticsService.Setup(s => s.GetCountryStatsAsync())
                .ThrowsAsync(new Exception("Authentication failed"));

            // Act
            var result = await _controller.GetCountryStats();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            
            var body = statusCodeResult.Value;
            Assert.False((bool)GetPropertyValue(body!, "success"));
            Assert.Equal("Authentication failed", GetPropertyValue(body!, "message"));
        }

        [Fact]
        public async Task GetCountryStats_ServiceReturnsEmptyList_ReturnsOkWithEmptyArray()
        {
            // Arrange
            _mockAnalyticsService.Setup(s => s.GetCountryStatsAsync())
                .ReturnsAsync(new List<CountryStatsDTO>());

            // Act
            var result = await _controller.GetCountryStats();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        #endregion

        #region GetDailyTraffic Tests

        [Fact]
        public async Task GetDailyTraffic_Success_ReturnsTimeSeriesData()
        {
            // Arrange
            var expectedData = new List<DailyTrafficDTO>
            {
                new DailyTrafficDTO { Date = "20241110", Users = 5000, Sessions = 7000, PageViews = 15000 },
                new DailyTrafficDTO { Date = "20241111", Users = 5500, Sessions = 7500, PageViews = 16000 }
            };

            _mockAnalyticsService.Setup(s => s.GetDailyTrafficAsync())
                .ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetDailyTraffic();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            
            var body = okResult.Value;
            Assert.True((bool)GetPropertyValue(body!, "success"));
            Assert.NotNull(GetPropertyValue(body!, "data"));
        }

        [Fact]
        public async Task GetDailyTraffic_ServiceThrowsException_Returns500()
        {
            // Arrange
            _mockAnalyticsService.Setup(s => s.GetDailyTrafficAsync())
                .ThrowsAsync(new Exception("Rate limit exceeded"));

            // Act
            var result = await _controller.GetDailyTraffic();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            
            var body = statusCodeResult.Value;
            Assert.False((bool)GetPropertyValue(body!, "success"));
            Assert.Equal("Rate limit exceeded", GetPropertyValue(body!, "message"));
        }

        [Fact]
        public async Task GetDailyTraffic_ServiceReturnsEmptyList_ReturnsOkWithEmptyArray()
        {
            // Arrange
            _mockAnalyticsService.Setup(s => s.GetDailyTrafficAsync())
                .ReturnsAsync(new List<DailyTrafficDTO>());

            // Act
            var result = await _controller.GetDailyTraffic();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        #endregion

        #region GetBrowserStats Tests

        [Fact]
        public async Task GetBrowserStats_Success_ReturnsBrowserDistribution()
        {
            // Arrange
            var expectedData = new List<BrowserStatsDTO>
            {
                new BrowserStatsDTO { Browser = "Chrome", Users = 15000, Sessions = 20000 },
                new BrowserStatsDTO { Browser = "Safari", Users = 5000, Sessions = 7000 }
            };

            _mockAnalyticsService.Setup(s => s.GetBrowserStatsAsync())
                .ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetBrowserStats();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            
            var body = okResult.Value;
            Assert.True((bool)GetPropertyValue(body!, "success"));
            Assert.NotNull(GetPropertyValue(body!, "data"));
        }

        [Fact]
        public async Task GetBrowserStats_ServiceThrowsException_Returns500()
        {
            // Arrange
            _mockAnalyticsService.Setup(s => s.GetBrowserStatsAsync())
                .ThrowsAsync(new Exception("Timeout"));

            // Act
            var result = await _controller.GetBrowserStats();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            
            var body = statusCodeResult.Value;
            Assert.False((bool)GetPropertyValue(body!, "success"));
            Assert.Equal("Timeout", GetPropertyValue(body!, "message"));
        }

        [Fact]
        public async Task GetBrowserStats_ServiceReturnsEmptyList_ReturnsOkWithEmptyArray()
        {
            // Arrange
            _mockAnalyticsService.Setup(s => s.GetBrowserStatsAsync())
                .ReturnsAsync(new List<BrowserStatsDTO>());

            // Act
            var result = await _controller.GetBrowserStats();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidService_CreatesController()
        {
            // Arrange & Act
            var controller = new AnalyticsController(_mockAnalyticsService.Object);

            // Assert
            Assert.NotNull(controller);
        }

        #endregion
    }
}