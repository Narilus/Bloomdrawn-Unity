# Bloomdrawn — Implementation Plan (v2)

**Status:** Draft for owner review
**Authority:** Companion to `docs/DESIGN.md`. Where this plan and `docs/DESIGN.md` disagree, `docs/DESIGN.md` wins and this plan is revised before work continues.
**Supersedes:** the prior implementation plan in its entirety. This document is a ground-up rewrite, not a patch. It does not inherit the prior plan's task decomposition, its milestone granularity, or its process assumptions except where those are explicitly re-stated here.

---

## PART I — GOVERNANCE AND CONTRACTS

### 0. Purpose, Authority, and Source of Truth

#### 0.1 What this document is

This is Bloomdrawn's sequencing, boundary, and governance contract. It answers four questions:

1. **In what order do we build**, and why that order?
2. **What may not change** without an explicit decision?
3. **How do we know a step is done?**
4. **How do we grow the game later** without rewriting the engine?

It is deliberately not a design document. Gameplay rules, content definitions, economy, UX commitments, and the ethical gacha contract live in `docs/DESIGN.md`. This plan never invents a rule. When implementation exposes a design flaw, the fix is a revision to `docs/DESIGN.md` first, then this plan, then code — never the reverse.

This plan is also not a task list. Task plans live under `plans/tasks/` and are written against this plan as needed. This document defines the *shape* a task must take, the *gates* it must pass, and the *order* in which milestones may be entered. It intentionally specifies less than its predecessor: it fixes invariants and acceptance criteria, and leaves implementation mechanism to the Builder, because over-specifying mechanism is how this project previously stalled.

#### 0.2 Source-of-truth hierarchy

In descending order of authority:

1. `docs/DESIGN.md`
2. `plans/design-decisions.md` — approved decision records that explicitly amend `docs/DESIGN.md`
3. This plan (`plans/implementation_plan.md`)
4. Active task plans under `plans/tasks/`
5. Automated tests that correctly encode approved rules
6. Engine implementation
7. Application/session adapters and persistence
8. Unity scenes, UI, animation, VFX, and presentation

A lower layer may reveal ambiguity in a higher layer; it may never silently override it. If two layers conflict, work on the affected task stops, the conflict is raised, and the higher layer is corrected before anything below it changes.

Two governance files sit beside this hierarchy without being in it:

- `.agents/skills/**` teaches agents how to operate the Unity toolchain. It must never restate gameplay rules or milestone acceptance criteria. Those live here and in `docs/DESIGN.md`.
- `AGENTS.md` and the Opencode agent definitions define role permissions. They govern *who may change what*, not what the game is.

#### 0.3 Planning as argument

Every sequencing choice in this plan carries a reason. If a milestone order, a gate, or a fixture policy seems arbitrary, that is a defect in this document, not a feature. The reason for each structural decision is stated where the decision is made, so a future planner can tell whether the reason still holds before changing the decision.

The three load-bearing arguments of this plan:

- **Visibility before breadth.** The player-facing game must become playable and feelable as early as possible, because a game that cannot be played cannot be playtested, and a game that cannot be playtested cannot be corrected. Milestones are ordered to reach the first winnable run quickly, then broaden.
- **Content exists before it is called.** Nothing may reference a content definition by stable ID until that definition's schema is locked and the definition itself exists in a validated registry. This is enforced per content family in §3.
- **Growth is routine, not exceptional.** Adding characters, cards, enemies, weapons, and gear after launch is a planned, lightly-gated activity (§5), not a heroic engineering event. The architecture is built to make expansion a data change.

---

### 1. Change Classification and Change Control

Every change to the project is one of four classes. The class determines who must approve it and which documents must move with it.

#### 1.1 Change classes

| Class | Definition | Approval | Documents touched |
|---|---|---|---|
| **Editorial** | Wording, typo, heading, or link correction with no semantic effect | Worklog note only | None beyond the edited file |
| **Plan-level** | Task order, task split, validation command, milestone entry/exit criteria, fixture policy mechanics | Planner proposes, owner approves | This plan or the active task plan |
| **Design-level** | Rules, timing, formulas, rewards, ownership, economy, UX commitments, content warnings, progression | Owner approves via decision record | `docs/DESIGN.md` first, then decision record, then this plan |
| **Save-affecting** | Authoritative state shape, content IDs, RNG state, profile inventory, persisted schema | Planner assesses, owner approves | This plan + migration/invalidation assessment |

A change may belong to more than one class. When it does, the strictest class governs.

#### 1.2 The change workflow

1. The change is classified.
2. If design-level, a decision record is opened in `plans/design-decisions.md` before dependent work begins, and `docs/DESIGN.md` is revised once approved.
3. This plan or the affected task plan is revised to reflect the change.
4. Tests that encode the old behaviour are updated in the same change, never after.
5. Implementation changes only after the documents above it are consistent.
6. A worklog entry records the change, its class, and its approval.

#### 1.3 What may not change by accident

The following are invariant unless a design-level change explicitly revises them:

- The deterministic engine boundary (§2.2).
- The no-hardcoding rule (§2.4).
- The fixture/production separation (§4).
- The ethical gacha contract (no monetisation, direct pulls, visible odds).
- The four-Domain structure (the Bloom is not a fifth Domain).
- The node-primary, edge-supported Labyrinth topology (DD-25).

These are the project's load-bearing walls. Everything else is negotiable through the correct class of change.

---

### 2. Core Implementation Commitments

These commitments are inherited from the approved architecture and re-stated here as the contract every milestone builds on. They are rewritten, not ported: where the prior plan asserted these as a list, this plan states *why* each exists so that a future agent cannot satisfy the letter while breaking the spirit.

#### 2.1 The determinism contract

Bloomdrawn is a deterministic game. Given the same engine version, content version, initial state, named RNG substream states, and ordered commands, the engine produces the same state transitions and ordered events.

This is not a nice-to-have. It is what makes the following possible:

- golden replay tests that catch regressions without a running scene;
- save/reload that restores an exact run;
- an auditable gacha, where every pull can be reproduced;
- bug reproduction that does not depend on animation timing.

Determinism is therefore protected at every layer, not only in the engine. Presentation may be non-deterministic for cosmetics (idle sway, particle variation), but any cosmetic randomness uses a separate, non-authoritative stream and never perturbs shuffle, map, targeting, reward, shop, gacha, or equipment streams.

#### 2.2 Engine purity

`Bloomdrawn.Engine` is a dedicated Assembly Definition with **No Engine References** enabled. It cannot reference `UnityEngine`, `UnityEditor`, scenes, `MonoBehaviour`, `ScriptableObject`, frame time, `UnityEngine.Random`, input, storage APIs, or presentation assets.

The engine is pure C# operating on value states. It receives a command and a state, and returns either an accepted next-state-plus-events or a rejection. It does not know it is in Unity. This is what allows it to be tested headlessly, replayed, and kept free of the scene-coupling that made earlier web-stack ports fragile.

The engine may call into pure content contracts and deterministic utilities. It may not call into anything that reads the running world.

#### 2.3 Schema-driven content

Authored gameplay content is data-first and schema-validated. Characters, cards, generated cards, enemies, encounters, statuses, keepsakes, Symptoms, Curses, Trials, rewards, banners, rarity tables, weapons, gear, Profile Shop offers, map motifs, growth tables, and tutorials live in version-controlled files.

Format policy (DD-13):

- Hand-authored production content uses **YAML** by default.
- Generated or machine-written artifacts use **JSON** by default.
- Each content family has exactly one canonical source format at a time.

YAML is an authoring format, not a runtime dependency. Editor/build tooling parses and validates canonical sources and emits a deterministic generated registry. The runtime consumes the validated registry, never raw YAML. A single maintained .NET YAML parser lives outside `Bloomdrawn.Engine` (DD-30 governs the save-facing JSON serializer; the YAML parser is a separate, explicitly pinned dependency).

#### 2.4 No production hardcoding

Production gameplay content must not appear in engine or UI code. Specifically, reviews reject:

- `MonoBehaviour`, presenter, scene, prefab, or UI conditionals keyed to a specific production character/card/enemy ID;
- engine switch statements that special-case a named character rather than dispatching over a generic operation kind;
- hand-written encounter construction outside validated content;
- hardcoded gacha pools, Trial reward tables, Profile Shop stock, weapon or gear definitions, or progression tables;
- GameObject names, prefab names, scene paths, or Animator state names treated as gameplay IDs.

Switches over *operation kinds, target kinds, status classes, node types, and presentation-token kinds* are legal, because they dispatch over reusable systems, not over specific content. The test is: does adding a new piece of content require touching this code? If yes, the code is wrong.

#### 2.5 Fixture discipline

Non-production fixtures prove systems before production content exists. They are schema-validated data in an explicit non-production namespace, loaded only from isolated test/development registries. They are never code, never hardcoded values, and never leak into production state. The full policy is §4.

#### 2.6 Generated art is always permitted

AI-generated art may be prototype, production, or release-quality (DD-29). Generation method is provenance, not placeholder status. Generated art is judged by the same quality, readability, continuity, and technical-import criteria as hand-authored art, and passes through the same human review before release lock. No gate rejects an asset solely because it was generated.

#### 2.7 Unity baseline

- Unity **6.5 (`6000.5.x`)**, exact patch pinned in `ProjectSettings/ProjectVersion.txt`. Windows is the primary development and validation target; release platforms are gated by DD-14.
- **URP 2D** for rendering. **uGUI + TextMesh Pro** for runtime UI. **Unity Input System** for input. UI Toolkit is reserved for Editor tooling unless a later decision says otherwise.
- **Independent actor views** for every party member and targetable enemy; no composite party/enemy render in the gameplay actor layer.
- **Unity CLI + `com.unity.pipeline`** are development-only automation. The project must build and test without them. Installed `--help` output is authoritative for CLI syntax, because the CLI is experimental.

**Agent Editor access:** any Unity Editor an agent controls through Pipeline, live evaluation, Play Mode interaction, or runtime inspection must be launched with the `-automated` flag. An Editor without `-automated` is unreachable to agents. The launcher scripts (`Tools/get-unity-editor-state.ps1`, `Tools/open-automated-editor.ps1`) are the safe path; agents never kill or restart a user-owned Editor without explicit authorization.

#### 2.8 Permanent presentation infrastructure from M1

The card hand, drag/Play-Area interaction, independent actor model, battlefield composition, and event-to-presentation sequence are built as **permanent** infrastructure in M1, not throwaway scaffolding. M9 and M10 polish and close these systems; they do not replace them. This commitment exists because card feel is the game's primary tactile surface and cannot be validated late.

The consequence: M1's exit is not "the fixture combat runs." It is "the fixture combat runs through the real presentation path, and that path feels and behaves correctly under repeated human interaction." Feel is an M1 acceptance criterion, not an M9 one.

---

### 3. Content Readiness Chain

This section encodes the rule that **content must exist before it is called**. It applies to every content family and every milestone.

#### 3.1 The chain

A content family advances through six stages, strictly in order. Nothing downstream of a stage may act until that stage is complete.

