---
name: custom-settings
description: >
  Keep approved project-level tunable limits/defaults synchronized when
  CUSTOM_SETTINGS.md changes. Use when adding, changing, removing, reviewing, or
  applying settings from CUSTOM_SETTINGS.md.
---

# Custom Settings Synchronization

## Purpose

`CUSTOM_SETTINGS.md` is the durable source of truth for approved project-level
tunable product limits and defaults.

It is not a runtime secret/configuration store.

`SPECIFICATION.md` and `CUSTOM_SETTINGS.md` must remain consistent.

`SPECIFICATION.md` owns behavioral intent. `CUSTOM_SETTINGS.md` owns approved
concrete values for tunable settings.

If they conflict, do not silently choose one source. Resolve the inconsistency
before changing implementation.

## What Belongs Here

Typical examples:

- maximum title/description lengths;
- page-size defaults and maximums;
- retention periods;
- product-level numeric thresholds;
- other stable tunable limits explicitly approved for the project.

Do not put here:

- secrets or credentials;
- environment-specific connection details;
- package versions;
- HTTP status codes;
- ports;
- migration identifiers;
- incidental implementation constants;
- framework defaults the project has not intentionally overridden.

## When Adding a New Setting

If a setting would materially change accepted input or observable behavior and
the value is not already defined by the specification or existing approved
behavior, ask for approval before introducing it.

After approval:

1. record the setting in `CUSTOM_SETTINGS.md`;
2. use a stable descriptive setting name;
3. update the owning code/validation/configuration;
4. update tests;
5. update user/developer documentation when the value is externally relevant.

Avoid duplicating the same literal across many layers when a normal single source
in code/configuration is appropriate.

## When CUSTOM_SETTINGS.md Changes

Search the repository for:

- the old value;
- the setting name;
- validators and DTO constraints;
- Domain/Application checks;
- database mapping constraints;
- frontend validation/UI hints;
- tests and test data;
- documentation and examples;
- configuration that mirrors the setting.

Determine which occurrences express the setting and which are unrelated numeric
coincidences.

Update only affected behavior.

## Database Impact

If a changed setting affects schema constraints, indexes, column lengths, or
other persistence metadata:

- update EF Core mapping;
- create a new migration when the changed setting requires a schema change;
- regenerate or edit an uncommitted migration only when that matches the
  project's normal migration workflow;
- verify Development seeding remains valid;
- add or update persistence/integration tests when provider behavior matters.

Do not alter existing migrations casually when the project requires a new
migration.

## Frontend / Backend Consistency

When both frontend and backend expose the same product constraint:

- backend validation remains authoritative;
- keep frontend validation/messages synchronized for user experience;
- do not rely on frontend validation for security or data integrity.

## Verification

Before completion:

1. search for stale references to the old value;
2. build affected backend/frontend projects;
3. run relevant tests;
4. verify persistence changes when applicable;
5. check `SPECIFICATION.md` for conflicts;
6. run the normal `verify` workflow.

Do not declare the setting synchronized merely because one constant was changed.