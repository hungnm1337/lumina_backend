using DataLayer.DTOs.Analytics;
using Google.Apis.AnalyticsData.v1beta;
using Google.Apis.AnalyticsData.v1beta.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Analytics
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IConfiguration _configuration;
        private readonly string _propertyId;

        public AnalyticsService(IConfiguration configuration)
        {
            _configuration = configuration;
            _propertyId = _configuration["GoogleAnalytics:PropertyId"] ?? "properties/467664647";
        }

        private AnalyticsDataService CreateGoogleAnalyticsService()
        {
            var serviceAccountSection = _configuration.GetSection("GoogleAnalytics:ServiceAccount");

            if (!serviceAccountSection.Exists())
            {
                throw new InvalidOperationException("Google Analytics service account configuration not found in appsettings.json");
            }

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

        public async Task<KeyMetricsDTO> GetKeyMetricsAsync()
        {
            var service = CreateGoogleAnalyticsService();

            var request = new RunReportRequest
            {
                DateRanges = new List<DateRange>
                {
                    new DateRange { StartDate = "30daysAgo", EndDate = "today" }
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
                return new KeyMetricsDTO
                {
                    TotalUsers = int.Parse(row.MetricValues[0].Value),
                    NewUsers = int.Parse(row.MetricValues[1].Value),
                    Sessions = int.Parse(row.MetricValues[2].Value),
                    PageViews = int.Parse(row.MetricValues[3].Value),
                    AvgSessionDuration = Math.Round(double.Parse(row.MetricValues[4].Value), 1),
                    BounceRate = Math.Round(double.Parse(row.MetricValues[5].Value) * 100, 1)
                };
            }

            return new KeyMetricsDTO();
        }

        public async Task<RealtimeUsersDTO> GetRealtimeUsersAsync()
        {
            var service = CreateGoogleAnalyticsService();

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

            return new RealtimeUsersDTO { ActiveUsers = activeUsers };
        }

        public async Task<List<TopPageDTO>> GetTopPagesAsync()
        {
            var service = CreateGoogleAnalyticsService();

            var request = new RunReportRequest
            {
                DateRanges = new List<DateRange>
                {
                    new DateRange { StartDate = "30daysAgo", EndDate = "today" }
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

            var pages = new List<TopPageDTO>();
            if (response.Rows != null && response.Rows.Count > 0)
            {
                pages = response.Rows.Select(row => new TopPageDTO
                {
                    Title = row.DimensionValues[0].Value,
                    Path = row.DimensionValues[1].Value,
                    Views = int.Parse(row.MetricValues[0].Value),
                    Users = int.Parse(row.MetricValues[1].Value),
                    AvgDuration = Math.Round(double.Parse(row.MetricValues[2].Value), 1)
                }).ToList();
            }

            return pages;
        }

        public async Task<List<TrafficSourceDTO>> GetTrafficSourcesAsync()
        {
            var service = CreateGoogleAnalyticsService();

            var request = new RunReportRequest
            {
                DateRanges = new List<DateRange>
                {
                    new DateRange { StartDate = "30daysAgo", EndDate = "today" }
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

            var sources = new List<TrafficSourceDTO>();
            if (response.Rows != null && response.Rows.Count > 0)
            {
                sources = response.Rows.Select(row => new TrafficSourceDTO
                {
                    Source = row.DimensionValues[0].Value,
                    Medium = row.DimensionValues[1].Value,
                    Sessions = int.Parse(row.MetricValues[0].Value),
                    Users = int.Parse(row.MetricValues[1].Value)
                }).ToList();
            }

            return sources;
        }

        public async Task<List<DeviceStatsDTO>> GetDeviceStatsAsync()
        {
            var service = CreateGoogleAnalyticsService();

            var request = new RunReportRequest
            {
                DateRanges = new List<DateRange>
                {
                    new DateRange { StartDate = "30daysAgo", EndDate = "today" }
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

            var devices = new List<DeviceStatsDTO>();
            if (response.Rows != null && response.Rows.Count > 0)
            {
                devices = response.Rows.Select(row => new DeviceStatsDTO
                {
                    Device = row.DimensionValues[0].Value,
                    Users = int.Parse(row.MetricValues[0].Value),
                    Sessions = int.Parse(row.MetricValues[1].Value),
                    PageViews = int.Parse(row.MetricValues[2].Value)
                }).ToList();
            }

            return devices;
        }

        public async Task<List<CountryStatsDTO>> GetCountryStatsAsync()
        {
            var service = CreateGoogleAnalyticsService();

            var request = new RunReportRequest
            {
                DateRanges = new List<DateRange>
                {
                    new DateRange { StartDate = "30daysAgo", EndDate = "today" }
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

            var countries = new List<CountryStatsDTO>();
            if (response.Rows != null && response.Rows.Count > 0)
            {
                countries = response.Rows.Select(row => new CountryStatsDTO
                {
                    Country = row.DimensionValues[0].Value,
                    City = row.DimensionValues[1].Value,
                    Users = int.Parse(row.MetricValues[0].Value),
                    Sessions = int.Parse(row.MetricValues[1].Value)
                }).ToList();
            }

            return countries;
        }

        public async Task<List<DailyTrafficDTO>> GetDailyTrafficAsync()
        {
            var service = CreateGoogleAnalyticsService();

            var request = new RunReportRequest
            {
                DateRanges = new List<DateRange>
                {
                    new DateRange { StartDate = "30daysAgo", EndDate = "today" }
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

            var dailyData = new List<DailyTrafficDTO>();
            if (response.Rows != null && response.Rows.Count > 0)
            {
                dailyData = response.Rows.Select(row => new DailyTrafficDTO
                {
                    Date = row.DimensionValues[0].Value,
                    Users = int.Parse(row.MetricValues[0].Value),
                    Sessions = int.Parse(row.MetricValues[1].Value),
                    PageViews = int.Parse(row.MetricValues[2].Value)
                }).ToList();
            }

            return dailyData;
        }

        public async Task<List<BrowserStatsDTO>> GetBrowserStatsAsync()
        {
            var service = CreateGoogleAnalyticsService();

            var request = new RunReportRequest
            {
                DateRanges = new List<DateRange>
                {
                    new DateRange { StartDate = "30daysAgo", EndDate = "today" }
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

            var browsers = new List<BrowserStatsDTO>();
            if (response.Rows != null && response.Rows.Count > 0)
            {
                browsers = response.Rows.Select(row => new BrowserStatsDTO
                {
                    Browser = row.DimensionValues[0].Value,
                    Users = int.Parse(row.MetricValues[0].Value),
                    Sessions = int.Parse(row.MetricValues[1].Value)
                }).ToList();
            }

            return browsers;
        }
    }
}