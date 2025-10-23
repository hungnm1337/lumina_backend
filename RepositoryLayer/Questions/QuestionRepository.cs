using DataLayer.DTOs.Passage;
using DataLayer.DTOs.Prompt;
using DataLayer.DTOs.Questions;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using RepositoryLayer.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepositoryLayer.Questions
{
    public class QuestionRepository : Repository<Question>, IQuestionRepository
    {

        private readonly LuminaSystemContext _context;

        public QuestionRepository(LuminaSystemContext context) : base(context)
        {
            _context = context;
        }
        
        /*public async Task<Passage> AddPassageAsync(Passage passage)
        {
            _context.Passages.Add(passage);
            await _context.SaveChangesAsync();
            return passage;
        }*/

        public async Task<Prompt> AddPromptAsync(Prompt prompt)
        {
            _context.Prompts.Add(prompt);
            await _context.SaveChangesAsync();
            return prompt;
        }

        public async Task<Question> AddQuestionAsync(Question question)
        {
            _context.Questions.Add(question);
            await _context.SaveChangesAsync();
            return question;
        }

        public async Task AddOptionsAsync(IEnumerable<Option> options)
        {
            _context.Options.AddRange(options);
            await _context.SaveChangesAsync();
        }
        /*    public async Task<(List<PassageDto> Items, int TotalPages)> GetPassagePromptQuestionsPagedAsync(int page, int size, int? partId)
            {
                var query = _context.Passages.AsQueryable();


                if (partId.HasValue)
                {
                    query = query.Where(p => p.Prompts.Any(pr => pr.Questions.Any(q => q.PartId == partId.Value)));
                }

                int totalRecords = await query.CountAsync();
                int totalPages = (int)Math.Ceiling(totalRecords / (double)size);

                var items = await query.OrderBy(p => p.PassageId)
                    .Skip((page - 1) * size)
                    .Take(size)
                    .Select(pa => new PassageDto
                    {
                        PassageId = pa.PassageId,
                        Title = pa.Title,
                        ContentText = pa.ContentText,
                        Prompt = pa.Prompts.Select(pr => new PromptDto
                        {
                            PromptId = pr.PromptId,
                            Skill = pr.Skill,
                            PromptText = pr.PromptText,
                            ReferenceImageUrl = pr.ReferenceImageUrl,
                            ReferenceAudioUrl = pr.ReferenceAudioUrl,
                            PartId = pr.Questions.Any() ? pr.Questions.First().PartId : 0, // Lấy partId từ question đầu tiên trong prompt
                            Questions = pr.Questions
                                .Where(q => !partId.HasValue || q.PartId == partId.Value)
                                .Select(q => new QuestionDto
                                {
                                    QuestionId = q.QuestionId,
                                    StemText = q.StemText,
                                    QuestionExplain = q.QuestionExplain,
                                    ScoreWeight = q.ScoreWeight,
                                    Time = q.Time,
                                    Options = q.Options.Select(o => new OptionDto
                                    {
                                        OptionId = o.OptionId,
                                        Content = o.Content,
                                        IsCorrect = o.IsCorrect ?? false
                                    }).ToList()
                                }).ToList()
                        }).FirstOrDefault()
                    })
                    .ToListAsync();

                return (items, totalPages);
            }*/


        /* public async Task<bool> EditPassageWithPromptAsync(PassageEditDto dto)
         {
             var passage = await _context.Passages.Include(p => p.Prompts)
                                            .FirstOrDefaultAsync(p => p.PassageId == dto.PassageId);
             if (passage == null) return false;

             // Update passage fields
             passage.Title = dto.Title;
             passage.ContentText = dto.ContentText;

             if (dto.Prompt != null)
             {
                 var prompt = passage.Prompts.FirstOrDefault(pr => pr.PromptId == dto.Prompt.PromptId);
                 if (prompt == null)
                 {
                     // Có thể tạo prompt mới nếu chưa có
                     prompt = new Prompt
                     {
                         PromptId = dto.Prompt.PromptId,
                         Skill = dto.Prompt.Skill,
                         PromptText = dto.Prompt.PromptText,
                         ReferenceImageUrl = dto.Prompt.ReferenceImageUrl,
                         ReferenceAudioUrl = dto.Prompt.ReferenceAudioUrl
                     };
                     passage.Prompts.Add(prompt);
                 }
                 else
                 {
                     // Update prompt fields
                     prompt.Skill = dto.Prompt.Skill;
                     prompt.PromptText = dto.Prompt.PromptText;
                     prompt.ReferenceImageUrl = dto.Prompt.ReferenceImageUrl;
                     prompt.ReferenceAudioUrl = dto.Prompt.ReferenceAudioUrl;
                 }
             }

             await _context.SaveChangesAsync();
             return true;
         }*/

        public async Task<bool> EditPromptWithQuestionsAsync(PromptEditDto dto)
        {
            // Lấy Prompt theo id, bao gồm câu hỏi (nếu có)
            var prompt = await _context.Prompts
                .Include(p => p.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(p => p.PromptId == dto.PromptId);

            if (prompt == null)
                return false;

            // Update các trường chính của Prompt (Title và ContentText thay cho Passage)
            prompt.Title = dto.Title;
            prompt.ContentText = dto.ContentText;
            prompt.Skill = dto.Skill;
            prompt.ReferenceImageUrl = dto.ReferenceImageUrl;
            prompt.ReferenceAudioUrl = dto.ReferenceAudioUrl;
           

            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<(List<PromptDto> Items, int TotalPages)> GetPromptsPagedAsync(int page, int size, int? partId)
        {
            var query = _context.Prompts.AsQueryable();

            if (partId.HasValue)
            {
                query = query.Where(p => p.Questions.Any(q => q.PartId == partId.Value));
            }

            int totalRecords = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalRecords / (double)size);

            var items = await query.OrderBy(p => p.PromptId)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(pr => new PromptDto
                {
                    PromptId = pr.PromptId,
                    Title = pr.Title,
                    ContentText = pr.ContentText,
                    Skill = pr.Skill,
                    ReferenceImageUrl = pr.ReferenceImageUrl,
                    ReferenceAudioUrl = pr.ReferenceAudioUrl,
                    PartId = pr.Questions.Any() ? pr.Questions.First().PartId : 0,
                    Questions = pr.Questions
                        .Where(q => !partId.HasValue || q.PartId == partId.Value)
                        .Select(q => new QuestionDto
                        {
                            QuestionId = q.QuestionId,
                            StemText = q.StemText,
                            QuestionExplain = q.QuestionExplain,
                            ScoreWeight = q.ScoreWeight,
                            Time = q.Time,
                            Options = q.Options.Select(o => new OptionDto
                            {
                                OptionId = o.OptionId,
                                Content = o.Content,
                                IsCorrect = o.IsCorrect ?? false
                            }).ToList()
                        }).ToList()
                })
                .ToListAsync();

            return (items, totalPages);
        }



        public async Task<int> AddQuestionAsync(QuestionCrudDto dto)
        {
            // Lấy ExamPart để biết max câu hỏi được phép
            var part = await _context.ExamParts.FindAsync(dto.PartId);
            if (part == null)
                throw new Exception($"ExamPart với Id={dto.PartId} không tồn tại.");

            // Đếm số câu hỏi hiện tại của Part
            int currentCount = await _context.Questions.CountAsync(q => q.PartId == dto.PartId);

            if (currentCount >= part.MaxQuestions)
                throw new Exception($"ExamPart id {dto.PartId} đã đủ {part.MaxQuestions} câu hỏi rồi.");

            // Nếu chưa đạt max thì thêm câu hỏi mới
            var question = new Question
            {
                StemText = dto.StemText,
                QuestionExplain = dto.QuestionExplain,
                ScoreWeight = dto.ScoreWeight,
                Time = dto.Time,
                PartId = dto.PartId,
                PromptId = dto.PromptId,
                QuestionType = dto.QuestionType,
                Options = dto.Options.Select(o => new Option
                {
                    Content = o.Content,
                    IsCorrect = o.IsCorrect
                }).ToList()
            };

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            return question.QuestionId;
        }


        public async Task<bool> UpdateQuestionAsync(QuestionCrudDto dto)
        {
            var question = await _context.Questions.Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.QuestionId == dto.QuestionId);
            if (question == null) return false;

            question.StemText = dto.StemText;
            question.QuestionExplain = dto.QuestionExplain;

            // Chỉ xử lý Option nếu có
            if (dto.Options != null && dto.Options.Any())
            {
                // Xóa hết và tạo lại
                _context.Options.RemoveRange(question.Options);
                question.Options = dto.Options.Select(o => new Option
                {
                    Content = o.Content,
                    IsCorrect = o.IsCorrect
                }).ToList();
            }
            else
            {
                // Nếu không có options, xóa hết option liên kết (nếu muốn: nếu kỹ năng không cần đáp án)
                if (question.Options.Any())
                {
                    _context.Options.RemoveRange(question.Options);
                    question.Options.Clear();
                }
            }
            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<bool> DeleteQuestionAsync(int questionId)
        {
            // Lấy question và options (chỉ cần)
            var question = await _context.Questions
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.QuestionId == questionId);

            if (question == null)
                return false;

            // Lấy ExamPart và Exam tương ứng qua PartId (trường trong Question)
            var examPart = await _context.ExamParts
                .Include(ep => ep.Exam)
                .FirstOrDefaultAsync(ep => ep.PartId == question.PartId);

            if (examPart.Exam != null && examPart.Exam.IsActive)
                throw new Exception("Không thể xóa câu hỏi vì bài thi đang hoạt động.");

            // Xoá options nếu có
            if (question.Options.Any())
            {
                _context.Options.RemoveRange(question.Options);
            }

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            return true;
        }



        public async Task<QuestionStatisticDto> GetQuestionStatisticsAsync()
        {
            var total = await _context.Questions.CountAsync();
            var used = await _context.UserAnswers
                .Select(eq => eq.QuestionId)
                .Distinct()
                .CountAsync();
            var unused = await _context.Questions
                .CountAsync(q => !_context.UserAnswers
                    .Select(eq => eq.QuestionId)
                    .Distinct()
                    .Contains(q.QuestionId));

            return new QuestionStatisticDto
            {
                TotalQuestions = total,
                UsedQuestions = used,
                UnusedQuestions = unused
            };
        }

        //public async Task<bool> UpdatePassageAndPrompt(PassageEditDto dto)
        //{
        //    var passage = await _context.Passages.Include(p => p.Prompts)
        //                               .FirstOrDefaultAsync(p => p.PassageId == dto.PassageId);
        //    if (passage == null) return false;

        //    passage.Title = dto.Title;
        //    passage.ContentText = dto.ContentText;

        //    if (dto.Prompt != null)
        //    {
        //        var prompt = passage.Prompts.FirstOrDefault(pr => pr.PromptId == dto.Prompt.PromptId);
        //        if (prompt == null)
        //        {
        //            prompt = new Prompt
        //            {
        //                PromptId = dto.Prompt.PromptId,
        //                Skill = dto.Prompt.Skill,
        //                Title = dto.Prompt.Title, // ✅ Thêm Title
        //                ContentText = dto.Prompt.ContentText, // ✅ Đổi từ PromptText
        //                ReferenceImageUrl = dto.Prompt.ReferenceImageUrl,
        //                ReferenceAudioUrl = dto.Prompt.ReferenceAudioUrl
        //            };
        //            passage.Prompts.Add(prompt);
        //        }
        //        else
        //        {
        //            prompt.Skill = dto.Prompt.Skill;
        //            prompt.Title = dto.Prompt.Title; // ✅ Thêm Title
        //            prompt.ContentText = dto.Prompt.ContentText; // ✅ Đổi từ PromptText
        //            prompt.ReferenceImageUrl = dto.Prompt.ReferenceImageUrl;
        //            prompt.ReferenceAudioUrl = dto.Prompt.ReferenceAudioUrl;
        //        }
        //    }

        //    await _context.SaveChangesAsync();
        //    return true;
        //}

    }
}
