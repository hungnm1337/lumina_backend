using DataLayer.DTOs.Packages;
using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface IPackageService
{
    Task<List<Package>> GetActivePackagesAsync();
    Task<Package?> GetByIdAsync(int id);

    Task AddPackageAsync(Package pkg);
    Task UpdatePackageAsync(Package pkg);
    Task TogglePackageStatusAsync(int id);
    Task DeletePackageAsync(int id);

    Task<UserActivePackageInfo?> GetUserActivePackageAsync(int userId);
}