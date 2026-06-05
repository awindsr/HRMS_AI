using HrmsAgent.Models;
using Microsoft.AspNetCore.Mvc;

namespace HRMSAgentV1.Api.Controllers;



[ApiController]
[Route("api/v1/tasks")]
public sealed class TasksController : ControllerBase
{
    // GET /api/v1/tasks?employeeId=&status=
    [HttpGet]
    public IActionResult List([FromQuery] string? employeeId, [FromQuery] string? status)
    {
        IEnumerable<EmployeeTask> q = HrmsData.Tasks;
        if (!string.IsNullOrWhiteSpace(employeeId))
            q = q.Where(t => t.EmployeeId.Equals(employeeId, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(t => t.Status.Equals(status, StringComparison.OrdinalIgnoreCase));

        var list = q.ToList();
        return Ok(new { total = list.Count, tasks = list });
    }
}
