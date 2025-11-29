using DataLayer.DTOs.UserAnswer;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepositoryLayer.Exam.Writting
{
    public class WrittingRepository : IWrittingRepository
    {
        private readonly LuminaSystemContext _context;

        public WrittingRepository(LuminaSystemContext context)
        {
            _context = context;
        }

        public async Task<bool> SaveWritingAnswer(WritingAnswerRequestDTO writingAnswerRequestDTO)
        {
            try
            {
                var existingAnswer = await _context.UserAnswerWritings
                    .FirstOrDefaultAsync(ua => ua.AttemptID == writingAnswerRequestDTO.AttemptID 
                                           && ua.QuestionId == writingAnswerRequestDTO.QuestionId);

                if (existingAnswer != null)
                {
                    existingAnswer.UserAnswerContent = writingAnswerRequestDTO.UserAnswerContent;
                    existingAnswer.FeedbackFromAI = writingAnswerRequestDTO.FeedbackFromAI;
                    _context.UserAnswerWritings.Update(existingAnswer);
                }
                else
                {
                    var newAnswer = new UserAnswerWriting
                    {
                        AttemptID = writingAnswerRequestDTO.AttemptID,
                        QuestionId = writingAnswerRequestDTO.QuestionId,
                        UserAnswerContent = writingAnswerRequestDTO.UserAnswerContent,
                        FeedbackFromAI = writingAnswerRequestDTO.FeedbackFromAI
                    };
                    await _context.UserAnswerWritings.AddAsync(newAnswer);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
