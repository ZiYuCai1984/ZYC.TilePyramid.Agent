# Agent Guide for ZYC.TilePyramid

This file defines rules for AI and code assistants working in this repository.

## Scope

- Apply these rules throughout the repository.
- Honor more specific rules in nested `AGENTS.md` files and explicit user overrides.

## Review Focus

- Prioritize API design, naming consistency, visibility, dependency boundaries, and correctness.
- Prefer minimal changes aligned with the existing architecture and coding style.

## Coding Restrictions

The following rules apply unless necessary to complete the task or explicitly requested by the user:

- Do not write or run tests.
- Do not use `sealed`.
- Do not add `ToolTip` / `Tooltip`.
- Do not add `AutomationProperties` or its attached properties.
- Do not implement localization or add localization resources.
- Prefer private get-only properties over `readonly` fields for stored values.
- Do not add calls to `ArgumentNullException.ThrowIfNull`.
- Do not use primary constructor syntax.

## Tooling and Build Boundaries

- Do not suggest enabling or changing analyzers, StyleCop, FXCop, Roslyn rules, or linting tools unless explicitly requested.
- Treat `RunAnalyzersDuringBuild` as given; do not discuss or recommend changing it unless explicitly requested.
- Do not propose changes to build pipelines, CI, or solution-wide props/targets unless explicitly requested.
- Do not introduce large dependencies or new frameworks unless explicitly requested.
- Do not run local builds unless explicitly requested.

## Technology Boundaries

- Keep Abstractions projects targeting `net10.0` with `UseWPF=false`.
- References to `System.Windows.Input.ICommand` are acceptable in Abstractions projects and do not justify changing the target framework or moving the interface.

## Testing Architecture

- Automated tests are maintained in a private external repository that references this repository as a Git submodule.
- The absence of test projects in this repository is intentional and must not be reported as a testing gap.
- Do not add test projects to this repository unless the user explicitly asks.
- When reporting verification, distinguish between external tests not being run for the current task and automated tests not existing.

## Naming and Conventions

- Use PascalCase for interfaces, methods, events, and DTOs.
- Prefix interfaces with `I`.
- Suffix asynchronous methods returning `Task` or `ValueTask` with `Async`.
- Use file-scoped namespaces and match the folder structure where practical.
- Require XML documentation for public APIs in `*.Abstractions` projects; documentation is optional elsewhere.
- Create new files using CRLF line endings and UTF-8 with BOM encoding.

## Change Policy

- Do not proactively filter, mask, or redact sensitive parameters unless explicitly requested.
- When renaming types, update file names and all references across the solution.
- Preserve existing target frameworks, output paths, and packaging structure.
- Do not proactively add compatibility handling.

## Verification and Response Style

- Accurately report what verification was performed.
- Keep responses concise, specific, and action-oriented.
- Use Mermaid `graph TD` for requested dependency diagrams.
- Use absolute paths when referencing files in responses.
