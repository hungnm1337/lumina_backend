using System;
using System.Collections.Generic;

namespace DataLayer.DTOs.Exam
{
    
    public class PartCompletionStatusDTO
    {
        public int PartId { get; set; }
        public string PartCode { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? Score { get; set; }
        public int? AttemptCount { get; set; }
    }

    
    public class ExamCompletionStatusDTO
    {
        public int ExamId { get; set; }
        public bool IsCompleted { get; set; }
        public int CompletedPartsCount { get; set; }
        public int TotalPartsCount { get; set; }
        public List<PartCompletionStatusDTO> Parts { get; set; } = new List<PartCompletionStatusDTO>();
        public double CompletionPercentage => TotalPartsCount > 0 
            ? Math.Round((double)CompletedPartsCount / TotalPartsCount * 100, 2) 
            : 0;
    }
}
