
using DataLayer.DTOs.Analytics;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Analytics
{
    public interface IAnalyticsService
    {
        Task<KeyMetricsDTO> GetKeyMetricsAsync();
        Task<RealtimeUsersDTO> GetRealtimeUsersAsync();
        Task<List<TopPageDTO>> GetTopPagesAsync();
        Task<List<TrafficSourceDTO>> GetTrafficSourcesAsync();
        Task<List<DeviceStatsDTO>> GetDeviceStatsAsync();
        Task<List<CountryStatsDTO>> GetCountryStatsAsync();
        Task<List<DailyTrafficDTO>> GetDailyTrafficAsync();
        Task<List<BrowserStatsDTO>> GetBrowserStatsAsync();
    }
}