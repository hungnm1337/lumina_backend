using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.Analytics
{
    public class KeyMetricsDTO
    {
        public int TotalUsers { get; set; }
        public int NewUsers { get; set; }
        public int Sessions { get; set; }
        public int PageViews { get; set; }
        public double AvgSessionDuration { get; set; }
        public double BounceRate { get; set; }
    }
    public class RealtimeUsersDTO
    {
        public int ActiveUsers { get; set; }
    }

    public class TopPageDTO
    {
        public string Title { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public int Views { get; set; }
        public int Users { get; set; }
        public double AvgDuration { get; set; }
    }

    public class TrafficSourceDTO
    {
        public string Source { get; set; } = string.Empty;
        public string Medium { get; set; } = string.Empty;
        public int Sessions { get; set; }
        public int Users { get; set; }
    }

    public class DeviceStatsDTO
    {
        public string Device { get; set; } = string.Empty;
        public int Users { get; set; }
        public int Sessions { get; set; }
        public int PageViews { get; set; }
    }

    public class CountryStatsDTO
    {
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int Users { get; set; }
        public int Sessions { get; set; }
    }

    public class DailyTrafficDTO
    {
        public string Date { get; set; } = string.Empty;
        public int Users { get; set; }
        public int Sessions { get; set; }
        public int PageViews { get; set; }
    }
    public class BrowserStatsDTO
    {
        public string Browser { get; set; } = string.Empty;
        public int Users { get; set; }
        public int Sessions { get; set; }
    }
}
