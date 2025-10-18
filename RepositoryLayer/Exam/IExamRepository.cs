using DataLayer.DTOs.Exam;
using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


    public interface IExamRepository
    {
        public Task<List<ExamDTO>> GetAllExams(string? examType = null, string? partCode = null);
        public Task<ExamDTO> GetExamDetailAndExamPartByExamID(int examId);

        public Task<ExamPartDTO> GetExamPartDetailAndQuestionByExamPartID(int partId);


        Task<List<Exam>> GetExamsBySetKeyAsync(string examSetKey);
        Task<List<ExamPart>> GetExamPartsByExamIdsAsync(List<int> examIds);
        Task InsertExamsAsync(List<Exam> exams);
        Task InsertExamPartsAsync(List<ExamPart> parts);

    }

