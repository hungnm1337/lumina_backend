using System;
using System.Collections.Generic;

namespace DataLayer.Models;

public partial class Exam
{
    public int ExamId { get; set; }

    public string ExamType { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public bool IsActive { get; set; }

    public int CreatedBy { get; set; }

    public int? UpdateBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public string ExamSetKey { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<ExamAttempt> ExamAttempts { get; set; } = new List<ExamAttempt>();

    public virtual ICollection<ExamPart> ExamParts { get; set; } = new List<ExamPart>();

    public virtual User? UpdateByNavigation { get; set; }
}
