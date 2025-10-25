using DataLayer.DTOs.UserAnswer;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore; // Cần thêm để sử dụng FindAsync và SaveChangesAsync
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepositoryLayer.Exam.ExamAttempt
{
    public static class ExamAttemptStatus
    {
        public const string Doing = "Doing";
        public const string Completed = "Completed";
    }

    public class ExamAttemptRepository : IExamAttemptRepository
    {
        private readonly LuminaSystemContext _context;

        public ExamAttemptRepository(LuminaSystemContext context)
        {
            _context = context;
        }

        public async Task<ExamAttemptDTO> EndAnExam(ExamAttemptDTO model)
        {
            var attempt = await _context.ExamAttempts.FindAsync(model.AttemptID);

            if (attempt == null)
            {
                throw new KeyNotFoundException($"Exam attempt with ID {model.AttemptID} not found.");
            }

            attempt.EndTime = model.EndTime;
            attempt.Score = model.Score;
            attempt.Status = ExamAttemptStatus.Completed;

            await _context.SaveChangesAsync();
            return new ExamAttemptDTO()
            {
                AttemptID = attempt.AttemptID,
                UserID = attempt.UserID,
                ExamID = attempt.ExamID,
                ExamPartId = attempt.ExamPartId,
                StartTime = attempt.StartTime,
                EndTime = attempt.EndTime,
                Score = attempt.Score,
                Status = attempt.Status
            };
        }

        public async Task<ExamAttemptDTO> StartAnExam(ExamAttemptDTO model)
        {
            DataLayer.Models.ExamAttempt attempt = new DataLayer.Models.ExamAttempt()
            {
                UserID = model.UserID,
                ExamID = model.ExamID,
                ExamPartId = model.ExamPartId,
                StartTime = model.StartTime,
                Status = ExamAttemptStatus.Doing 
            };

            await _context.ExamAttempts.AddAsync(attempt);

            await _context.SaveChangesAsync();        
            return new ExamAttemptDTO()
            {
                AttemptID = attempt.AttemptID, 
                UserID = attempt.UserID,
                ExamID = attempt.ExamID,
                ExamPartId = attempt.ExamPartId,
                StartTime = attempt.StartTime,
                EndTime = attempt.EndTime, 
                Score = attempt.Score,    
                Status = attempt.Status
            };
        }
    }
}