using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RepositoryLayer.Statistic
{
    public interface IStatisticRepository
    {
        // Dashboard Stats
        Task<int> GetTotalUsersAsync();
        Task<int> GetNewUsersThisMonthAsync();
        Task<int> GetProUserCountAsync();
        Task<decimal> GetMonthlyRevenueAsync(int year, int month);
        
        // Revenue Chart
        Task<List<MonthlyRevenueDTO>> GetMonthlyRevenueForYearAsync(int year);
        
        // User Growth
        Task<List<UserGrowthDTO>> GetUserGrowthForMonthsAsync(int months);
        
        // Plan Distribution
        Task<List<PlanDistributionDTO>> GetPlanDistributionAsync();
        
        // Daily Analytics
        Task<List<DailyAnalyticsDTO>> GetDailyAnalyticsAsync(int days);
    }

    public class MonthlyRevenueDTO
    {
        public int Month { get; set; }
        public decimal Revenue { get; set; }
    }

    public class UserGrowthDTO
    {
        public int Month { get; set; }
        public int FreeUsers { get; set; }
        public int ProUsers { get; set; }
    }

    public class PlanDistributionDTO
    {
        public string PlanName { get; set; }
        public int UserCount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class DailyAnalyticsDTO
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int NewUsers { get; set; }
        public int ProConversions { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal TrendPercentage { get; set; }
    }
}
