using DataLayer.DTOs.Vocabulary;
using DataLayer.Models;
using RepositoryLayer.UnitOfWork;
using System.Linq;

namespace ServiceLayer.Vocabulary
{
    public class VocabularyListService : IVocabularyListService
    {
        private readonly IUnitOfWork _unitOfWork;

        public VocabularyListService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<VocabularyListDTO> CreateListAsync(VocabularyListCreateDTO dto, int creatorUserId)
        {
            var creator = await _unitOfWork.Users.GetUserByIdAsync(creatorUserId);
            if (creator == null)
            {
                throw new KeyNotFoundException("Creator user not found.");
            }

            var newList = new VocabularyList
            {
                Name = dto.Name,
                IsPublic = dto.IsPublic,
                MakeBy = creatorUserId,
                CreateAt = DateTime.UtcNow,
                IsDeleted = false,
                Status = "Draft" // Luôn bắt đầu với Draft, cần approval để Published
            };

            await _unitOfWork.VocabularyLists.AddAsync(newList);
            await _unitOfWork.CompleteAsync();

            return new VocabularyListDTO
            {
                VocabularyListId = newList.VocabularyListId,
                Name = newList.Name,
                IsPublic = newList.IsPublic,
                MakeByName = creator.FullName,
                MakeByRoleId = creator.RoleId,
                CreateAt = newList.CreateAt,
                VocabularyCount = 0,
                Status = newList.Status,
                RejectionReason = newList.RejectionReason
            };
        }

        public async Task<IEnumerable<VocabularyListDTO>> GetListsAsync(string? searchTerm)
        {
            return await _unitOfWork.VocabularyLists.GetAllAsync(searchTerm);
        }

        public async Task<IEnumerable<VocabularyListDTO>> GetListsByUserAsync(int userId, string? searchTerm)
        {
            return await _unitOfWork.VocabularyLists.GetByUserAsync(userId, searchTerm);
        }

        public async Task<IEnumerable<VocabularyListDTO>> GetPublishedListsAsync(string? searchTerm)
        {
            return await _unitOfWork.VocabularyLists.GetPublishedAsync(searchTerm);
        }

        public async Task<IEnumerable<VocabularyListDTO>> GetMyAndStaffListsAsync(int userId, string? searchTerm)
        {
            return await _unitOfWork.VocabularyLists.GetMyAndStaffListsAsync(userId, searchTerm);
        }

        public async Task<bool> RequestApprovalAsync(int listId, int staffUserId)
        {
            var vocabularyList = await _unitOfWork.VocabularyLists.FindByIdAsync(listId);
            if (vocabularyList == null || (vocabularyList.Status != "Draft" && vocabularyList.Status != "Rejected"))
            {
                // Chỉ cho phép gửi duyệt vocabulary list đang là Draft hoặc đã bị từ chối
                return false;
            }

            // Kiểm tra số lượng từ vựng trong folder - phải có ít nhất 1 từ mới được gửi duyệt
            var vocabularies = await _unitOfWork.Vocabularies.GetByListAsync(listId, null);
            var vocabularyCount = vocabularies.Count(v => v.IsDeleted != true);
            if (vocabularyCount == 0)
            {
                // Không cho phép gửi duyệt folder chưa có từ vựng nào
                return false;
            }

            vocabularyList.Status = "Pending";
            vocabularyList.IsPublic = false; // Vẫn chưa public
            vocabularyList.UpdatedBy = staffUserId;
            vocabularyList.UpdateAt = DateTime.UtcNow;

            await _unitOfWork.VocabularyLists.UpdateAsync(vocabularyList);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<bool> ReviewListAsync(int listId, bool isApproved, string? comment, int managerUserId)
        {
            var vocabularyList = await _unitOfWork.VocabularyLists.FindByIdAsync(listId);
            if (vocabularyList == null || vocabularyList.Status != "Pending")
            {
                // Chỉ duyệt được vocabulary list đang chờ
                return false;
            }

            if (isApproved)
            {
                vocabularyList.Status = "Published";
                vocabularyList.IsPublic = true;
                vocabularyList.RejectionReason = null; // Clear rejection reason when approved
            }
            else
            {
                vocabularyList.Status = "Rejected";
                vocabularyList.IsPublic = false;
                vocabularyList.RejectionReason = comment; // Save rejection reason
            }

            vocabularyList.UpdatedBy = managerUserId;
            vocabularyList.UpdateAt = DateTime.UtcNow;

            await _unitOfWork.VocabularyLists.UpdateAsync(vocabularyList);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<bool> DeleteListAsync(int listId, int userId)
        {
            var vocabularyList = await _unitOfWork.VocabularyLists.FindByIdAsync(listId);
            if (vocabularyList == null || vocabularyList.IsDeleted == true)
            {
                return false;
            }

            // Kiểm tra quyền: User chỉ có thể xóa folder của chính họ
            if (vocabularyList.MakeBy != userId)
            {
                return false;
            }

            await _unitOfWork.VocabularyLists.DeleteAsync(listId);
            await _unitOfWork.CompleteAsync();
            return true;
        }
    }
}