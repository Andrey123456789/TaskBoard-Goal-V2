# Project Instructions

This repository is a reusable template for ASP.NET Core + Angular applications
based on Clean Architecture.

## Product Specification

`SPECIFICATION.md` is the authoritative source for project-specific functional
requirements and observable product behavior when the file is non-empty.

For a new application or substantial feature:

- read `SPECIFICATION.md` before planning or implementing;
- do not silently weaken, contradict, or materially extend approved behavior;
- do not invent user-visible rules, restrictions, limits, defaults, or workflow
  behavior merely because they seem reasonable.

If an omitted product detail must be decided in order to implement the requested
behavior, and that decision materially affects the product, propose the smallest
reasonable option and ask for approval.

Reasonable internal implementation details that do not change observable product
behavior do not require approval.

When a new behavioral rule is approved, keep the durable source of truth
synchronized by updating `SPECIFICATION.md` when appropriate.

An empty `SPECIFICATION.md` in the reusable template is intentional.

## Custom Settings

`CUSTOM_SETTINGS.md` contains approved project-level tunable product limits and
defaults.

`SPECIFICATION.md` defines behavioral intent, while `CUSTOM_SETTINGS.md` owns
approved concrete values for tunable settings.

The two files must not contradict each other.

If they appear to conflict, stop and ask which source should be corrected rather
than silently choosing one.

Typical examples include:

- maximum lengths;
- page-size defaults and maximums;
- retention periods;
- numeric thresholds;
- other stable project-specific tunable values.

Do not silently invent a value that materially changes accepted input or
observable behavior when the value is not already defined by the specification,
existing approved behavior, or `CUSTOM_SETTINGS.md`.

When a new value is needed:

1. propose the value and explain why it is needed;
2. ask for approval;
3. after approval, record it in `CUSTOM_SETTINGS.md`;
4. keep code, validation, tests, configuration, frontend behavior, and
   documentation synchronized.

When `CUSTOM_SETTINGS.md` changes, use the `custom-settings` skill.

Do not use `CUSTOM_SETTINGS.md` for secrets, environment-specific connection
details, package versions, HTTP status codes, ports, migration identifiers, or
incidental implementation constants.

## Core Defaults

- Use Clean Architecture as the default architectural style.
- Prefer simple, maintainable solutions over unnecessary abstractions.
- Keep architectural complexity proportional to the problem being solved.
- Do not introduce frameworks, libraries, infrastructure, or architectural
  patterns without a concrete current need.
- Do not introduce a consequential architectural pattern or infrastructure
  decision without explicit approval unless it is already required by the
  specification or established project structure.
- Keep changes focused on the requested scope.
- Preserve existing behavior unless the task or approved specification requires
  changing it.

Detailed architecture policy belongs to `.claude/rules/architecture.md`.

## Development Workflow

- Inspect the relevant existing code before making non-trivial changes.
- Follow applicable rules from `.claude/rules/`.
- Use relevant skills from `.claude/skills/` for specialized procedures.
- Before adding or updating any NuGet or npm dependency, follow
  `.claude/rules/dependencies.md`.
- Do not silently introduce new product behavior just because the specification
  did not explicitly forbid it.
- For substantial feature work or a newly implemented application, apply the
  `code-review` workflow before the final `verify` workflow.
- Before declaring implementation work complete, apply the `verify` workflow.

If a consequential requirement, product rule, or architectural decision is
genuinely ambiguous, ask before implementing it.

## Git

Do not commit, push, merge, rebase, reset, force-push, delete branches, or
perform other repository-changing Git operations unless explicitly requested.

Keep unrelated changes out of the current branch.

Parallel implementation work must use separate branches/worktrees.