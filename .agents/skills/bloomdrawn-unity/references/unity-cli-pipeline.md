# Unity CLI and Pipeline Reference for Bloomdrawn

Use this as a workflow reference, not as a frozen command specification.

## Current project assumptions

- Unity Editor: 6.5 (`6000.5.x`), exact patch pinned by `ProjectSettings/ProjectVersion.txt`.
- Host: Windows 11 / PowerShell.
- Unity CLI is a standalone experimental tool and may update independently from the Editor.
- Unity Pipeline is the project package that exposes a running Editor to CLI/automation.

## Discovery first

The installed CLI help is authoritative for the installed binary.

Start with:

```powershell
unity --help
unity pipeline list
unity command --help
unity command
```

Useful commands may include `unity status`, structured output flags, project-path targeting, and live evaluation, but confirm the installed form before using them.

Do not copy historical syntax from an old task plan, previous session, blog post, or model memory without checking current help.

## Pipeline connection

A healthy local workflow should be able to:

1. identify the intended project/running Editor;
2. report Pipeline as installed/available;
3. list exposed Editor/project commands;
4. execute a bounded diagnostic command;
5. return useful output and a meaningful failure result.

If more than one Editor instance exists, target the project explicitly. Never send mutating commands to an arbitrary discovered Editor.

## Structured output

For automation, prefer machine-readable CLI output and exit codes where the command supports them. Keep stdout data and diagnostics distinguishable. Do not parse animated progress output.

## Live evaluation

Live C# evaluation is an escape hatch for inspection and bounded diagnostics. Because the experimental command shape has changed during CLI development, discover the local syntax rather than embedding one assumed form in durable automation.

Appropriate uses:

- read Editor/application/project state;
- inspect a known object/component/property;
- confirm a scene/prefab/import condition;
- test a tiny hypothesis before implementing a durable command.

Poor uses:

- hundreds of lines of hidden one-off C#;
- gameplay logic that exists only in an eval expression;
- repeated authoring operations that should be source-controlled tooling;
- mutation with no verification or rollback path.

## Project-owned commands

Repeated automation should become source-controlled Editor/Pipeline tooling when its owning task allows it.

Preferred naming:

```text
bloom.health
bloom.validate-content
bloom.scene-summary
bloom.load-combat-fixture
bloom.reset-combat-fixture
bloom.dump-combat-state
bloom.validate-combat-layout
```

A good command:

- has a narrow purpose;
- validates inputs;
- targets explicit project/scene state;
- emits concise human output and/or structured data;
- fails loudly on invalid preconditions;
- does not encode production content by ID;
- does not mutate authoritative gameplay except when the command's explicit purpose is to drive a test fixture through normal commands.

## Official references

Consult current official documentation when behavior or syntax is uncertain:

- Unity CLI: https://docs.unity.com/en-us/unity-cli/
- Unity CLI reference: https://docs.unity.com/en-us/unity-cli/unity-cli-reference
- Unity CLI release notes: https://docs.unity.com/en-us/unity-cli/release-notes
- Unity Pipeline package: https://docs.unity.com/en-us/unity-production-pipeline/local-tools-cli/unity-pipeline-package
- Unity 6.5 Manual/API: use the current Unity 6.5 documentation appropriate to the installed package/API.
