namespace TeamAI.Models.Hrms;

/// <summary>
/// Subset of HRMS <c>EmployeeDetailsModel</c> from GET /m/api/Employee/GetEmployeeDetails, used to
/// enrich the signed-in user's display profile (name, email, designation, department, photo).
/// Deserialized case-insensitively. Only display-friendly fields are mapped; the rest are ignored.
/// </summary>
public sealed record EmployeeProfileResponse(
    int EmployeeId,
    string? EmployeeCode,
    string? FullName,
    string? ProfilePhoto,
    EmployeeOfficialContact? OfficalContactDetails,
    EmployeePersonalContact? PersonalContactDetails,
    EmploymentDetails? EmploymentDetails);

public sealed record EmployeeOfficialContact(string? OfficalEmailId, string? MobileNumber);

public sealed record EmployeePersonalContact(string? PersonalEmailId, string? MobileNumber);

public sealed record EmploymentDetails(
    string? Designation,
    string? Department,
    string? BusinessUnit,
    string? Company,
    string? ShiftName,
    DateTime? JoiningDate);
