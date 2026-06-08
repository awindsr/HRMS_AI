using OpenAI.Chat;

namespace HrmsAgent.Llm;

/// <summary>
/// The JSON-Schema contracts the model reads to decide which tool to call and how to fill
/// its arguments. Descriptions say *when* to use each tool, not just what it does.
/// </summary>
public static class ToolSchemas
{
    public static readonly ChatTool GetEmployeeList = ChatTool.CreateFunctionTool(
        functionName: "getEmployeeList",
        functionDescription: "List employees in the company directory, optionally filtered by " +
            "department or employment status. Use when the user asks who is in a team, wants a " +
            "roster/headcount, or asks 'who works in <department>'. Read-only.",
        functionParameters: BinaryData.FromString("""
        {
          "type": "object",
          "properties": {
            "department": {
              "type": "string",
              "description": "Filter by department, e.g. 'engineering', 'sales', 'product'. Omit for all departments."
            },
            "status": {
              "type": "string",
              "enum": ["active", "on_leave", "inactive"],
              "description": "Filter by employment status. Omit for all."
            },
            "limit": {
              "type": "integer",
              "description": "Max records to return (1-100). Defaults to 20.",
              "minimum": 1, "maximum": 100
            }
          },
          "required": []
        }
        """));

    public static readonly ChatTool GetEmployeeDetails = ChatTool.CreateFunctionTool(
        functionName: "getEmployeeDetails",
        functionDescription: "Fetch the full profile of one employee by ID: name, title, department, " +
            "manager, location, status, join date. Use when the user asks about a specific person's " +
            "details. Employees may only look up their own profile. Read-only.",
        functionParameters: BinaryData.FromString("""
        {
          "type": "object",
          "properties": {
            "employeeId": {
              "type": "string",
              "description": "The employee ID to look up, e.g. 'E1001'."
            }
          },
          "required": ["employeeId"]
        }
        """));

    public static readonly ChatTool GetTaskDetails = ChatTool.CreateFunctionTool(
        functionName: "getTaskDetails",
        functionDescription: "Fetch ONE task by its ID: title, current assignee, status, priority, due date. " +
            "Use this to look a task up YOURSELF before assigning or deleting it — never ask the user for a " +
            "task's current assignee or details you can fetch with this tool. Read-only.",
        functionParameters: BinaryData.FromString("""
        {
          "type": "object",
          "properties": {
            "taskId": {
              "type": "string",
              "description": "The task ID to look up, e.g. 'T-504'."
            }
          },
          "required": ["taskId"]
        }
        """));

    public static readonly ChatTool GetTaskList = ChatTool.CreateFunctionTool(
        functionName: "getTaskList",
        functionDescription: "List work tasks, optionally filtered by employee and/or status. Use when " +
            "the user asks what they (or a named employee) are working on, what is open, in progress, " +
            "blocked, or done. Read-only.",
        functionParameters: BinaryData.FromString("""
        {
          "type": "object",
          "properties": {
            "employeeId": {
              "type": "string",
              "description": "Filter tasks to a single employee by ID, e.g. 'E1001'. Omit for all."
            },
            "status": {
              "type": "string",
              "enum": ["open", "in_progress", "done", "blocked"],
              "description": "Filter by task status. Omit for all."
            }
          },
          "required": []
        }
        """));

    // ========================================================================
    // Day 6 — WRITE tools. Each description states the action is a write and that
    // a summary + explicit confirmation must precede the call. The `confirmed`
    // flag is the model's attestation that it has done so; the binding layer
    // refuses the write when it is false (see HrmsTools / confirmation-flow.md).
    // ========================================================================

    public static readonly ChatTool CreateTask = ChatTool.CreateFunctionTool(
        functionName: "createTask",
        functionDescription: "Create a new work task in the task tracker, optionally assigned to an employee. " +
            "WRITE ACTION: before calling, present a plain-language summary (title, who it is assigned to, " +
            "priority, due date) and obtain the user's explicit 'yes'. Only then call this with confirmed=true. " +
            "Never call it speculatively or just to acknowledge a request.",
        functionParameters: BinaryData.FromString("""
        {
          "type": "object",
          "properties": {
            "title": {
              "type": "string",
              "description": "Short, action-oriented task title. Required."
            },
            "description": {
              "type": "string",
              "description": "Optional longer detail about the task."
            },
            "assigneeId": {
              "type": "string",
              "description": "Employee ID to assign the task to, e.g. 'E1001'. Omit to leave it unassigned. Assigning to someone notifies them."
            },
            "priority": {
              "type": "string",
              "enum": ["low", "medium", "high"],
              "description": "Task priority. Defaults to medium."
            },
            "dueDate": {
              "type": "string",
              "format": "date",
              "description": "Due date YYYY-MM-DD, today or later. Resolve relative dates ('next Friday') to absolute first."
            },
            "confirmed": {
              "type": "boolean",
              "description": "Set true ONLY after you have summarized the task and the user has explicitly confirmed. If false, the task is NOT created."
            }
          },
          "required": ["title", "confirmed"]
        }
        """));

