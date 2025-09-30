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

    // GET api/vocabularies/stats
    [HttpGet("stats")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> GetStats()
    {
        var counts = await _unitOfWork.Vocabularies.GetCountsByListAsync();
        // Ideally we join with VocabularyList to get names; do a simple projection here
        return Ok(counts.Select(kv => new { listId = kv.Key, total = kv.Value }));
    }
}


