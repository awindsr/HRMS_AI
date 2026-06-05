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

    public static IReadOnlyList<ChatTool> All => new[] { GetEmployeeList, GetEmployeeDetails, GetTaskList };
}
