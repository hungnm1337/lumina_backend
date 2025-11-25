using DataLayer.DTOs.UserReport;
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
    public async Task<IActionResult> CreateReport([FromBody] UserReportRequest request)
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
                return Unauthorized(new { message = "Invalid token - User ID could not be determined." });
            }

            // Set SendBy from authenticated user
            request.SendBy = userId;

            var result = await _reportService.AddAsync(request);

            if (result)
            {
                return Ok(new { message = "Report created successfully", success = true });
            }

            return BadRequest(new { message = "Failed to create report", success = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while creating a report.");
            return StatusCode(500, new { message = "An internal server error occurred. Please try again later." });
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
            var report = await _reportService.FindByIdAsync(id);

            if (report == null)
            {
                return NotFound(new { message = $"Report with ID {id} not found." });
            }

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving report with ID {ReportId}.", id);
            return StatusCode(500, new { message = "An internal server error occurred. Please try again later." });
        }
    }

    /// <summary>
    /// Get all reports by role (Admin: System, Manager/Staff: Article/Exam)
    /// </summary>
    [HttpGet("by-role")]
    //[Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> GetReportsByRole()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "Invalid token - User ID could not be determined." });
            }

            // Get user role
            var user = await _unitOfWork.Users.GetUserByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized(new { message = "User not found." });
            }

            var reports = await _reportService.GetAllAsync(user.RoleId);
            return Ok(reports);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving reports by role.");
            return StatusCode(500, new { message = "An internal server error occurred. Please try again later." });
        }
    }

    /// <summary>
    /// Get all reports created by the current user
    /// </summary>
    [HttpGet("my-reports")]
    public async Task<IActionResult> GetMyReports()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "Invalid token - User ID could not be determined." });
            }

            var reports = await _reportService.GetAllByUserIdAsync(userId);
            return Ok(reports);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving user reports.");
            return StatusCode(500, new { message = "An internal server error occurred. Please try again later." });
        }
    }

    /// <summary>
    /// Reply to a report (Admin and Manager only)
    /// </summary>
    [HttpPut("{id}/reply")]
    //[Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ReplyToReport(int id, [FromBody] UserReportRequest request)
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
                return Unauthorized(new { message = "Invalid token - User ID could not be determined." });
            }

            // Get user role to validate permissions
            var user = await _unitOfWork.Users.GetUserByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized(new { message = "User not found." });
            }

            // Check if report exists first
            var existingReport = await _reportService.FindByIdAsync(id);
            if (existingReport == null)
            {
                return NotFound(new { message = $"Report with ID {id} not found." });
            }

            // Validate role-based permissions
            if (user.RoleId == 2) // Manager
            {
                // Manager can only reply to Article and Exam reports
                if (existingReport.Type == "System")
                {
                    return Forbid("Manager can only reply to Article and Exam reports.");
                }
            }
            // Admin (RoleId = 1) can reply to all reports

            // Set required fields for update
            request.ReportId = id;
            request.ReplyBy = userId;

            var result = await _reportService.UpdateAsync(request);

            if (result)
            {
                // Get updated report to return
                var updatedReport = await _reportService.FindByIdAsync(id);
                return Ok(updatedReport);
            }

            return BadRequest(new { message = "Failed to update report", success = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while replying to report with ID {ReportId}.", id);
            return StatusCode(500, new { message = "An internal server error occurred. Please try again later." });
        }
    }
}

