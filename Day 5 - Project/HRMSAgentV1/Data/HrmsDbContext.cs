using HrmsAgent.Models;
using Microsoft.EntityFrameworkCore;

namespace HrmsAgent.Data;

/// <summary>
/// EF Core context backing the mock HRMS API with SQLite. Replaces the old in-memory
/// <c>HrmsData</c> static lists: the same three entities (employees, tasks, attendance) now
/// live in a real database file and are seeded at startup by <see cref="DbSeeder"/>.
/// </summary>
public sealed class HrmsDbContext : DbContext
{
    public HrmsDbContext(DbContextOptions<HrmsDbContext> options) : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeTask> Tasks => Set<EmployeeTask>();
    public DbSet<AttendanceRecord> Attendance => Set<AttendanceRecord>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // The models are immutable records with JSON attributes; their string IDs are the keys.
        b.Entity<Employee>().HasKey(e => e.EmployeeId);
        b.Entity<EmployeeTask>().HasKey(t => t.TaskId);
        b.Entity<AttendanceRecord>().HasKey(a => a.AttendanceId);
    }
}
