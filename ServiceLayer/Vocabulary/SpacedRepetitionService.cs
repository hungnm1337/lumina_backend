using DataLayer.DTOs.Vocabulary;
using DataLayer.Models;
using RepositoryLayer.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceLayer.Vocabulary
{
    public class SpacedRepetitionService : ISpacedRepetitionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SpacedRepetitionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<SpacedRepetitionDTO>> GetDueForReviewAsync(int userId)
        {
            var dueItems = await _unitOfWork.UserSpacedRepetitions.GetDueForReviewAsync(userId);
            var now = DateTime.UtcNow;
            
            return dueItems.Select(item => new SpacedRepetitionDTO
            {
                UserSpacedRepetitionId = item.UserSpacedRepetitionId,
                UserId = item.UserId,
                VocabularyListId = item.VocabularyListId,
                VocabularyListName = item.VocabularyList?.Name ?? "Unknown",
                LastReviewedAt = item.LastReviewedAt,
                NextReviewAt = item.NextReviewAt,
                ReviewCount = item.ReviewCount ?? 0,
                Intervals = item.Intervals ?? 1,
                Status = item.Status ?? "New",
                IsDue = true,
                DaysUntilReview = item.NextReviewAt.HasValue 
                    ? Math.Max(0, (int)(item.NextReviewAt.Value - now).TotalDays)
                    : 0
            });
        }

        public async Task<IEnumerable<SpacedRepetitionDTO>> GetUserRepetitionsAsync(int userId)
        {
            var items = await _unitOfWork.UserSpacedRepetitions.GetByUserIdAsync(userId);
            var now = DateTime.UtcNow;
            
            return items.Select(item => new SpacedRepetitionDTO
            {
                UserSpacedRepetitionId = item.UserSpacedRepetitionId,
                UserId = item.UserId,
                VocabularyListId = item.VocabularyListId,
                VocabularyListName = item.VocabularyList?.Name ?? "Unknown",
                LastReviewedAt = item.LastReviewedAt,
                NextReviewAt = item.NextReviewAt,
                ReviewCount = item.ReviewCount ?? 0,
                Intervals = item.Intervals ?? 1,
                Status = item.Status ?? "New",
                IsDue = item.NextReviewAt.HasValue && item.NextReviewAt.Value <= now,
                DaysUntilReview = item.NextReviewAt.HasValue 
                    ? Math.Max(0, (int)(item.NextReviewAt.Value - now).TotalDays)
                    : 0
            });
        }

        public async Task<ReviewVocabularyResponseDTO> ReviewVocabularyAsync(int userId, ReviewVocabularyRequestDTO request)
        {
            var repetition = await _unitOfWork.UserSpacedRepetitions.GetByIdAsync(request.UserSpacedRepetitionId);
            
            if (repetition == null || repetition.UserId != userId)
            {
                return new ReviewVocabularyResponseDTO
                {
                    Success = false,
                    Message = "Không tìm thấy bản ghi lặp lại"
                };
            }

            // Tính toán khoảng thời gian mới dựa trên thuật toán SM-2
            var quality = Math.Clamp(request.Quality, 0, 5);
            var (newIntervals, newStatus) = CalculateNextReview(quality, repetition.Intervals ?? 1, repetition.ReviewCount ?? 0);

            // Cập nhật thông tin
            repetition.LastReviewedAt = DateTime.UtcNow;
            repetition.NextReviewAt = DateTime.UtcNow.AddDays(newIntervals);
            repetition.ReviewCount = (repetition.ReviewCount ?? 0) + 1;
            repetition.Intervals = newIntervals;
            repetition.Status = newStatus;

            await _unitOfWork.UserSpacedRepetitions.UpdateAsync(repetition);
            await _unitOfWork.CompleteAsync();

            return new ReviewVocabularyResponseDTO
            {
                Success = true,
                Message = "Đã cập nhật tiến độ học tập",
                UpdatedRepetition = new SpacedRepetitionDTO
                {
                    UserSpacedRepetitionId = repetition.UserSpacedRepetitionId,
                    UserId = repetition.UserId,
                    VocabularyListId = repetition.VocabularyListId,
                    VocabularyListName = repetition.VocabularyList?.Name ?? "Unknown",
                    LastReviewedAt = repetition.LastReviewedAt,
                    NextReviewAt = repetition.NextReviewAt,
                    ReviewCount = repetition.ReviewCount ?? 0,
                    Intervals = repetition.Intervals ?? 1,
                    Status = repetition.Status ?? "New",
                    IsDue = false,
                    DaysUntilReview = newIntervals
                },
                NextReviewAt = repetition.NextReviewAt,
                NewIntervals = newIntervals
            };
        }

        public async Task<SpacedRepetitionDTO?> GetByUserAndListAsync(int userId, int vocabularyListId)
        {
            var item = await _unitOfWork.UserSpacedRepetitions.GetByUserAndListAsync(userId, vocabularyListId);
            
            if (item == null) return null;

            var now = DateTime.UtcNow;

            return new SpacedRepetitionDTO
            {
                UserSpacedRepetitionId = item.UserSpacedRepetitionId,
                UserId = item.UserId,
                VocabularyListId = item.VocabularyListId,
                VocabularyListName = item.VocabularyList?.Name ?? "Unknown",
                LastReviewedAt = item.LastReviewedAt,
                NextReviewAt = item.NextReviewAt,
                ReviewCount = item.ReviewCount ?? 0,
                Intervals = item.Intervals ?? 1,
                Status = item.Status ?? "New",
                IsDue = item.NextReviewAt.HasValue && item.NextReviewAt.Value <= now,
                DaysUntilReview = item.NextReviewAt.HasValue 
                    ? Math.Max(0, (int)(item.NextReviewAt.Value - now).TotalDays)
                    : 0
            };
        }

        public async Task<SpacedRepetitionDTO> CreateRepetitionAsync(int userId, int vocabularyListId)
        {
            // Kiểm tra xem đã tồn tại chưa
            var exists = await _unitOfWork.UserSpacedRepetitions.ExistsAsync(userId, vocabularyListId);
            
            if (exists)
            {
                var existing = await GetByUserAndListAsync(userId, vocabularyListId);
                if (existing != null)
                    return existing;
                throw new Exception("Lỗi khi tạo bản ghi lặp lại");
            }

            // Kiểm tra vocabulary list có tồn tại không
            var vocabularyList = await _unitOfWork.VocabularyLists.FindByIdAsync(vocabularyListId);
            if (vocabularyList == null)
            {
                throw new KeyNotFoundException("Vocabulary list not found.");
            }

            var repetition = new UserSpacedRepetition
            {
                UserId = userId,
                VocabularyListId = vocabularyListId,
                LastReviewedAt = DateTime.UtcNow,
                NextReviewAt = DateTime.UtcNow.AddDays(1),
                ReviewCount = 0,
                Intervals = 1,
                Status = "New"
            };

            await _unitOfWork.UserSpacedRepetitions.AddAsync(repetition);
            await _unitOfWork.CompleteAsync();

            return new SpacedRepetitionDTO
            {
                UserSpacedRepetitionId = repetition.UserSpacedRepetitionId,
                UserId = repetition.UserId,
                VocabularyListId = repetition.VocabularyListId,
                VocabularyListName = vocabularyList.Name,
                LastReviewedAt = repetition.LastReviewedAt,
                NextReviewAt = repetition.NextReviewAt,
                ReviewCount = 0,
                Intervals = 1,
                Status = "New",
                IsDue = false,
                DaysUntilReview = 1
            };
        }

        // Thuật toán SM-2 (Simplified) để tính khoảng thời gian review tiếp theo
        private (int intervals, string status) CalculateNextReview(int quality, int currentIntervals, int reviewCount)
        {
            // Quality: 0-5 (0 = không nhớ, 5 = nhớ rất tốt)
            int newIntervals;
            string newStatus;

            if (quality < 3)
            {
                // Nếu không nhớ tốt, reset về 1 ngày
                newIntervals = 1;
                newStatus = reviewCount == 0 ? "New" : "Learning";
            }
            else if (quality == 3)
            {
                // Nhớ tạm, giữ nguyên hoặc tăng nhẹ
                newIntervals = currentIntervals;
                newStatus = "Learning";
            }
            else
            {
                // Nhớ tốt, tăng khoảng thời gian
                // Công thức: interval = interval * (1 + (quality - 3) * 0.5)
                newIntervals = (int)Math.Ceiling(currentIntervals * (1 + (quality - 3) * 0.5));
                newStatus = newIntervals >= 30 ? "Mastered" : "Learning";
            }

            // Giới hạn tối đa là 90 ngày
            newIntervals = Math.Min(newIntervals, 90);

            return (newIntervals, newStatus);
        }
    }
}

