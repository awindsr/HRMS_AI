# Day 6 — Screenshots

Drop captured screenshots of the write-tool / confirmation behavior here and link them from [test-results.md](../test-results.md). Suggested captures:

| Filename (suggested) | What to capture |
|---|---|
| `confirm-create-task.png` | `createTask` flow: the agent's summary + the user's "yes" + the created task (W01) |
| `confirm-assign-task.png` | `assignTask` showing the **from → to** summary before reassigning (W02) |
| `mark-attendance-self.png` | Self check-in confirmation and result (W03) |
| `delete-double-confirm.png` | `deleteTask` asking the user to **re-type the task ID**, and refusing a bare "yes" (D01) |
| `empty-result.png` | An empty `getTaskList` answered as "none found" with no invented data (R01) |
| `no-raw-json.png` | A tool result rendered as friendly prose, not JSON (R04) |

Keep file names lowercase-kebab. Reference them in test-results with `![caption](screenshots/<file>.png)`. If filenames contain spaces, URL-encode them as `%20`.
