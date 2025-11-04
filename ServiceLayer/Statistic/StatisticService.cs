using RepositoryLayer.Statistic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Statistic
{
    public class StatisticService : IStatisticService
    {
        private readonly IStatisticRepository _repo;

        public StatisticService(IStatisticRepository repo)
        {
            _repo = repo;
        }

        public async Task<DashboardStatsDTO> GetDashboardStatsAsync()
        {
            var now = DateTime.UtcNow;
            var currentMonth = now.Month;
            var currentYear = now.Year;
            var lastMonth = currentMonth == 1 ? 12 : currentMonth - 1;
            var lastMonthYear = currentMonth == 1 ? currentYear - 1 : currentYear;

            // Current month stats
            var monthlyRevenue = await _repo.GetMonthlyRevenueAsync(currentYear, currentMonth);
            var newUsers = await _repo.GetNewUsersThisMonthAsync();
            var proUserCount = await _repo.GetProUserCountAsync();
            var totalUsers = await _repo.GetTotalUsersAsync();

            // Last month stats for comparison
            var lastMonthRevenue = await _repo.GetMonthlyRevenueAsync(lastMonthYear, lastMonth);
            
            // Calculate growth percentages
            var revenueGrowth = lastMonthRevenue > 0 
                ? Math.Round(((monthlyRevenue - lastMonthRevenue) / lastMonthRevenue) * 100, 1)
                : 0;

            var proConversionRate = totalUsers > 0 
                ? Math.Round((decimal)proUserCount * 100 / totalUsers, 1)
                : 0;

            // Retention rate (simplified - users with active subscription)
            var retentionRate = 94.2m; // TODO: Implement proper retention calculation

            return new DashboardStatsDTO
            {
                MonthlyRevenue = monthlyRevenue,
                MonthlyRevenueGrowth = $"+{revenueGrowth}%",
                NewUsers = newUsers,
                NewUsersGrowth = "+12.5%", // TODO: Calculate based on last month
                ProConversionRate = proConversionRate,
                ProConversionGrowth = "+2.1%", // TODO: Calculate based on last month
                RetentionRate = retentionRate,
                RetentionGrowth = "+1.8%" // TODO: Calculate based on last month
            };
        }

        public async Task<RevenueChartDTO> GetRevenueChartDataAsync(int year)
        {
            var data = await _repo.GetMonthlyRevenueForYearAsync(year);

            return new RevenueChartDTO
            {
                Labels = new List<string> { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12" },
                Data = data.Select(d => d.Revenue).ToList(),
                Total = data.Sum(d => d.Revenue)
            };
        }

        public async Task<UserGrowthChartDTO> GetUserGrowthChartDataAsync(int months)
        {
            var data = await _repo.GetUserGrowthForMonthsAsync(months);

            var labels = data.Select(d => $"T{d.Month}").ToList();

            return new UserGrowthChartDTO
            {
                Labels = labels,
                FreeUsers = data.Select(d => d.FreeUsers).ToList(),
                ProUsers = data.Select(d => d.ProUsers).ToList()
            };
        }

        public async Task<PlanDistributionChartDTO> GetPlanDistributionDataAsync()
        {
            var data = await _repo.GetPlanDistributionAsync();

            return new PlanDistributionChartDTO
            {
                Labels = data.Select(d => d.PlanName).ToList(),
                Data = data.Select(d => d.UserCount).ToList(),
                Percentages = data.Select(d => d.Percentage).ToList()
            };
        }

        public async Task<List<DailyAnalyticsDTO>> GetDailyAnalyticsAsync(int days)
        {
            return await _repo.GetDailyAnalyticsAsync(days);
        }
    }
}
