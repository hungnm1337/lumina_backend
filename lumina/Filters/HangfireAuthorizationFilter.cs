using Hangfire.Dashboard;

namespace lumina.Filters
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // ⚠️ PRODUCTION: Kiểm tra authentication/authorization
            // return httpContext.User.IsInRole("Admin");

            // ✅ DEVELOPMENT: Cho phép tất cả (CHỈ dùng trong dev!)
            return true;
        }
    }
}