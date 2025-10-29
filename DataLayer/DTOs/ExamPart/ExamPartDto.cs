using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DTOs.ExamPart
{
    public class ExamPartDto
    {
        public int PartId { get; set; }
        public string PartCode { get; set; }
        public string Title { get; set; }
        public int MaxQuestions { get; set; }
        public string? SkillType { get; set; }
        public string? ExamSetKey { get; set; } // lấy từ bảng Exam
    }



    public class ExamPartBriefDto
    {
        public int PartId { get; set; }
        public string PartCode { get; set; }
        public string? Title { get; set; }
        public int MaxQuestions { get; set; }
        public int QuestionCount { get; set; }
    }

    public class ExamGroupBySetKeyDto
    {
        public string ExamSetKey { get; set; }
        public List<ExamWithPartsDto> Exams { get; set; }
    }

    public class ExamWithPartsDto
    {
        public int ExamId { get; set; }
        public string Name { get; set; }
        public bool? IsActive { get; set; }
        public string? Description { get; set; }
        public List<ExamPartBriefDto> Parts { get; set; }
    }

}
