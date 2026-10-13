# OS-Robot User Manual

OS-Robot is a free, open-source automation server. This manual explains how to install and
configure the server, and how to build, run and monitor jobs with the browser-based Job Editor.

## Contents

1. [Introduction](01-introduction.md) - what OS-Robot is, key concepts, architecture
2. [Installation and first start](02-installation.md)
3. [Server configuration](03-configuration.md) - `appsettings.json` reference
4. [The Job Editor at a glance](04-job-editor.md)
5. [Building jobs](05-building-jobs.md) - folders, objects, connections, saving, manual runs
6. [Settings common to all objects](06-common-settings.md) - general settings and iterations
7. [Connections and execution conditions](07-connections.md)
8. [Dynamic data](08-dynamic-data.md) - passing values between objects, `[CODE]` expressions
9. [Events reference](09-events.md)
10. [Tasks reference](10-tasks.md)
11. [Execution logs and monitoring](11-logs.md)
12. [Security and deployment](12-security.md)
13. [Troubleshooting](13-troubleshooting.md)

Appendices

- [A. Worked examples](appendix-a-examples.md)
- [B. Quick reference](appendix-b-quick-reference.md) - shortcuts and built-in plugins

## Related documents

- [README](../../README.md) - project overview and build instructions
- [SECURITY.md](../../SECURITY.md) - security model and trust boundary
- [DEPLOYMENT.md](../../OSRobot.Server/DEPLOYMENT.md) - exposing the server over HTTPS

## Generating a printable copy

The manual can be converted to Word or PDF with [pandoc](https://pandoc.org/), for example:

```bash
cd docs/user-manual
pandoc README.md 0*.md 1*.md appendix-*.md --toc -o OSRobot_User_Manual.docx
```
