using DataLayer.DTOs;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepositoryLayer.Exam
{
    public class ExamRepository : IExamRepository
    {
        private readonly LuminaSystemContext _luminaSystemContext;

        public ExamRepository(LuminaSystemContext luminaSystemContext)
        {
            _luminaSystemContext = luminaSystemContext;
        }

        /// <summary>
        /// Người dùng sẽ xem danh sách các bài thi có sẵn để chọn nhưng không bao gồm thông tin của các part
        /// 
        /// </summary>
        /// <returns>Danh sách các bài thi </returns>
        public async Task<List<ExamDTO>> GetAllExams()
        {
            var exams = await _luminaSystemContext.Exams.Where(e => e.IsActive == true).ToListAsync();

            var userIds = exams.Select(e => e.CreatedBy).Distinct();
            var users = await _luminaSystemContext.Users
                          .Where(u => userIds.Contains(u.UserId))
                          .ToDictionaryAsync(u => u.UserId, u => u.FullName);

            var examDtos = exams.Select(e => new ExamDTO
            {
                ExamId = e.ExamId,
                ExamType = e.ExamType,
                Name = e.Name,
                Description = e.Description,
                IsActive = e.IsActive,
                CreatedBy = e.CreatedBy,
                CreatedByName = users.ContainsKey(e.CreatedBy) ? users[e.CreatedBy] : "Unknown",
                UpdateBy = e.UpdateBy,
                UpdateByName = e.UpdateBy.HasValue && users.ContainsKey(e.UpdateBy.Value) ? users[e.UpdateBy.Value] : null,
                CreatedAt = e.CreatedAt,
                UpdateAt = e.UpdateAt,
                ExamParts = null
            }).ToList();

            return examDtos;
        }


        /// <summary>
        /// Lấy thông tin của Exam và Thông tin của các Exam part của exam đó
        /// 
        /// </summary>
        /// <param name="examId"></param>
        /// <returns>Thông tin của Exam và Thông tin của các Exam part của exam đó</returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<ExamDTO> GetExamDetailAndExamPartByExamID(int examId)
        {
            var exam = await _luminaSystemContext.Exams
                .Where(e => e.ExamId == examId && e.IsActive == true)
                .Include(e => e.ExamParts)
                .FirstOrDefaultAsync();

            if (exam == null) return null;

            var userIds = new List<int> { exam.CreatedBy };
            if (exam.UpdateBy.HasValue)
                userIds.Add(exam.UpdateBy.Value);

            var users = await _luminaSystemContext.Users
                .Where(u => userIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => u.FullName);

            var examDetail = new ExamDTO
            {
                ExamId = exam.ExamId,
                ExamType = exam.ExamType,
                Name = exam.Name,
                Description = exam.Description,
                IsActive = exam.IsActive,
                CreatedBy = exam.CreatedBy,
                CreatedByName = users.ContainsKey(exam.CreatedBy) ? users[exam.CreatedBy] : "Unknown",
                UpdateBy = exam.UpdateBy,
                UpdateByName = exam.UpdateBy.HasValue && users.ContainsKey(exam.UpdateBy.Value) ? users[exam.UpdateBy.Value] : null,
                CreatedAt = exam.CreatedAt,
                UpdateAt = exam.UpdateAt,
                ExamParts = exam.ExamParts.Select(ep => new ExamPartDTO
                {
                    PartId = ep.PartId,
                    ExamId = ep.ExamId,
                    PartCode = ep.PartCode,
                    Title = ep.Title,
                    OrderIndex = ep.OrderIndex,
                    Questions = null // Không load câu hỏi
                }).ToList()
            };

            return examDetail;
        }




        /// <summary>
        /// Lấy tất cả các câu hỏi và thông tin của một phần part trong bài thi
        /// </summary>
        /// <param name="partId"></param>
        /// <returns></returns>
        public async Task<ExamPartDTO> GetExamPartDetailAndQuestionByExamPartID(int partId)
        {
            var examPartDetail = await _luminaSystemContext.ExamParts
                .Where(ep => ep.PartId == partId)
                .Select(ep => new ExamPartDTO
                {
                    PartId = ep.PartId,
                    ExamId = ep.ExamId,
                    PartCode = ep.PartCode,
                    Title = ep.Title,
                    OrderIndex = ep.OrderIndex,
                    Questions = ep.Questions.Select(q => new QuestionDTO
                    {
                        QuestionId = q.QuestionId,
                        PartId = q.PartId,
                        QuestionType = q.QuestionType,
                        StemText = q.StemText,
                        PromptId = q.PromptId,
                        ScoreWeight = q.ScoreWeight,
                        QuestionExplain = q.QuestionExplain,
                        Time = q.Time,
                        QuestionNumber = q.QuestionNumber,
                        Options = q.Options.Select(o => new OptionDTO
                        {
                            OptionId = o.OptionId,
                            QuestionId = o.QuestionId,
                            Content = o.Content,
                            IsCorrect = o.IsCorrect
                        }).ToList(),
                        Prompt = q.Prompt == null ? null : new PromptDTO
                        {
                            PromptId = q.Prompt.PromptId,
                            PassageId = q.Prompt.PassageId,
                            Skill = q.Prompt.Skill,
                            PromptText = q.Prompt.PromptText,
                            ReferenceImageUrl = q.Prompt.ReferenceImageUrl,
                            ReferenceAudioUrl = q.Prompt.ReferenceAudioUrl,
                            Passage = q.Prompt.Passage == null ? null : new PassageDTO
                            {
                                PassageId = q.Prompt.Passage.PassageId,
                                Title = q.Prompt.Passage.Title,
                                ContentText = q.Prompt.Passage.ContentText
                            }
                        }
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            return examPartDetail;
        }
    }
}
