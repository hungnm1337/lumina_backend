using DataLayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryLayer.UnitOfWork;

namespace lumina.Controllers;

[ApiController]
[Route("api/vocabularies")]
public class VocabulariesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public VocabulariesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

  
    [HttpGet]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> GetList([FromQuery] int? listId, [FromQuery] string? search)
    {
        var items = await _unitOfWork.Vocabularies.GetByListAsync(listId, search);
        return Ok(items.Select(v => new
        {
            id = v.VocabularyId,
            listId = v.VocabularyListId,
            word = v.Word,
            type = v.TypeOfWord,
            definition = v.Definition,
            example = v.Example
        }));
    }

    public sealed class CreateVocabularyRequest
    {
        public int VocabularyListId { get; set; }
        public string Word { get; set; } = string.Empty;
        public string TypeOfWord { get; set; } = string.Empty; // noun, verb, adj...
        public string Definition { get; set; } = string.Empty;
        public string? Example { get; set; }
    }

    // POST api/vocabularies
    [HttpPost]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> Create([FromBody] CreateVocabularyRequest req)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var vocab = new Vocabulary
        {
            VocabularyListId = req.VocabularyListId,
            Word = req.Word,
            TypeOfWord = req.TypeOfWord,
            Definition = req.Definition,
            Example = req.Example,
            IsDeleted = false
        };
        await _unitOfWork.Vocabularies.AddAsync(vocab);
        await _unitOfWork.CompleteAsync();

        return CreatedAtAction(nameof(GetList), new { listId = req.VocabularyListId }, new { id = vocab.VocabularyId });
    }

    // GET api/vocabularies/{id}
    [HttpGet("{id}")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> GetById(int id)
    {
        var vocab = await _unitOfWork.Vocabularies.GetByIdAsync(id);
        if (vocab == null)
        {
            return NotFound(new { message = "Vocabulary not found" });
        }

        return Ok(new
        {
            id = vocab.VocabularyId,
            listId = vocab.VocabularyListId,
            word = vocab.Word,
            type = vocab.TypeOfWord,
            definition = vocab.Definition,
            example = vocab.Example
        });
    }

    // PUT api/vocabularies/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateVocabularyRequest req)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var vocab = await _unitOfWork.Vocabularies.GetByIdAsync(id);
        if (vocab == null)
        {
            return NotFound(new { message = "Vocabulary not found" });
        }

        vocab.Word = req.Word;
        vocab.TypeOfWord = req.TypeOfWord;
        vocab.Definition = req.Definition;
        vocab.Example = req.Example;

        await _unitOfWork.Vocabularies.UpdateAsync(vocab);
        await _unitOfWork.CompleteAsync();

        return Ok(new { message = "Vocabulary updated successfully" });
    }

    // DELETE api/vocabularies/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> Delete(int id)
    {
        var vocab = await _unitOfWork.Vocabularies.GetByIdAsync(id);
        if (vocab == null)
        {
            return NotFound(new { message = "Vocabulary not found" });
        }

        await _unitOfWork.Vocabularies.DeleteAsync(id);
        await _unitOfWork.CompleteAsync();

        return Ok(new { message = "Vocabulary deleted successfully" });
    }

    // GET api/vocabularies/search
    [HttpGet("search")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> Search([FromQuery] string term, [FromQuery] int? listId)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return BadRequest(new { message = "Search term is required" });
        }

        var results = await _unitOfWork.Vocabularies.SearchAsync(term, listId);
        return Ok(results.Select(v => new
        {
            id = v.VocabularyId,
            listId = v.VocabularyListId,
            word = v.Word,
            type = v.TypeOfWord,
            definition = v.Definition,
            example = v.Example
        }));
    }

    // GET api/vocabularies/by-type/{type}
    [HttpGet("by-type/{type}")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> GetByType(string type)
    {
        var results = await _unitOfWork.Vocabularies.GetByTypeAsync(type);
        return Ok(results.Select(v => new
        {
            id = v.VocabularyId,
            listId = v.VocabularyListId,
            word = v.Word,
            type = v.TypeOfWord,
            definition = v.Definition,
            example = v.Example
        }));
    }

    // GET api/vocabularies/stats
    [HttpGet("stats")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> GetStats()
    {
        var counts = await _unitOfWork.Vocabularies.GetCountsByListAsync();
        var totalCount = await _unitOfWork.Vocabularies.GetTotalCountAsync();
        
        return Ok(new
        {
            totalCount = totalCount,
            countsByList = counts.Select(kv => new { listId = kv.Key, total = kv.Value })
        });
    }

    public sealed class UpdateVocabularyRequest
    {
        public string Word { get; set; } = string.Empty;
        public string TypeOfWord { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string? Example { get; set; }
    }
}







