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
                VocabularyId = item.VocabularyId,
                VocabularyListId = item.VocabularyListId,
                VocabularyListName = item.VocabularyList?.Name ?? "Unknown",
                VocabularyWord = item.Vocabulary?.Word,
                LastReviewedAt = item.LastReviewedAt,
                NextReviewAt = item.NextReviewAt,
                ReviewCount = item.ReviewCount ?? 0,
                Intervals = item.Intervals ?? 1,
                Status = CalculateStatus(item),
                IsDue = true,
                DaysUntilReview = item.NextReviewAt.HasValue 
                    ? Math.Max(0, (int)(item.NextReviewAt.Value - now).TotalDays)
                    : 0,
                BestQuizScore = item.BestQuizScore,
                LastQuizScore = item.LastQuizScore,
                LastQuizCompletedAt = item.LastQuizCompletedAt,
                TotalQuizAttempts = item.TotalQuizAttempts
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
                VocabularyId = item.VocabularyId,
                VocabularyListId = item.VocabularyListId,
                VocabularyListName = item.VocabularyList?.Name ?? "Unknown",
                VocabularyWord = item.Vocabulary?.Word,
                LastReviewedAt = item.LastReviewedAt,
                NextReviewAt = item.NextReviewAt,
                ReviewCount = item.ReviewCount ?? 0,
                Intervals = item.Intervals ?? 1,
                Status = CalculateStatus(item),
                IsDue = item.NextReviewAt.HasValue && item.NextReviewAt.Value <= now,
                DaysUntilReview = item.NextReviewAt.HasValue 
                    ? Math.Max(0, (int)(item.NextReviewAt.Value - now).TotalDays)
                    : 0,
                BestQuizScore = item.BestQuizScore,
                LastQuizScore = item.LastQuizScore,
                LastQuizCompletedAt = item.LastQuizCompletedAt,
                TotalQuizAttempts = item.TotalQuizAttempts
            });
        }

        public async Task<ReviewVocabularyResponseDTO> ReviewVocabularyAsync(int userId, ReviewVocabularyRequestDTO request)
        {
            UserSpacedRepetition? repetition = null;

            // Option 1: Use existing UserSpacedRepetitionId (backward compatible)
            if (request.UserSpacedRepetitionId.HasValue)
            {
                repetition = await _unitOfWork.UserSpacedRepetitions.GetByIdAsync(request.UserSpacedRepetitionId.Value);
                
                if (repetition == null || repetition.UserId != userId)
                {
                    return new ReviewVocabularyResponseDTO
                    {
                        Success = false,
                        Message = "Không tìm thấy bản ghi lặp lại"
                    };
                }
            }
            // Option 2: Create or get by VocabularyId (new way - word level)
            else if (request.VocabularyId.HasValue && request.VocabularyListId.HasValue)
            {
                // Tìm record theo VocabularyId
                repetition = await _unitOfWork.UserSpacedRepetitions.GetByUserAndVocabularyAsync(userId, request.VocabularyId.Value);
                
                // Nếu chưa có, tạo mới
                if (repetition == null)
                {
                    var vocabulary = await _unitOfWork.VocabularyLists.FindByIdAsync(request.VocabularyListId.Value);
                    if (vocabulary == null)
                    {
                        return new ReviewVocabularyResponseDTO
                        {
                            Success = false,
                            Message = "Không tìm thấy vocabulary list"
                        };
                    }

                    repetition = new UserSpacedRepetition
                    {
                        UserId = userId,
                        VocabularyId = request.VocabularyId.Value,
                        VocabularyListId = request.VocabularyListId.Value,
                        LastReviewedAt = DateTime.UtcNow,
                        NextReviewAt = DateTime.UtcNow.AddDays(1),
                        ReviewCount = 0,
                        Intervals = 1,
                        Status = "New"
                    };

                    await _unitOfWork.UserSpacedRepetitions.AddAsync(repetition);
                    await _unitOfWork.CompleteAsync();
                }
            }
            else
            {
                return new ReviewVocabularyResponseDTO
                {
                    Success = false,
                    Message = "Thiếu thông tin: cần UserSpacedRepetitionId hoặc (VocabularyId và VocabularyListId)"
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
                    VocabularyId = repetition.VocabularyId,
                    VocabularyListId = repetition.VocabularyListId,
                    VocabularyListName = repetition.VocabularyList?.Name ?? "Unknown",
                    VocabularyWord = repetition.Vocabulary?.Word,
                    LastReviewedAt = repetition.LastReviewedAt,
                    NextReviewAt = repetition.NextReviewAt,
                    ReviewCount = repetition.ReviewCount ?? 0,
                    Intervals = repetition.Intervals ?? 1,
                    Status = CalculateStatus(repetition),
                    IsDue = false,
                    DaysUntilReview = newIntervals,
                    BestQuizScore = repetition.BestQuizScore,
                    LastQuizScore = repetition.LastQuizScore,
                    LastQuizCompletedAt = repetition.LastQuizCompletedAt,
                    TotalQuizAttempts = repetition.TotalQuizAttempts
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
                VocabularyId = item.VocabularyId,
                VocabularyListId = item.VocabularyListId,
                VocabularyListName = item.VocabularyList?.Name ?? "Unknown",
                VocabularyWord = item.Vocabulary?.Word,
                LastReviewedAt = item.LastReviewedAt,
                NextReviewAt = item.NextReviewAt,
                ReviewCount = item.ReviewCount ?? 0,
                Intervals = item.Intervals ?? 1,
                Status = CalculateStatus(item),
                IsDue = item.NextReviewAt.HasValue && item.NextReviewAt.Value <= now,
                DaysUntilReview = item.NextReviewAt.HasValue 
                    ? Math.Max(0, (int)(item.NextReviewAt.Value - now).TotalDays)
                    : 0,
                BestQuizScore = item.BestQuizScore,
                LastQuizScore = item.LastQuizScore,
                LastQuizCompletedAt = item.LastQuizCompletedAt,
                TotalQuizAttempts = item.TotalQuizAttempts
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
                VocabularyId = repetition.VocabularyId,
                VocabularyListId = repetition.VocabularyListId,
                VocabularyListName = vocabularyList.Name,
                VocabularyWord = repetition.Vocabulary?.Word,
                LastReviewedAt = repetition.LastReviewedAt,
                NextReviewAt = repetition.NextReviewAt,
                ReviewCount = 0,
                Intervals = 1,
                Status = "New",
                IsDue = false,
                DaysUntilReview = 1,
                BestQuizScore = null,
                LastQuizScore = null,
                LastQuizCompletedAt = null,
                TotalQuizAttempts = null
            };
        }

        // Tính toán status động dựa trên dữ liệu hiện có
        private string CalculateStatus(UserSpacedRepetition item)
        {
            // Nếu intervals >= 30, coi như đã Mastered
            if (item.Intervals.HasValue && item.Intervals.Value >= 30)
            {
                return "Mastered";
            }

            // Nếu có reviewCount > 0 hoặc có quiz scores, coi như đang Learning
            bool hasProgress = (item.ReviewCount.HasValue && item.ReviewCount.Value > 0) ||
                              (item.BestQuizScore.HasValue || item.LastQuizScore.HasValue);

            if (hasProgress)
            {
                return "Learning";
            }

            // Nếu chưa có tiến độ nào, coi như New
            return "New";
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
                // Sửa: Lần review thứ 2 (reviewCount = 1) với quality >= 3 nên là 6 ngày
                if (reviewCount == 1 && currentIntervals == 1)
                {
                    newIntervals = 6;  // Đúng với SM-2
                }
                else
                {
                    newIntervals = currentIntervals;  // Giữ nguyên cho các lần sau
                }
                newStatus = "Learning";
            }
            else
            {
                // Sửa: Lần review thứ 2 (reviewCount = 1) với quality > 3 cũng nên là 6 ngày
                if (reviewCount == 1 && currentIntervals == 1)
                {
                    newIntervals = 6;  // Đúng với SM-2
                }
                else
                {
                    // Nhớ tốt, tăng khoảng thời gian
                    // Công thức: interval = interval * (1 + (quality - 3) * 0.5)
                    newIntervals = (int)Math.Ceiling(currentIntervals * (1 + (quality - 3) * 0.5));
                }
                newStatus = newIntervals >= 30 ? "Mastered" : "Learning";
            }

            // Giới hạn tối đa là 90 ngày
            newIntervals = Math.Min(newIntervals, 90);

            return (newIntervals, newStatus);
        }

        // Save quiz result
        public async Task<bool> SaveQuizResultAsync(int userId, SaveQuizResultRequestDTO request)
        {
            // Tìm hoặc tạo UserSpacedRepetition record
            var repetition = await _unitOfWork.UserSpacedRepetitions.GetByUserAndListAsync(
                userId, 
                request.VocabularyListId
            );

            if (repetition == null)
            {
                // Nếu chưa có, tạo mới
                var vocabularyList = await _unitOfWork.VocabularyLists.FindByIdAsync(request.VocabularyListId);
                if (vocabularyList == null)
                {
                    throw new KeyNotFoundException("Vocabulary list not found.");
                }

                repetition = new UserSpacedRepetition
                {
                    UserId = userId,
                    VocabularyListId = request.VocabularyListId,
                    LastReviewedAt = DateTime.UtcNow,
                    NextReviewAt = DateTime.UtcNow.AddDays(1),
                    ReviewCount = 0,
                    Intervals = 1,
                    Status = "New"
                };
                await _unitOfWork.UserSpacedRepetitions.AddAsync(repetition);
            }

            // Cập nhật quiz scores
            repetition.LastQuizScore = request.Score;
            repetition.LastQuizCompletedAt = DateTime.UtcNow;
            repetition.TotalQuizAttempts = (repetition.TotalQuizAttempts ?? 0) + 1;

            // Cập nhật best score nếu điểm mới cao hơn
            if (!repetition.BestQuizScore.HasValue || request.Score > repetition.BestQuizScore.Value)
            {
                repetition.BestQuizScore = request.Score;
            }

            // Cập nhật status dựa trên dữ liệu hiện có
            repetition.Status = CalculateStatus(repetition);

            await _unitOfWork.UserSpacedRepetitions.UpdateAsync(repetition);
            await _unitOfWork.CompleteAsync();

            return true;
        }

        // Get quiz scores
        public async Task<IEnumerable<QuizScoreDTO>> GetQuizScoresAsync(int userId, int? vocabularyListId = null)
        {
            IEnumerable<UserSpacedRepetition> items;

            if (vocabularyListId.HasValue)
            {
                var item = await _unitOfWork.UserSpacedRepetitions.GetByUserAndListAsync(userId, vocabularyListId.Value);
                items = item != null ? new[] { item } : Enumerable.Empty<UserSpacedRepetition>();
            }
            else
            {
                items = await _unitOfWork.UserSpacedRepetitions.GetByUserIdAsync(userId);
            }

            return items
                .Where(item => item.BestQuizScore.HasValue || item.LastQuizScore.HasValue) // Chỉ lấy những folder đã làm quiz
                .Select(item => new QuizScoreDTO
                {
                    VocabularyListId = item.VocabularyListId,
                    VocabularyListName = item.VocabularyList?.Name ?? "Unknown",
                    BestScore = item.BestQuizScore,
                    LastScore = item.LastQuizScore,
                    LastCompletedAt = item.LastQuizCompletedAt,
                    TotalAttempts = item.TotalQuizAttempts
                })
                .ToList();
        }
    }
}

