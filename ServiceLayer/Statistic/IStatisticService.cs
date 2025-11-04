using RepositoryLayer.Statistic;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Statistic
{
    public interface IStatisticService
    {
        Task<DashboardStatsDTO> GetDashboardStatsAsync();
        Task<RevenueChartDTO> GetRevenueChartDataAsync(int year);
        Task<UserGrowthChartDTO> GetUserGrowthChartDataAsync(int months);
        Task<PlanDistributionChartDTO> GetPlanDistributionDataAsync();
        Task<List<DailyAnalyticsDTO>> GetDailyAnalyticsAsync(int days);
    }

    public class DashboardStatsDTO
    {
        public decimal MonthlyRevenue { get; set; }
        public string MonthlyRevenueGrowth { get; set; }
        public int NewUsers { get; set; }
        public string NewUsersGrowth { get; set; }
        public decimal ProConversionRate { get; set; }
        public string ProConversionGrowth { get; set; }
        public decimal RetentionRate { get; set; }
        public string RetentionGrowth { get; set; }
    }

    public class RevenueChartDTO
    {
        public List<string> Labels { get; set; }
        public List<decimal> Data { get; set; }
        public decimal Total { get; set; }
    }

    public class UserGrowthChartDTO
    {
        public List<string> Labels { get; set; }
        public List<int> FreeUsers { get; set; }
        public List<int> ProUsers { get; set; }
    }

    public class PlanDistributionChartDTO
    {
        public List<string> Labels { get; set; }
        public List<int> Data { get; set; }
        public List<decimal> Percentages { get; set; }
    }
}
