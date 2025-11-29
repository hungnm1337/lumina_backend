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

                var durationDays = payment.Package.DurationInDays ?? 0;
                if (durationDays == 0) continue;

                var revenuePerDay = payment.Amount / durationDays;
                var paymentDate = payment.CreatedAt.Date;
                var subscriptionEnd = paymentDate.AddDays(durationDays);

                var overlapStart = paymentDate > startOfMonth ? paymentDate : startOfMonth;
                var overlapEnd = subscriptionEnd < endOfMonth ? subscriptionEnd : endOfMonth;

                if (overlapStart < overlapEnd)
                {
                    var overlapDays = (overlapEnd - overlapStart).Days;
                    totalRevenue += revenuePerDay * overlapDays;
                }
            }

            return Math.Round(totalRevenue, 2);
        }

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

        public async Task<List<UserGrowthDTO>> GetUserGrowthForMonthsAsync(int months)
        {
            var result = new List<UserGrowthDTO>();
            var now = DateTime.UtcNow;

            for (int i = months - 1; i >= 0; i--)
            {
                var targetDate = now.AddMonths(-i);
                var startOfMonth = new DateTime(targetDate.Year, targetDate.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1);

                var allUsersAtEndOfMonth = await _context.Users
                    .Where(u => u.RoleId == 4 && 
                                u.Accounts.Any(a => a.CreateAt < endOfMonth))
                    .Select(u => u.UserId)
                    .ToListAsync();

                var proUserIdsInMonth = await _context.Subscriptions
                    .Where(s => s.Status == "Active" &&
                                s.StartTime < endOfMonth &&
                                s.EndTime >= startOfMonth)
                    .Select(s => s.UserId)
                    .Distinct()
                    .ToListAsync();

                var freeUsersCount = allUsersAtEndOfMonth.Count - proUserIdsInMonth.Count;

                result.Add(new UserGrowthDTO
                {
                    Month = targetDate.Month,
                    FreeUsers = freeUsersCount > 0 ? freeUsersCount : 0,
                    ProUsers = proUserIdsInMonth.Count
                });
            }

            return result;
        }

        public async Task<List<PlanDistributionDTO>> GetPlanDistributionAsync()
        {
            var now = DateTime.UtcNow;
            
            var totalUsers = await _context.Users.Where(u => u.RoleId == 4).CountAsync();

            var proUserIds = await _context.Subscriptions
                .Where(s => s.Status == "Active" && s.EndTime >= now)
                .Select(s => s.UserId)
                .Distinct()
                .ToListAsync();

            var freeUsersCount = totalUsers - proUserIds.Count;

            var activeSubscriptionsByPackage = await _context.Subscriptions
                .Where(s => s.Status == "Active" && s.EndTime >= now)
                .Include(s => s.Package)
                .ToListAsync();

            var packageGroups = activeSubscriptionsByPackage
                .GroupBy(s => s.Package.PackageName)
                .Select(g => new
                {
                    PackageName = g.Key,
                    UserCount = g.Select(s => s.UserId).Distinct().Count()
                })
                .ToList();

            var result = new List<PlanDistributionDTO>
            {
                new PlanDistributionDTO
                {
                    PlanName = "Free",
                    UserCount = freeUsersCount,
                    Percentage = totalUsers > 0 ? Math.Round((decimal)freeUsersCount * 100 / totalUsers, 1) : 0
                }
            };

            foreach (var pkg in packageGroups)
            {
                result.Add(new PlanDistributionDTO
                {
                    PlanName = pkg.PackageName,
                    UserCount = pkg.UserCount,
                    Percentage = totalUsers > 0 ? Math.Round((decimal)pkg.UserCount * 100 / totalUsers, 1) : 0
                });
            }

            return result;
        }

        public async Task<List<DailyAnalyticsDTO>> GetDailyAnalyticsAsync(int days)
        {
            var result = new List<DailyAnalyticsDTO>();
            var now = DateTime.UtcNow.Date;

            for (int i = days - 1; i >= 0; i--)
            {
                var targetDate = now.AddDays(-i);
                var nextDate = targetDate.AddDays(1);

                var newUsers = await _context.Users
                    .Where(u => u.RoleId == 4 &&
                                u.Accounts.Any(a => a.CreateAt >= targetDate && a.CreateAt < nextDate))
                    .CountAsync();

                var proConversions = await _context.Subscriptions
                    .Where(s => s.StartTime >= targetDate && s.StartTime < nextDate)
                    .Select(s => s.UserId)
                    .Distinct()
                    .CountAsync();

                var dailyRevenue = await CalculateDailyRevenueAsync(targetDate);

                var conversionRate = newUsers > 0 ? Math.Round((decimal)proConversions * 100 / newUsers, 1) : 0;

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

                var durationDays = payment.Package.DurationInDays ?? 0;
                if (durationDays == 0) continue;

                var revenuePerDay = payment.Amount / durationDays;

                var paymentDate = payment.CreatedAt.Date;
                var subscriptionEnd = paymentDate.AddDays(durationDays);

                if (date >= paymentDate && date < subscriptionEnd)
                {
                    totalRevenue += revenuePerDay;
                }
            }

            return Math.Round(totalRevenue, 2);
        }
    }
}
