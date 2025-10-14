using System;
using System.Collections.Generic;

namespace lumina.Models;

public partial class ExamPart
{
    public int PartId { get; set; }

    public int ExamId { get; set; }

    public string PartCode { get; set; } = null!;

    public string Title { get; set; } = null!;

    public int OrderIndex { get; set; }

    public int MaxQuestions { get; set; }

    public virtual Exam Exam { get; set; } = null!;

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}
