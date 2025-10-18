using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ExamPartService : IExamPartService
{
    private readonly IExamPartRepository _examRepository;

    public ExamPartService(IExamPartRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public async Task<IEnumerable<ExamPart>> GetAllPartsAsync()
    {
        return await _examRepository.GetAllPartsAsync();
    }
}
