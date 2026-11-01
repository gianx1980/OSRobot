# 13. Troubleshooting

| Problem | Likely cause and solution |
|---|---|
| Changes saved but jobs behave as before | Saving does not reload the engine. Click **Reload server configuration**. |
| *"Cannot reload server's config because there are running jobs"* | Wait for running jobs to finish and retry. |
| A task never runs after its predecessor | Check that the connection and the task are enabled, that the connection has at least one execute condition, and that no don't-execute condition matches. Check the log of the predecessor: with *Object executes* the target runs only if the predecessor succeeded. |
| A task runs many more times than expected | Its Iterations setting iterates over a recordset, and/or the incoming connection runs *Once for each result*. Use *Once, after all iterations* or *Iterate this exact number of times*. |
| Save warns that dynamic data references will fail | The referenced object does not run before the task in that chain, the Id is wrong, or the field does not exist. Use the dynamic data browser to pick the reference. |
| *"Variable 'X' is not defined"* / *"Secret 'X' is not defined"* | The name is misspelled or was deleted. Check **Variables and secrets** (names ignore case, but `{var[...]}` and `{secret[...]}` are separate). |
| *"Secret 'X' cannot be decrypted"*, or a secret marked *Cannot be decrypted* | The keys folder (`Data\keys`) is missing or doesn't match `config.db`, the certificate changed, or (Windows without a certificate) the service now runs under another account or on another computer. Restore the keys, or enter the secret's value again. |
| The server doesn't start: *"Data Protection: cannot open the certificate"* | `AppSettings:DataProtection:CertificatePath` is set but the `.pfx` password is wrong or missing. See [Server configuration](03-configuration.md#35-secrets-encryption-appsettingsdataprotection). |
| A webhook call gets `404` | The path is wrong, the hook or its folder is disabled, the jobs weren't reloaded after saving, or the token or signature is wrong (see `ExecLogs\audit-*.log`). |
| *"C# scripting ([CODE] expressions) is disabled"* | `ScriptingEnabled` is `false` on this server. Rewrite the value without `[CODE]` or ask the administrator. |
| Run program fails with *"exited with code N"* | The program returned a non-zero exit code. Fix the program call or turn off *Fail if the program exits with a non-zero code*. |
| Account locked out | Too many failed logins. Wait `LockoutDurationMinutes` (15 by default). |
| *"Connection to server lost"* | The server is stopped or unreachable. The editor retries automatically. |
| Editor not reachable from another PC | The server listens on localhost only by default. Configure HTTPS remote access (see [Security and deployment](12-security.md)). |
| CPU event does not work on macOS | The CPU event supports Windows and Linux; hover the computer icon on a card to see supported platforms. |
| CPU event fires later than expected after a restart | The first check only takes the starting point: usage is measured from the second one. |
| Old execution logs are missing | They were cleaned up after `CleanUpLogsOlderThanHours`. Increase the value. |

---

Previous: [12. Security and deployment](12-security.md) · Next: [Appendix A. Worked examples](appendix-a-examples.md)
