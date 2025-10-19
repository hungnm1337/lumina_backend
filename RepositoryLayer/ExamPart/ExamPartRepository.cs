using DataLayer.DTOs.ExamPart;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ExamPartRepository : IExamPartRepository
{
    private readonly LuminaSystemContext _context;

    public ExamPartRepository(LuminaSystemContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ExamPart>> GetAllPartsAsync()
    {
        return await _context.ExamParts.ToListAsync();
    }

    public async Task<List<ExamPartDto>> GetAllExamPartsAsync()
    {
        return await _context.ExamParts
            .Select(part => new ExamPartDto
            {
                PartId = part.PartId,
                PartCode = part.PartCode,
                Title = part.Title,
                MaxQuestions = part.MaxQuestions,
                SkillType = part.Exam.ExamType,
                ExamSetKey = part.Exam != null ? part.Exam.ExamSetKey : null
            })
            .ToListAsync();
    }

}

