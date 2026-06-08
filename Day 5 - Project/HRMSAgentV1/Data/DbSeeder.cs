using HrmsAgent.Models;

namespace HrmsAgent.Data;

/// <summary>
/// Seeds the SQLite database with a realistic "Acme" org. The first five employees and tasks
/// (E1000–E1004, T-501…T-505) and the 5-Jun attendance record are the CANONICAL rows the Day 5
/// and Day 6 tests depend on — keep their attributes stable. Everything after them fleshes out a
/// believable company (engineering, product, design, sales, marketing, HR, finance, operations)
/// with a reporting hierarchy, multiple locations, and varied task statuses.
/// </summary>
public static class DbSeeder
{
    public static void Seed(HrmsDbContext db)
    {
        if (db.Employees.Any()) return; // idempotent

        db.Employees.AddRange(
            // ---- canonical (do not change: referenced by Day 5/6 tests & screenshots) ----
            new() { EmployeeId = "E1000", FullName = "Meena Iyer",       Email = "meena.iyer@acme.com",       JobTitle = "Engineering Manager",      Department = "engineering", ManagerId = null,    Location = "Bengaluru", Status = "active",   JoinDate = DateOnly.Parse("2018-05-30") },
            new() { EmployeeId = "E1001", FullName = "Priya Sharma",     Email = "priya.sharma@acme.com",     JobTitle = "Software Engineer",        Department = "engineering", ManagerId = "E1000", Location = "Bengaluru", Status = "active",   JoinDate = DateOnly.Parse("2022-03-14") },
            new() { EmployeeId = "E1002", FullName = "Arjun Mehta",      Email = "arjun.mehta@acme.com",      JobTitle = "Senior Engineer",          Department = "engineering", ManagerId = "E1000", Location = "Bengaluru", Status = "active",   JoinDate = DateOnly.Parse("2020-07-01") },
            new() { EmployeeId = "E1003", FullName = "Sara Khan",        Email = "sara.khan@acme.com",        JobTitle = "Product Manager",          Department = "product",     ManagerId = "E1020", Location = "Mumbai",    Status = "on_leave", JoinDate = DateOnly.Parse("2021-11-22") },
            new() { EmployeeId = "E1004", FullName = "David Lee",        Email = "david.lee@acme.com",        JobTitle = "Sales Executive",          Department = "sales",       ManagerId = "E1010", Location = "Delhi",     Status = "active",   JoinDate = DateOnly.Parse("2023-01-09") },

            // ---- leadership ----
            new() { EmployeeId = "E1020", FullName = "Rajesh Khanna",    Email = "rajesh.khanna@acme.com",    JobTitle = "Chief Executive Officer",  Department = "leadership",  ManagerId = null,    Location = "Bengaluru", Status = "active",   JoinDate = DateOnly.Parse("2015-01-12") },

            // ---- engineering ----
            new() { EmployeeId = "E1005", FullName = "Rohan Gupta",      Email = "rohan.gupta@acme.com",      JobTitle = "Software Engineer",        Department = "engineering", ManagerId = "E1000", Location = "Hyderabad", Status = "active",   JoinDate = DateOnly.Parse("2023-06-19") },
            new() { EmployeeId = "E1006", FullName = "Ananya Reddy",     Email = "ananya.reddy@acme.com",     JobTitle = "Senior Software Engineer", Department = "engineering", ManagerId = "E1000", Location = "Bengaluru", Status = "active",   JoinDate = DateOnly.Parse("2019-09-02") },
            new() { EmployeeId = "E1007", FullName = "Vikram Singh",     Email = "vikram.singh@acme.com",     JobTitle = "DevOps Engineer",          Department = "engineering", ManagerId = "E1000", Location = "Pune",      Status = "active",   JoinDate = DateOnly.Parse("2022-08-15") },
            new() { EmployeeId = "E1008", FullName = "Neha Joshi",       Email = "neha.joshi@acme.com",       JobTitle = "QA Engineer",              Department = "engineering", ManagerId = "E1000", Location = "Bengaluru", Status = "active",   JoinDate = DateOnly.Parse("2024-01-08") },
            new() { EmployeeId = "E1009", FullName = "Karthik Nair",     Email = "karthik.nair@acme.com",     JobTitle = "Staff Engineer",           Department = "engineering", ManagerId = "E1000", Location = "Remote",    Status = "active",   JoinDate = DateOnly.Parse("2017-04-11") },
            new() { EmployeeId = "E1024", FullName = "Wei Chen",         Email = "wei.chen@acme.com",         JobTitle = "Software Engineer",        Department = "engineering", ManagerId = "E1000", Location = "Remote",    Status = "inactive", JoinDate = DateOnly.Parse("2019-03-01") },

            // ---- sales ----
            new() { EmployeeId = "E1010", FullName = "Sunita Rao",       Email = "sunita.rao@acme.com",       JobTitle = "Sales Manager",            Department = "sales",       ManagerId = "E1020", Location = "Delhi",     Status = "active",   JoinDate = DateOnly.Parse("2016-02-20") },
            new() { EmployeeId = "E1011", FullName = "Aditya Kapoor",    Email = "aditya.kapoor@acme.com",    JobTitle = "Account Executive",        Department = "sales",       ManagerId = "E1010", Location = "Mumbai",    Status = "active",   JoinDate = DateOnly.Parse("2021-05-30") },
            new() { EmployeeId = "E1012", FullName = "Fatima Sheikh",    Email = "fatima.sheikh@acme.com",    JobTitle = "Sales Development Rep",     Department = "sales",       ManagerId = "E1010", Location = "Delhi",     Status = "active",   JoinDate = DateOnly.Parse("2023-10-03") },

            // ---- product & design ----
            new() { EmployeeId = "E1013", FullName = "Manish Patel",     Email = "manish.patel@acme.com",     JobTitle = "Product Manager",          Department = "product",     ManagerId = "E1020", Location = "Bengaluru", Status = "active",   JoinDate = DateOnly.Parse("2020-12-01") },
            new() { EmployeeId = "E1014", FullName = "Divya Menon",      Email = "divya.menon@acme.com",      JobTitle = "Product Designer",         Department = "design",      ManagerId = "E1013", Location = "Chennai",   Status = "active",   JoinDate = DateOnly.Parse("2022-02-14") },
            new() { EmployeeId = "E1015", FullName = "Rahul Verma",      Email = "rahul.verma@acme.com",      JobTitle = "UX Researcher",            Department = "design",      ManagerId = "E1013", Location = "Remote",    Status = "active",   JoinDate = DateOnly.Parse("2023-03-27") },

            // ---- HR, finance, marketing, operations ----
            new() { EmployeeId = "E1016", FullName = "Pooja Bhat",       Email = "pooja.bhat@acme.com",       JobTitle = "HR Manager",               Department = "hr",          ManagerId = "E1020", Location = "Bengaluru", Status = "active",   JoinDate = DateOnly.Parse("2018-11-05") },
            new() { EmployeeId = "E1017", FullName = "Sneha Pillai",     Email = "sneha.pillai@acme.com",     JobTitle = "HR Executive",             Department = "hr",          ManagerId = "E1016", Location = "Bengaluru", Status = "active",   JoinDate = DateOnly.Parse("2023-07-18") },
            new() { EmployeeId = "E1018", FullName = "Amit Desai",       Email = "amit.desai@acme.com",       JobTitle = "Finance Manager",          Department = "finance",     ManagerId = "E1020", Location = "Mumbai",    Status = "active",   JoinDate = DateOnly.Parse("2017-08-22") },
            new() { EmployeeId = "E1019", FullName = "Lakshmi Krishnan", Email = "lakshmi.krishnan@acme.com", JobTitle = "Accountant",               Department = "finance",     ManagerId = "E1018", Location = "Chennai",   Status = "active",   JoinDate = DateOnly.Parse("2021-09-13") },
            new() { EmployeeId = "E1021", FullName = "Tom Fernandes",    Email = "tom.fernandes@acme.com",    JobTitle = "Marketing Manager",        Department = "marketing",   ManagerId = "E1020", Location = "Mumbai",    Status = "active",   JoinDate = DateOnly.Parse("2019-06-25") },
            new() { EmployeeId = "E1022", FullName = "Isha Agarwal",     Email = "isha.agarwal@acme.com",     JobTitle = "Content Strategist",       Department = "marketing",   ManagerId = "E1021", Location = "Remote",    Status = "active",   JoinDate = DateOnly.Parse("2024-02-19") },
            new() { EmployeeId = "E1023", FullName = "George Mathew",    Email = "george.mathew@acme.com",    JobTitle = "IT Support Specialist",    Department = "operations",  ManagerId = "E1016", Location = "Bengaluru", Status = "active",   JoinDate = DateOnly.Parse("2022-11-28") }
        );

        db.Tasks.AddRange(
            // ---- canonical (do not change: T-504 is the ONLY blocked task; T-505 the delete target) ----
            new() { TaskId = "T-501", EmployeeId = "E1001", Title = "Implement getEmployeeList tool", Status = "in_progress", Priority = "high",   DueDate = DateOnly.Parse("2026-06-09") },
            new() { TaskId = "T-502", EmployeeId = "E1001", Title = "Write API error-handling notes", Status = "open",        Priority = "medium", DueDate = DateOnly.Parse("2026-06-10") },
            new() { TaskId = "T-503", EmployeeId = "E1002", Title = "Review function-calling loop",    Status = "open",        Priority = "high",   DueDate = DateOnly.Parse("2026-06-08") },
            new() { TaskId = "T-504", EmployeeId = "E1003", Title = "Draft Q3 product roadmap",        Status = "blocked",     Priority = "high",   DueDate = DateOnly.Parse("2026-06-15") },
            new() { TaskId = "T-505", EmployeeId = "E1004", Title = "Close ACME-corp renewal",         Status = "done",        Priority = "medium", DueDate = DateOnly.Parse("2026-06-01") },

            // ---- additional realistic work items ----
            new() { TaskId = "T-506", EmployeeId = "E1002", Title = "Design write-tool confirmation gate",      Status = "in_progress", Priority = "high",   DueDate = DateOnly.Parse("2026-06-12") },
            new() { TaskId = "T-507", EmployeeId = "E1005", Title = "Fix flaky attendance integration test",    Status = "open",        Priority = "medium", DueDate = DateOnly.Parse("2026-06-13") },
            new() { TaskId = "T-508", EmployeeId = "E1006", Title = "Migrate mock datastore to SQLite",         Status = "in_progress", Priority = "high",   DueDate = DateOnly.Parse("2026-06-14") },
            new() { TaskId = "T-509", EmployeeId = "E1007", Title = "Set up CI pipeline for nightly builds",    Status = "open",        Priority = "medium", DueDate = DateOnly.Parse("2026-06-18") },
            new() { TaskId = "T-510", EmployeeId = "E1008", Title = "Write regression tests for delete flow",   Status = "open",        Priority = "high",   DueDate = DateOnly.Parse("2026-06-11") },
            new() { TaskId = "T-511", EmployeeId = "E1009", Title = "Architecture review: tool risk levels",    Status = "open",        Priority = "low",    DueDate = DateOnly.Parse("2026-06-25") },
            new() { TaskId = "T-512", EmployeeId = "E1013", Title = "Prioritize Q3 feature backlog",            Status = "in_progress", Priority = "high",   DueDate = DateOnly.Parse("2026-06-16") },
            new() { TaskId = "T-513", EmployeeId = "E1014", Title = "Redesign attendance marking screen",       Status = "in_progress", Priority = "medium", DueDate = DateOnly.Parse("2026-06-20") },
            new() { TaskId = "T-514", EmployeeId = "E1015", Title = "Usability study on confirmation prompts",  Status = "open",        Priority = "medium", DueDate = DateOnly.Parse("2026-06-22") },
            new() { TaskId = "T-515", EmployeeId = "E1010", Title = "Forecast Q3 sales pipeline",               Status = "open",        Priority = "high",   DueDate = DateOnly.Parse("2026-06-17") },
            new() { TaskId = "T-516", EmployeeId = "E1011", Title = "Renew Globex annual contract",             Status = "in_progress", Priority = "high",   DueDate = DateOnly.Parse("2026-06-19") },
            new() { TaskId = "T-517", EmployeeId = "E1012", Title = "Qualify inbound leads from webinar",       Status = "open",        Priority = "low",    DueDate = DateOnly.Parse("2026-06-12") },
            new() { TaskId = "T-518", EmployeeId = "E1016", Title = "Roll out updated leave policy",            Status = "open",        Priority = "medium", DueDate = DateOnly.Parse("2026-06-21") },
            new() { TaskId = "T-519", EmployeeId = "E1017", Title = "Onboard three new engineering hires",      Status = "in_progress", Priority = "medium", DueDate = DateOnly.Parse("2026-06-15") },
            new() { TaskId = "T-520", EmployeeId = "E1018", Title = "Close May financial books",               Status = "done",        Priority = "high",   DueDate = DateOnly.Parse("2026-06-05") },
            new() { TaskId = "T-521", EmployeeId = "E1019", Title = "Reconcile outstanding vendor invoices",    Status = "open",        Priority = "medium", DueDate = DateOnly.Parse("2026-06-23") },
            new() { TaskId = "T-522", EmployeeId = "E1021", Title = "Launch product newsletter campaign",       Status = "in_progress", Priority = "medium", DueDate = DateOnly.Parse("2026-06-24") },
            new() { TaskId = "T-523", EmployeeId = "E1022", Title = "Draft Q3 content calendar",                Status = "open",        Priority = "low",    DueDate = DateOnly.Parse("2026-06-26") },
            new() { TaskId = "T-524", EmployeeId = "E1023", Title = "Upgrade office VPN appliances",            Status = "open",        Priority = "medium", DueDate = DateOnly.Parse("2026-06-27") },
            new() { TaskId = "T-525", EmployeeId = "E1006", Title = "Document API auth header handling",        Status = "done",        Priority = "low",    DueDate = DateOnly.Parse("2026-06-02") }
        );

        db.Attendance.AddRange(
            // canonical: E1001 absent on 5 Jun — the markAttendance overwrite test depends on this
            new() { AttendanceId = "ATT-20260605-E1001", EmployeeId = "E1001", Date = DateOnly.Parse("2026-06-05"), Status = "absent",  RecordedBy = "system", RecordedAt = DateTimeOffset.Parse("2026-06-05T19:00:00+05:30") },
            // a few realistic recent entries (all past dates; none for E1002 on the run date so self-check-in is a fresh insert)
            new() { AttendanceId = "ATT-20260604-E1001", EmployeeId = "E1001", Date = DateOnly.Parse("2026-06-04"), Status = "present", CheckIn = "09:32", CheckOut = "18:40", RecordedBy = "E1001", RecordedAt = DateTimeOffset.Parse("2026-06-04T18:40:00+05:30") },
            new() { AttendanceId = "ATT-20260605-E1002", EmployeeId = "E1002", Date = DateOnly.Parse("2026-06-05"), Status = "present", CheckIn = "09:05", CheckOut = "18:20", RecordedBy = "E1002", RecordedAt = DateTimeOffset.Parse("2026-06-05T18:20:00+05:30") },
            new() { AttendanceId = "ATT-20260605-E1005", EmployeeId = "E1005", Date = DateOnly.Parse("2026-06-05"), Status = "wfh",     CheckIn = "10:00", CheckOut = "19:10", RecordedBy = "E1005", RecordedAt = DateTimeOffset.Parse("2026-06-05T19:10:00+05:30") },
            new() { AttendanceId = "ATT-20260605-E1008", EmployeeId = "E1008", Date = DateOnly.Parse("2026-06-05"), Status = "half_day", CheckIn = "09:15", CheckOut = "13:30", Note = "Doctor's appointment", RecordedBy = "E1008", RecordedAt = DateTimeOffset.Parse("2026-06-05T13:30:00+05:30") },
            new() { AttendanceId = "ATT-20260606-E1002", EmployeeId = "E1002", Date = DateOnly.Parse("2026-06-06"), Status = "present", CheckIn = "09:12", CheckOut = "17:55", RecordedBy = "E1002", RecordedAt = DateTimeOffset.Parse("2026-06-06T17:55:00+05:30") }
        );

        db.SaveChanges();
    }
}