1. **Schema contract locked.** The C# DTO/contracts, validators, and any required design-decision gates for the family are approved. No content of this family may exist in a registry before this.
2. **Content authored.** Definitions are written in the family's canonical format (DD-13) into a validated registry — either the isolated fixture namespace or the production namespace.
3. **Registry validation passes.** IDs, cross-references, formulas, targeting, costs, and logical presentation-asset references all validate. The registry fails fast on invalid content.
4. **Engine/system consumers may reference by stable ID.** Only now may engine, application, or presentation code look up definitions of this family.
5. **Presentation catalog binds logical asset IDs.** Logical presentation IDs resolve through the presentation asset catalogue to Unity assets. Generic fallbacks are acceptable until real art is reviewed; deterministic content stores only logical IDs, never Unity objects.
6. **Persistence includes the content.** A family enters saves only once any save-affecting migration for it exists.

#### 3.2 The no-forward-reference rule

No system may reference a content definition that has not reached stage 2 in a registry that system is permitted to read. In particular:

- Production registries never reference fixture definitions.
- A milestone may not author content whose schema lock belongs to a later milestone.
- A system may not branch on a content ID that does not yet exist "because it will later."

If a task appears to need a forward reference, that is a signal the sequencing is wrong, and the plan is revised rather than the rule.

#### 3.3 Worked example: the boss split

The first winnable run needs a boss, but the production boss is an M6 deliverable. The chain resolves this cleanly:

- **M3/M4** use a **fixture boss** — schema-validated, non-production, loaded from the isolated M3/M4 development registry. It exists (stage 2) before M4 calls it to close the run.
- **M6** authors the **production phased boss** after the enemy/encounter schema lock (stage 1), then replaces the fixture boss. The cutover is an explicit retirement step (§4.5).

At both stages, content exists before it is called, and no production content is authored prematurely to satisfy a test.

#### 3.4 Entry criteria encode the chain

Every milestone's entry criteria (§13 onward) name the content families it consumes and assert that each has reached the required stage. A milestone may not begin until its input families are ready. This is the mechanism that keeps the plan in a sensible order without micromanaging tasks.

---

### 4. Fixture vs. Production Content Policy

This section defines the boundary between non-production fixtures and production content. It exists to satisfy two constraints at once: tests and early loops need content to run, but production content must never be authored prematurely or hardcoded.

#### 4.1 What a fixture is

A fixture is schema-validated content in an explicit non-production namespace (for example `fixture.*`), loaded only from isolated test/development registries. Fixtures exist to:

- prove engine and presentation systems before production content exists;
- close early loops (such as the fixture boss that ends the first winnable run);
- provide deterministic test data for golden and property tests.

Fixtures are **data**. They are YAML/JSON definitions passing the same validators as production content. They are never code, never hardcoded constants, and never inline literals in engine or UI.

#### 4.2 What a fixture is not

- A fixture is not an excuse to hardcode. Engine and UI code may not branch on fixture IDs any more than production IDs.
- A fixture is not premature production content. We never author production definitions early just to make a test pass; the test uses a fixture instead.
- A fixture is not invisible. Fixture definitions carry an explicit non-production marker and live only in isolated sources.

#### 4.3 Where fixtures may and may not appear

| May appear in | Must never appear in |
|---|---|
| Isolated test/development registries | Production runtime registries |
| Edit/Play Mode tests | Production profiles or saves |
| Development saves (explicitly marked non-release) | Banners, Shops, inventories, equipment snapshots |
| Golden/property test fixtures | Presentation production catalogues |
| — | Any release build |

Production profiles, saves, banners, Shops, inventories, equipment snapshots, and release registries must reject fixture IDs. A validator and release scan enforce this.

#### 4.4 Production content appears only after its gates

Production content of a family is authored only after that family's schema lock and any design-decision gates are approved (the Content Readiness Chain, §3). For example:

- Production characters enter at M2, after the M2A schema lock (DD-02, DD-03, DD-13 approved).
- Production gacha content enters at M8, after DD-05/06/07/10/11/15.
- Production equipment enters at M8X, after DD-15 through DD-20.

Before those gates, the relevant systems run on fixtures.

#### 4.5 Cutover and retirement

Every milestone that replaces fixtures with production content defines an explicit cutover step:

1. Production content is authored and validated.
2. Runtime content sources switch from fixture to production.
3. Fixture definitions are removed from app/runtime bundles, developer-facing registries, and production saves.
4. Minimal fixture equivalents may remain under isolated test sources where they provide production-independent regression coverage.
5. Development saves referencing retired fixture IDs are migrated or invalidated, never silently loaded as production state.
6. A release scan proves normal application startup cannot load the retired fixture IDs.

The M1→M2 character cutover and the M3→M6 run-content cutover are the two required instances. Later cutovers follow the same shape.

---

### 5. Content Expansion Tiers and Extension Policy

This section governs how the game grows after launch. It replaces the prior plan's tendency to route every non-content change through a heavyweight gate, which would have made every future character feel like a project crisis. Engine growth for new content is a **planned, audited routine**, not a hard blocker and not a full design decision.

#### 5.1 The three tiers

| Tier | Trigger | Engine change? | Governance |
|---|---|---|---|
| **1 — Content-only** | New character, card, enemy, Trial, weapon, gear, motif, or shop offer built from **existing** typed operations | None | Schema validation + normal task plan. No special gate. This is the state modularity should maximise. |
| **2 — Engine-extending** | New **generic** operation, status class, trigger timing, targeting kind, or stat key | Yes, generic | **Extension Brief** + Builder + Auditor. Not a full design decision, not a blocker for planning the content. |
| **3 — Architectural** | Changes authoritative state *shape*, information boundaries, or cross-system contracts | Yes, architectural | Full **design decision** + dedicated implementation task. This is the DD-22/DD-23 class and remains a hard gate. |

#### 5.2 The boundary between Tier 2 and Tier 3

The test is precise:

- **Tier 2** introduces a *new atomic operation on existing authoritative state*. A new damage operation, a new status, a new passive trigger timing, a new stat key — these operate on state whose shape is already defined.
- **Tier 3** *changes the shape of authoritative state, what information is hidden, or how systems contract with each other*. Card copy with lineage, hidden-Draw selection, run-persistent self-debt, Domain engine replacement, and per-hit reaction systems are Tier 3 because they alter state shape, hidden information, or cross-system contracts, and they pressure saves, replays, and previews.

When a proposed mechanic is ambiguous, the Planner classifies it and records the reasoning in the Extension Brief or decision record. The default in doubt is the higher tier, because the cost of over-gating a Tier 2 change is small and the cost of under-gating a Tier 3 change is large.

#### 5.3 Tier 2: the Extension Brief

A Tier 2 change is authorized by a lightweight **Extension Brief** — a brief, not a full task plan. The Planner drafts it, the Builder implements it, the Auditor certifies it. It covers exactly the things that protect a deterministic engine:

```
# Extension Brief <ID> — <Operation Name>
## Operation identity        — kind, payload schema, operation family
## Generality statement      — which content uses it; explicit confirmation it is
                               not a character-ID branch
## RNG stream assignment     — which named substream it touches, or none
## Timing / ordering window  — where it sits in stable resolution order
## Preview parity            — confirmation it flows through the same evaluator
                               as resolution
## Save & content-version    — does it change combat-state shape (save-affecting)
   impact                      or only add content (content-version only)
## Deterministic test plan   — golden/property coverage
## Replay compatibility      — existing golden replays unaffected, or intentionally
                               migrated
```

The brief lives under `plans/tasks/` (or an approved `plans/extensions/` directory). It is approved before the operation lands in the engine. The character content that motivated it may be *planned* before the brief is approved, but may not be *authored into a registry* until the operation exists.

#### 5.4 Tier 3: the standing gates

DD-22 (advanced character mechanics and selfish costs) and DD-23 (advanced card memory, copy, and hidden-zone selection) are the standing Tier 3 gates. They are approved as *extension policies* — they reserve generic extension points and state the conditions under which the mechanic may later be implemented — but the actual mechanics require a dedicated post-launch implementation task with schema, save, UI, replay, and test review.

Tier 3 work is never embedded in a launch milestone. Launch implementation preserves the extension points (source-kind damage metadata, explicit status persistence scope, generic Domain operations, copy-lineage guardrails) without implementing the mechanics.

#### 5.5 How this maps to the agent workflow

- **Planner** classifies the change, drafts the Extension Brief (Tier 2) or opens the decision record (Tier 3).
- **Builder** implements the approved operation and its tests.
- **Auditor** certifies determinism, generality, and save integrity.
- **Acceptance Engineer** is invoked only if the change is also a milestone gate or high-risk cross-layer integration; a routine Tier 2 operation does not require protected acceptance.
- **Git Steward** commits once the Auditor returns `PASS`.

This keeps engine growth bounded and auditable without recreating the bureaucratic deadlock that previously froze a simple interaction task.

---

### 6. Modularity and Content Authoring Contract

This section states the modularity invariant and the authoring workflow that makes expansion cheap. It is a first-class contract, audited at M11, not a footnote.

#### 6.1 The modularity invariant

**Adding content is a data change.** Adding a new character, card, enemy, encounter, Trial, weapon, gear set, motif, shop offer, or reward table should require **no** engine or UI edits, except where the content genuinely needs a new generic operation (a Tier 2 Extension Brief, §5.3).

Concretely:

- Adding a new character must not require editing combat engine code.
- Adding a new card must not require editing Unity UI/presentation code, except for a genuinely new generic renderer/operation.
- Adding a new enemy must not require editing the combat state machine unless it introduces a new generic operation.
- Adding a new Trial must mean adding a Trial definition and reward table, not a bespoke scene path.
- Adding a new weapon, gear set, stat table, or Profile Shop offer must primarily be a content-data change.

New effect behaviour is added as **typed, reusable engine operations**, then used by content. Content composes operations; it does not inject code.

#### 6.2 Generic typed operations

Card, status, weapon, and keepsake effects use a typed, serializable operation tree, not arbitrary callbacks or unparsed formula strings. Requirements:

- exhaustive operation kinds and validated payloads;
- schema validation for static content;
- no runtime code evaluation for authored gameplay effects;
- formula preview and resolution share the same evaluator;
- explicit conditional and iteration bounds;
- escape hatches require named generic engine operations and tests, not inline `MonoBehaviour` callbacks or character-ID branches.

Unity CLI `eval` is a developer inspection tool only and is never an authored gameplay-effect mechanism.

#### 6.3 The authoring workflow

The workflow is designed so a human can add content with fast, clear feedback:

1. Author the definition in the family's canonical YAML (or JSON if machine-written).
2. Run content validation. Errors point at the **source path and definition ID**, not at a stack trace.
3. The import tooling compiles validated content into the generated registry with a content version/hash.
4. The registry exposes typed lookup, family indexes, cross-reference reports, and dependency reports.
5. Presentation asset references are validated against the presentation catalogue.

Every definition has a stable ID, content version, display-name key, and family discriminator. Cross-references use stable IDs. Balance values live in content data unless they are universal rules.

#### 6.4 Stable IDs and versioning

