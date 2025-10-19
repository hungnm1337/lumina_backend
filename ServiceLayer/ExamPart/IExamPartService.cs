using DataLayer.DTOs.ExamPart;
using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface IExamPartService
{
    Task<IEnumerable<ExamPart>> GetAllPartsAsync();

    Task<List<ExamPartDto>> GetAllExamPartAsync();
}

