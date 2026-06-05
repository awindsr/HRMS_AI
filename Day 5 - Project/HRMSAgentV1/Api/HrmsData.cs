using HrmsAgent.Models;

namespace HRMSAgentV1.Api;

/// <summary>In-memory seed data shared by the mock API controllers.</summary>
public static class HrmsData
{
    public static readonly List<Employee> Employees = new()
    {
        new() { EmployeeId = "E1001", FullName = "Priya Sharma",  Email = "priya.sharma@acme.com",  JobTitle = "Software Engineer",  Department = "engineering", ManagerId = "E1000", Location = "Bengaluru", Status = "active",   JoinDate = DateOnly.Parse("2022-03-14") },
        new() { EmployeeId = "E1002", FullName = "Arjun Mehta",   Email = "arjun.mehta@acme.com",   JobTitle = "Senior Engineer",    Department = "engineering", ManagerId = "E1000", Location = "Bengaluru", Status = "active",   JoinDate = DateOnly.Parse("2020-07-01") },
        new() { EmployeeId = "E1003", FullName = "Sara Khan",     Email = "sara.khan@acme.com",     JobTitle = "Product Manager",    Department = "product",     ManagerId = "E1000", Location = "Mumbai",    Status = "on_leave", JoinDate = DateOnly.Parse("2021-11-22") },
        new() { EmployeeId = "E1004", FullName = "David Lee",     Email = "david.lee@acme.com",     JobTitle = "Sales Executive",    Department = "sales",       ManagerId = "E1010", Location = "Delhi",     Status = "active",   JoinDate = DateOnly.Parse("2023-01-09") },
        new() { EmployeeId = "E1000", FullName = "Meena Iyer",    Email = "meena.iyer@acme.com",    JobTitle = "Engineering Manager",Department = "engineering", ManagerId = null,    Location = "Bengaluru", Status = "active",   JoinDate = DateOnly.Parse("2018-05-30") },
    };

    public static readonly List<EmployeeTask> Tasks = new()
    {
        new() { TaskId = "T-501", EmployeeId = "E1001", Title = "Implement getEmployeeList tool", Status = "in_progress", Priority = "high",   DueDate = DateOnly.Parse("2026-06-09") },
        new() { TaskId = "T-502", EmployeeId = "E1001", Title = "Write API error-handling notes", Status = "open",        Priority = "medium", DueDate = DateOnly.Parse("2026-06-10") },
        new() { TaskId = "T-503", EmployeeId = "E1002", Title = "Review function-calling loop",    Status = "open",        Priority = "high",   DueDate = DateOnly.Parse("2026-06-08") },
        new() { TaskId = "T-504", EmployeeId = "E1003", Title = "Draft Q3 product roadmap",        Status = "blocked",     Priority = "high",   DueDate = DateOnly.Parse("2026-06-15") },
        new() { TaskId = "T-505", EmployeeId = "E1004", Title = "Close ACME-corp renewal",         Status = "done",        Priority = "medium", DueDate = DateOnly.Parse("2026-06-01")  },
    };
}