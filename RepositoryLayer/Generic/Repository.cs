using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace RepositoryLayer.Generic
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly LuminaSystemContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(LuminaSystemContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public IQueryable<T> Get()
        {
            return _dbSet.AsQueryable();
        }

        public async Task<T?> GetAsync(Expression<Func<T, bool>> expression)
        {
            return await _dbSet.FirstOrDefaultAsync(expression);
        }

        // **THÊM PHẦN TRIỂN KHAI CHO HÀM NÀY**
        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }
    }
}