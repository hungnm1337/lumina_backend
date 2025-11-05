using DataLayer.Models;
using Microsoft.AspNetCore.Authorization;
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
            
            var totalUsers = await _systemContext.Users.CountAsync(u => u.RoleId == 4);

            
            var today = DateTime.UtcNow.Date;
            var registeredToday = await _systemContext.Users
     .Where(u => u.RoleId == 4
         && u.Accounts.Any()
         && u.Accounts.OrderBy(a => a.CreateAt).FirstOrDefault()!.CreateAt.Date == today)
     .CountAsync();

            
            var now = DateTime.UtcNow;
            var firstOfMonth = new DateTime(now.Year, now.Month, 1);
            var monthlyRevenue = await _systemContext.Payments
                .Where(p => p.Status == "Success" && p.CreatedAt >= firstOfMonth)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

           
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
                        totalDays += (int)((currentEnd!.Value - currentStart!.Value).TotalDays) + 1;
                        currentStart = sub.StartDate;
                        currentEnd = sub.EndDate;
                    }
                }
            }

            if (currentStart != null && currentEnd != null)
            {
                totalDays += (int)((currentEnd!.Value - currentStart!.Value).TotalDays) + 1;
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

        [HttpGet("staff-dashboard")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> GetStaffDashboardStats()
        {
            var now = DateTime.UtcNow;
            var firstOfMonth = new DateTime(now.Year, now.Month, 1);
            var lastMonth = firstOfMonth.AddMonths(-1);
            var firstOfLastMonth = new DateTime(lastMonth.Year, lastMonth.Month, 1);

            // 1. Thống kê Articles
            var totalArticles = await _systemContext.Articles.CountAsync();
            var articlesThisMonth = await _systemContext.Articles
                .Where(a => a.CreatedAt >= firstOfMonth)
                .CountAsync();
            var articlesLastMonth = await _systemContext.Articles
                .Where(a => a.CreatedAt >= firstOfLastMonth && a.CreatedAt < firstOfMonth)
                .CountAsync();

            // 2. Thống kê Vocabulary (không có CreatedAt field)
            var totalVocabulary = await _systemContext.Vocabularies.CountAsync();
            var vocabularyThisMonth = 0; // Vocabulary model không có CreatedAt field
            var vocabularyLastMonth = 0;

            // 3. Thống kê Questions (từ ExamParts -> Questions)
            var totalQuestions = await _systemContext.Questions.CountAsync();
            var questionsThisMonth = await _systemContext.Questions
                .Where(q => q.Part.Exam.CreatedAt >= firstOfMonth)
                .CountAsync();
            var questionsLastMonth = await _systemContext.Questions
                .Where(q => q.Part.Exam.CreatedAt >= firstOfLastMonth && q.Part.Exam.CreatedAt < firstOfMonth)
                .CountAsync();

            // 4. Thống kê Tests (Exams)
            var totalTests = await _systemContext.Exams.CountAsync();
            var testsThisMonth = await _systemContext.Exams
                .Where(e => e.CreatedAt >= firstOfMonth)
                .CountAsync();
            var testsLastMonth = await _systemContext.Exams
                .Where(e => e.CreatedAt >= firstOfLastMonth && e.CreatedAt < firstOfMonth)
                .CountAsync();

            // 5. Recent Activities - Lấy từ nhiều nguồn
            var recentActivities = new List<object>();

            // Articles activities
            var articleActivities = await _systemContext.Articles
                .OrderByDescending(a => a.CreatedAt)
                .Take(3)
                .Select(a => new
                {
                    id = a.ArticleId,
                    type = "article",
                    title = a.Title,
                    action = a.IsPublished == true ? "Đã xuất bản bài viết" : "Tạo bài viết mới",
                    timestamp = a.CreatedAt,
                    status = a.IsPublished == true ? "published" : "created"
                })
                .ToListAsync();

            // Exam activities
            var examActivities = await _systemContext.Exams
                .OrderByDescending(e => e.CreatedAt)
                .Take(2)
                .Select(e => new
                {
                    id = e.ExamId,
                    type = "test",
                    title = e.Name,
                    action = e.IsActive == true ? "Đã kích hoạt bài thi" : "Tạo bài thi mới",
                    timestamp = e.CreatedAt,
                    status = e.IsActive == true ? "published" : "created"
                })
                .ToListAsync();

            // Vocabulary activities (không có CreatedAt field)
            var vocabularyActivities = await _systemContext.Vocabularies
                .Take(2)
                .Select(v => new
                {
                    id = v.VocabularyId,
                    type = "vocabulary",
                    title = v.Word,
                    action = "Thêm từ vựng mới",
                    timestamp = DateTime.UtcNow.AddDays(-1), // Sử dụng thời gian giả định
                    status = "created"
                })
                .ToListAsync();

            // Combine và sort by timestamp
            recentActivities.AddRange(articleActivities);
            recentActivities.AddRange(examActivities);
            recentActivities.AddRange(vocabularyActivities);

            var sortedActivities = recentActivities
                .OrderByDescending(a => ((dynamic)a).timestamp)
                .Take(5)
                .Select(a => new
                {
                    id = ((dynamic)a).id,
                    type = ((dynamic)a).type,
                    title = ((dynamic)a).title,
                    action = ((dynamic)a).action,
                    timestamp = GetTimeAgo(((dynamic)a).timestamp),
                    status = ((dynamic)a).status
                })
                .ToList();

            // 6. Productivity metrics - tính dựa trên tổng content
            var totalContentThisMonth = articlesThisMonth + questionsThisMonth + testsThisMonth + vocabularyThisMonth;
            var totalContentLastMonth = articlesLastMonth + questionsLastMonth + testsLastMonth + vocabularyLastMonth;
            var productivityGrowth = totalContentLastMonth > 0 
                ? Math.Round((double)(totalContentThisMonth - totalContentLastMonth) / totalContentLastMonth * 100, 1)
                : 0;

            // 7. Engagement metrics
            var totalExamAttempts = await _systemContext.ExamAttempts.CountAsync();
            var contentLikes = totalExamAttempts; // Sử dụng số lượt thi làm proxy cho engagement
            var qualityRating = 4.8; // TODO: Implement rating system

            return Ok(new
            {
                stats = new
                {
                    totalArticles,
                    totalQuestions,
                    totalTests,
                    totalVocabulary,
                    articlesThisMonth,
                    questionsThisMonth,
                    testsThisMonth,
                    vocabularyThisMonth,
                    articlesLastMonth,
                    questionsLastMonth,
                    testsLastMonth,
                    vocabularyLastMonth
                },
                recentActivities = sortedActivities,
                metrics = new
                {
                    productivityGrowth,
                    contentLikes,
                    qualityRating
                }
            });
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;
            
            if (timeSpan.TotalMinutes < 1)
                return "Vừa xong";
            else if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} phút trước";
            else if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} giờ trước";
            else if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} ngày trước";
            else
                return dateTime.ToString("dd/MM/yyyy");
        }

    }
}
