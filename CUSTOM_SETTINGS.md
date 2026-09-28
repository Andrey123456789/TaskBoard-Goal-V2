# Custom Settings

Approved project-level tunable product limits and defaults belong here.

| Setting | Value | Purpose |
| --- | ---: | --- |
| `UserNameMaxLength` | 200 | Maximum number of characters in a user's `Name`. |
| `UserEmailMaxLength` | 320 | Maximum number of characters in a user's `Email`; bounded so the case-insensitive unique email index can be enforced by SQL Server. |
| `ProjectNameMaxLength` | 200 | Maximum number of characters in a project's `Name`; bounded so the unique project-name index can be enforced by SQL Server. |
| `ProjectDescriptionMaxLength` | 4000 | Maximum number of characters in a project's optional `Description`. |
| `TaskTitleMaxLength` | 200 | Maximum number of characters in a task's `Title`. |
| `TaskDescriptionMaxLength` | 4000 | Maximum number of characters in a task's optional `Description`. |

Input exceeding a maximum length is request-contract-invalid and is rejected
with `400 Bad Request`.

Typical examples include maximum lengths, page-size limits, retention periods,
numeric thresholds, and other stable project-specific tunable values.

Do not store secrets, environment-specific connection details, package versions,
HTTP status codes, ports, migration identifiers, or incidental implementation
constants here.

This file must remain consistent with `SPECIFICATION.md`.

If `CUSTOM_SETTINGS.md` and `SPECIFICATION.md` appear to conflict, do not choose
one silently. Resolve which source must be updated before implementation
continues.
