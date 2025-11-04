using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RepositoryLayer.Statistic
{
    public class StatisticRepository : IStatisticRepository
    {
        private readonly LuminaSystemContext _context;

        public StatisticRepository(LuminaSystemContext context)
        {
            _context = context;
        }

        public async Task<int> GetTotalUsersAsync()
        {
            return await _context.Users.Where(u => u.RoleId == 4).CountAsync();
        }

        public async Task<int> GetNewUsersThisMonthAsync()
        {
            var now = DateTime.UtcNow;
            var firstOfMonth = new DateTime(now.Year, now.Month, 1);
            
            return await _context.Users
                .Where(u => u.RoleId == 4 && 
                            u.Accounts.Any(a => a.CreateAt >= firstOfMonth))
                .CountAsync();
        }

        public async Task<int> GetProUserCountAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.Subscriptions
                .Where(s => s.Status == "Active" && 
                            s.EndTime >= now)
                .Select(s => s.UserId)
                .Distinct()
                .CountAsync();
        }

        // ✅ Tính doanh thu theo tháng (chia đều từ subscription)
        public async Task<decimal> GetMonthlyRevenueAsync(int year, int month)
        {
            var startOfMonth = new DateTime(year, month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            // Lấy tất cả payments success
            var payments = await _context.Payments
                .Where(p => p.Status == "Success" && p.PackageId != null)
                .Include(p => p.Package)
                .ToListAsync();

            decimal totalRevenue = 0;

            foreach (var payment in payments)
            {
                if (payment.Package == null) continue;

                var durationMonths = payment.Package.DurationInDays / 30.0;
                if (durationMonths == 0) continue; // Tránh chia cho 0

                var revenuePerMonth = payment.Amount / (decimal)durationMonths;

                var paymentDate = payment.CreatedAt.Date;
                var subscriptionEnd = paymentDate.AddDays((double)payment.Package.DurationInDays);

                // ✅ Kiểm tra xem subscription có overlap với tháng này không
                // Subscription phải bắt đầu trước hoặc trong tháng này
                // VÀ kết thúc sau hoặc trong tháng này
                if (paymentDate < endOfMonth && subscriptionEnd >= startOfMonth)
                {
                    totalRevenue += revenuePerMonth;
                }
            }

            return Math.Round(totalRevenue, 2);
        }

        // ✅ Lấy doanh thu 12 tháng gần nhất
        public async Task<List<MonthlyRevenueDTO>> GetMonthlyRevenueForYearAsync(int year)
        {
            var result = new List<MonthlyRevenueDTO>();

            for (int month = 1; month <= 12; month++)
            {
                var revenue = await GetMonthlyRevenueAsync(year, month);
                result.Add(new MonthlyRevenueDTO
                {
                    Month = month,
                    Revenue = revenue
                });
            }

            return result;
        }

        // ✅ Tăng trưởng người dùng 6 tháng gần nhất
        public async Task<List<UserGrowthDTO>> GetUserGrowthForMonthsAsync(int months)
        {
            var result = new List<UserGrowthDTO>();
            var now = DateTime.UtcNow;

            for (int i = months - 1; i >= 0; i--)
            {
                var targetDate = now.AddMonths(-i);
                var startOfMonth = new DateTime(targetDate.Year, targetDate.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1);

                // Free users: Users không có subscription active trong tháng đó
                var allUsersInMonth = await _context.Users
                    .Where(u => u.RoleId == 4 && 
                                u.Accounts.Any(a => a.CreateAt < endOfMonth))
                    .Select(u => u.UserId)
                    .ToListAsync();

                var proUsersInMonth = await _context.Subscriptions
                    .Where(s => s.Status == "Active" &&
                                s.StartTime < endOfMonth &&
                                s.EndTime >= startOfMonth)
                    .Select(s => s.UserId)
                    .Distinct()
                    .ToListAsync();

                var freeUsers = allUsersInMonth.Count - proUsersInMonth.Count;

                result.Add(new UserGrowthDTO
                {
                    Month = targetDate.Month,
                    FreeUsers = freeUsers > 0 ? freeUsers : 0,
                    ProUsers = proUsersInMonth.Count
                });
            }

            return result;
        }

        // ✅ Phân bổ gói dịch vụ
        public async Task<List<PlanDistributionDTO>> GetPlanDistributionAsync()
        {
            var now = DateTime.UtcNow;
            
            // Tổng số users
            var totalUsers = await _context.Users.Where(u => u.RoleId == 4).CountAsync();

            // Users có subscription active
            var activeSubscriptions = await _context.Subscriptions
                .Where(s => s.Status == "Active" && s.EndTime >= now)
                .Include(s => s.Package)
                .GroupBy(s => s.Package.PackageName)
                .Select(g => new
                {
                    PackageName = g.Key,
                    UserCount = g.Select(s => s.UserId).Distinct().Count()
                })
                .ToListAsync();

            var proUsersCount = activeSubscriptions.Sum(x => x.UserCount);
            var freeUsersCount = totalUsers - proUsersCount;

            var result = new List<PlanDistributionDTO>
            {
                new PlanDistributionDTO
                {
                    PlanName = "Free",
                    UserCount = freeUsersCount,
                    Percentage = totalUsers > 0 ? Math.Round((decimal)freeUsersCount * 100 / totalUsers, 1) : 0
                }
            };

            foreach (var sub in activeSubscriptions)
            {
                result.Add(new PlanDistributionDTO
                {
                    PlanName = sub.PackageName,
                    UserCount = sub.UserCount,
                    Percentage = totalUsers > 0 ? Math.Round((decimal)sub.UserCount * 100 / totalUsers, 1) : 0
                });
            }

            return result;
        }

        // ✅ Phân tích theo ngày (7 ngày gần nhất)
        public async Task<List<DailyAnalyticsDTO>> GetDailyAnalyticsAsync(int days)
        {
            var result = new List<DailyAnalyticsDTO>();
            var now = DateTime.UtcNow.Date;

            for (int i = days - 1; i >= 0; i--)
            {
                var targetDate = now.AddDays(-i);
                var nextDate = targetDate.AddDays(1);

                // New users trong ngày
                var newUsers = await _context.Users
                    .Where(u => u.RoleId == 4 &&
                                u.Accounts.Any(a => a.CreateAt >= targetDate && a.CreateAt < nextDate))
                    .CountAsync();

                // Pro conversions trong ngày
                var proConversions = await _context.Subscriptions
                    .Where(s => s.StartTime >= targetDate && s.StartTime < nextDate)
                    .Select(s => s.UserId)
                    .Distinct()
                    .CountAsync();

                // Revenue trong ngày (tính theo subscription active trong ngày đó)
                var dailyRevenue = await CalculateDailyRevenueAsync(targetDate);

                // Conversion rate
                var conversionRate = newUsers > 0 ? Math.Round((decimal)proConversions * 100 / newUsers, 1) : 0;

                // Trend (so với ngày trước đó)
                decimal trendPercentage = 0;
                if (i < days - 1)
                {
                    var prevRevenue = result.LastOrDefault()?.Revenue ?? 0;
                    if (prevRevenue > 0)
                    {
                        trendPercentage = Math.Round(((dailyRevenue - prevRevenue) / prevRevenue) * 100, 1);
                    }
                }

                result.Add(new DailyAnalyticsDTO
                {
                    Date = targetDate,
                    Revenue = dailyRevenue,
                    NewUsers = newUsers,
                    ProConversions = proConversions,
                    ConversionRate = conversionRate,
                    TrendPercentage = trendPercentage
                });
            }

            return result;
        }

        // ✅ Helper: Tính doanh thu trong 1 ngày cụ thể
        private async Task<decimal> CalculateDailyRevenueAsync(DateTime date)
        {
            var payments = await _context.Payments
                .Where(p => p.Status == "Success" && p.PackageId != null)
                .Include(p => p.Package)
                .ToListAsync();

            decimal totalRevenue = 0;

            foreach (var payment in payments)
            {
                if (payment.Package == null) continue;

                var durationDays = payment.Package.DurationInDays;
                if (durationDays == 0) continue; // Tránh chia cho 0

                var revenuePerDay = payment.Amount / durationDays;

                var paymentDate = payment.CreatedAt.Date;
                var subscriptionEnd = paymentDate.AddDays((double)durationDays);

                // ✅ Kiểm tra xem subscription có cover ngày này không
                if (date >= paymentDate && date < subscriptionEnd)
                {
                    totalRevenue += (decimal)revenuePerDay;
                }
            }

            return Math.Round(totalRevenue, 2);
        }
    }
}
