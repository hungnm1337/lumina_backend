using DataLayer.DTOs.Packages;
using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface IPackageRepository
{
    Task<List<Package>> GetActivePackagesAsync();
    Task<Package?> GetByIdAsync(int id);

    Task AddPackageAsync(Package pkg);
    Task UpdateAsync(Package pkg);
    Task ToggleActiveStatusAsync(int id);
    Task DeleteAsync(int id);

    Task<UserActivePackageInfo?> GetUserActivePackageAsync(int userId);
}
