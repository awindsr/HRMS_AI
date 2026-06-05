using HrmsAgent.Models;
using Microsoft.AspNetCore.Mvc;

namespace HRMSAgentV1.Api.Controllers;

[ApiController]
[Route("api/v1/employees")]
public sealed class EmployeesController : ControllerBase
{
    [HttpGet]
    public IActionResult List(
        [FromQuery] string? department,
        [FromQuery] string? status,
        [FromQuery] int? limit,
        [FromQuery] string? fail)
    {
        if (fail == "timeout")
            Thread.Sleep(TimeSpan.FromSeconds(30)); // > client timeout → forces a client-side timeout
        if (fail == "500")
            return Problem("Simulated upstream failure", statusCode: 500);

        IEnumerable<Employee> q = HrmsData.Employees;
        if (!string.IsNullOrWhiteSpace(department))
            q = q.Where(e => e.Department.Equals(department, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(e => e.Status.Equals(status, StringComparison.OrdinalIgnoreCase));

        var clamped = Math.Clamp(limit ?? 20, 1, 100);
        var page = q.Take(clamped).ToList();
        return Ok(new { total = page.Count, employees = page });
    }

    // GET /api/v1/employees/{id}
    [HttpGet("{id}")]
    public IActionResult GetById(string id)
    {
        var emp = HrmsData.Employees.FirstOrDefault(
            e => e.EmployeeId.Equals(id, StringComparison.OrdinalIgnoreCase));

        return emp is null
            ? NotFound(new { error = "employee_not_found", employeeId = id })
            : Ok(emp);
    }
}