---
trigger: always_on
---

The agent must always ensure that all generated or modified code follows standard formatting conventions. After making any code changes, the agent is required to run dotnet format to automatically format the codebase and guarantee consistency with the project's coding standards. No code should be considered complete unless it has been successfully formatted using this command.