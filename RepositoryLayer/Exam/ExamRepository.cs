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
            var exams = await _luminaSystemContext.Exams
                .Where(e => e.IsActive == true)
                .Select(e => new ExamDTO
                {
                    ExamId = e.ExamId,
                    ExamType = e.ExamType,
                    Name = e.Name,
                    Description = e.Description,
                    IsActive = e.IsActive,
                    CreatedBy = e.CreatedBy,
                    UpdateBy = e.UpdateBy,
                    CreatedAt = e.CreatedAt,
                    UpdateAt = e.UpdateAt,
                    ExamParts = null // Không bao gồm thông tin của các part
                })
                .ToListAsync();
            return exams;
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
            var examDetail = await _luminaSystemContext.Exams
                .Where(e => e.ExamId == examId && e.IsActive == true)
                .Select(e => new ExamDTO
                {
                    ExamId = e.ExamId,
                    ExamType = e.ExamType,
                    Name = e.Name,
                    Description = e.Description,
                    IsActive = e.IsActive,
                    CreatedBy = e.CreatedBy,
                    UpdateBy = e.UpdateBy,
                    CreatedAt = e.CreatedAt,
                    UpdateAt = e.UpdateAt,
                    ExamParts = e.ExamParts.Select(ep => new ExamPartDTO
                    {
                        PartId = ep.PartId,
                        ExamId = ep.ExamId,
                        PartCode = ep.PartCode,
                        Title = ep.Title,
                        OrderIndex = ep.OrderIndex,
                        Questions = null // Không load câu hỏi ở đây
                    }).ToList()
                })
                .FirstOrDefaultAsync();

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
