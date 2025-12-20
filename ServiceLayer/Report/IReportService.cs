using DataLayer.DTOs;
using DataLayer.DTOs.Report;
using DataLayer.DTOs.UserReport;

namespace ServiceLayer.Report;

public interface IReportService
{
    Task<bool> AddAsync(UserReportRequest report);
    Task<UserReportResponse> FindByIdAsync(int id);
    Task<List<UserReportResponse>> GetAllAsync(int roleid);
    Task<List<UserReportResponse>> GetAllByUserIdAsync(int Userid);
    Task<bool> UpdateAsync(UserReportRequest report);
}

