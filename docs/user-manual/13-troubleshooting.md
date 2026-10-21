# 13. Troubleshooting

| Problem | Likely cause and solution |
|---|---|
| Changes saved but jobs behave as before | Saving does not reload the engine. Click **Reload server configuration**. |
| *"Cannot reload server's config because there are running jobs"* | Wait for running jobs to finish and retry. |
| A task never runs after its predecessor | Check that the connection and the task are enabled, that the connection has at least one execute condition, and that no don't-execute condition matches. Check the log of the predecessor: with *Object executes* the target runs only if the predecessor succeeded. |
| A task runs many more times than expected | Its Iterations setting iterates over a recordset, and/or the incoming connection runs *Once for each result*. Use *Once, after all iterations* or *Iterate this exact number of times*. |
| Save warns that dynamic data references will fail | The referenced object does not run before the task in that chain, the Id is wrong, or the field does not exist. Use the dynamic data browser to pick the reference. |
| *"C# scripting ([CODE] expressions) is disabled"* | `ScriptingEnabled` is `false` on this server. Rewrite the value without `[CODE]` or ask the administrator. |
| Run program fails with *"exited with code N"* | The program returned a non-zero exit code. Fix the program call or turn off *Fail if the program exits with a non-zero code*. |
| Account locked out | Too many failed logins. Wait `LockoutDurationMinutes` (15 by default). |
| *"Connection to server lost"* | The server is stopped or unreachable. The editor retries automatically. |
| Editor not reachable from another PC | The server listens on localhost only by default. Configure HTTPS remote access (see [Security and deployment](12-security.md)). |
| CPU event does not work on Linux/macOS | The CPU event is Windows-only; hover the computer icon on a card to see supported platforms. |
| Old execution logs are missing | They were cleaned up after `CleanUpLogsOlderThanHours`. Increase the value. |

---

Previous: [12. Security and deployment](12-security.md) · Next: [Appendix A. Worked examples](appendix-a-examples.md)
