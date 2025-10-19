using DataLayer.DTOs.Exam;
using DataLayer.DTOs.ExamPart;
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

    Task<ExamPartDTO> GetExamPartDetailAndQuestionByExamPartID(int partId);

    Task<bool> ExamSetKeyExistsAsync(string setKey);
    Task<List<Exam>> GetExamsBySetKeyAsync(string examSetKey);
        Task<List<ExamPart>> GetExamPartsByExamIdsAsync(List<int> examIds);
        Task InsertExamsAsync(List<Exam> exams);
        Task InsertExamPartsAsync(List<ExamPart> parts);


    Task<List<ExamGroupBySetKeyDto>> GetExamsGroupedBySetKeyAsync();

    Task<bool> ToggleExamStatusAsync(int examId);

    }

