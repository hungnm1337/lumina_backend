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

        public async Task<T?> GetAsync(Expression<Func<T, bool>> expression, string includeProperties = "")
        {
            IQueryable<T> query = _dbSet;

            if (!string.IsNullOrWhiteSpace(includeProperties))
            {
                foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty.Trim());
                }
            }

            return await query.FirstOrDefaultAsync(expression);
        }

        public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>> expression)
        {
            return await _dbSet.Where(expression).ToListAsync();
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }
    }
}