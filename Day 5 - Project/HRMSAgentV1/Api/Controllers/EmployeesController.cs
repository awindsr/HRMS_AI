using HrmsAgent.Data;
using HrmsAgent.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRMSAgentV1.Api.Controllers;

[ApiController]
[Route("api/v1/employees")]
public sealed class EmployeesController : ControllerBase
{
    private readonly HrmsDbContext _db;
    public EmployeesController(HrmsDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? department,
        [FromQuery] string? status,
        [FromQuery] int? limit,
        [FromQuery] string? fail)
    {
        if (fail == "timeout")
            Thread.Sleep(TimeSpan.FromSeconds(30)); // > client timeout → forces a client-side timeout
        if (fail == "500")
            return Problem("Simulated upstream failure", statusCode: 500);

        IQueryable<Employee> q = _db.Employees;
        if (!string.IsNullOrWhiteSpace(department))
            q = q.Where(e => e.Department.ToLower() == department.ToLower());
        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(e => e.Status.ToLower() == status.ToLower());

        var clamped = Math.Clamp(limit ?? 20, 1, 100);
        var page = await q.OrderBy(e => e.EmployeeId).Take(clamped).ToListAsync();
        return Ok(new { total = page.Count, employees = page });
    }

    // GET /api/v1/employees/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var emp = await _db.Employees.FirstOrDefaultAsync(e => e.EmployeeId.ToLower() == id.ToLower());

        return emp is null
            ? NotFound(new { error = "employee_not_found", employeeId = id })
            : Ok(emp);
    }
}
