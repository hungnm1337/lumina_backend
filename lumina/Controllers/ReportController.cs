using DataLayer.DTOs;
using DataLayer.DTOs.Auth;
using DataLayer.DTOs.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.Report;
using System.Security.Claims;

namespace lumina.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportController> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public ReportController(
        IReportService reportService, 
        ILogger<ReportController> logger,
        IUnitOfWork unitOfWork)
    {
        _reportService = reportService;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Create a new report
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateReport([FromBody] ReportCreateRequestDTO request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("Invalid token: Claim 'NameIdentifier' not found or invalid.");
                return Unauthorized(new ErrorResponse("Invalid token - User ID could not be determined."));
            }

            var createdReport = await _reportService.CreateReportAsync(request, userId);
            return CreatedAtAction(nameof(GetReportById), new { id = createdReport.ReportId }, createdReport);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex.Message);
            return BadRequest(new ErrorResponse(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex.Message);
            return NotFound(new ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while creating a report.");
            return StatusCode(500, new ErrorResponse("An internal server error occurred. Please try again later."));
        }
    }

    /// <summary>
    /// Get report by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetReportById(int id)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            int? userId = null;
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            // Get user role
            int? roleId = null;
            if (userId.HasValue)
            {
                var user = await _unitOfWork.Users.GetUserByIdAsync(userId.Value);
                if (user != null)
                {
                    roleId = user.RoleId;
                }
            }

            var report = await _reportService.GetReportByIdAsync(id, userId, roleId);
            if (report == null)
            {
                return NotFound(new ErrorResponse($"Report with ID {id} not found."));
            }

            return Ok(report);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex.Message);
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving report with ID {ReportId}.", id);
            return StatusCode(500, new ErrorResponse("An internal server error occurred. Please try again later."));
        }
    }

    /// <summary>
    /// Get reports with query and pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetReports([FromQuery] ReportQueryParams query)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            int? userId = null;
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            // Get user role
            int? roleId = null;
            if (userId.HasValue)
            {
                var user = await _unitOfWork.Users.GetUserByIdAsync(userId.Value);
                if (user != null)
                {
                    roleId = user.RoleId;
                }
            }

            var result = await _reportService.GetReportsAsync(query, userId, roleId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving reports.");
            return StatusCode(500, new ErrorResponse("An internal server error occurred. Please try again later."));
        }
    }

    /// <summary>
    /// Reply to a report (Admin and Manager only)
    /// </summary>
    [HttpPost("{id}/reply")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ReplyToReport(int id, [FromBody] ReportReplyRequestDTO request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new ErrorResponse("Invalid token - User ID could not be determined."));
            }

            // Get user role
            var user = await _unitOfWork.Users.GetUserByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized(new ErrorResponse("User not found."));
            }

            var updatedReport = await _reportService.ReplyToReportAsync(id, request, userId, user.RoleId);
            if (updatedReport == null)
            {
                return NotFound(new ErrorResponse($"Report with ID {id} not found."));
            }

            return Ok(updatedReport);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex.Message);
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while replying to report with ID {ReportId}.", id);
            return StatusCode(500, new ErrorResponse("An internal server error occurred. Please try again later."));
        }
    }
}