- Definition IDs are stable kebab-case strings scoped by family where useful (e.g. `character.mara`, `card.mara.incise`).
- Runtime instance IDs are distinct from definition IDs and from Unity instance IDs.
- Content versions are explicit and recorded in saves and replays.
- Definition IDs are never casually reused. Reusing an ID is a compatibility-breaking act and requires a decision.
- Display names are content/localization fields, never IDs.

#### 6.5 Extension points preserved

The architecture preserves the DD-22/DD-23 extension points so post-launch content does not force rework:

- damage and HP-loss outcomes carry source metadata (command ID, owner, source kind, Shield absorbed, HP dealt);
- status target scope and persistence scope are explicit even when launch statuses are combat-scoped;
- Domain automatic effects are typed operations, not character branches;
- card instance ownership, pile, generated/combat-scoped flags, costs, and spent once-per-instance flags are explicit;
- Draw order stays hidden from previews and rejected commands.

Preserving a door is not implementing what is behind it. Launch content must not use these hooks until their Tier 3 gate is implemented.

#### 6.6 The modularity audit

M11 runs a modularity audit that proves the invariant held:

- no production content ID appears in engine or UI conditionals;
- every content family loads through the registry;
- a sample "add a new card" and "add a new enemy" exercise completes as a data-only change;
- fixture IDs have not leaked into release registries, profiles, saves, or snapshots;
- generated registry artifacts reproduce from canonical content.

If the audit finds a place where adding content requires editing engine code, that is a release-blocking defect, because it means the expansion contract is broken.

## PART II — PROCESS AND WORKFLOW

### 7. Task Governance and Ceremony Model

This section defines how work is decomposed, who does it, and how it is certified. It exists to fix the two failure modes this project has already paid for: Gloomdrawn's absence of governance, and the previous Unity plan's excess of it. Governance here is invariants, gates, and certification. Everything else is kept deliberately light.

#### 7.1 Two kinds of task document

Work is carried by exactly two document shapes:

1. **Full task plan** (template in §30). Required only for:
   - milestone exit gates;
   - schema or content-family locks (e.g. M2A, M8X-A);
   - save-affecting work and migrations;
   - DD-gated system implementation (M8, M8T, M8X families);
   - cross-layer integration where engine, application, and presentation change together;
   - any task that touches a §1.3 invariant.

2. **One-page brief** (template in §30). Used for all other intra-milestone work. A brief states objective, in-scope work, non-goals, authority references, observable acceptance behaviour, and stop conditions. It does not freeze mechanism, API shape, helper design, or tooling internals.

The prior plan decomposed the project into ~90 pre-specified task files and treated each as a contract. That inverted the purpose of planning: it specified mechanism (which Unity owns) and under-specified feel and visibility (which only humans own). This plan specifies fewer documents, but each one that exists is authoritative.

#### 7.2 What task documents must not contain

A task document, full or brief, must not contain:

- implementation hashes, correction budgets, attempt counts, or polling algorithms;
- manifest packets, parallel-packet requirements, or frozen runner contracts;
- approval tiers, workforces, or escalation ladders not defined in this plan or the agent configurations;
- restated gameplay rules — a task document points at `docs/DESIGN.md` sections and approved DDs; it does not re-derive them;
- mechanism prescriptions for presentation internals (coordinate conversion strategy, tween curves, layout algorithms). The plan states the invariant (no drift, no off-screen loss, no mutation before acceptance); the Builder chooses the mechanism.

If a task document appears to need any of the above, that is a signal the task is mis-scoped, not that the document needs more ceremony.

#### 7.3 Roles and their governance interface

The project runs inside the Opencode harness. Role *permissions* live in the `.opencode/` agent definitions and `AGENTS.md`; this plan defines *when a role is invoked and what it certifies*, and nothing more. The governance interface is:

