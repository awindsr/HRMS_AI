using HrmsAgent.Data;
using HrmsAgent.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMSAgentV1.Api.Controllers;

/// <summary>
/// Day 6 mock attendance endpoints. POST records an entry (the markAttendance write target);
/// GET supports the read-before-write correction pattern. Attendance is payroll-grade data,
/// so a POST that lands on an existing date is an overwrite (wasOverwrite = true).
/// </summary>
[ApiController]
[Route("api/v1/attendance")]
public sealed class AttendanceController : ControllerBase
{
    private readonly HrmsDbContext _db;
    public AttendanceController(HrmsDbContext db) => _db = db;

    public sealed record MarkAttendanceRequest(
        string? EmployeeId, DateOnly? Date, string? Status, string? CheckIn, string? CheckOut, string? Note);

    // POST /api/v1/attendance  (markAttendance, R2 self / R3 HR)
    [HttpPost]
    public async Task<IActionResult> Mark([FromBody] MarkAttendanceRequest req)
    {
        if (string.IsNullOrWhiteSpace(req?.EmployeeId))
            return BadRequest(new { error = "invalid_argument", message = "employeeId is required." });
        if (string.IsNullOrWhiteSpace(req.Status))
            return BadRequest(new { error = "invalid_argument", message = "status is required." });

        if (!await _db.Employees.AnyAsync(e => e.EmployeeId.ToLower() == req.EmployeeId.ToLower()))
            return NotFound(new { error = "employee_not_found", employeeId = req.EmployeeId });

        var date = req.Date ?? DateOnly.FromDateTime(DateTime.Today);
        var attendanceId = $"ATT-{date:yyyyMMdd}-{req.EmployeeId!.ToUpperInvariant()}";

        // An entry already on this id (employee + date) means we are overwriting the day.
        var wasOverwrite = await _db.Attendance.AnyAsync(a => a.AttendanceId == attendanceId);
        if (wasOverwrite)
            await _db.Attendance.Where(a => a.AttendanceId == attendanceId).ExecuteDeleteAsync();

        var record = new AttendanceRecord
        {
            AttendanceId = attendanceId,
            EmployeeId = req.EmployeeId!,
            Date = date,
            Status = req.Status!.ToLowerInvariant(),
            CheckIn = req.CheckIn,
            CheckOut = req.CheckOut,
            Note = req.Note,
            RecordedBy = req.EmployeeId!,
            RecordedAt = DateTimeOffset.Now
        };
        _db.Attendance.Add(record);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            attendanceId = record.AttendanceId,
            employeeId = record.EmployeeId,
            date = record.Date.ToString("yyyy-MM-dd"),
            status = record.Status,
            checkIn = record.CheckIn,
            checkOut = record.CheckOut,
            recordedBy = record.RecordedBy,
            recordedAt = record.RecordedAt,
            wasOverwrite
        });
    } 

    // GET /api/v1/attendance/{employeeId}?from=&to=  (read-before-write; getAttendance is not yet a connected tool)
    [HttpGet("{employeeId}")]
    public async Task<IActionResult> Get(string employeeId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        IQueryable<AttendanceRecord> q = _db.Attendance
            .Where(a => a.EmployeeId.ToLower() == employeeId.ToLower());
        if (from is not null) q = q.Where(a => a.Date >= from);
        if (to is not null) q = q.Where(a => a.Date <= to);

        var records = await q.OrderBy(a => a.Date).ToListAsync();
        return Ok(new { employeeId, total = records.Count, records });
    }
}
