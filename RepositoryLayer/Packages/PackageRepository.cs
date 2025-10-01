using DataLayer.DTOs.Packages;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class PackageRepository : IPackageRepository
{
    private readonly LuminaSystemContext _context;
    public PackageRepository(LuminaSystemContext context)
    {
        _context = context;
    }

    public async Task<List<Package>> GetActivePackagesAsync()
    {
        return await _context.Packages.Where(p => p.IsActive == true).ToListAsync();
    }

    public async Task<Package?> GetByIdAsync(int id)
    {
        return await _context.Packages.FindAsync(id);
    }

    public async Task AddPackageAsync(Package pkg)
    {
        _context.Packages.Add(pkg);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Package pkg)
    {
        _context.Packages.Update(pkg);
        await _context.SaveChangesAsync();
    }

    public async Task ToggleActiveStatusAsync(int id)
    {
        var pkg = await _context.Packages.FindAsync(id);
        if (pkg != null)
        {
            pkg.IsActive = !(pkg.IsActive ?? true);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(int id)
    {
        var pkg = await _context.Packages.FindAsync(id);
        if (pkg != null)
        {
            _context.Packages.Remove(pkg);
            await _context.SaveChangesAsync();
        }
    }


    public async Task<UserActivePackageInfo?> GetUserActivePackageAsync(int userId)
    {
        var now = DateTime.UtcNow;

        var query = from s in _context.Subscriptions
                    join p in _context.Packages on s.PackageId equals p.PackageId
                    where s.UserId == userId &&
                          s.Status == "Active" &&
                          s.StartTime <= now && s.EndTime >= now &&
                          p.IsActive == true
                    select new UserActivePackageInfo
                    {
                        Package = p,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime
                    };

        return await query.FirstOrDefaultAsync();
    }
}