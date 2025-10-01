using DataLayer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Khai báo namespace EF Core
using System;
using System.Threading.Tasks;

namespace lumina.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatisticController : ControllerBase
    {
        private readonly LuminaSystemContext _systemContext;

        public StatisticController(LuminaSystemContext systemContext)
        {
            _systemContext = systemContext;
        }

        [HttpGet("dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            // Tổng người dùng (roleId = 4)
            var totalUsers = await _systemContext.Users.CountAsync(u => u.RoleId == 4);

            // Người đăng ký hôm nay (roleId = 4, CreateAt là ngày hôm nay)
            var today = DateTime.UtcNow.Date;
            var registeredToday = await _systemContext.Users
     .Where(u => u.RoleId == 4
         && u.Accounts.Any()
         && u.Accounts.OrderBy(a => a.CreateAt).FirstOrDefault().CreateAt.Date == today)
     .CountAsync();

            // Doanh thu tháng (Payment.Status = 'Success', tháng hiện tại)
            var now = DateTime.UtcNow;
            var firstOfMonth = new DateTime(now.Year, now.Month, 1);
            var monthlyRevenue = await _systemContext.Payments
                .Where(p => p.Status == "Success" && p.CreatedAt >= firstOfMonth)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            // Người dùng Pro (Subscription active, roleId=4, đếm unique UserID)
            var proUserCount = await _systemContext.Subscriptions
                .Where(s => s.Status == "Active")
                .Select(s => s.UserId)
                .Distinct()
                .CountAsync();

            var proPercent = totalUsers > 0 ? Math.Round((double)proUserCount * 100 / totalUsers, 2) : 0;

            return Ok(new
            {
                totalUsers,
                monthlyRevenue,
                registeredToday,
                proUserCount,
                proPercent
            });
        }

        [HttpGet("statistic-packages")]
        public async Task<IActionResult> GetFullDashboardStats()
        {
            var now = DateTime.UtcNow;
            var firstOfMonth = new DateTime(now.Year, now.Month, 1);
            var last30Days = now.AddDays(-30);

            // 1. Lấy các Packages Pro active (id 1,2,3 và IsActive = true)
            var proPackageIds = new int[] { 1, 2, 3 };
            var proPackages = await _systemContext.Packages
                .Where(p => proPackageIds.Contains(p.PackageId) && p.IsActive == true)
                .ToListAsync();

            // 2. Số người dùng từng gói (subscription đang active, date trong khoảng StartTime đến EndTime)
            var usersByPackage = await _systemContext.Subscriptions
                .Where(s => s.Status == "Active"
                            && proPackageIds.Contains(s.PackageId)
                            && now >= s.StartTime && now <= s.EndTime)
                .GroupBy(s => s.PackageId)
                .Select(g => new
                {
                    PackageId = g.Key,
                    UserCount = g.Select(x => x.UserId).Distinct().Count()
                }).ToListAsync();

            int totalProUsers = usersByPackage.Sum(x => x.UserCount);

            // 3. Doanh thu từng gói (30 ngày gần nhất, Payment.Status = Success)
            var revenueByPackage = await _systemContext.Payments
                .Where(p => p.Status == "Success"
                            && proPackageIds.Contains(p.PackageId)
                            && p.CreatedAt >= last30Days)
                .GroupBy(p => p.PackageId)
                .Select(g => new
                {
                    PackageId = g.Key,
                    Revenue = g.Sum(x => x.Amount)
                }).ToListAsync();

            decimal totalRevenue = revenueByPackage.Sum(x => x.Revenue);

            // 4. Tăng trưởng tháng này - Người dùng mới đăng ký trong tháng (roleId =4)
            var newUsers = await _systemContext.Users
                .Where(u => u.RoleId == 4 &&
                            u.Accounts.Any(a => a.CreateAt >= firstOfMonth))
                .CountAsync();

            // 5. Tăng trưởng tháng này - Người nâng cấp Pro (subscriptions active bắt đầu tháng này)
            var upgradedPro = await _systemContext.Subscriptions
                .Where(s => s.Status == "Active"
                            && s.StartTime >= firstOfMonth)
                .Select(s => s.UserId)
                .Distinct()
                .CountAsync();

            // 6. Doanh thu tháng này từ payments status success
            var monthlyRevenue = await _systemContext.Payments
                .Where(p => p.Status == "Success"
                            && p.CreatedAt >= firstOfMonth)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            // Chuẩn bị trả data theo gói cho người dùng và doanh thu (có giá, tên)
            var packageStats = proPackages.Select(pkg =>
            {
                var userCount = usersByPackage.FirstOrDefault(x => x.PackageId == pkg.PackageId)?.UserCount ?? 0;
                var revenue = revenueByPackage.FirstOrDefault(x => x.PackageId == pkg.PackageId)?.Revenue ?? 0m;
                var userPercent = totalProUsers > 0 ? Math.Round((double)userCount * 100 / totalProUsers, 1) : 0;
                var revenuePercent = totalRevenue > 0 ? Math.Round((double)revenue * 100 / (double)totalRevenue, 1) : 0;

                return new
                {
                    PackageId = pkg.PackageId,
                    PackageName = pkg.PackageName,
                    Price = pkg.Price,
                    DurationInDays = pkg.DurationInDays,
                    UserCount = userCount,
                    UserPercent = userPercent,
                    Revenue = revenue,
                    RevenuePercent = revenuePercent
                };
            }).ToList();

            return Ok(new
            {
                TotalProUsers = totalProUsers,
                TotalRevenue = totalRevenue,
                NewUsers = newUsers,
                UpgradedPro = upgradedPro,
                MonthlyRevenue = monthlyRevenue,
                PackageStats = packageStats
            });
        }

        [HttpGet("user-pro-summary/{userId}")]
        public async Task<IActionResult> GetUserProSummary(int userId)
        {
            var now = DateTime.UtcNow.Date;

            var proPackageIds = new int[] { 1, 2, 3 };

            // Tổng số tiền user đã chi cho các gói Pro (trạng thái Success, package Pro)
            var totalMoney = await _systemContext.Payments
                .Where(p => p.UserId == userId && p.Status == "Success" && proPackageIds.Contains(p.PackageId))
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            // Tổng số gói Pro user đã mua (đếm số subscription package Pro)
            var totalPackages = await _systemContext.Subscriptions
                .Where(s => s.UserId == userId && proPackageIds.Contains(s.PackageId))
                .CountAsync();

            // Lấy tất cả subscription Pro của user, giới hạn ngày kết thúc không vượt quá ngày hôm nay (tính đúng tổng ngày chưa trùng)
            var allSubs = await _systemContext.Subscriptions
       .Where(s => s.UserId == userId && proPackageIds.Contains(s.PackageId))
       .Select(s => new
       {
           StartDate = s.StartTime.HasValue ? s.StartTime.Value.Date : DateTime.MinValue,
           EndDate = s.EndTime.HasValue && s.EndTime.Value.Date < now ? s.EndTime.Value.Date : now
       })
       .OrderBy(s => s.StartDate)
       .ToListAsync();

            DateTime? currentStart = null;
            DateTime? currentEnd = null;
            int totalDays = 0;

            foreach (var sub in allSubs)
            {
                if (currentStart == null)
                {
                    currentStart = sub.StartDate;
                    currentEnd = sub.EndDate;
                }
                else
                {
                    if (sub.StartDate <= currentEnd)
                    {
                        if (sub.EndDate > currentEnd)
                            currentEnd = sub.EndDate;
                    }
                    else
                    {
                        totalDays += (int)((currentEnd.Value - currentStart.Value).TotalDays) + 1;
                        currentStart = sub.StartDate;
                        currentEnd = sub.EndDate;
                    }
                }
            }

            if (currentStart != null && currentEnd != null)
            {
                totalDays += (int)((currentEnd.Value - currentStart.Value).TotalDays) + 1;
            }


            // Tổng ngày còn lại của subscription đang active (nếu có)
            var activeSub = await _systemContext.Subscriptions
                .Where(s => s.UserId == userId
                    && proPackageIds.Contains(s.PackageId)
                    && s.Status == "Active"
                    && s.EndTime >= now)
                .OrderByDescending(s => s.EndTime)
                .FirstOrDefaultAsync();

            int remainDays = 0;
            if (activeSub != null && activeSub.EndTime.HasValue && activeSub.EndTime.Value.Date > now)
            {
                remainDays = (int)(activeSub.EndTime.Value.Date - now).TotalDays;
            }

            return Ok(new
            {
                totalMoney,
                totalPackages,
                totalDays,
                remainDays
            });
        }




    }
}
