using DataLayer.DTOs.Packages;
using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class PackageService : IPackageService
{
    private readonly IPackageRepository _repository;

    public PackageService(IPackageRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Package>> GetActivePackagesAsync() => _repository.GetActivePackagesAsync();
    public Task<Package?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);
    public Task UpdatePackageAsync(Package pkg) => _repository.UpdateAsync(pkg);
    public Task TogglePackageStatusAsync(int id) => _repository.ToggleActiveStatusAsync(id);
    public Task DeletePackageAsync(int id) => _repository.DeleteAsync(id);

    public Task AddPackageAsync(Package pkg) => _repository.AddPackageAsync(pkg);

    public Task<UserActivePackageInfo?> GetUserActivePackageAsync(int userId)  => _repository.GetUserActivePackageAsync(userId);
}
