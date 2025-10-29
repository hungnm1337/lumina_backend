using DataLayer.Models;
using RepositoryLayer.Generic;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace RepositoryLayer.User
{
    public interface IUserAnswerRepository : IRepository<UserAnswerMultipleChoice>
    {
        Task<List<UserAnswerMultipleChoice>> GetAllAsync(Expression<Func<UserAnswerMultipleChoice, bool>> expression);
        void Update(UserAnswerMultipleChoice entity);
    }

    public class UserAnswerRepository : Repository<UserAnswerMultipleChoice>, IUserAnswerRepository
    {
        public UserAnswerRepository(LuminaSystemContext context) : base(context)
        {
        }

        public async Task<List<UserAnswerMultipleChoice>> GetAllAsync(Expression<Func<UserAnswerMultipleChoice, bool>> expression)
        {
            return await _dbSet.Where(expression).ToListAsync();
        }

        public void Update(UserAnswerMultipleChoice entity)
        {
            _dbSet.Update(entity);
        }
    }
}
