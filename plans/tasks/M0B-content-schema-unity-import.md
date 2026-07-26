# Task M0B - Content Schema and Unity Import Foundation

## Objective

Establish validation-first, schema-authored content ingestion before any production gameplay content exists. Authoring sources are YAML by default and generated artifacts are JSON by default, with parsing restricted to Editor/build tooling.

## In Scope

- Create canonical `GameContent/production`, isolated `GameContent/fixtures`, and generated runtime-data locations.
- Define pure C# DTOs/contracts, stable-ID rules, family discriminators, content-version fields, logical presentation-asset reference IDs, and explicit validators.
- Select, pin, and isolate one maintained .NET YAML parser for Editor/build tooling; never add YAML parsing to `Bloomdrawn.Engine` or runtime player code.
- Implement YAML authoring import and JSON generated-artifact read/write support following DD-13, deterministic registry generation/lookup by stable ID, canonical ordering, and reproducible content hash/version output.
- Add non-production/sample character, card, enemy, and encounter records through the normal import path.
- Make runtime registry construction reject unvalidated production content.

## Non-Goals

- Production characters, cards, enemies, encounters, assets, balance values, or scene-authored gameplay data.
- Gameplay resolution, card operations, combat actor presentation, asset binding, save persistence, or M1 content systems.
- A bespoke general YAML parser or runtime YAML parsing.

## Source Documents To Inspect

- `AGENTS.md` sections 1, 6-8, 10-12.
- `docs/DESIGN.md` section 23, glossary entries for Schema/Content, and approved DD-13/DD-29 text.
- `plans/design-decisions.md` DD-13 and DD-29.
- `plans/implementation_plan.md` sections 0.1, 4, Appendix E, and Task M0B.
- Completed M0A assembly and wrapper contracts.

## Public Contract Changes

- Stable content IDs, family discriminators, version fields, logical presentation-reference ID syntax, validation results, canonical registry ordering, and content hash become M0 contracts.
- YAML is an Editor/build authoring format; JSON is a generated artifact interchange format. Neither format choice permits bypassing validation.
- Runtime registry APIs accept only validated canonical data and report deterministic diagnostics for invalid input.

## Schema or Content Changes

- Introduce versioned foundation schemas for sample character, card, enemy, and encounter families only.
- Add only records explicitly marked sample/non-production and located under fixtures; fixtures must not enter normal player-facing registries, profiles, saves, banners, rewards, or release scans.
- Add logical presentation references as data identifiers only; do not bind Unity assets in this task.

## Implementation Steps

1. Inspect M0A assembly boundaries and create content DTOs in a no-engine-reference location.
2. Select and pin the maintained YAML dependency outside engine/player runtime code; document its package/version and license location in the implementation/worklog.
3. Implement canonical source parsing, explicit validation, stable-ID and reference validation, deterministic ordering, content hashing, and generated JSON serialization.
4. Create sample fixture content for each required family and drive it through the same validation/import/registry pipeline.
5. Add reproducibility and negative-path tests. Generated data is cache/output only and is regenerated from canonical sources during validation.
6. Extend `Tools/validate.ps1` so validation failure prevents registry generation/use.

## Required Tests

- Valid sample content passes validation and produces a registry.
- Duplicate IDs, missing required fields, invalid cross-references, and malformed/invalid logical presentation-reference IDs fail with diagnostics.
- The same canonical content yields the same content hash and registry ordering across repeated runs.
- Production-location input cannot enter a runtime registry without a successful validation result.
- Fixtures remain isolated from production registry selection.

## Validation Commands

```powershell
Tools/validate.ps1
unity test --project-path . --test-mode EditMode
unity command --project-path . bloom.validate-content
```

If `bloom.validate-content` is not available until M0F, run the equivalent project validator directly and record that M0F later verifies the Pipeline command wrapper.

## Exit Criteria

- `Tools/validate.ps1` validates sample/import content.
- Registry generation is deterministic and non-authoritative generated artifacts are reproducible.
- No production content can enter a runtime registry without validation.
- YAML parsing has no engine/runtime player dependency.

## Worklog Entry Requirements

- Record parser package/version, source/generated locations, stable-ID and hash contracts, sample fixture isolation, and all validation results.
- Record content-schema/version impact and explicitly state no production gameplay content or Unity presentation binding was introduced.