| Role | Invoked for | Certifies / produces | Must not |
|---|---|---|---|
| Lead Planner (this document's author) | Implementation plan, milestone gates, sequencing, DD preparation, re-baseline | This plan; gate status; decision drafts | Implement product code |
| Opencode Planner | One bounded task that is genuinely ambiguous or substantial | A concise `plans/tasks/**` plan | Edit product code, authority, governance, skills |
| Builder | One active task at a time | Product implementation + developer tests + routine validation evidence | Edit plans/, docs/, acceptance/, governance; commit |
| Auditor | After Builder completion | Exactly one verdict: PASS / PASS WITH FOLLOW-UPS / FAIL / BLOCKED | Repair anything; weaken acceptance |
| Acceptance Engineer | Risk-based only (§7.5) | Protected executable acceptance for flagged gates | Edit product code or developer tests |
| Git Steward | Owner-invoked after certification | Explicit-path staging, one non-amending commit, non-force push | Broad staging; force pushes; edits |
| Sol-Specialist | Builder escalation on a genuinely hard, bounded technical blocker | Deep-reasoning repair within a stated boundary | Open-ended redesign |

#### 7.4 Default flow of work

1. **Entry.** The task's milestone entry criteria and content-readiness prerequisites (§3) are met. The task exists as a brief or full plan.
2. **Build.** The Builder implements, writes or extends developer tests, and validates with `Tools/*.ps1` and `bloom.*` commands. Player-facing claims require ordinary-runtime evidence through an `-automated` Editor (§2.7), not controller calls or session injection.
3. **Certify.** The Auditor independently verifies and returns a verdict. `FAIL` returns bounded findings to the Builder. `BLOCKED` routes to the Planner or owner as an authority question, never as an implementation guess.
4. **Accept (conditional).** The Acceptance Engineer runs only if the task is flagged in §7.5.
5. **Commit.** The Git Steward commits on `PASS` or `PASS WITH FOLLOW-UPS`, staging only the allowlisted paths.
6. **Record.** A worklog entry records the task, verdict, evidence, and any follow-ups.

This is the entire routine. Any step added beyond it is a plan-level change (§1) and requires owner approval.

#### 7.5 Risk-based acceptance and the anti-bureaucracy clause

Protected executable acceptance is **risk-based, not default**. It is appropriate for:

- milestone exit gates (M1 feel/stability gate, M2 exit, M4 winnable-run gate, and later gates);
- cross-layer Unity integration where developer tests could bypass the real runtime;
- previously façade-prone areas (the card drag system is permanently flagged, given its history);
- important player-facing interaction regressions.

Ordinary tasks are certified by Builder tests plus independent Auditor verification. That is the whole gate.

**The anti-bureaucracy clause.** No role may invent a gate, manifest, hash pin, approval tier, correction budget, or workflow that does not exist in this plan, the agent configurations, or `AGENTS.md`. The previous planning regime froze a simple interaction task behind a "protected acceptance infrastructure recovery" it had itself invented; that failure mode is now explicitly forbidden. When uncertainty arises, the cure is a bounded question to the owner with a recommended option — never an invented ritual. A task that cannot proceed under the flow in §7.4 is stopped and raised as a plan-level or design-level change (§1), and the stop itself is recorded in the worklog.

#### 7.6 Owner-decision discipline

Roles ask the owner only when a choice materially changes player-visible behaviour, milestone scope, architecture, authority, persistence/schema/content contracts, acceptance behaviour, destructive operations, or the definition of completion. Questions carry a recommended option first and meaningful trade-offs. Private implementation and tooling details are never owner questions.

---

### 8. The Weekly Playable Ratchet

#### 8.1 The rule

Every merge to the working branch keeps the project playable: a human can open the pinned Unity Editor, press Play, and interact with at least one meaningful scene. The floor rises with the project:

- M0 onward: the bootstrap/dev scene enters Play Mode and reports health.
- M1 onward: the committed combat scene bootstraps fixture combat and accepts real input.
- M3 onward: the map scene is traversable in a development run.
- M4 onward: a run can be saved, closed, and resumed.

#### 8.2 What breaks the ratchet

A merge breaks the ratchet if it introduces: compile errors on open; a scene that throws or soft-locks on Play; an interaction path that submits unintended commands or loses input; a content-validation failure that blocks registry build; or a save/load path that corrupts previously valid state.

A ratchet break is fixed or reverted before further feature work lands. This is non-negotiable, because the ratchet is the mechanism that enforces *visibility before breadth* (§0.3). It is what prevents this project from becoming its predecessor: a large correct scaffold that nobody could play.

#### 8.3 Evidence

Brief-level tasks prove the ratchet with the existing wrappers (`Tools/validate.ps1`, `test-playmode.ps1`, `build-smoke.ps1`). Milestone closures add interaction evidence: what was clicked/dragged, at which aspect ratios, with what result. Screenshots support review; they never replace behavioural evidence.

#### 8.4 Relationship to tests

The ratchet is a sanity floor, not a substitute for the test contract (§9). A project can pass every headless test and still feel wrong; the ratchet guarantees there is always a living surface on which feel can be judged.

---

### 9. Validation and Testing Contract

This section condenses the testing doctrine into process terms. The authoritative list of invariants lives in `docs/DESIGN.md` §17; this section states how evidence is produced and weighed.

#### 9.1 Test layers and what each proves

| Layer | Proves | Runs |
|---|---|---|
| Edit Mode unit | Formulas, piles, statuses, resources, targeting, RNG, repositories, import validation | Headless, fast |
| State-machine | Legal/illegal commands, phase transitions, rejection, terminal states | Headless |
| Content/import validation | IDs, references, formulas, asset refs, registry hashes | Editor/build |
| Seed/property stress | Map reachability, combat invariants across large seed samples | Headless batch |
| Golden deterministic | Fixed content + seed + commands → fixed semantic trace and checksum | Headless |
| Play Mode integration | Hand layout, drag threshold/return, targeting, actor binding, previews, sequential presentation | `-automated` Editor |
| Built-player E2E | Profile → run → rewards → save/reload flows at milestone gates | Built/development Player |
| Visual/layout evidence | Composition and aspect-ratio review | Supports, never replaces |

No layer substitutes for another. Golden tests cannot prove feel; Play Mode tests cannot prove determinism; screenshots prove neither.

#### 9.2 The feel/determinism split

Deterministic behaviour is proven headlessly: same inputs, same events, same checksum, independent of frame rate and presentation. Feel is proven by human interaction in the `-automated` Editor: repeated drag/cancel cycles, hover behaviour, cancellation smoothness, aspect-ratio stability.

This split is load-bearing. The previous regime optimised a headless harness for a feel problem and produced a combat that passed its tests and felt wrong. From M1 onward, every milestone whose exit criteria include feel or layout carries an explicit human-verified gate, and that gate is recorded as interaction evidence, not as a test count.

#### 9.3 Non-weakenable discipline

- Failing deterministic, content-validation, or required Edit/Play Mode tests block task completion.
- A task may add tests; it may never weaken an invariant to pass.
- Any preview/resolution mismatch is an engine or adapter defect, not cosmetic drift.
- Any drag/layout defect that can lose a card off-screen, accumulate drift, submit an unintended command, or mutate state before acceptance blocks the owning combat milestone.
- Rejected commands must be shown to consume no RNG and mutate no state; this is asserted in tests, not assumed.
- Compilation alone is never evidence of correct Unity behaviour.

#### 9.4 Validation entry points

Stable wrappers exist so no role memorises raw Editor syntax:

- `Tools/validate.ps1`
- `Tools/test-editmode.ps1`
- `Tools/test-playmode.ps1`
- `Tools/build-smoke.ps1`
- `Tools/simulate.ps1`
- `Tools/get-unity-editor-state.ps1` and `Tools/open-automated-editor.ps1` for the `-automated` Editor path

Project-owned `bloom.*` CLI commands (`bloom.health`, `bloom.validate-content`, `bloom.scene-summary`, `bloom.load-combat-fixture`, `bloom.reset-combat-fixture`, `bloom.dump-combat-state`, `bloom.validate-combat-layout`) grow with the milestones that own them. The installed `unity --help` / `unity command --help` output is authoritative; the CLI is experimental and must never become a runtime dependency.

#### 9.5 Evidence language

Completion reports state evidence, not confidence: files changed; commands and tests run; pass/fail counts; runtime interactions checked; aspect ratios checked; concerns left unresolved and why. "Should work" and "appears fine" are not evidence and are treated as missing evidence.

---

### 10. Repository, Assembly, and Tooling Layout

This section condenses the structural contract. Exact folder names may be refined, but dependency direction and fixture separation are contractual.

#### 10.1 Assemblies

| Assembly | Contains | Boundary |
|---|---|---|
| `Bloomdrawn.Engine` | Combat state machine, cards, statuses, map, rewards, gacha, profile/equipment rules, RNG, save-model helpers | No Engine References: no `UnityEngine`, `UnityEditor`, scenes, `MonoBehaviour`, frame time, `UnityEngine.Random`, input, storage, presentation assets |
| `Bloomdrawn.Content` | Pure content contracts, registry interfaces, validation models | Unity-object-free |
| `Bloomdrawn.Application` | Sessions (`ProfileSession`, `RunSession`, `CombatSession`), persistence repositories, bootstrap | References Engine/Content; no Editor tooling |
| `Bloomdrawn.Presentation` | Actors, uGUI/TMP, Input System adaptation, animation, VFX, audio, `PresentationAssetCatalog` | Consumes authoritative state/events; never the reverse |
| `Bloomdrawn.Editor` | Import/validators, `[CliCommand]` tools, build helpers, developer windows | Never enters production Player logic |
| Tests (Edit/Play/Acceptance) | Layered per §9 | Acceptance isolated with its own edit boundary |

Dependency direction is one-way: Presentation → Application → Engine/Content; Editor may reference everything for tooling but is referenced by nothing at runtime. Circular references are never solved by collapsing boundaries.

#### 10.2 Content layout

- `GameContent/production/` — validated production definitions (YAML by default, DD-13).
- `GameContent/fixtures/` — isolated non-production definitions (§4), loaded only by test/development sources.
- Generated runtime registries, hashes, and manifests — derived artifacts, reproducible from canonical content, never hand-edited, never authoritative.

#### 10.3 IDs and naming

Definition IDs are stable kebab-case strings scoped by family (`character.mara`, `card.mara.incise`). Runtime instance IDs are distinct from definition IDs and from Unity instance IDs. Display names are content/localization fields, never IDs. No gameplay rule may depend on a GameObject name, prefab name, scene path, or Animator state name.

#### 10.4 Tooling stance

Unity CLI and `com.unity.pipeline` close the edit/observe/verify loop for agents and humans. They are development surfaces: the project builds, tests, and plays without them. Repeated operations become source-controlled `bloom.*` commands; `unity command eval` is ad-hoc inspection, never authored gameplay behaviour; Pipeline runtime control never ships in release builds.

## PART III — RE-BASELINE

### 11. Task 0 — Gap Audit & Re-Baseline

#### 11.1 Why this task exists

This plan is written from first principles and deliberately does not assume the current repository. The repository, however, contains two weeks of prior work — most of it sound spine, some of it bureaucratic overlay. Task 0 is the bridge: it inventories what exists, classifies every significant artifact against this plan, and produces an owner-approved re-baseline task list. Nothing is kept by inertia and nothing is deleted by reflex.

#### 11.2 Audit scope

The audit covers, at minimum:

- assembly boundaries and engine purity (§2.2, §10.1);
- schema/content import and registry pipeline (§2.3, §10.2);
- named RNG substreams and golden replay fixtures;
- command/event protocol and rejection semantics;
- save envelope and repository interfaces;
- M1 presentation: combat scene, independent actors, card fan, drag/Play Area system, presentation adapter;
- test estate: Edit Mode, Play Mode, golden, and acceptance tests;
- tooling: `Tools/*.ps1` wrappers, `bloom.*` commands, `-automated` Editor launchers;
- process artifacts: `agent-tasks/`, `acceptance/manifests/`, `acceptance/locks/`, worklog entries, and any frozen or blocked task states.

#### 11.3 Classification rules

Every audited artifact is classified exactly once:

- **Keep** — matches this plan's contracts and passes current evidence. Committed without change or with editorial cleanup only.
- **Refactor** — sound spine in the wrong shape: over-specified ceremony, webstack-shaped abstractions, mechanism frozen where this plan leaves mechanism open. Carried forward through a bounded brief.
- **Discard** — hallucinated governance (invented gates, manifests, workforces, correction budgets), hardcoded placeholders, fixture leakage, or code whose only purpose is satisfying a discarded process. Removed or archived with a worklog note.

The test for Discard is precise: if an artifact exists to satisfy a process this plan does not define, and removing it changes no approved behaviour, it is discarded.

#### 11.4 Known items entering the audit

The following are recorded as audit inputs, not pre-judged outcomes. The audit confirms or revises each classification with evidence:

- Engine/Content/Application/Presentation/Editor assembly spine — expected Keep, subject to purity evidence.
- `CardDragLayer`-class presentation utilities — expected Keep; the mechanism is standard and plan-consistent.
- M1 fixture content under isolated sources — expected Keep, subject to §4 namespace and leakage checks.
- The frozen M1-D01 task state, its manifest, and its "protected acceptance infrastructure recovery" — expected Discard as process. Any Builder implementation it produced is re-evaluated on behaviour alone under §12.
- The M1-D01 runtime drag acceptance test — reclassified under §7.5: legitimate as the M1 milestone gate harness if it drives public input through the ordinary committed scene; illegitimate as a per-task blocker. Renamed and re-scoped accordingly.
- `worklog.md` — Keep as historical record; future entries follow §7.4.

#### 11.5 Deliverables and exit

Task 0 delivers:

1. a Gap Audit report (plan-level document) listing every audited artifact, its classification, and its evidence;
2. a re-baseline task list of bounded briefs for every Refactor item;
3. removal or archival of every Discard item, staged by the Git Steward under owner authority;
4. a corrected milestone ledger stating which M0/M1 exit criteria are currently evidenced, partially evidenced, or unevidenced.

Exit: no artifact remains unclassified; no frozen or blocked phantom gate remains; the repository's governance surface matches this plan (`AGENTS.md`, agent definitions, this plan, task briefs, skills as toolchain teaching only); and the corrected milestone ledger is owner-approved.

---

### 12. M1 Feel & Stability Pass

#### 12.1 Why this pass exists

M1 was validated by headless and harness evidence. It has never been tuned by a human hand, and the prior regime's cardinal error was optimising a threshold harness for a feel problem. M2 builds every future combat on M1's interaction surface; a defect in feel compounds across the whole project. Therefore feel is fixed before breadth, and this pass is the first application of the feel/determinism split (§9.2).

#### 12.2 Scope

In scope:

- resting fan composition and expansion/contraction within safe bounds;
- hover/focus rise and scale timing;
- drag smoothing and pointer fidelity;
- armed indication readability (non-colour-only);
- cancellation/return animation;
- explicit-target staging and legal-target highlight;
- actor idle/act/hit/return reactions sufficient to read ownership;
- stability at 16:9, 16:10, and one ultrawide reference.

Out of scope:

- production characters, Domain mechanics, or production content;
- replacement of the M1 actor model, fan model, drag state machine, or presentation adapter (polish, never replace — §2.8);
- preview systems beyond what M1 already owns (the minimal authoritative evaluator arrives at M2H).

#### 12.3 Mechanism freedom

The Builder chooses the interpolation/tween mechanism. The plan's preference is a small presentation-only interpolation helper consistent with the approved architecture; a third-party tween package requires a brief-level justification and must remain presentation-only, deterministic-state-free, and removable. Mechanism choices are recorded in the brief, not frozen by this plan.

#### 12.4 The feel gate

The feel gate is human-verified interaction evidence, recorded per §9.5:

- repeated drag/cancel/play cycles read as smooth and intentional, with no visible snap, jump, or dead zone;
- cancellation returns the card to a correctly recomputed fan every time;
- arming and disarming are readable at a glance;
- owner acknowledgement and enemy reactions make each action attributable;
- the interaction is qualitatively benchmarked against the reference bar: Gloomdrawn's card feel. Where the two differ, the difference must be a deliberate improvement, not an unexamined regression.

The feel gate cannot be satisfied by test counts, harness traces, or screenshots alone. It is signed off by the owner (or the owner's delegate) playing the ordinary committed scene through public input.

#### 12.5 The stability gate

Simultaneously, the M1 stability invariants re-run and must pass:

- no cumulative positional or rotational drift across repeated interaction;
- no card lost off-screen at any required aspect ratio;
- no unintended command submission from any gesture path;
- no authoritative mutation or RNG consumption before command acceptance;
- click/keyboard parity with drag paths.

#### 12.6 Exit

M1 exits when both gates are evidenced: the feel gate as interaction evidence (§12.4) and the stability gate as test evidence (§12.5), plus the corrected milestone ledger from Task 0 confirming M1's remaining exit criteria. M2 entry is unlocked only by that combined evidence.

## PART IV — MILESTONES

### 13. Milestone Spine Overview and Dependency Graph

#### 13.1 The spine

| # | Milestone | Delivers | Exit gate | Change vs. prior plan |
|---|---|---|---|---|
| 0 | Task 0 — Gap Audit & Re-Baseline | Inventory + keep/refactor/discard ledger | No unclassified artifact; no phantom gate | NEW |
| 0 | M1 Feel & Stability Pass | Tactile correction of the M1 interaction surface | Feel gate + stability gate evidenced | NEW |
| 0 | M0 Foundation | Project, pure engine, schema/content, RNG, commands, save envelope, tooling | Golden test passes; engine has no Unity refs | Kept, marked complete |
| 1 | M1 Combat Foundation + Feel | Fixture combat through the permanent presentation path | Deterministic combat playable + feel/stability gate | Feel moved up from M9 |
| 2 | M2 First Playable Slice | Schema lock + starter four, four Domain engines, Transcend, real art bindable | Starter party playable, all engines, feel gate, M1 fixtures retired | Feel gate + immediate art added |
| 3 | M3 Minimal Labyrinth | Map, nodes, motifs, collapsing nodes, shops, symptoms, **fixture boss**, Obols | Temptation loop playable & deterministic | Fixture boss made explicit |
| 4 | M4 First Winnable Run | Run completion, banking, persistence, save/resume | **First winnable, saveable, resumable run** (fixture boss) | NEW explicit visibility gate |
| 5 | M5 Full Roster | Remaining four characters, roster/party UI, onboarding | All eight playable; onboarding completes | Granularity reduced |
| 6 | M6 Encounters & Boss | Enemy/encounter pools, **production phased boss**, keepsakes, symptoms/curses, rewards | Production boss replaces fixture boss; M3 fixtures retired | Boss split |
| 7 | M7 Map Breadth | Motif library, constraints, reveal, route reports | Large-seed validation passes | Kept |
| 8 | M8 Gacha & Meta | Direct pulls, banners, pity, dupes, EXP/Sigils, profile caps | Direct-pull loop works | DD gates named as critical path |
| 8T | M8T Trials | Trial families, difficulties, targeted rewards | DD-gated | Kept |
| 8X | M8X Equipment & Shop | Weapons, gear, Profile Shop, inventory, snapshots | DD-gated | Kept |
| 9 | M9 Combat UI/UX Closure | Polish M1/M2 systems, previews, accessibility | Production-usable combat | Polishes, doesn't replace |
| 10 | M10 Art/Audio Closure | Presentation catalog, assets, VFX/audio, performance (DD-31) | All roles resolve; perf baseline met | Kept |
| 11 | M11 Release Gate | Balance, audits, seeded E2E, release candidate | Content lock passes all gates | Kept |

#### 13.2 Dependency graph

```
Task0 -> M1(pass) -> M2 -> M3 -> M4 -> M5 -> M6 -> M7
  M0 --'                |     |     |
                        |     |     +--> M8 -> M8T -> M8X
                        |     +--> M6
                        +--> M5 -> M6
  M6 -> M8T
  M7/M8/M8T/M8X/M9/M10 -> M11
  M1 -> M9 ; M2 -> M9 ; M9 -> M10
```

Reading rules: an arrow means the target's entry criteria consume the source's exit evidence. The fixture boss is authored in M3 (stage 2 of the Content Readiness Chain) and consumed by M4; the production boss is authored in M6 and replaces it. No milestone references content that has not reached registry stage 2 in a registry it may read (§3).

#### 13.3 The gate model

Every milestone exit is evidenced by up to three gate kinds, and a milestone is not complete until every kind it declares is satisfied:

1. **Deterministic gates** — headless Edit Mode, golden, content-validation, and property evidence. Proves the rules.
2. **Runtime gates** — Play Mode or built-Player evidence through the ordinary scene and public input, via an `-automated` Editor. Proves the integration.
3. **Human gates** — owner- or delegate-verified interaction/visibility evidence (feel, layout, readability). Proves the experience.

The prior plan collapsed kind 3 into kinds 1 and 2 and therefore shipped combat that passed its tests and felt wrong. From M1 onward, any milestone whose exit includes feel, layout, or visibility declares a human gate, and that gate is recorded as interaction evidence (§9.5), never as a test count.

#### 13.4 Granularity rule for this part

Milestones below are described as **work clusters**, not pre-decomposed task files. A cluster becomes a full task plan only if it is a milestone gate, a schema/decision lock, save-affecting, DD-gated, or cross-layer (§7.1). All other clusters are carried by one-page briefs at execution time. This document fixes each milestone's entry criteria, invariants, and gates; it deliberately does not freeze intra-cluster mechanism.

---

### 14. M0 — Foundation (present, marked complete)

#### 14.1 Goal

Establish the Unity 6.5 project, the pure engine boundary, schema/content import discipline, named RNG, command/event protocol, save envelope, validation entry points, and agent-visible Editor tooling, before real gameplay content.

#### 14.2 Status

M0 is treated as **complete**, subject to confirmation by the Task 0 Gap Audit (§11). The audit re-verifies, rather than assumes:

- `Bloomdrawn.Engine` compiles with No Engine References and no `UnityEngine`/`UnityEditor` dependency;
- YAML/JSON canonical content imports through validators into a reproducible generated registry;
- named RNG substreams serialize and roundtrip through the DD-30 serializer;
- command/event protocol returns accepted/rejected results with stable golden checksums;
- save envelope and repository interfaces exist with atomic write and previous-valid fallback;
- `Tools/*.ps1` wrappers and `bloom.health` / `bloom.validate-content` / `bloom.scene-summary` work;
- the bootstrap/dev scene enters Play Mode with no production content.

#### 14.3 Exit (as evidenced, to be confirmed)

Fixed-seed golden test passes; sample content imports; project opens/builds under the pinned Unity version; engine assembly has no Unity dependency; an agent can query project health through documented tooling.

M0 is not re-planned. Where the audit finds a gap, it is carried as a Refactor or Discard item in the re-baseline task list, not as a new M0 task in this plan.

---

### 15. M1 — Combat Foundation + Feel (re-baselined)

#### 15.1 Goal

Prove the permanent combat presentation architecture with a non-production fixture party: independent actors, anchored battlefield, bottom-centred deterministic fan, drag/Play-Area interaction, explicit targeting, and ordered event-to-presentation sequencing — and make that surface **feel and behave correctly** before anything is built on it.

#### 15.2 Entry criteria

- Task 0 audit complete; M0 evidenced.
- The M1 Feel & Stability Pass (§12) is either folded into this milestone's execution or completed immediately before it; M1 does not exit without it.

#### 15.3 Work clusters

- **M1-Setup** — fixture party of four, owner-scaled Strike/Shield each, fixture enemy and encounter, generic combat setup from the validated fixture registry. Brief.
- **M1-State** — combat state machine, piles, hand target/maximum, Mana, damage/Shield/healing, Atomic Stop, enemy intents and sequential actions. Brief.
- **M1-Stage** — combat scene, `PartyFormationView`/`EnemyFormationView` with independent actor roots, anchored layout per DESIGN.md §8.10, aspect-ratio safe zones. Brief.
- **M1-Hand** — bottom-centred fan, hover rise, upward drag, responsive Play Area threshold, disarm on return, release-to-cast, explicit target-selection, click/keyboard parity (DD-28). Brief, but the drag system is permanently flagged for acceptance under §7.5 given its history.
- **M1-Presenter** — event→presentation-token adapter, actor lookup by stable runtime ID, fixture fallback animations, presentation lock, basic reduced-motion hooks. Brief.
- **M1-Gate** — golden combat replay + Play Mode interaction coverage + the §12 feel/stability pass. Full task plan (milestone gate).

#### 15.4 Invariants carried

- Fixtures are data in an isolated namespace; no production character, Domain mechanic, or hardcoded fixture ID.
- Changing fixture stats or card values requires no engine or UI edit.
- Hover/drag/arming/target-selection never mutate authoritative state or consume RNG before acceptance.
- Repeated interaction cannot drift, lose cards off-screen, or submit unintended plays.

#### 15.5 Exit

- One complete deterministic combat playable through the real scene and reproducible headlessly.
- Every runtime card has a valid owner; Strike/Shield resolve from owner stats.
- **Stability gate** (§12.5) passes as test evidence.
- **Feel gate** (§12.4) passes as human interaction evidence, benchmarked against Gloomdrawn.
- The M1-D01-era harness, if retained, runs only as this milestone's gate, never as a per-task blocker (§11.4).

---

### 16. M2 — First Playable Slice + Feel Gate

#### 16.1 Goal

Replace the fixture party with the production starter four — Mara, Thalassa, Sephira, Azael — implementing all four Domain engines, ultimates, Transcend, generated cards, and statuses through generic operations, and bind real art through the presentation catalog. M2's exit is the first combat that is playable, watchable, and feels like the reference.

#### 16.2 Entry criteria

- M1 exit evidenced (including the feel/stability gate).
- DD-02, DD-03, DD-13 approved (they are). No production character enters a registry before M2-Schema passes (Content Readiness Chain stage 1).

#### 16.3 Work clusters

- **M2-Schema** — production character/card schema and content-format lock; presentation-catalog expansion for portrait/combat-sprite/ultimate-VFX references with generic fallbacks. Full task plan (schema lock).
- **M2-Domains** — Flesh Embryo, Abyss Tentacles/Potency + automatic and immediate volleys, Spirit Essence/Ritual, Void economy/control; all as generic typed operations. Brief.
- **M2-Starters** — the four starter kits as schema-authored content plus the generic operations they require. Brief per character pair, or one brief; no per-character task files required.
- **M2-Transcend** — ultimate gauges, once-per-combat Transcend, Graveyard, generated combat-scoped cards. Brief.
- **M2-Preview** — the minimal authoritative, side-effect-free evaluator for target/resource previews (DESIGN.md §15.5). Brief.
- **M2-Bind** — bind starters to independent actor views via logical asset references; generated/reviewed art permitted immediately (DD-29); basic idle/act/hit/return via generic token contracts. Brief.
- **M2-Cutover** — switch runtime content to the starters; retire M1 fixtures from app/runtime bundles and developer registries; release scan proving normal startup cannot load them. Full task plan (registry/save-adjacent impact).

#### 16.4 Invariants carried

- No production character-specific engine or UI branches; Domain automatic effects are typed operations.
- Launch Abyss stays the simple Tentacles/Potency engine (DD-22 guardrail); no self-debt, aspect, or per-hit reaction systems.
- Launch M2 does not implement runtime copy or hidden-zone selection (DD-23 guardrail).

#### 16.5 Exit

- Starter party plays one combat exercising all four Domain engines through the M1 actor/card/presentation architecture.
- **Feel gate:** the starter combat is human-verified against the Gloomdrawn reference bar; differences are deliberate improvements.
- M1 fixture characters/cards exist only in isolated test sources; production registries contain no fixture IDs.
- Real or generated production-capable art is bound through the catalog for the starters.

---

### 17. M3 — Minimal Labyrinth

#### 17.1 Goal

Implement the freely traversable Labyrinth: axial hex map, node-primary topology, canonical temptation loop, Collapsing Node lifecycle, persistent Shops, finite Obols, and a **fixture boss** reference that will close the first winnable run in M4.

#### 17.2 Entry criteria

- M2 exit evidenced.
- DD-24 and DD-25 approved (they are). M3 run content is authored as isolated non-production fixtures (stage 2) before any M3 system consumes it.

#### 17.3 Work clusters

- **M3-RunContent** — isolated validated run-content contract: starter lineup reference, normal-combat reward table, premium Shop offer and keepsake, Symptom with owner-assignment rule, **Boss encounter reference**, and the node/content slots the temptation loop needs. Full task plan (content-family lock for run content).
- **M3-MapModel** — axial coordinates, node/edge instance IDs, connectivity-only edges, Collapsing Node lifecycle (`intact`/`occupied`/`collapsed`), current position. Brief.
- **M3-Motifs** — motif schema and sample Start/spine/side-loop/shop-loop/boss-approach motifs; translation-only placement. Brief.
- **M3-Stitch** — seeded motif placement with bounded backtracking; `map.layout` / `map.content` / `map.nodeModifiers` stream discipline; full topology/reachability/economy validation. Brief.
- **M3-Nodes** — movement and first-pass resolution for Travel, Collapsing Node, Normal Combat, Shop, Symptom, Boss; destination preview/confirmation before departure-collapse commits. Brief.
- **M3-Economy** — run-scoped Obols, first-clear rewards, seeded Shop stock/reveal/sold-out, purchase and rejected-transaction invariants, Symptom ledger marker. Brief.
- **M3-MapUI** — derived 2D map renderer consuming authoritative reachability/preview output; collapsed nodes readable without colour alone. Brief.
- **M3-Loop** — temptation-loop E2E covering Collapsing-first and Symptom-first routes plus preview/cancel no-mutation. Full task plan (milestone-relevant gate).

#### 17.4 Invariants carried

- Nodes own consequences; edges own connectivity. No edge-authored rewards, costs, Symptoms, Shops, or events.
- Collapsing Nodes collapse only after accepted departure; preview/cancel/reject consumes no state or RNG; save preserves occupied state.
- Boss reachability survives every legal Collapsing Node/Symptom sequence.
- No production reward quantity or fixture ID hardcoded.

#### 17.5 Exit

- The canonical temptation loop is playable and deterministic from seed.
- The fixture boss exists in the isolated registry and is reachable as the run's terminal node.
- Map, Shop, node, reward, and mutation state are canonically serializable for M4.

---

### 18. M4 — First Winnable Run

#### 18.1 Goal

Close the run lifecycle and add local persistence so the project reaches its first **winnable, saveable, resumable run**: start, traverse, fight the fixture boss, win, bank rewards, and resume correctly after closing the game. This is the project's first complete play loop and its most important early playtesting surface.

#### 18.2 Entry criteria

- M3 exit evidenced, including the fixture boss and serializable run state.
- DD-04 approved (it is). Persistence exposes only the approved checkpoints.

#### 18.3 Work clusters

- **M4-Save** — canonical active-run save: seed/difficulty, RNG streams, party snapshot, map/mutations, Shop state, run deck/card instances, current HP, Obols, queued rewards; rejection or explicit migration of retired-fixture payloads. Full task plan (save-affecting).
- **M4-Profile** — profile persistence for systems live through M4: ID/name, owned characters, saved parties, generic inventory quantities, reward ledger, run history. Full task plan (save-affecting).
- **M4-Banking** — transactional banking of declared profile rewards; never bank Obols, run cards, keepsakes, Symptoms/Curses, or run HP/map state. Brief.
- **M4-Resume** — save/resume UX exposing DD-04 checkpoints only; no mid-resolution, mid-animation, or interaction-state recovery. Brief.
- **M4-Finalize** — victory, defeat, abandon confirmation, run history, active-run clearing. Brief.
- **M4-Gate** — persistence E2E plus the first-winnable-run gate. Full task plan (milestone gate).

#### 18.4 The first-winnable-run gate

A human plays, on the ordinary runtime:

1. start a run with the starter party;
2. traverse the Labyrinth, resolving at least one combat, one Shop visit, and one loop cost;
3. reach and defeat the fixture boss;
4. observe correct banking into the profile;
5. close and resume a mid-run save with deterministic state intact.

This gate is the visibility ratchet's first full expression (§8). It is evidenced as a played run, not as a test count.

#### 18.5 Invariants carried

- The save contains no speculative banner, Trial, equipment, profile-level, duplicate, or progression payloads; M8/M8T/M8X add those later with explicit migrations.
- Obols never persist; rejected transactions consume no currency, item, state, or RNG.
- Retired M1 fixture IDs are rejected or explicitly migrated, never silently loaded as production state.

#### 18.6 Exit

- A run can be started, closed, resumed, completed (boss defeated), and reflected correctly in the profile.
- The first-winnable-run gate passes as played-run evidence.
- Production reward quantities remain gated by DD-10; M3/M4 quantities are fixtures only.

### 19. M5 — Full Launch Roster

#### 19.1 Goal

Complete the launch roster — Venelis, Nyxalia, Kibane, Mira Nox — plus roster/party screens and the DD-09 onboarding sequence, all through the same schema and presentation contracts proven in M2. M5 expands content breadth; it introduces no new Domain engines.

#### 19.2 Entry criteria

- M4 exit evidenced (persistence live, so onboarding state can persist correctly).
- The M2A production character contract exists. M5 **extends** it; it never creates a parallel schema.
- DD-03 approved (it is). DD-09 approved (it is).

#### 19.3 Work clusters

- **M5-Schema** — extend the production character contract for the four remaining kits' presentation and operation needs. Brief (the schema exists; this is an extension, not a lock).
- **M5-Operations** — the new generic engine operations the kits require: scheduled end-of-player-turn effects (Maws), non-lethal HP-loss costs, once-per-turn card discounts, the Spirit Essence-consume exception, Stun, ignore-Shield, Vulnerable/Weak/Doom, and removable-positive-status removal. Each is a **Tier 2 engine extension** and carries an Extension Brief (§5.3) before the character content that uses it is authored into a registry. This is the expansion policy working as designed.
- **M5-Characters** — the four kits as schema-authored content composing M2/M5 operations. Brief per character; no per-character task files required.
- **M5-Screens** — roster, character detail, and exact-four party builder with order persistence. Brief.
- **M5-Onboarding** — the DD-09 sequence: one focused teaching beat per Domain ending in a complete starter-party combat, with one-time tutorial/reward persistence. Full task plan (save-affecting one-time flags; player-visible).
- **M5-Profiles** — production starter profile and developer profile owning all eight. Brief.

#### 19.4 Invariants carried

- No run-persistent self-debt, party stances, Domain replacement, per-hit reactions, or copy/hidden-zone mechanics (DD-22/DD-23 guardrails).
- Onboarding persists only its live tutorial/reward state; it reserves no banner, Trial, equipment, or duplicate payloads.
- M5 does not assemble a versioned banner pool; M8 consumes banner-eligible metadata when its schema and decisions lock.

#### 19.5 Exit

- All eight launch characters load from content and play without production hardcoding.
- The onboarding sequence completes with the correct starter party and persists only its live state.
- Every new generic operation has an approved Extension Brief and deterministic tests.

---

### 20. M6 — Encounters, Boss, and Reward Breadth

#### 20.1 Goal

Replace the M3 fixture catalog with production run content: enemy/encounter pools, the **production phased boss** (which retires the M4 fixture boss), keepsakes, Rest/Event/Treasure nodes, Symptoms/Curses, and reward pool breadth. M6 is the content-breadth milestone that makes runs varied and closes the boss split opened in §3.3.

#### 20.2 Entry criteria

- M4 and M5 exit evidenced.
- DD-21 approved (it is) — release-quality enemy/Symptom/keepsake briefs must translate Bloom/Domain identity into concrete gameplay-scale 2D requirements.
- DD-10 remains the gate for **production reward quantities**; M6 authors pools and references, and production quantities enter only once DD-10 is approved.

#### 20.3 Work clusters

- **M6-EnemySchema** — enemy/intent framework expansion: HP/Shield, intent decks, targeting, traits, Control Resistance, phase hooks, and presentation metadata (size class, footprint, target bounds, anchors) per DD-26. Full task plan (schema lock).
- **M6-Encounters** — Normal/Elite pools with biome/depth tags, anti-repeat, and no secret party hard-countering. Brief.
- **M6-Boss** — the production launch boss: phase thresholds, Control Resistance, telegraphed transitions, no hidden instant-kill or unexplained immunity. Replaces the M4 fixture boss at cutover. Brief (the boss is content composing existing operations; a Tier 2 brief only if a new generic operation is needed).
- **M6-Keepsakes** — run-scoped keepsake definitions with typed effect operations and UI status representation. Brief.
- **M6-Nodes** — Rest options, Event/Treasure schemas, ≥4 Symptoms, ≥4 Curses, with content-warning metadata. Brief.
- **M6-Rewards** — card/keepsake reward pools and profile grant hooks; production quantities blocked on DD-10. Brief.
- **M6-Cutover** — replace M3 runtime fixtures with production content; retire M3 fixture IDs from runtime bundles, registries, and saves; release scan. Full task plan (registry/save-adjacent).

#### 20.4 Invariants carried

- Enemy placement metadata is presentation-only; it never alters authoritative slot order, targeting, or resolution (DD-26).
- Independently targetable enemies never overlap into ambiguity.
- Retired M3 fixtures survive only as isolated test equivalents.

#### 20.5 Exit

- Multiple seeds generate varied playable runs on validated production content.
- The first-winnable-run gate (§18.4) now passes against the production boss.
- Production registries and saves contain no M3 run-content fixture IDs.

---

### 21. M7 — Labyrinth Generation Breadth

#### 21.1 Goal

Expand map variety and prove the generator at scale: an expanded motif library with semantic presentation tags, distribution/difficulty constraints, fog/reveal rules, and large-seed property validation plus route-economy reporting.

#### 21.2 Entry criteria

- M6 exit evidenced. DD-24/DD-25 approved (they are).

#### 21.3 Work clusters

- **M7-Motifs** — shop/recovery/elite/boss-approach/travel motifs with concrete semantic presentation tags (spine/loop metaphor, connector material, landmark role, symmetry, branching) that M10 can map to assets. Brief.
- **M7-Constraints** — combat/Shop/Rest/Elite counts, Symptom/Curse exposure, and direct-route viability. Brief.
- **M7-Reveal** — topology and node-category reveal, Shop reveal on first visit, Symptom preview before traversal. Brief.
- **M7-Reports** — property/stress validation across large seed samples; min/max combats, Obol totals, Shop affordability, boss reachability, side-loop integrity. Brief.

#### 21.4 Invariants carried

- Tags are data for later asset mapping; they embed no asset paths or rendering logic in generation.
- All §3.3-era map invariants (reachability, one-time triggers, revisit safety) hold at scale.

#### 21.5 Exit

- The generator passes large-seed validation and produces route-economy reports.
- Every production motif exposes validated presentation tags.

---

### 22. M8 — Gacha and Meta Progression

#### 22.1 Goal

Implement the ethical gacha and meta progression: direct-pull resolution, banners, pity/guarantee, the duplicate ladder, character growth (EXP/Sigils/ascension), profile level and roster cap, and persistent inventory. This is the first milestone whose start is gated primarily by **decisions**, not code.

#### 22.2 Entry criteria — the critical path

M8 production work is blocked until **DD-05, DD-06, DD-07, DD-10, DD-11, and DD-15** are approved. The plan treats *resolving these decisions* as the gating work: the Lead Planner drafts the decision records; the owner approves; mirrors land in `docs/DESIGN.md` before resolver work begins (§1). No production gacha content is authored, and no resolver is implemented, ahead of these gates.

- M5 exit evidenced (banner-eligible character metadata exists).
- M4 persistence evidenced (profile save migration builds on it).

#### 22.3 Work clusters

- **M8-Lock** — decision and schema lock: exact pity table/formula, featured guarantee, first-acquisition protection, C1–C5 and post-cap outcomes, production reward quantities, cap bands, SSR/SR/R result model, result-family splits, 10-pull guarantee precedence, pre-M8X banner composition, audit fields. Full task plan (decision + schema lock).
- **M8-Save** — M8 profile-save version: profile EXP/level, character progression, EXP items/Sigils, direct-pull inventory, versioned banner pity/guarantee, gacha audit history, named profile/banner RNG; migration from M4 with active-Run state unchanged. Full task plan (save-affecting).
- **M8-Resolver** — one direct pull consumed per pull, no intermediary resource, pity/guarantee/ten-pull state, result rarity/family, audit log. Brief.
- **M8-Ladder** — C1–C5 duplicate progression, C0 viability, approved post-cap compensation. Brief.
- **M8-Growth** — three EXP tiers, Domain Sigils, every-10-level ascension, profile level and roster-cap enforcement. Brief.
- **M8-UI** — banner screen showing pulls, rates, rarity/result-family table, pity, guarantee, pool contents, and recent history. Brief.
- **M8-E2E** — earn → pull → duplicate/new → level → ascend → cap-enforce. Full task plan (milestone gate; Acceptance Engineer invoked).

#### 22.4 Invariants carried

- Direct pulls only; no intermediary conversion resource; no paid path of any kind.
- Hard pity and the 10-pull SR-or-better guarantee cannot fail; a lost 50/50 guarantees the next eligible featured result in the same pity family.
- Pity/guarantee never expire on banner rotation.
- Every pull is auditable and deterministically reproducible from the profile/banner RNG stream.

#### 22.5 Exit

- The direct-pull loop works end-to-end with visible rates, pity, guarantee, and history.
- All banner/progression behaviour is schema-authored and decision-locked.
- M4→M8 migration preserves profile state and continues profile/banner RNG exactly.

---

### 23. M8T — Trials

#### 23.1 Goal

Implement direct boss challenges with selectable difficulty tiers and targeted persistent rewards, so progression resources can be pursued deliberately. Corresponds to the design document's "M8B - Trials"; this plan uses `M8T` to keep task IDs unique.

#### 23.2 Entry criteria

- M8 exit evidenced. M6 exit evidenced (Trial bosses reuse the production boss framework with stable Trial rulesets).
- DD-10 and DD-12 approved.

#### 23.3 Work clusters

- **M8T-Lock** — Trial decision/schema lock: families, difficulty tiers, boss references, first-clear/repeat rewards, unlocks. Full task plan (decision + schema lock).
- **M8T-Definitions** — Flesh/Abyss/Spirit/Void Sigil Trials, EXP Trial, Money Trial; each grants only its declared reward family. Brief.
- **M8T-UI** — family list, difficulty selection, reward preview, first-clear/repeat indicators. Brief.
- **M8T-Save** — M8T profile-save version and migration; clear history persistence. Full task plan (save-affecting).
- **M8T-Rewards** — transactional first-clear/repeat grants; rejected or duplicate claims consume nothing. Brief.
- **M8T-E2E** — select → difficulty → clear → targeted reward; active Run unchanged. Full task plan (milestone gate).

#### 23.4 Invariants carried

- Trials award only declared persistent categories; they never grant Obols or mutate the active Run.
- Trial reward preview matches awarded inventory exactly.

#### 23.5 Exit

- A player can select a difficulty, clear a production Trial, and receive targeted persistent rewards.
- M8→M8T migration loses no banner, inventory, progression, Run, or RNG state.

---

### 24. M8X — Equipment, Profile Shop, and Inventory

#### 24.1 Goal

Implement the rarity expansion, character weapons, six-slot gear sets, Profile Shop, conversion-currency sinks, equipment inventory, and deterministic Run/Trial equipment snapshots. This is the milestone that most stresses the modularity contract (§6): weapons and gear must be content-driven, and equipment effects must be generic typed operations.

#### 24.2 Entry criteria

- M8T exit evidenced.
- **DD-15 through DD-20** approved, with inherited DD-07 and DD-10 through DD-12 re-verified. No equipment schema, save contract, or content precedes these gates.

#### 24.3 Work clusters

- **M8X-Lock** — rarity/result-family tables; weapon definitions (signature links, growth, ascension, duplicate bonuses, acquisition); gear slots/sets/stat pools/reroll/desynthesis; Profile Shop offers; inventory categories. Full task plan (schema lock).
- **M8X-Save** — M8X profile/run save versions; owned weapons/gear, loadouts, Shop state, conversion/reroll currencies, named `profile.equipment` RNG, versioned equipment snapshots; migration preserving all pre-existing state and RNG. Full task plan (save-affecting).
- **M8X-Weapons** — one weapon slot, level/ascension commands, three EXP tiers, ascension materials, +0→+5 duplicates, maxed conversion, banner result handling; generic passive hooks only where their owning gate (DD-22/DD-23) is satisfied. Brief.
- **M8X-Gear** — six slots, 3/6-piece bonuses, main-stat enhancement to +12, three substats, deterministic rerolls on `profile.equipment`, desynthesis, lock/favorite protection. Brief.
- **M8X-Shop** — Profile Shop stock/state, targeted offers, purchases on persistent general and conversion currency, profile-money upgrade sinks, audit entries. Brief.
- **M8X-Inventory** — categories, detail views, lock/favorite, equip/unequip, desynthesis with yield preview, Profile Shop screen, rarity/family result display. Brief.
- **M8X-Snapshot** — acquire via isolated validated test content, equip, set bonuses, reroll, desynth, Shop purchase, and snapshot stability after profile equipment changes. Full task plan (milestone gate).

#### 24.4 Invariants carried

- Active Run/Trial snapshots never mutate when profile equipment changes later.
- Weapon duplicate bonuses cap at +5; +0 weapons remain useful and are never required for standard content.
- Conversion currency never buys direct pulls and never becomes an intermediary pull resource.
- Rejected upgrade/reroll/desynthesis/discard/Shop commands consume no currency, items, or RNG.
- Production equipment content never references non-production fixture IDs.

#### 24.5 Exit

- A player can pull/acquire weapons, equip weapon+gear loadouts, upgrade, reroll/desynth, buy targeted Shop items, and start a deterministic Run/Trial from a stable snapshot.
- M8T→M8X migration loses no progression, Trial, banner, inventory, Run, or RNG data.

---

### 25. M9 — Combat UI/UX Closure

#### 25.1 Goal

Close and polish the combat interaction and presentation systems proven in M1/M2 — final layout, production card feel, authoritative previews, formation readability, pile viewers, accessibility, and cross-aspect E2E. M9 polishes; it never replaces the M1 actor model, fan model, drag state machine, or presentation adapter (§2.8).

#### 25.2 Entry criteria

- M6 exit evidenced (production enemy breadth is needed for formation readability).
- DD-26 and DD-28 approved (they are).

#### 25.3 Work clusters

- **M9-Layout** — final information hierarchy and responsive battlefield constraints per DESIGN.md §8.10. Brief.
- **M9-Interaction** — production fan/hover/drag feel, cancel/return animation, click/keyboard parity. Brief, but carries a **human feel gate** (§13.3) given the drag system's flagged history.
- **M9-Preview** — expand the M2H authoritative evaluator to damage/Shield/resource/control-conversion/Safe-skull previews; no UI-side formula duplication. Brief.
- **M9-Sequence** — complete event→token mapping, ordered playback across multi-effect cards, ultimates, volleys, status ticks, sequential enemies, terminal transitions; speed/skip/reduced-motion; reload behaviour. Brief.
- **M9-Viewers** — draw (hidden order), discard, Graveyard viewers; log filters; formula detail. Brief.
- **M9-Access** — keyboard/controller navigation, accessible labels, reduced motion, text scaling, colourblind-safe indicators, aspect matrix. Brief.
- **M9-E2E** — production combat E2E across the interaction and formation matrix. Full task plan (milestone gate; Acceptance Engineer invoked).

#### 25.4 Invariants carried

- Preview and resolution share one evaluator; any mismatch is an engine/adapter defect.
- Repeated interaction produces no drift, off-screen loss, or unintended submission.
- The final layout preserves the authoritative/presentation separation.

#### 25.5 Exit

- Combat is production-usable by click, drag, and keyboard/controller where required; feel is stable across declared resolutions; enemy targets remain readable and unambiguous.

---

### 26. M10 — Art, Audio, VFX, and Performance

#### 26.1 Goal

Close the production presentation catalogue, asset breadth, animation/VFX/audio, technical budgets, and visual consistency — without sacrificing readability or determinism. Generated art is already permitted and may already be release-quality; M10 is the closure and audit milestone, not the first point real art is allowed.

#### 26.2 Entry criteria

- M9 exit evidenced. DD-21, DD-26, DD-29 approved (they are).
- **DD-31 must be approved before M10-Perf evaluates performance.** No hardware class, resolution, frame-time target, memory budget, or tolerance is selected by this plan while DD-31 is open.

#### 26.3 Work clusters

- **M10-Catalog** — finalize the logical presentation catalogue, import presets, loading/fallbacks, budgets, provenance/review/release-readiness status, and a release scan proving every required production logical asset ID resolves. Full task plan (release-affecting).
- **M10-Characters** — portraits and independent full-body combat sprites from concrete briefs; per-asset animation method (transform layers, Animator, frame animation, 2D Animation, or approved alternative); horror reveal at gameplay scale. Brief.
- **M10-World** — card, map, connector, landmark, Shop, Symptom, Curse, keepsake, and environment assets mapped from M7 tags; fallbacks for every required role. Brief.
- **M10-VFXAudio** — Domain VFX families through URP 2D/Particle/Shader Graph (VFX Graph only where useful); full/reduced-motion variants; UI/combat sound and music hooks. Brief.
- **M10-Perf** — readability, loading, and performance validation against the approved DD-31 baseline using Unity Profiler/Memory/2D Profiler. Full task plan (milestone gate).

#### 26.4 Invariants carried

- Generated and non-generated assets are judged by identical quality/readability requirements; generation method is provenance, not placeholder status.
- Reduced motion removes large movement and shake without removing mechanical information.
- Asset load/import failures cannot corrupt deterministic state.

#### 26.5 Exit

- The presentation pass reduces no readability, targetability, accessibility, interaction stability, or determinism; every required role resolves to a reviewed asset or approved fallback; the DD-31 baseline is met.

---

### 27. M11 — Balance, Reliability, and Release Gate

#### 27.1 Goal

Validate the release candidate across determinism, schema discipline, saves, balance, accessibility, and performance, and lock content for release. M11 runs the project's standing audits, including the modularity audit (§6.6) and fixture-leak release scans (§4.5).

#### 27.2 Entry criteria

- M7, M8, M8T, M8X, M9, and M10 exit evidenced.
- DD-14 (release platform lock) resolved before packaging; DD-08 (content intensity) resolved before content lock.

#### 27.3 Work clusters

- **M11-Determinism** — engine dependency, RNG stream, replay checksum, preview-parity, and no-UI-rule-duplication audit. Full task plan (release gate).
- **M11-Hardcoding** — hardcoding/schema audit plus the **modularity audit** (§6.6): data-only "add a card" and "add an enemy" exercises, registry coverage, and zero production content IDs in engine/UI conditionals. Full task plan (release gate).
- **M11-Saves** — save roundtrip, invalid-version handling, failed-write recovery, and the M4→M8→M8T→M8X migration chain; retired-fixture migration/invalidation behaviour. Full task plan (release gate).
- **M11-E2E** — seeded matrix over starter/alternate parties, multiple seeds, victory/defeat/abandon, save/resume, pull, Trial, snapshot, Shop, reroll/desynth. Full task plan (release gate).
- **M11-Sim** — Domain-aware simulation and tuning reports (§9.1 simulation hooks inform tuning; they do not replace human playtesting). Brief.
- **M11-AccessPerf** — accessibility and performance audit against approved baselines. Brief.
- **M11-Lock** — content/schema/save version lock, known issues, release checklist. Full task plan (release gate).

#### 27.4 Exit

- The content-lock candidate passes deterministic, schema, persistence, UI, accessibility, and performance gates; the modularity audit and fixture-leak scans are clean.

## PART V — REGISTERS AND APPENDICES

### 28. Design Decision Gate Table and Critical Path

This section is the plan's view of the decision register. `plans/design-decisions.md` remains the audit trail; this table states what each decision gates in sequencing terms. A milestone may not begin work that a listed open decision gates.

#### 28.1 Approved decisions (sequencing effect already consumed)

| DD | Locked | Sequencing effect |
|---|---|---|
| DD-01 | Atomic Stop | M1 combat finalization |
| DD-02 | Domain tuning | M2A schema lock |
| DD-03 | Launch character kits | M2A / M5A content locks |
| DD-04 | Save checkpoints | M4 resume UX |
| DD-09 | Starter onboarding | M5H onboarding |
| DD-13 | YAML/JSON format policy | all production authoring |
| DD-21 | Bloom identity/refraction | release-quality content locks |
| DD-22 | Advanced character mechanics | standing Tier 3 gate (§5); launch guardrails only |
| DD-23 | Card memory/copy | standing Tier 3 gate (§5); launch guardrails only |
| DD-24 | Collapsing Node lifecycle | M3 map work, M4 serialization |
| DD-25 | Node-primary topology | M3/M4/M7 map work |
| DD-26 | Enemy placement/readability | M6 metadata, M9 layout, M10 validation |
| DD-27 | Unity architecture | M0/M1 |
| DD-28 | Card hand/threshold interaction | M1H, M9 |
| DD-29 | Generated art policy | all art milestones |
| DD-30 | Save-facing JSON serializer | M0C/M0E persistence |

#### 28.2 Open decisions (the critical path)

| DD | Question (one line) | Blocks | Earliest dependent milestone |
|---|---|---|---|
| DD-05 | Exact rates, soft/hard pity, rounding | M8A lock | M8 |
| DD-06 | First-acquisition protection | M8A lock | M8 |
| DD-07 | C1–C5 and +1–+5 duplicate outcomes | M8A lock; M8X dupes | M8 |
| DD-08 | Content warning taxonomy/intensity | M10/M11 content lock | M11 |
| DD-10 | Reward quantities per mode/difficulty | production M6 quantities; M8/M8T/M8X rewards | M6 |
| DD-11 | Profile-to-roster cap bands | M8A lock; M8X cap bands | M8 |
| DD-12 | Trial difficulty/reward tables | M8T-A lock | M8T |
| DD-14 | Release platform set | packaging | M11 |
| DD-15 | SSR/SR/R rates, result families, 10-pull rules | M8A lock; M8X banners | M8 |
| DD-16 | Stat keys, scalers, stacking, snapshot rules | M8X-A/B | M8X |
| DD-17 | Weapon progression and +1–+5 bonuses | M8X-A/C | M8X |
| DD-18 | Gear slots, stats, sets, reroll, desynthesis | M8X-A/D | M8X |
| DD-19 | Profile Shop stock/prices/conversion rules | M8X-A/E | M8X |
| DD-20 | Economy and item naming | M8X content/UI lock | M8X |
| DD-31 | Windows performance baseline | M10E acceptance | M10 |

#### 28.3 Critical paths

- **Meta path:** DD-05/06/07/11/15 (+DD-10) → M8A → M8 → M8T → M8X → M11. This is the longest decision-bound chain in the project. The Lead Planner drafts these records with recommended baselines (the register already carries proposed baselines); the owner approves; mirrors land in `docs/DESIGN.md` before resolver work (§1).
- **Content-quantity path:** DD-10 → production M6 reward quantities and all meta reward tables. M3/M4 and M8 development work proceed on fixtures until then (§4).
- **Performance path:** DD-31 → M10E. Cheap to decide now; expensive to discover late. Recommended for early owner attention even though nothing before M10 needs it.
- **Packaging path:** DD-14 → release builds. Windows-only is the viable default; the decision is required only before M11 packaging.
- **Content-lock path:** DD-08 → M11 content lock.

The plan sequences so that **no implementation waits on a decision that is not on its critical path**: M1–M5 and M7 are unblocked by open decisions except production reward quantities (DD-10), which fixtures cover.

---

### 29. Global Risk Register

Revised from the prior register. Retained risks keep their mitigations, now cited to this plan's sections; new risks are the ones this project has already paid for or is structurally exposed to.

| Risk | Consequence | Mitigation |
|---|---|---|
| Bureaucratic re-inflation (invented gates, manifests, workforces) | Work freezes behind phantom process, as in M1-D01 | §7.5 anti-bureaucracy clause; §7.4 fixed flow; stops raised as plan-level changes |
| Feel/visibility debt (game unplayable until late) | Design errors discovered too late to correct cheaply | §8 weekly playable ratchet; §13.3 human gates; §12 feel pass |
| Mechanism over-specification | Builder fights frozen mechanism instead of solving feel/integration | §7.2 task documents must not contain; plan owns invariants, Builder owns mechanism |
| Skill context pollution | Agents ingest game rules in toolchain context; competing sources of truth | §0.2 skills are toolchain teaching only; sanitized skill files |
| Tier 2 over-gating | Routine engine extensions stall content expansion | §5 three-tier policy; Extension Brief is bounded, not a DD |
| Schema bypass or hardcoding | Content becomes brittle and untestable | §2.4, §6, §10 registry; M11 hardcoding/modularity audit |
| RNG nondeterminism | Replays/saves diverge | §2.1 named streams; golden tests |
| Save corruption | Player loses progress | §10 repositories; M11 save audit |
| Gacha trust failure | Ethical contract breaks | §22 invariants; audit log; direct pulls |
| Equipment economy creep | Permanent power overwhelms roguelike balance | §24 gates; snapshots; simulation |
| Map softlock | Run cannot reach boss | §17/§21 validation and property tests |
| UI preview mismatch | Player cannot trust decisions | §9.3 preview parity as engine defect |
| Unity presentation/UI performance | Readability or input stability suffers | §26 M10-Perf against DD-31 |
| Trial reward imbalance | Meta economy collapses | §23 declared tables; simulations |
| AI-assisted/generated content inconsistency | Tone/quality drifts | §2.6 provenance vs readiness; human review |
| Non-production fixture leakage | Test content enters player state or release | §4 policy; §31 retirement table; release scans |
| Production content precedes schema/decisions | One-off branches and unstable rewrites | §3 readiness chain; §28 gates |
| Speculative persisted state | Empty payloads become accidental contracts | §18.5; versioned migrations at M8/M8T/M8X |
| Abstract art direction | Assets cannot communicate mechanics | §20/§26 concrete briefs and tags |
| Advanced character retrofit pressure | Post-launch kits force rewrites | §5 Tier 3; DD-22 guardrails |
| Unsafe card-copy semantics | Transcend bypass, hidden-order leaks, recursion | §5 Tier 3; DD-23 guardrails |
| Card drag/canvas instability | Drift, off-screen cards, unintended plays | §12/§15 stability gates; drag system permanently flagged for acceptance (§7.5) |
| Unity scene/prefab rule leakage | Gameplay coupled to GameObjects/Animator names | §2.2 engine purity; §10 boundaries; M11 audit |
| Experimental CLI/Pipeline churn | Agent scripts break on syntax change | §9.4 wrappers; `--help` authoritative; §10.4 |

---

### 30. Task Document Templates

Three templates exist. §7.1 states when each is used; nothing else is permitted. A task document that does not match one of these shapes is not a task document and carries no authority.

#### 30.1 Full task plan (gates, schema locks, save-affecting, DD-gated, cross-layer)

```
# Task <ID> — <Title>
## Objective and player-visible outcome
## Authority references          — DESIGN.md sections, DDs, plan sections
## In scope / Non-goals
## Entry criteria                — content readiness stages (§3) and prior gates
## Public contract / schema / save changes
## Invariants carried            — from §1.3 and milestone invariants
## Acceptance behaviour          — observable, ordinary-runtime where player-facing
## Required tests and gate kind  — deterministic / runtime / human (§13.3)
## Validation commands           — Tools/*.ps1 and bloom.* names
## Stop conditions
## Exit criteria
## Worklog requirements
```

#### 30.2 One-page brief (all other intra-milestone work)

```
# Brief <ID> — <Title>
## Objective
## In scope / Non-goals
## Authority references
## Acceptance behaviour
## Stop conditions
```

Briefs deliberately omit mechanism, helper design, API shape, and tooling internals. A brief that freezes mechanism has exceeded its authority and is revised, not executed.

#### 30.3 Extension Brief (Tier 2 engine extensions, §5.3)

```
# Extension Brief <ID> — <Operation Name>
## Operation identity        — kind, payload schema, operation family
## Generality statement      — which content uses it; not a character-ID branch
## RNG stream assignment     — named substream touched, or none
## Timing / ordering window  — position in stable resolution order
## Preview parity            — same evaluator as resolution
## Save & content-version    — save-affecting or content-version only
   impact
## Deterministic test plan   — golden/property coverage
## Replay compatibility      — existing replays unaffected, or migrated
```

---

### 31. Fixture Lifecycle and Retirement Table

Every fixture family has a declared birth, consumption window, and retirement. This table is the standing contract; individual cutovers (§16.3 M2-Cutover, §20.3 M6-Cutover) execute it.

| Fixture family | Introduced | Consumed | Retired | Retirement mechanism |
|---|---|---|---|---|
| M1 fixture party and cards | M1-Setup | M1 | M2-Cutover | Removed from runtime bundles and developer registries; minimal isolated test equivalents retained; release scan proves normal startup cannot load them |
| M3 run content (reward table, Shop offer, keepsake, Symptom, boss reference) | M3-RunContent | M3, M4 | M6-Cutover | Production run content replaces; fixture-bearing development saves migrated or invalidated; release scan |
| Equipment fixtures (weapons/gear) | M8X tests | M8X | Never enter production | Isolated validated injection only; production catalogs, banners, Shops, inventories, and snapshots reject fixture IDs |
| Golden/combat regression fixtures | M0/M1 | Ongoing | Never (regression value) | Retained under isolated test sources; regenerated only when canonical traces change, without weakening assertions |

Standing rules (full policy in §4):

- Fixtures carry an explicit non-production namespace and marker, and load only from isolated sources.
- Engine, UI, persistence, and presentation code never branch on fixture IDs.
- Production profiles, saves, banners, Shops, inventories, snapshots, and release registries reject fixture IDs; validators and release scans enforce this.
- A fixture that has outlived its table row is a plan-level defect, not a silent survivor.

---

*End of document. This plan supersedes its predecessor in full. It takes effect on owner approval; Task 0 (§11) is its first executable act, and the DD critical path (§28.3) is its first planning act.*