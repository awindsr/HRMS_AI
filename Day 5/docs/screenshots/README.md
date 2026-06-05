# Day 5 — Screenshots

Drop your captured screenshots here and link them from [test-results.md](../test-results.md). Suggested captures:

| Filename (suggested) | What to capture |
|---|---|
| `test-runner.png` | `dotnet run` console output showing all three tools returning data + the 404/missing-arg cases |
| `api-log.png` | The `[API]` log lines in the console (or `logs/api-calls.log` open in an editor) |
| `chat-employee-list.png` | `--chat` session: "Who works in engineering?" → model calls `getEmployeeList` |
| `chat-task-list.png` | `--chat` session: "What is E1001 working on?" → model calls `getTaskList` |
| `chat-no-tool.png` | `--chat` session: "What's my leave balance?" → model honestly declines (no fabrication) |
| `error-timeout.png` | A forced `timeout` / `500` run showing the error reported cleanly |

Keep file names lowercase-kebab. Reference them in test-results with `![caption](screenshots/<file>.png)`.
