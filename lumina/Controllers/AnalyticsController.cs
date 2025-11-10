using Google.Apis.AnalyticsData.v1beta;
using Google.Apis.AnalyticsData.v1beta.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace lumina.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _propertyId;

        public AnalyticsController(IConfiguration configuration)
        {
            _configuration = configuration;
            _propertyId = _configuration["GoogleAnalytics:PropertyId"] ?? "properties/467664647";
        }

        private AnalyticsDataService GetAnalyticsService()
        {
            // ✅ Đọc service account từ appsettings.json
            var serviceAccountSection = _configuration.GetSection("GoogleAnalytics:ServiceAccount");
            
            if (!serviceAccountSection.Exists())
            {
                throw new InvalidOperationException("Google Analytics service account configuration not found in appsettings.json");
            }

            // Convert IConfiguration section thành JSON string
            var serviceAccountJson = JsonConvert.SerializeObject(new
            {
                type = serviceAccountSection["type"],
                project_id = serviceAccountSection["project_id"],
                private_key_id = serviceAccountSection["private_key_id"],
                private_key = serviceAccountSection["private_key"],
                client_email = serviceAccountSection["client_email"],
                client_id = serviceAccountSection["client_id"],
                auth_uri = serviceAccountSection["auth_uri"],
                token_uri = serviceAccountSection["token_uri"],
                auth_provider_x509_cert_url = serviceAccountSection["auth_provider_x509_cert_url"],
                client_x509_cert_url = serviceAccountSection["client_x509_cert_url"],
                universe_domain = serviceAccountSection["universe_domain"]
            });

            var credential = GoogleCredential.FromJson(serviceAccountJson)
                .CreateScoped(AnalyticsDataService.Scope.AnalyticsReadonly);

            return new AnalyticsDataService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "LuminaTOEIC"
            });
        }

        /// <summary>
        /// Lấy key metrics tổng quan (7 ngày qua)
        /// </summary>
        [HttpGet("key-metrics")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetKeyMetrics()
        {
            try
            {
                var service = GetAnalyticsService();

                var request = new RunReportRequest
                {
                    DateRanges = new List<DateRange>
                    {
                        new DateRange { StartDate = "7daysAgo", EndDate = "today" }
                    },
                    Metrics = new List<Metric>
                    {
                        new Metric { Name = "totalUsers" },
                        new Metric { Name = "newUsers" },
                        new Metric { Name = "sessions" },
                        new Metric { Name = "screenPageViews" },
                        new Metric { Name = "averageSessionDuration" },
                        new Metric { Name = "bounceRate" }
                    }
                };

                var response = await service.Properties.RunReport(request, _propertyId).ExecuteAsync();

                if (response.Rows != null && response.Rows.Count > 0)
                {
                    var row = response.Rows[0];
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            totalUsers = int.Parse(row.MetricValues[0].Value),
                            newUsers = int.Parse(row.MetricValues[1].Value),
                            sessions = int.Parse(row.MetricValues[2].Value),
                            pageViews = int.Parse(row.MetricValues[3].Value),
                            avgSessionDuration = Math.Round(double.Parse(row.MetricValues[4].Value), 1),
                            bounceRate = Math.Round(double.Parse(row.MetricValues[5].Value) * 100, 1)
                        }
                    });
                }

                return Ok(new { success = false, message = "No data available" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy số người dùng đang online (realtime)
        /// </summary>
        [HttpGet("realtime")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRealtimeUsers()
        {
            try
            {
                var service = GetAnalyticsService();

                var request = new RunRealtimeReportRequest
                {
                    Metrics = new List<Metric>
                    {
                        new Metric { Name = "activeUsers" }
                    }
                };

                var response = await service.Properties.RunRealtimeReport(request, _propertyId).ExecuteAsync();
                
                var activeUsers = response.Rows != null && response.Rows.Count > 0
                    ? int.Parse(response.Rows[0].MetricValues[0].Value)
                    : 0;

                return Ok(new
                {
                    success = true,
                    data = new { activeUsers }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy top 10 trang được xem nhiều nhất (7 ngày qua)
        /// </summary>
        [HttpGet("top-pages")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTopPages()
        {
            try
            {
                var service = GetAnalyticsService();

                var request = new RunReportRequest
                {
                    DateRanges = new List<DateRange>
                    {
                        new DateRange { StartDate = "7daysAgo", EndDate = "today" }
                    },
                    Dimensions = new List<Dimension>
                    {
                        new Dimension { Name = "pageTitle" },
                        new Dimension { Name = "pagePath" }
                    },
                    Metrics = new List<Metric>
                    {
                        new Metric { Name = "screenPageViews" },
                        new Metric { Name = "activeUsers" },
                        new Metric { Name = "averageSessionDuration" }
                    },
                    OrderBys = new List<OrderBy>
                    {
                        new OrderBy
                        {
                            Metric = new MetricOrderBy { MetricName = "screenPageViews" },
                            Desc = true
                        }
                    },
                    Limit = 10
                };

                var response = await service.Properties.RunReport(request, _propertyId).ExecuteAsync();

                var pages = new List<object>();
                if (response.Rows != null && response.Rows.Count > 0)
                {
                    pages = response.Rows.Select(row => new
                    {
                        title = row.DimensionValues[0].Value,
                        path = row.DimensionValues[1].Value,
                        views = int.Parse(row.MetricValues[0].Value),
                        users = int.Parse(row.MetricValues[1].Value),
                        avgDuration = Math.Round(double.Parse(row.MetricValues[2].Value), 1)
                    }).ToList<object>();
                }

                return Ok(new { success = true, data = pages });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy nguồn traffic (7 ngày qua)
        /// </summary>
        [HttpGet("traffic-sources")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTrafficSources()
        {
            try
            {
                var service = GetAnalyticsService();

                var request = new RunReportRequest
                {
                    DateRanges = new List<DateRange>
                    {
                        new DateRange { StartDate = "7daysAgo", EndDate = "today" }
                    },
                    Dimensions = new List<Dimension>
                    {
                        new Dimension { Name = "sessionSource" },
                        new Dimension { Name = "sessionMedium" }
                    },
                    Metrics = new List<Metric>
                    {
                        new Metric { Name = "sessions" },
                        new Metric { Name = "activeUsers" }
                    },
                    OrderBys = new List<OrderBy>
                    {
                        new OrderBy
                        {
                            Metric = new MetricOrderBy { MetricName = "sessions" },
                            Desc = true
                        }
                    },
                    Limit = 10
                };

                var response = await service.Properties.RunReport(request, _propertyId).ExecuteAsync();

                var sources = new List<object>();
                if (response.Rows != null && response.Rows.Count > 0)
                {
                    sources = response.Rows.Select(row => new
                    {
                        source = row.DimensionValues[0].Value,
                        medium = row.DimensionValues[1].Value,
                        sessions = int.Parse(row.MetricValues[0].Value),
                        users = int.Parse(row.MetricValues[1].Value)
                    }).ToList<object>();
                }

                return Ok(new { success = true, data = sources });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy thống kê theo thiết bị (7 ngày qua)
        /// </summary>
        [HttpGet("devices")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetDeviceStats()
        {
            try
            {
                var service = GetAnalyticsService();

                var request = new RunReportRequest
                {
                    DateRanges = new List<DateRange>
                    {
                        new DateRange { StartDate = "7daysAgo", EndDate = "today" }
                    },
                    Dimensions = new List<Dimension>
                    {
                        new Dimension { Name = "deviceCategory" }
                    },
                    Metrics = new List<Metric>
                    {
                        new Metric { Name = "activeUsers" },
                        new Metric { Name = "sessions" },
                        new Metric { Name = "screenPageViews" }
                    },
                    OrderBys = new List<OrderBy>
                    {
                        new OrderBy
                        {
                            Metric = new MetricOrderBy { MetricName = "activeUsers" },
                            Desc = true
                        }
                    }
                };

                var response = await service.Properties.RunReport(request, _propertyId).ExecuteAsync();

                var devices = new List<object>();
                if (response.Rows != null && response.Rows.Count > 0)
                {
                    devices = response.Rows.Select(row => new
                    {
                        device = row.DimensionValues[0].Value,
                        users = int.Parse(row.MetricValues[0].Value),
                        sessions = int.Parse(row.MetricValues[1].Value),
                        pageViews = int.Parse(row.MetricValues[2].Value)
                    }).ToList<object>();
                }

                return Ok(new { success = true, data = devices });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy thống kê theo quốc gia (7 ngày qua)
        /// </summary>
        [HttpGet("countries")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetCountryStats()
        {
            try
            {
                var service = GetAnalyticsService();

                var request = new RunReportRequest
                {
                    DateRanges = new List<DateRange>
                    {
                        new DateRange { StartDate = "7daysAgo", EndDate = "today" }
                    },
                    Dimensions = new List<Dimension>
                    {
                        new Dimension { Name = "country" },
                        new Dimension { Name = "city" }
                    },
                    Metrics = new List<Metric>
                    {
                        new Metric { Name = "activeUsers" },
                        new Metric { Name = "sessions" }
                    },
                    OrderBys = new List<OrderBy>
                    {
                        new OrderBy
                        {
                            Metric = new MetricOrderBy { MetricName = "activeUsers" },
                            Desc = true
                        }
                    },
                    Limit = 10
                };

                var response = await service.Properties.RunReport(request, _propertyId).ExecuteAsync();

                var countries = new List<object>();
                if (response.Rows != null && response.Rows.Count > 0)
                {
                    countries = response.Rows.Select(row => new
                    {
                        country = row.DimensionValues[0].Value,
                        city = row.DimensionValues[1].Value,
                        users = int.Parse(row.MetricValues[0].Value),
                        sessions = int.Parse(row.MetricValues[1].Value)
                    }).ToList<object>();
                }

                return Ok(new { success = true, data = countries });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy traffic theo ngày (7 ngày qua)
        /// </summary>
        [HttpGet("daily-traffic")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetDailyTraffic()
        {
            try
            {
                var service = GetAnalyticsService();

                var request = new RunReportRequest
                {
                    DateRanges = new List<DateRange>
                    {
                        new DateRange { StartDate = "7daysAgo", EndDate = "today" }
                    },
                    Dimensions = new List<Dimension>
                    {
                        new Dimension { Name = "date" }
                    },
                    Metrics = new List<Metric>
                    {
                        new Metric { Name = "activeUsers" },
                        new Metric { Name = "sessions" },
                        new Metric { Name = "screenPageViews" }
                    },
                    OrderBys = new List<OrderBy>
                    {
                        new OrderBy
                        {
                            Dimension = new DimensionOrderBy { DimensionName = "date" },
                            Desc = false
                        }
                    }
                };

                var response = await service.Properties.RunReport(request, _propertyId).ExecuteAsync();

                var dailyData = new List<object>();
                if (response.Rows != null && response.Rows.Count > 0)
                {
                    dailyData = response.Rows.Select(row => new
                    {
                        date = row.DimensionValues[0].Value,
                        users = int.Parse(row.MetricValues[0].Value),
                        sessions = int.Parse(row.MetricValues[1].Value),
                        pageViews = int.Parse(row.MetricValues[2].Value)
                    }).ToList<object>();
                }

                return Ok(new { success = true, data = dailyData });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy thống kê theo browser (7 ngày qua)
        /// </summary>
        [HttpGet("browsers")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetBrowserStats()
        {
            try
            {
                var service = GetAnalyticsService();

                var request = new RunReportRequest
                {
                    DateRanges = new List<DateRange>
                    {
                        new DateRange { StartDate = "7daysAgo", EndDate = "today" }
                    },
                    Dimensions = new List<Dimension>
                    {
                        new Dimension { Name = "browser" }
                    },
                    Metrics = new List<Metric>
                    {
                        new Metric { Name = "activeUsers" },
                        new Metric { Name = "sessions" }
                    },
                    OrderBys = new List<OrderBy>
                    {
                        new OrderBy
                        {
                            Metric = new MetricOrderBy { MetricName = "activeUsers" },
                            Desc = true
                        }
                    },
                    Limit = 10
                };

                var response = await service.Properties.RunReport(request, _propertyId).ExecuteAsync();

                var browsers = new List<object>();
                if (response.Rows != null && response.Rows.Count > 0)
                {
                    browsers = response.Rows.Select(row => new
                    {
                        browser = row.DimensionValues[0].Value,
                        users = int.Parse(row.MetricValues[0].Value),
                        sessions = int.Parse(row.MetricValues[1].Value)
                    }).ToList<object>();
                }

                return Ok(new { success = true, data = browsers });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}