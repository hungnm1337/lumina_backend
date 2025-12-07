using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using RepositoryLayer.Generic;
using System;
using System.Threading.Tasks;

namespace RepositoryLayer.Speaking
{
    /// <summary>
    /// Repository implementation for Speaking module - handles all database operations for speaking exams
    /// </summary>
    public class SpeakingRepository : Repository<UserAnswerSpeaking>, ISpeakingRepository
    {
        private readonly new LuminaSystemContext _context;

        public SpeakingRepository(LuminaSystemContext context) : base(context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<UserAnswerSpeaking?> GetExistingAnswerAsync(int attemptId, int questionId)
        {
            return await _context.UserAnswerSpeakings
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AttemptID == attemptId && a.QuestionId == questionId);
        }

        /// <inheritdoc />
        public async Task<Question?> GetQuestionWithPartAsync(int questionId)
        {
            return await _context.Questions
                .Include(q => q.Part)
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.QuestionId == questionId);
        }

        /// <inheritdoc />
        public async Task<ExamAttempt> GetOrCreateExamAttemptAsync(int userId, int examId)
        {
            var existingAttempt = await _context.ExamAttempts
                .FirstOrDefaultAsync(e => e.UserID == userId && e.ExamID == examId && e.Status == "In Progress");

            if (existingAttempt != null)
            {
                return existingAttempt;
            }

            var newAttempt = new ExamAttempt
            {
                UserID = userId,
                ExamID = examId,
                StartTime = DateTime.UtcNow,
                Status = "In Progress"
            };

            await _context.ExamAttempts.AddAsync(newAttempt);
            await _context.SaveChangesAsync();

            return newAttempt;
        }

        /// <inheritdoc />
        public async Task<ExamAttempt?> GetExamAttemptByIdAsync(int attemptId)
        {
            return await _context.ExamAttempts
                .Include(a => a.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AttemptID == attemptId);
        }

        /// <inheritdoc />
        public async Task AddSpeakingAnswerAsync(UserAnswerSpeaking answer)
        {
            await _context.UserAnswerSpeakings.AddAsync(answer);
            await _context.SaveChangesAsync();
        }
    }
}