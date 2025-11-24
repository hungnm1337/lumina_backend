using DataLayer.DTOs.UserReport;
using DataLayer.Models;

namespace RepositoryLayer.Report;

public interface IReportRepository
{
    Task<bool> AddAsync(UserReportRequest report);
    Task<UserReportResponse> FindByIdAsync(int id);
    Task<List<UserReportResponse>> GetAllAsync(int roleid);

    Task<List<UserReportResponse>> GetAllByUserIdAsync(int Userid);

    Task<bool> UpdateAsync(UserReportRequest report);

}

