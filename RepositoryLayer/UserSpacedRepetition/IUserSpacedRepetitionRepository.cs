using DataLayer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RepositoryLayer.UserSpacedRepetition
{
    public interface IUserSpacedRepetitionRepository
    {
        Task<DataLayer.Models.UserSpacedRepetition?> GetByUserAndListAsync(int userId, int vocabularyListId);
        Task<IEnumerable<DataLayer.Models.UserSpacedRepetition>> GetDueForReviewAsync(int userId);
        Task<IEnumerable<DataLayer.Models.UserSpacedRepetition>> GetByUserIdAsync(int userId);
        Task<DataLayer.Models.UserSpacedRepetition?> GetByIdAsync(int id);
        Task<DataLayer.Models.UserSpacedRepetition> AddAsync(DataLayer.Models.UserSpacedRepetition entity);
        Task<DataLayer.Models.UserSpacedRepetition> UpdateAsync(DataLayer.Models.UserSpacedRepetition entity);
        Task<bool> ExistsAsync(int userId, int vocabularyListId);
    }
}