    public static readonly ChatTool AssignTask = ChatTool.CreateFunctionTool(
        functionName: "assignTask",
        functionDescription: "Assign or re-assign an existing task to an employee. WRITE ACTION that notifies the " +
            "new assignee (and the previous one on re-assignment). Before calling, show the task, its current " +
            "assignee, and the new assignee, and get an explicit 'yes'. Then call with confirmed=true.",
        functionParameters: BinaryData.FromString("""
        {
          "type": "object",
          "properties": {
            "taskId": {
              "type": "string",
              "description": "ID of the task to assign, e.g. 'T-501'."
            },
            "assigneeId": {
              "type": "string",
              "description": "Employee ID of the new assignee, e.g. 'E1002'."
            },
            "confirmed": {
              "type": "boolean",
              "description": "Set true ONLY after summarizing the reassignment and receiving explicit confirmation."
            }
          },
          "required": ["taskId", "assigneeId", "confirmed"]
        }
        """));

    public static readonly ChatTool MarkAttendance = ChatTool.CreateFunctionTool(
        functionName: "markAttendance",
        functionDescription: "Record an attendance entry for an employee on a date (present/absent/wfh/leave/half_day). " +
            "WRITE ACTION that can affect payroll. Employees may mark only their OWN attendance for TODAY; marking " +
            "another employee or a past date is an HR regularization. Confirm the employee, date, and status before " +
            "calling. For corrections, read the existing record first so the user sees the before/after.",
        functionParameters: BinaryData.FromString("""
        {
          "type": "object",
          "properties": {
            "employeeId": {
              "type": "string",
              "description": "Employee whose attendance is recorded, e.g. 'E1002'. For self check-in, the signed-in user's own ID."
            },
            "date": {
              "type": "string",
              "format": "date",
              "description": "Date of the entry YYYY-MM-DD. Defaults to today. A past date is a regularization (HR); a future date is rejected."
            },
            "status": {
              "type": "string",
              "enum": ["present", "absent", "wfh", "leave", "half_day"],
              "description": "Attendance status for the day."
            },
            "checkIn": {
              "type": "string",
              "description": "Optional check-in time HH:mm (24h)."
            },
            "checkOut": {
              "type": "string",
              "description": "Optional check-out time HH:mm (24h). Must be after checkIn."
            },
            "note": {
              "type": "string",
              "description": "Optional reason / regularization note."
            },
            "confirmed": {
              "type": "boolean",
              "description": "Set true ONLY after summarizing the entry and receiving explicit confirmation."
            }
          },
          "required": ["employeeId", "status", "confirmed"]
        }
        """));

    public static readonly ChatTool DeleteTask = ChatTool.CreateFunctionTool(
        functionName: "deleteTask",
        functionDescription: "PERMANENTLY delete a task. DESTRUCTIVE and IRREVERSIBLE; HR/Admin only. You MUST first " +
            "fetch and show the task's details, state plainly that deletion cannot be undone, and ask the user to " +
            "re-type the exact task ID to confirm. Pass that re-typed ID as confirmationToken — it must equal taskId, " +
            "or the deletion is refused. Never delete more than one task per call.",
        functionParameters: BinaryData.FromString("""
        {
          "type": "object",
          "properties": {
            "taskId": {
              "type": "string",
              "description": "ID of the single task to delete, e.g. 'T-501'."
            },
            "confirmationToken": {
              "type": "string",
              "description": "The task ID the user re-typed to confirm. Must exactly equal taskId. A generic 'yes' is NOT sufficient."
            }
          },
          "required": ["taskId", "confirmationToken"]
        }
        """));

    public static IReadOnlyList<ChatTool> All => new[]
    {
        // reads (Day 5 + getTaskDetails)
        GetEmployeeList, GetEmployeeDetails, GetTaskList, GetTaskDetails,
        // writes (Day 6)
        CreateTask, AssignTask, MarkAttendance, DeleteTask
    };
}
