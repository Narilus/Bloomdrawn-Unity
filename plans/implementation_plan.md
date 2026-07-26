# Bloomdrawn - Implementation Plan

> Companion to `docs/DESIGN.md`. This document defines sequencing, architectural boundaries, executable task groups, verification, and change control for implementation. When this plan and `docs/DESIGN.md` conflict, `docs/DESIGN.md` wins and this plan must be revised before implementation continues.

---

## 0. Implementation Baseline

Bloomdrawn is implemented as a Unity 6.5, C#-based 2D project with a deterministic rules engine, explicit task boundaries, schema-driven content, isolated non-production fixtures, golden tests, source-of-truth governance, and milestone gates. Permanent combat presentation infrastructure begins in M1 so battlefield composition, independent actors, card feel, drag stability, target selection, and event sequencing are validated as part of the first playable combat path.

### 0.1 Core Implementation Commitments

1. **Unity 6.5 (`6000.5.x`) + C# is the host stack.** Windows is the primary development/validation target; public release platforms remain gated by DD-14. The repository pins the exact Unity 6.5 patch in `ProjectSettings/ProjectVersion.txt`.
2. **The deterministic engine is presentation-independent.** `Bloomdrawn.Engine` is a dedicated Assembly Definition with No Engine References so it cannot depend on `UnityEngine`, `UnityEditor`, scenes, frame time, or presentation assets.
3. **URP 2D and independent Unity actors own battlefield rendering.** Party members and targetable enemies are individual actor roots with their own transforms, target bounds, UI/VFX anchors, and presentation state.
4. **uGUI + TextMesh Pro is the initial runtime UI system.** It owns combat HUD, the card hand, menus, targeting overlays, and other player UI. UI Toolkit is reserved initially for Editor/developer tooling so runtime card drag/focus/raycast behaviour is not split across UI systems without an explicit later decision.
5. **The Input System owns pointer/keyboard/controller input.** Input gestures remain presentation state until a complete engine command is accepted.
6. **Card feel/stability is an M1 responsibility.** The first vertical slice includes a bottom-centred fanned hand, hover/select, upward drag, responsive Play Area threshold, cancellation/return, and explicit target-selection state.
7. **Sequential presentation begins in M1.** Engine events map to presentation tokens and independent actor/UI animations from the first playable combat; M9/M10 extend, harden, and polish the same presentation path.
8. **Local persistence uses versioned file-backed repositories.** Saves live under Unity's persistent data location with validation, atomic replacement, and previous-valid fallback behaviour.
9. **Unity Test Framework is the primary in-project harness.** Edit Mode covers pure engine/content; Play Mode covers scene/UI/input; project PowerShell wrappers provide stable validation entrypoints.
10. **Unity CLI + `com.unity.pipeline` is an agent-development surface.** It is experimental, isolated from gameplay architecture, and wrapped by project-specific commands where repeated operations are useful. Installed `--help` output is authoritative for exact CLI syntax.
11. **Schema-driven content is mandatory.** Hand-authored YAML is canonical by default; editor/build tooling validates and compiles it to generated runtime data. Machine-generated artifacts use JSON by default.
12. **Production gameplay hardcoding is forbidden.** Unity scenes, prefabs, MonoBehaviours, Animator state names, and GameObject names cannot become hidden gameplay databases.
13. **Generated art is always permitted.** AI-generated art may be prototype, production, or release-quality; generation method is provenance, not placeholder status. Human review and technical/readability gates still apply.

## 1. Authority, Change Control, and Design Gates

### 1.1 Source-of-Truth Hierarchy

1. `docs/DESIGN.md`
2. `plans/design-decisions.md`, for approved decision records mirrored into `docs/DESIGN.md`
3. `plans/implementation_plan.md`
4. Active task plans under `plans/tasks/` or an approved equivalent
5. Automated tests that correctly encode approved rules
6. Engine implementation
7. Application/session adapters and persistence
8. Unity scenes, UI, animation, VFX, and presentation

A lower layer may not redefine a higher layer. If implementation exposes a design flaw, stop the affected task, update `docs/DESIGN.md`, revise this plan or the active task plan, update tests, and only then change code.

### 1.2 Required Design Gates

| Gate | Required before | Decision required |
|---|---|---|
| **DD-01 - Combat terminal timing** | Resolved for M1 combat finalization | Approved Atomic Stop: terminal state is checked after each atomic terminal-capable effect, and remaining non-terminal sub-effects are skipped once victory or defeat is reached. |
| **DD-02 - Domain tuning lock** | Resolved for M2A production schema/content lock | Approved launch Domain resource values, UI strings, reset/persistence rules, and edge-case invariants from `docs/DESIGN.md` section 3; values marked tuning remain adjustable. |
| **DD-03 - Launch character tuning lock** | Resolved for M2A and M5A content lock | Approved all eight section 4 launch kits as implementation anchors; M2 implements the starter four first and M5 implements the remaining four. |
| **DD-04 - Save checkpoints** | M4 resume UX | Decide which checkpoints are exposed: map/node only, stable combat action boundaries, or broader mid-combat recovery. |
| **DD-05 - Gacha rates and pity** | M8A decision lock | Approve exact rates, soft-pity table/formula, hard pity, featured guarantee, and rounding. |
| **DD-06 - First-acquisition protection** | M8A decision lock | Approve early duplicate-frustration protection after the starter party. |
| **DD-07 - Duplicate ladder** | M8A decision lock | Approve exact C1-C5 duplicate benefits and any post-cap compensation. |
| **DD-08 - Content intensity settings** | M10/M11 content lock | Approve warning taxonomy and player-facing intensity/accessibility settings. |
| **DD-09 - Starter onboarding** | Tutorial implementation | Approve tutorial order, curated encounters, and first-run guidance for Mara, Thalassa, Sephira, and Azael. |
| **DD-10 - Reward economy** | Production M6/M8/M8T/M8X reward tables | Approve direct pulls, EXP items, Sigils, persistent currency, Obols, first-clear, and repeat reward quantities. M3/M4 may prove transactions with isolated non-production tables. |
| **DD-11 - Profile roster cap table** | M8A decision lock | Approve profile-level bands and maximum character level caps. |
| **DD-12 - Trial difficulty/reward table** | M8T-A decision/schema lock | Approve Trial boss scaling, reward quantities, first-clear rewards, and repeat rewards. |
| **DD-13 - Content format policy** | Resolved for production content authoring | YAML is canonical for hand-authored content by default; JSON is canonical for generated/machine-written content by default; Unity editor/build tooling validates canonical source and emits generated runtime data; each family has one canonical source format. |
| **DD-14 - Release platform lock** | Release packaging | Windows is the primary development/validation target. Decide which additional Unity platforms, if any, are part of the first public release. |
| **DD-15 - Rarity and banner result model** | M8A decision lock/M8X banner expansion | Approve SSR/SR/R rates, result-family splits, 10-pull guarantee rules, and equipment banner pool structures. |
| **DD-16 - Stats and equipment scaling** | M8X equipment snapshot implementation | Approve stat keys, scaler formulas, stacking order, caps, and snapshot calculation rules. |
| **DD-17 - Weapon progression** | M8X weapon implementation | Approve weapon level caps, EXP/ascension costs, signature restrictions, and +1 through +5 duplicate bonuses. |
| **DD-18 - Gear set and stat system** | M8X gear implementation | Approve six slots, main stat pools, substat pools, set bonuses, reroll rules, and desynthesis yields. |
| **DD-19 - Profile Shop and conversion economy** | M8X Profile Shop implementation | Approve stock, targeted dupe policy, prices, refresh rules, conversion currency rules, and profile-money sinks. |
| **DD-20 - Economy and item naming** | M8X content/UI text lock | Approve final names for direct pulls, profile money, conversion currency, reroll currency, weapon EXP tiers, weapon ascension materials, and gear terminology. |
| **DD-21 - Bloom identity and refraction premise** | Release-quality enemy, Symptom, Labyrinth art, major event, and character-writing content locks | Approve the Bloom premise, Domain refraction rules, lifecycle language, and art/writing constraints. |
| **DD-22 - Advanced character mechanics and selfish costs** | Resolved as a post-launch future gate | Reserve generic extension points for self-debt, party stances/aspects, damage-to-status conversion, per-hit reactions, and signature weapon hooks. Actual mechanics require future tasks. |
| **DD-23 - Advanced card memory, copy, and hidden-zone selection** | Resolved as a post-launch future gate | Reserve copy eligibility, copy lifetime, hidden-zone reveal/selection, copy-lineage, and copy-triggered weapon hook guardrails. Actual mechanics require future tasks. |
| **DD-24 - Collapsing Node and safe-path topology** | M3B map state, M3D validation, M3E node resolution, M3G map UI, and M4 run serialization | Approve node-scoped Collapsing Nodes, collapse-on-departure timing, safe-path loop behavior, and visibility/accessibility requirements. |
| **DD-25 - Node-primary Labyrinth topology** | M3B/M3C/M3D/M3E/M3G map work, M4 run serialization, and M7 map breadth | Approve nodes as places/consequence owners and edges as connectivity/topology only. |
| **DD-26 - Combat enemy placement and target readability** | M6 enemy content metadata, M9 final combat layout and target interaction, and M10 enemy asset validation | Approve right/right-center enemy staging, formation readability, target bounds, anchors, and focus mapping without changing authoritative enemy slot order. |
| **DD-27 - Unity runtime/presentation architecture** | Resolved for M0/M1 | Approved: Unity 6.5 Supported line (`6000.5.x`), C#, pure no-Unity engine assembly, URP 2D, uGUI runtime UI, Input System, independent actor views, and experimental CLI/Pipeline only as development automation. |
| **DD-28 - Card hand and play-threshold interaction** | Resolved for M1H/M9 polish | Approved: bottom-centred fan, hover rise, upward drag, responsive Play Area threshold, disarm on return below threshold, release-to-cast for target-complete cards, target-selection state for explicit targets, and click/keyboard cancellation parity. |
| **DD-29 - Generated art policy** | Resolved for all art milestones | Approved: AI-generated art is allowed at every stage and may be release-quality after human review; generation method is provenance, not placeholder status. |

### 1.3 Change Classification

- **Editorial:** wording, typo, heading, or link correction. Requires a worklog note only.
- **Plan-level:** task order, package split, validation command, or milestone criteria. Requires plan revision.
- **Design-level:** rules, timing, formulas, rewards, ownership, economy, UX commitments, content warnings, or progression. Requires `docs/DESIGN.md` revision first.
- **Save-affecting:** authoritative state shape, content IDs, RNG state, profile inventory, or persisted schema. Requires migration/invalidation assessment.

---

## 2. Architectural Contracts

### 2.1 Deterministic Engine Boundary

```text
Unity input gesture
  -> UI interaction state (hover / drag / armed / targeting)
  -> PlayerCommand
  -> pure engine validation/transition
  -> accepted authoritative state + gameplay events OR rejected diagnostic
  -> application/session adapter
  -> presentation-token queue
  -> Unity actor/UI/VFX/audio rendering
  -> persistence checkpoint
```

`Bloomdrawn.Engine` must compile in an Assembly Definition with **No Engine References** enabled. It may not import/reference `UnityEngine`, `UnityEditor`, `MonoBehaviour`, `ScriptableObject`, scene APIs, Input System, UI, animation, timers/frame time, storage APIs, Pipeline APIs, or presentation assets. Engine modules may reference pure content contracts/registries, deterministic utilities, and pure helpers.

No authoritative rule may read `Time`, `UnityEngine.Random`, GameObject names, scene hierarchy, Animator state, current frame, pointer position, or transient Unity instance IDs.

### 2.2 Command Result Contract

All engine command entrypoints return an explicit accepted/rejected result.

```csharp
public readonly struct CommandResult<TState, TEvent>
{
    public bool Accepted { get; }
    public TState State { get; }
    public IReadOnlyList<TEvent> Events { get; }
    public CommandRejection Rejection { get; }
}
```

Accepted results carry the next authoritative state and ordered events. Rejected results carry the unchanged state and a rejection, with an empty event list. Dedicated accepted/rejected types may replace this illustrative shape if implementation ergonomics justify it, but the semantic contract is fixed.

Rejected commands:

- mutate no state;
- consume no RNG;
- emit no gameplay events;
- may emit diagnostics outside authoritative event history.

Accepted commands resolve completely after validation. General rollback is not part of normal gameplay resolution; content operations must be total once accepted.

### 2.3 Public Command Families

Initial command families are:

- **Combat:** `PlayCard`, `CastUltimate`, `EndTurn`, `AbandonRun`.
- **Map/Run:** `MoveToNode`, `ResolveNodeChoice`, `PurchaseShopItem`, `ChooseReward`, `FinalizeRun`.
- **Profile:** `LevelCharacter`, `AscendCharacter`, `SpendDirectPull`, `StartTrial`, `CompleteTrial`, `SaveParty`, `EquipWeapon`, `LevelWeapon`, `AscendWeapon`, `EquipGear`, `EnhanceGearMainStat`, `RerollGearSubstats`, `DesynthesizeInventoryItem`, `PurchaseProfileShopOffer`.
- **System:** `LoadProfile`, `SaveProfile`, `LoadRunSave`, `SaveRunSave`, `ExportReplay`.

Internal phase advancement, enemy iteration, status timing windows, automatic Tentacle volleys, reward generation, and pity updates are private engine transitions. UI code cannot submit those as public commands.

### 2.4 Preview Contract

Preview functions reuse the same validation, formula, targeting, modifier, and conversion logic as committed resolution.

Requirements:

- previews do not mutate authoritative state;
- previews do not consume live RNG;
- random outcomes return labelled ranges or uncertainty;
- command preview and command resolution share calculation helpers;
- UI may render previews but may not reproduce gameplay calculations independently.

### 2.4.1 Future Advanced Character Guardrails

Future character kits may propose selfish costs, Domain engine transformations, party-level stances/aspects, per-hit reaction loops, or party-scoped Domain-resource reaction/amplifier systems only through the approved DD-22 future gate and a dedicated post-launch implementation task.

The launch implementation should avoid closing these doors by:

- representing damage and HP-loss outcomes with source metadata: command ID, owner, source kind, Shield absorbed, and HP damage dealt;
- keeping status target scope and persistence scope explicit, even when launch statuses are combat-scoped;
- expressing Abyss automatic volleys and immediate volleys as typed Domain operations rather than production-character branches;
- supporting reaction guards such as per-command caps, source-kind exclusions, and recursion prevention when those operations are later approved;
- preserving generic Domain-resource events for successful generation and consumption;
- keeping resource activity ledgers explicit when future tasks add first-builder, first-consumer, first-Ultimate, first-Transcended-Ultimate, or per-combat consumed-resource checks;
- supporting operation-tag amplification for declared categories such as damage, Shield, healing, and fixed offensive status stacks;
- leaving run-persistent self-debt, delayed debt conversion, Tide/Aspect-style state, Womb-Sea-style Abyss replacement, Mother's Pearl-style resource amplifiers, and advanced signature-weapon stance/resource/Ultimate triggers out of launch production content.

### 2.4.2 Future Card Memory and Copy Guardrails

Future character kits may propose card-memory structures, owner-preserving temporary copies, hidden-zone reveal/selection, and copy-triggered weapon hooks only through the approved DD-23 future gate and a dedicated post-launch implementation task.

The launch implementation should avoid closing these doors by:

- keeping card instance ownership, pile, generated/combat-scoped flags, base cost, current cost modifiers, and spent once-per-instance flags explicit;
- treating copied cards as combat-scoped by default unless a future approved rule defines run-scoped persistence and migrations;
- preserving the invariant that Transcend resolves at most once per owner per combat;
- keeping Draw order hidden from previews and rejected commands;
- leaving Pattern, Recollection, Reprise, Wing, Fleeting, Foregone Hour, copy-lineage, and hidden Draw selection systems out of launch production content.
- treating visible hand-only copies as DD-23 runtime copy effects even when they do not use hidden-zone selection.

Default future copy safety:

- allowed sources are Hand, Draw, Discard, and approved stored snapshots;
- blocked by default are Transcend, Graveyard, Exhaust, generated, combat-scoped, Curse, Symptom, X-cost, existing copy cards, consumed once-per-instance cards, and `copyProhibited`;
- combat-scoped copies must be removed from all piles at combat end;
- safe hand-copy effects may further require a starting-deck source in Hand, source owner and upgrade ID preservation, printed-base-cost discounts, no temporary modifier or spent-flag inheritance, Exhaust, `copyProhibited`, and deterministic overflow to Draw.

### 2.5 Content Registry Contract

The content registry is built before gameplay. It validates and indexes content by stable ID.

Registry responsibilities:

- load JSON/YAML content files;
- validate each content family against schemas;
- build typed lookup indexes;
- validate cross-references;
- compute content version/hash;
- expose dependency and validation diagnostics;
- fail fast on invalid production content.

Adding a production character, card, enemy, encounter, Trial, reward table, banner pool, weapon, gear set, Profile Shop offer, level cap table, or map motif should primarily be a content-data change.

### 2.6 Presentation Contract

- The Unity battlefield uses independent actor roots for every party member and independently targetable enemy. A production party/enemy formation may not be flattened into one composite render.
- World/stage presentation uses URP 2D, SpriteRenderer/sorting, actor anchors, and approved animation/VFX components; gameplay-critical target identity remains separate from decorative background art.
- Runtime UI uses uGUI/TextMesh Pro initially. Combat cards are production-shaped UI prefabs bound to authoritative card instances through stable IDs.
- The hand is bottom-centred and uses a deterministic fan layout derived from current hand order. Presentation may never persist ad-hoc dragged transforms as the next resting layout.
- Dragging uses a dedicated interaction/drag layer and a responsive Play Area region. Coordinate conversion/reparenting must preserve screen position and must not make cards jump or leave usable bounds.
- Crossing the Play Area arms a card; returning below it disarms. Releasing armed target-complete cards submits the command; explicit-target cards enter target selection and submit only after a legal target is chosen.
- `CombatPresenter` maps ordered engine events to presentation tokens and actor/UI reactions. Presentation does not mutate authoritative state.
- Input is disabled/ignored only where accepted gameplay events are intentionally resolving or simultaneous commands would violate state consistency; commands are not invisibly queued.
- Every required presentation token has a safe fallback binding so missing optional animation/VFX cannot block gameplay.
- Generated art is permitted throughout development and may bind to production content as soon as its schema/reference contract exists; generation method does not imply placeholder status.

## 3. Repository and Unity Assembly Layout

### 3.1 Intended Layout

```text
Assets/
  Bloomdrawn/
    Engine/
      Combat/
      Cards/
      Status/
      Map/
      Rewards/
      Gacha/
      Profile/
      Equipment/
      Trials/
      Rng/
      Save/
      Bloomdrawn.Engine.asmdef

    Content/
      Runtime/
      Validation/
      Generated/
      Bloomdrawn.Content.asmdef

    Application/
      Sessions/
      Persistence/
      Bootstrap/
      Bloomdrawn.Application.asmdef

    Presentation/
      Combat/
        Actors/
        Cards/
        HUD/
        Targeting/
        VFX/
      Map/
      MetaUI/
      Audio/
      Assets/
      Bloomdrawn.Presentation.asmdef

    Editor/
      ContentImport/
      PipelineCommands/
      BuildTools/
      DeveloperWindows/
      Bloomdrawn.Editor.asmdef

    Tests/
      EditMode/
      PlayMode/
      Fixtures/
      Golden/
      Simulation/

GameContent/
  production/
  fixtures/
  generated/

ProjectSettings/
Packages/
Tools/
plans/
```

Exact folder names may be refined in M0A, but dependency direction and fixture separation are contractual.

### 3.2 Assembly and Package Boundaries

- `Bloomdrawn.Engine` contains deterministic rules and has `noEngineReferences` enabled.
- `Bloomdrawn.Content` contains pure content contracts/runtime registry interfaces and validation data models; Unity-specific import tooling belongs in `Bloomdrawn.Editor`.
- `Bloomdrawn.Application` bridges repositories and authoritative sessions; it may reference Engine/Content but not Editor tooling.
- `Bloomdrawn.Presentation` contains Unity actors, uGUI, SpriteRenderer/URP bindings, animation, VFX, audio, and user input adaptation; it may reference Application/Engine/Content but never the reverse.
- `Bloomdrawn.Editor` contains custom importers/validators, Pipeline `[CliCommand]` tools, build helpers, and developer-only utilities and must not enter production Player logic.
- Tests are split between Edit Mode and Play Mode assemblies and may use isolated fixture registries.
- Persistence access goes through repository interfaces. UI components and engine modules must not read/write save files directly.

### 3.3 Naming and ID Rules

- Definition IDs are stable kebab-case strings, scoped by family where useful, such as `character.mara` or `card.mara.incise`.
- Runtime instance IDs are distinct from definition IDs and from Unity instance IDs.
- Content versions are explicit.
- Display names are content/localization fields, not IDs.
- Production content may not depend on filename-derived, prefab-name-derived, or GameObject-name-derived implicit IDs.
- Scene/prefab object names are presentation/debug conveniences only.

### 3.4 Non-Production Fixture Lifecycle

- Fixture definitions use an explicit non-production ID namespace and marker.
- Fixture content is loaded only from isolated test/development sources, never by an undifferentiated production registry.
- Engine, UI, persistence, and presentation code may not branch on fixture definition IDs.
- Production profiles, saves, banners, Shops, inventories, equipment snapshots, presentation catalogs, and release registries may not reference fixture IDs.
- A milestone that replaces a runtime fixture with production content must define its cutover and retirement step explicitly.
- Retired runtime fixtures may remain under `Assets/Bloomdrawn/Tests/Fixtures` or `GameContent/fixtures` when they provide stable regression coverage without coupling tests to production tuning.

## 4. Testing and Verification Contract

### 4.1 Required Test Layers

1. **Edit Mode unit tests:** formulas, piles, status timing, resources, targeting, RNG, saves, gacha/pity helpers, equipment stat calculation, profile-shop transactions, content parsing/validation, and pure geometry.
2. **State-machine tests:** legal/illegal commands, phase transitions, command rejection, terminal states, and rejected-command RNG invariants.
3. **Content/import validation:** canonical YAML/JSON, IDs, formulas, references, presentation asset IDs, Trial tables, banner pools, rarity tables, weapon definitions, gear sets/stat pools, Profile Shop offers, motifs, and generated runtime registry hashes.
4. **Seed/property stress tests:** map reachability, no softlocks, resource non-negativity, deterministic command outcomes, and content generation across large fixed seed ranges.
5. **Golden deterministic tests:** fixed content + seed + commands produce a semantic event trace and checksum independent of frame rate/presentation.
6. **Play Mode integration tests:** card fan layout, hover/select, drag threshold/return, target selection, actor binding, preview parity, event presentation order, map traversal, and representative meta flows.
7. **Built-player/E2E checks:** profile creation through run completion, save/reload, direct pull, character leveling, Trial reward persistence, equipment loadout snapshot, gear reroll/desynthesis, and Profile Shop purchase where the milestone owns those systems.
8. **Visual/layout evidence:** automated/manual screenshots across declared aspect ratios may support review, but they never replace functional assertions.

### 4.2 Stable Validation Entry Points

M0A/M0F create project-owned PowerShell wrappers so agents and humans do not have to remember raw Unity Editor batch-mode syntax:

```text
Tools/validate.ps1
Tools/test-editmode.ps1
Tools/test-playmode.ps1
Tools/build-smoke.ps1
Tools/simulate.ps1
```

The scripts must resolve/use the Editor version declared by the project, return non-zero on failure, and produce concise logs/results. Exact internal Unity invocation is implementation detail and may change with Editor/CLI versions.

Unity CLI/Pipeline project commands supplement rather than replace these scripts. At minimum M0F should expose project health and content validation through the connected Editor. Later milestones add combat/map-specific commands.

### 4.3 Non-Weakenable Discipline

- Failing deterministic tests block task completion.
- Failing content validation blocks task completion.
- Failing required Edit Mode/Play Mode tests block task completion.
- Production gameplay content hardcoded outside validated content files blocks task completion.
- A task may add tests or fixtures, but may not weaken invariants to make implementation pass.
- Any preview/resolution mismatch is treated as an engine or adapter defect, not as cosmetic UI drift.
- Any card drag/layout defect that can lose a card off-screen, accumulate resting-position drift, submit an unintended command, or mutate state before acceptance blocks the relevant combat milestone.
- Agent verification must include actual test/Editor/runtime evidence where available; compilation alone is not proof of correct Unity behaviour.

## 5. Corrected Dependency Graph

```text
M0 -> M1 -> M2 -> M3 -> M4
             |     |     |
             |     |     -> M8
             |     -> M6 -> M7 -> M10
             -> M5 -> M6 -> M9 -> M10
M4 -> M8 -> M8T -> M8X
M5 -> M8
M6 -> M8T
M7/M8/M8T/M8X/M9/M10 -> M11
```

Notes:

- M1 owns the permanent independent-actor, card-hand, Play Area, target-selection, and initial presentation-token architecture. M9/M10 polish/close those systems rather than replacing them wholesale.
- M8T is the implementation-plan task phase for Trials, corresponding to the `docs/DESIGN.md` launch milestone labelled `M8B - Trials`. The distinct `M8T` ID prevents duplicate task IDs.
- M2 must not author production character content before M2A locks the production schema using the approved DD-02, DD-03, and DD-13 decisions.
- M3/M4 may prove reward transactions only with isolated non-production tables; production quantities remain blocked by DD-10.
- M6 must retire M3 runtime content fixtures when production run content replaces them.
- M8 must not implement production gacha/progression state before M8A/M8B lock DD-05, DD-06, DD-07, DD-10, DD-11, and DD-15; all production content must follow the DD-13-approved format policy.
- M8T must not implement production Trial definitions or rewards before DD-10 and DD-12; all Trial content must follow the DD-13-approved format policy.
- M8X must not implement rarity/equipment/Profile Shop systems before DD-15 through DD-20 are approved.
- Future advanced character mechanics must not enter production content before a DD-22 follow-up task implements their generic engine, UI, save, and test contracts.
- Future advanced card-memory/copy mechanics must not enter production content before a DD-23 follow-up task implements copy eligibility, lifetime, hidden-information, save, UI, and test contracts.
- M4 may implement schema capability for combat saves before DD-04, but exposed UX must obey the approved checkpoint decision.
- M4 persists only systems implemented through M4; M8, M8T, and M8X own later save-schema additions and migrations.

---

# PHASE M0 - Unity Repository Contract and Foundations

## M0 Goal

Establish the Unity project, deterministic engine boundary, schema/content import discipline, test/validation entrypoints, local persistence boundary, and agent-visible Editor tooling before real gameplay content implementation.

## Task M0A - Unity Project, Assemblies, and Quality Contract

**Objective:** Create the production-shaped Unity workspace and dependency gates.

**Implementation:**

- Create the project on the Unity 6.5 Supported line and commit `ProjectSettings/ProjectVersion.txt` to pin the exact `6000.5.x` patch used by the repository.
- Set the coding baseline to Unity's documented C# 9.0 support; avoid C# 10+ constructs and avoid unsupported/caveated C# 9 features unless a task explicitly proves the workaround/toolchain need.
- Configure URP with the 2D Renderer and the baseline Windows development Build Profile/settings.
- Install/enable required first-party packages for the initial workflow, including Input System and Unity Test Framework; keep optional packages out until a task needs them.
- Create Assembly Definitions following section 3, with `Bloomdrawn.Engine` and the deterministic/runtime content assembly using No Engine References; Unity-specific import/asset glue belongs in Editor/Application/Presentation assemblies.
- Configure Git ignore/LFS policy for Unity generated directories and binary art/audio where appropriate.
- Add repository-level `AGENTS.md`/skill references separately from game design docs; agents must prefer current Unity documentation and project commands over remembered experimental CLI syntax.
- Create the `Tools/*.ps1` validation wrapper skeletons.

**Required tests:**

- project opens in the pinned Editor without compile errors;
- Engine and deterministic/runtime content assemblies cannot compile code that references `UnityEngine`/`UnityEditor`;
- Windows smoke build/bootstrap scene starts;
- Edit Mode and Play Mode test assemblies discover at least one smoke test.

**Exit criteria:**

- `Tools/validate.ps1` and `Tools/build-smoke.ps1` succeed;
- pinned Unity version and assembly dependency rules are committed;
- Engine has no Unity/presentation/persistence implementation dependency.

## Task M0B - Content Schema and Unity Import Foundation

**Objective:** Establish schema-driven content before production content exists.

**Implementation:**

- Create canonical `GameContent/production`, isolated `GameContent/fixtures`, and generated runtime-data locations.
- Implement C# content DTO/contracts and explicit validators.
- Implement YAML authoring parser/import tooling and JSON generated-artifact support according to DD-13; runtime does not need to parse YAML.
- M0B must select and pin one maintained .NET YAML parser for Editor/build tooling (for example YamlDotNet) or record an explicit equivalent; do not build a bespoke general YAML parser inside the gameplay engine. The parser dependency belongs outside `Bloomdrawn.Engine`.
- Define stable ID rules, content version fields, family discriminators, and logical presentation-asset references.
- Add sample character, card, enemy, and encounter content using non-production/sample IDs.
- Implement deterministic runtime registry generation/lookup by stable ID and content hash/version output.
- Ensure generated registry/cache artifacts are reproducible and clearly non-authoritative.

**Required tests:**

- valid sample content passes;
- duplicate IDs fail;
- missing required fields fail;
- invalid cross-reference fails;
- malformed/invalid logical presentation-reference IDs fail content validation;
- same canonical content produces the same content hash/generated registry ordering.

**Exit criteria:**

- `Tools/validate.ps1` validates sample/import content;
- no production content can enter a runtime registry without validation.

## Task M0C - Deterministic RNG

**Objective:** Implement serializable named RNG streams without Unity random state.

**Implementation:**

- Define RNG state type and deterministic next-value function in `Bloomdrawn.Engine`.
- Define authoritative named streams: `combat.shuffle`, `combat.targeting`, `enemy.intent`, `map.layout`, `map.content`, `map.nodeModifiers`, `reward`, `shop`, and `gacha`; later milestones add `profile.equipment` when that system becomes live.
- Store authoritative RNG stream state inside authoritative state.
- Add helpers to derive substreams from profile/run seeds and stable IDs.
- `UnityEngine.Random` is forbidden for authoritative gameplay. Cosmetic-only presentation randomness is separate, non-authoritative, unsaved, and unable to alter game state.

**Required tests:**

- same seed produces same sequence;
- stream advancement is isolated;
- cosmetic/presentation random calls do not affect or advance authoritative gameplay streams;
- rejected command fixture consumes no RNG.

**Exit criteria:**

- RNG state is serializable and roundtrips through the chosen save serializer.

## Task M0D - Engine Command/Event Protocol

**Objective:** Define reusable command result, rejection, and ordered gameplay-event envelopes.

**Implementation:**

- Define `CommandResult` in pure C#.
- Define accepted event envelope with stable ordering fields and runtime source/target IDs where applicable.
- Define rejected diagnostic shape.
- Create first no-op/smoke command fixture.
- Create first golden fixture format: initial state, commands, semantic events, checksum.
- Reserve semantic presentation metadata only where it is a true engine event fact; do not reference Animator states, prefabs, or VFX names.

**Required tests:**

- accepted command changes state and emits event;
- rejected command returns unchanged state and diagnostic;
- golden fixture checksum is stable;
- event ordering is independent of frame/render state.

**Exit criteria:**

- fixed-seed command smoke test passes in Edit Mode.

## Task M0E - Save Envelope and Repository Interfaces

**Objective:** Establish persistence contracts without committing future systems prematurely.

**Implementation:**

- Define save envelope fields: `saveSchemaVersion`, `engineVersion`, `contentVersion`, checksum, payload.
- Define profile/run repository interfaces in non-UI application code.
- Implement in-memory repository for tests.
- Implement minimal local file repository under `Application.persistentDataPath` sufficient for smoke roundtrip, with temp-write/replace and previous-valid fallback behaviour.
- Add save validation before load.
- Save DTOs may contain stable IDs/data only; no Unity object references, scene paths, or instance IDs.

**Required tests:**

- save envelope validates;
- incompatible schema rejects;
- in-memory repository saves/loads profile and run payload;
- local file smoke roundtrip works;
- interrupted/invalid replacement preserves or recovers the last valid snapshot in the tested failure case.

**Exit criteria:**

- persistence consumers use repository interfaces only.

## Task M0F - Bootstrap Scene and Agent/Editor Tooling

**Objective:** Create the minimal user/developer shell and prove the Unity CLI/Pipeline feedback loop without production gameplay content.

**Implementation:**

- Create a minimal bootstrap/dev scene with Content Validation, Settings/Reduced Motion seed state, and developer status access; do not build a fake production menu hierarchy.
- Install/configure `com.unity.pipeline` for development according to the current Unity CLI/Package workflow.
- Add `Bloomdrawn.Editor` project commands for at least `bloom.health`, `bloom.validate-content`, and `bloom.scene-summary`.
- Establish the minimal typed `PresentationAssetCatalog` contract in the presentation layer: logical presentation ID -> Unity asset binding. It may begin with generic UI/fallback entries, but it must support production character/enemy/card/background bindings without changing deterministic content.
- Add Editor validation that detects duplicate catalogue IDs, wrong asset role/type, and any content reference marked required-for-current-milestone that does not resolve.
- Project commands return concise structured data where practical and never become gameplay dependencies.
- Document that `unity --help`, `unity command --help`, and live command discovery are authoritative because the CLI/Pipeline is experimental.
- Keep Pipeline runtime components out of production release Player configuration.

**Required tests:**

- bootstrap scene opens and enters Play Mode;
- project health command can identify project/editor version and registry status;
- content validation command reports success/failure correctly;
- presentation catalogue validation reports duplicate/wrong-type/unresolved required bindings;
- Editor can be queried through Pipeline without manual copy/paste from the Inspector.

**Exit criteria:**

- `Tools/validate.ps1`, Edit Mode smoke tests, Play Mode smoke tests, and `bloom.health` succeed;
- the Unity CLI/Pipeline layer can fail or change without invalidating the deterministic game architecture.

# PHASE M1 - Minimal Combat Vertical Slice and Presentation Foundation

## M1 Goal

Create playable deterministic combat with an isolated schema-authored fixture party and shared-deck basics **through the permanent production-shaped Unity combat presentation path**. M1 proves generic party construction, owner stat lookup, combat resolution, independent actor binding, battlefield composition, stable bottom-centred card-hand behaviour, drag/Play Area interaction, explicit targeting, and ordered presentation without implementing production characters or Domain mechanics.

## Task M1A - Fixture Party and Combat Setup

Implement:

- four clearly non-production fixture characters with schema-authored HP, Attack, and Defense;
- one owner-scaled Strike and one owner-scaled Shield for each fixture character;
- one exact-four fixture lineup;
- one fixture enemy and one fixture encounter;
- generic combat setup from a validated content registry plus lineup and encounter input;
- stable runtime character-owner and enemy-instance IDs unrelated to Unity instance IDs;
- shared party Max HP calculated from the four character contributions;
- owner-aware card instances and an eight-card fixture starting deck;
- encounter enemy instantiation and visible initial intent data.

Rules:

- all fixture IDs, values, references, and operation metadata live in validated non-production content;
- M1 fixture content is separate from M0B schema-validation samples;
- engine and presentation code cannot identify or special-case fixture definitions;
- changing fixture stats or card values requires no engine or UI edit;
- no Mara, Thalassa, Sephira, Azael, Domain resource, passive, ultimate, Transcend, generated-card, or production character gameplay content enters M1.

## Task M1B - Combat State Machine

Implement `COMBAT_SETUP`, `PLAYER_TURN_START`, `PLAYER_ACTION`, `PLAYER_CLEANUP`, `PLAYER_END`, `ENEMY_PHASE_START`, `ENEMY_ACTION`, `ENEMY_END`, `ROUND_END`, `VICTORY`, and `DEFEAT` in the pure engine.

Requirements:

- public commands only during legal phases;
- terminal state rejects normal combat commands;
- phase transitions are internal engine operations;
- no state transition depends on frame time or scene state.

## Task M1C - Card Instances and Piles

Implement runtime card instances with owner ID, definition ID, pile, base cost, current cost modifiers, tags, and generated/combat-scoped flags.

Piles:

- draw;
- hand;
- discard;
- Graveyard;
- resolving.

Rules:

- draw to hand target 5;
- max hand 10;
- retained cards count toward target;
- reshuffle discard only when draw pile cannot satisfy draw request.

## Task M1D - Mana and Card Play

Implement:

- base max Mana 6;
- final cost minimum 0;
- validation for phase, hand location, owner, target, cost, and card-specific preconditions;
- no partial mutation on rejected command;
- hover/selection/drag/armed/target-selection presentation state is not represented as authoritative combat mutation;
- a `PlayCard` command is submitted only after all required target choices are complete.

## Task M1E - Damage, Shield, and Healing

Implement:

- shared party HP;
- ordinary party Shield;
- enemy HP/Shield;
- generic owner stat lookup and formula evaluator foundation;
- owner-Attack Strike damage and owner-Defense Shield gain;
- healing capped by max HP;
- HP-loss cost distinction from damage;
- Atomic Stop terminal checks after damage, healing, HP-loss, status, or other terminal-capable atomic effects.

## Task M1F - Enemy Intent and Sequential Actions

Implement:

- visible attack intent data;
- stable enemy slot ordering;
- sequential enemy action resolution;
- intent regeneration after enemy end;
- event metadata sufficient for the presentation adapter to animate one enemy action at a time without redefining slot order.

## Task M1G - Combat Stage and Independent Actor Views

Implement the permanent combat scene/presentation skeleton:

- orthographic 2D combat stage using URP 2D;
- `PartyFormationView` with four independent `CombatActorView` roots arranged diagonally on the left/lower-left;
- `EnemyFormationView` with independent targetable enemy actor roots on the right/right-center;
- every actor has a visual root, target/selection bounds or anchor, UI/status anchor, and VFX anchor;
- no composite party render and no composite multi-target enemy render in the gameplay actor layer;
- compact portrait/HUD region upper-left, fixture Domain area reserved beneath it, shared survival bar below the party, Mana lower-left, enemy intent anchor near each enemy, hand-safe area bottom-centre;
- background is a separate decorative layer and cannot own targetability or critical labels;
- generic fixture/fallback visuals are allowed; generated art is allowed, but M1 visuals remain explicitly non-production content unless they belong only to generic UI chrome.

Layout requirements:

- implement the reference information relationship from `docs/DESIGN.md` §8.10 using anchors/constraints rather than fixed transforms copied from one screenshot;
- use one screen-space uGUI combat-canvas scale strategy for HUD/cards/target overlays; record the chosen `CanvasScaler` reference resolution/match policy in the task plan and verify it rather than relying on Editor Game-view coincidence;
- actors and HUD remain readable at required M1 reference aspect ratios (minimum 16:9 and 16:10; include one ultrawide check);
- the bottom-centred hand, party/shared-survival lane, enemy target lane, and End Turn control have explicit non-collision/safe-zone assertions;
- actor overlap cannot make independently targetable enemies ambiguous;
- no large opaque party/enemy composite panel is used to simulate actor presence;
- the Combat Log is collapsed/minimal by default or overlays without permanently consuming the right-side enemy stage.

## Task M1H - Bottom-Centred Card Fan, Drag Play Area, and Targeting

Implement the production-shaped card interaction system in uGUI/Input System.

Resting hand:

- anchored/centred at the bottom of the combat canvas;
- custom deterministic fan layout computes card position, rotation, overlap, scale/depth from authoritative hand order;
- layout recomputes after hand mutations and after every drag/cancel; it never adopts accumulated dragged transforms as new rest positions;
- hover/focus raises the card and keeps it above neighbours without changing hand order.

Drag state machine:

1. pointer-down captures one card interaction session;
2. card moves in a dedicated drag/interaction layer while preserving its screen position when reparented;
3. drag uses `RectTransformUtility.ScreenPointToLocalPointInRectangle` or an equivalent tested screen-to-local conversion against the correct drag canvas/camera rather than mixing world/screen/local coordinates;
4. crossing the responsive `PlayArea` threshold arms the card and shows a non-colour-only indicator;
5. moving back below the threshold disarms it;
6. release while disarmed/cancelled animates back into the current fan;
7. release while armed:
   - target-complete card submits the command immediately;
   - explicit-target card enters target-selection state with the card visibly staged above the hand and legal targets highlighted;
8. clicking a legal target submits the complete command; Escape/right-click/cancel returns the card to hand without cost;
9. rejected command presentation resynchronizes to authoritative hand state.

Stability requirements:

- only one drag/target session owns interaction at once;
- dragged card remains within safe usable visual bounds and cannot disappear because of canvas parenting or aspect ratio;
- no duplicate card view is created by drag/reparenting;
- no pointer release below the Play Area submits a command;
- no armed explicit-target card consumes Mana before target selection/engine acceptance;
- click and keyboard flows reach the same interaction states without requiring drag.

## Task M1I - Initial Sequential Presentation Adapter

Implement:

- `GameEvent -> PresentationToken` mapping for M1 events;
- actor lookup by stable runtime instance ID;
- fixture/fallback token bindings for card play, owner act acknowledgement, damage, Shield gain, enemy action, hit reaction, victory, and defeat;
- simple idle/act/hit/return transforms/Animator clips sufficient to prove independent actors;
- presentation lock and completion signalling;
- animation speed and reduced-motion hooks at a basic level;
- no authoritative state mutation in presentation.

Add agent-facing development commands where useful:

- `bloom.load-combat-fixture`;
- `bloom.reset-combat-fixture`;
- `bloom.dump-combat-state`;
- `bloom.validate-combat-layout`.

## Task M1J - Golden Combat Replay and Play Mode Interaction Gate

Implement replay fixture support:

- initial state;
- seed/RNG streams;
- command list;
- semantic event trace;
- final checksum;
- Atomic Stop terminal timing evidence.

Play Mode coverage must include:

- five-card fan centring;
- repeated hover/drag/cancel cycles with no drift;
- drag above threshold then back below and release -> no play;
- armed target-complete release -> accepted play;
- armed explicit-target release -> target-selection state -> target click -> accepted play;
- target-selection cancel -> no mutation;
- card view remains in usable bounds at 16:9, 16:10, and one ultrawide reference resolution/aspect;
- independent enemy target selection and sequential enemy action presentation.

**M1 exit criteria:**

- one complete combat can be played through the real Unity combat scene;
- the same combat can be reproduced headlessly/Edit Mode from the same registry-derived setup;
- every runtime card has a valid owner character instance;
- Strike and Shield resolve from the owning fixture character's stats;
- changing fixture definitions requires no engine or UI edit;
- rejected commands and cancelled drag/target sessions consume no RNG and emit no gameplay events;
- repeated interaction cannot drift/fan cards off-screen or submit unintended plays;
- party members and targetable enemies are independently addressable actor views;
- no production character definition or Domain mechanic is implemented.

# PHASE M2 - One Character Per Domain

## M2 Goal

Implement Mara, Thalassa, Sephira, and Azael as schema-authored content using generic engine operations.

M1 already owns generic lineup construction, runtime ownership, shared HP construction, owner stat lookup, and basic owner-scaled Strike/Shield formulas. M2 replaces the runtime fixture party and adds production starter definitions, Domain resources, passives, ultimates, Transcend, generated cards, statuses, and Domain presentation.

## Task M2A - Production Character Schema and Decision Lock

DD-02, DD-03, and DD-13 are approved. M2A now defines the production schema/content contract before any production character enters normal runtime registries.

Complete and validate the production contract for:

- character identity, Domain, acquisition, stats, passive, ultimate, Transcend, five-card set, and generated cards;
- typed formula and effect-operation references required by the starter kits;
- stable character/card IDs, content versions, cross-references, and runtime ownership;
- apparent adult age band, gameplay-scale silhouette, costume structure, palette, concrete material/anatomy cues, horror motif, pose language, content warnings, and portrait/combat-sprite/ultimate-VFX references;
- explicit review/readiness status for presentation fields; generated art is permitted and is not automatically placeholder content.
- logical presentation asset IDs that resolve through the Unity presentation catalog rather than direct gameplay references to prefab/filename paths.

No production character may enter the normal registry before this contract and its gates pass.

## Task M2B - Domain Resource Engine

Implement:

- Flesh Embryo;
- Abyss Tentacles/Potency;
- Spirit Essence;
- Void rule modifiers and control/economy hooks.

Resources persist/reset according to `docs/DESIGN.md`.

Represent Domain automatic effects and modifiers through generic typed operations. Do not special-case a production character when initializing, previewing, suppressing, or resolving a Domain resource effect.

## Task M2C - Flesh / Mara

Implement generic operations required by Mara:

- build Embryo;
- enhanced branch with resource requirement;
- consume Embryo only when enhanced branch resolves;
- Bleed;
- healing from passive;
- generated card in hand.

Content definitions:

- Mara stats;
- passive;
- ultimate;
- Transcend;
- five-card set;
- `Graft: Hepatic`.

## Task M2D - Abyss / Thalassa

Implement:

- add Tentacles;
- add Potency;
- automatic ordinary player-end volley;
- immediate volley;
- overflow targeting by lowest current HP;
- Drown.

The launch Abyss implementation must remain the simple Tentacles/Potency engine in `docs/DESIGN.md`. Do not implement run-persistent Sacrifice, Tide Aspects, Womb-Sea replacement, or per-hit Tentacle reactions during M2; keep event/source metadata sufficient for a later DD-22 follow-up task to add those systems generically.

Content definitions:

- Thalassa stats;
- passive;
- ultimate;
- Transcend;
- five-card set;
- `Undertow`.

## Task M2E - Spirit / Sephira

Implement:

- Essence;
- Retain;
- Ritual held-cost reduction at ordinary cleanup;
- held-cost reset when leaving hand;
- Essence-scaling formulas.

Content definitions:

- Sephira stats;
- passive;
- ultimate;
- Transcend;
- five-card set;
- `Kyrie`.

## Task M2F - Void / Azael

Implement:

- Mana Debt;
- Threshold Mana;
- Delay;
- Falter conversion under Control Resistance;
- choose up to two cards in hand and set cost to 0 for current action phase or until leaving hand;
- Azael ultimate `Still the Hinge`.

Forbidden:

- no invisible queued actions.

Content definitions:

- Azael stats;
- passive;
- ultimate;
- Transcend;
- five-card set;
- `Lag`.

## Task M2G - Transcend and Ultimates

Implement:

- owner ultimate gauge;
- gauge cap;
- once-per-combat Transcend flag;
- Graveyard movement;
- Transcended ultimate substitution;
- generated combat-scoped cards.

## Task M2H - Four-Domain Integration UI and Starter Actor Binding

Implement:

- Domain helper in the reserved upper-left party-resource area;
- owner badges on cards;
- resource counters;
- target/resource previews;
- ultimate readiness UI;
- bind Mara, Thalassa, Sephira, and Azael to independent production actor views through logical asset references resolved by the M0F `PresentationAssetCatalog`;
- generated/reviewed portraits and combat art may be used immediately when available; generated provenance does not downgrade them to placeholder status;
- basic idle/act/hit/return presentation for each starter character using generic token/actor contracts, not character-ID branches;
- Domain-resource changes animate through presentation tokens without delaying or altering authoritative state.

## Task M2I - Production Starter Cutover and Fixture Retirement

Implement:

- switch normal runtime content sources to Mara, Thalassa, Sephira, and Azael;
- remove M1 fixture characters and cards from app/runtime bundles, developer-facing registries, and selectable developer profiles;
- retain only minimal fixture equivalents under dedicated `GameContent/fixtures` / test sources where production-independent regression coverage is valuable;
- regenerate or replace M1 golden combat evidence if its content source or canonical trace changes, without weakening deterministic assertions;
- reject fixture character/card IDs from production profiles, saves, runtime registries, and active-run payloads;
- add release-oriented scans proving normal application startup cannot load the retired M1 fixture party.

**M2 exit criteria:**

- starter party plays through combat using all four Domain engines through the same M1 actor/card/presentation architecture;
- no production character-specific engine/UI branches;
- all starter content loads from schemas;
- the application and production-facing registries contain no M1 fixture character or card definitions;
- generic combat regression tests remain independent of production balance tuning.

---

# PHASE M3 - Minimal Labyrinth and Run Loop

## M3 Goal

Implement map traversal, canonical temptation loop, finite Obols, persistent Shops, and a minimal boss route using isolated validated non-production run content.

## Task M3A - Non-Production Run Content Contract

Define an isolated validated development content source containing:

- one exact-four production starter lineup reference;
- one normal combat reward table and deterministic reward grant;
- one premium Shop offer and one purchasable keepsake;
- one Symptom with an explicit owner-assignment rule;
- one Boss encounter reference;
- the minimum node/content-slot references needed by the canonical temptation loop.

Rules:

- values and quantities are non-production fixtures and do not resolve DD-10;
- definitions use explicit non-production markers and may appear only in M3/M4 development registries and development saves;
- fixture-bearing saves are not release-compatible and must be migrated, invalidated, or retained only as test fixtures during the M6 cutover;
- engine/UI code cannot branch on fixture IDs;
- M6 replaces these runtime fixtures with production content while preserving only isolated test equivalents.

## Task M3B - Axial Hex, Node State, and Edge Connectivity Model

Implement:

- axial coordinates;
- node instance IDs;
- edge instance IDs;
- traversal edges as connectivity/topology only;
- Collapsing Node lifecycle state: `intact`, `occupied/armed`, and `collapsed`;
- current map position.

Rules:

- nodes own player-facing content, consequences, lifecycle state, previews, and one-time resolution rules;
- edges own connected/unconnected, traversable/blocked, motif anchors, pathfinding, and validation structure;
- M3 Collapsing Nodes are travel-only bypass nodes, not hazards on traversal edges;
- entering a Collapsing Node does not collapse it;
- accepted movement away from a Collapsing Node collapses it regardless of destination;
- rejected or cancelled movement leaves the node intact and consumes no state or RNG;
- save/reload while standing on an intact Collapsing Node preserves the occupied/armed state.

## Task M3C - Motif Content Schemas

Implement non-production motif schemas and sample motifs for:

- motif ID/version;
- local motif node keys and local axial coordinates;
- node kinds and validated content slot references;
- connectivity-only internal edges;
- anchors;
- role tags;
- lifecycle/modifier constraints;
- stable instance ID derivation from motif instance ID plus local node/edge key.

M3 motif placement is translation-only. Arbitrary rotation and reflection are deferred to M7 unless a later approved task explicitly scopes them.

Extend the M3 fixture content contract with explicit non-production `fixture.m3.*` Normal Combat and Boss encounter references before M3E can launch real map combat. These references remain fixture-only and do not create release enemy, encounter, or Boss content.

Create sample Start, spine, side loop, shop loop, and boss approach motifs.

## Task M3D - Stitching and Validation

Implement seeded motif placement with bounded deterministic backtracking.

RNG ownership:

- `map.layout` drives motif choice, placement order, and backtracking choices;
- `map.content` is used only when selecting among content-slot variants;
- `map.nodeModifiers` is used only for node-owned modifier or lifecycle variants;
- failed stitching returns deterministic diagnostics instead of silently falling back to hand-authored topology.

Validation:

- no overlap;
- physical adjacency;
- Start reaches Boss;
- node consequences cannot be authored on edges;
- Collapsing Node use cannot orphan the Boss route;
- side loops have distinct gates;
- side loops that claim Shop revisitability remain revisitable after every legal sequence of Collapsing Node use and Symptom resolution;
- M3 Collapsing Nodes are degree-2 gate/bypass nodes by default;
- node distribution bounds;
- legal Collapsing Node/Symptom route states are enumerated, not checked only as a static graph;
- a canonical safe-path fixture/topology matches the behavior shown in `plans/reference/collapsing-node-safe-path-reference.png`.

## Task M3E - Node Resolution

Implement first-pass movement and node-entry resolution for:

- Travel;
- Collapsing Node travel and collapse-on-departure;
- Normal Combat;
- Shop;
- Symptom;
- Boss.

Destination previews and confirmations must resolve before departure collapse commits. Previewing a route from a Collapsing Node shows the impending collapse but does not mutate state; accepted movement collapses the prior node before or alongside the destination consequence without ever stranding the party on a collapsed node.

M3E owns movement preview, accepted movement, destination confirmation, Collapsing Node departure collapse, first-pass visit/spent/cleared state, and combat launch from explicit non-production Normal/Boss encounter references.

M3E may create pending node interactions for Shop, Symptom, and reward-bearing combat results, but it must not grant Obols, seed Shop stock, sell Shop items, add keepsakes, add Symptom effects, or claim rewards. Those economy and transaction commits belong to M3F.

M3 movement resolves destination node consequences and prior-node lifecycle effects only. Edges may block or permit movement, but they must not own rewards, costs, Symptoms, Shops, Events, Corrupted effects, or other run consequences.

Later node families remain absent until their implementing milestone; do not reserve speculative runtime payloads.

## Task M3F - Obols, Shop State, and Reward Transactions

Implement:

- run-scoped Obols starting at zero;
- fixed M3 fixture reward grants after Normal Combat victory;
- first-clear reward claiming and no-repeat reward state;
- seeded Shop stock and first-visit reveal state;
- prices, affordability, purchase, and sold-out state;
- premium item can be unaffordable on first visit;
- keepsake acquisition as a no-op runtime fixture item;
- Symptom acceptance as an explicit M3 ledger/deck-pressure marker while the fixture runtime effect remains placeholder;
- generic validated reward selection and grant transactions;
- rejected purchases or reward claims consume no currency, item, state, or RNG;
- rejected Symptom interactions consume no state or RNG;
- non-production reward quantities loaded from the M3A fixture source.

## Task M3G - Unity 2D Map UI

Implement a derived Unity 2D map renderer:

- individual node views at engine-provided flat-top axial positions;
- connector/edge views generated only from authoritative engine edges;
- current node;
- reachable nodes;
- spent nodes;
- collapsed Collapsing Nodes;
- selected node preview;
- destination node consequence, prior-node lifecycle, and connectivity preview.

The renderer may use SpriteRenderer, LineRenderer, generated mesh, Grid/Tilemap helpers, or equivalent Unity 2D primitives, but those primitives never own traversal rules. The map UI consumes authoritative reachability and preview output and must not duplicate collapse timing, Shop state, reward state, or node-consequence logic.

Collapsed nodes remain visible as disabled, ruined, faded, locked-out, or equivalent. Icon, label, texture, tooltip, or shape treatment must carry the meaning so the state is not colour-only.

## Task M3H - Temptation Loop E2E

Implement E2E scenario:

1. discover unaffordable Shop item;
2. leave through route;
3. earn Obols;
4. choose between accepting the Symptom route or spending the Collapsing Node bypass;
5. return;
6. purchase item;
7. reach Boss.

The E2E must cover the reference safe-path behavior shown in `plans/reference/collapsing-node-safe-path-reference.png`: entering a Collapsing Node causes no immediate mutation, leaving it collapses that node, and a later return to the Shop may be forced through the once-triggered Symptom route.

The primary E2E covers the Collapsing-first route. Engine or integration tests must also cover the Symptom-first route plus preview/cancel no-mutation cases for Symptom or Boss confirmation while standing on a Collapsing Node.

**M3 exit criteria:**

- canonical temptation loop is playable and deterministic from seed;
- map, Shop, node, reward, and mutation state are canonically serializable for M4 without implementing repository save/reload in M3;
- no production reward quantity or M3 fixture ID is hardcoded in engine or UI code.

---

# PHASE M4 - Run Completion and Persistence

## M4 Goal

Complete run lifecycle and persistent local profile inventory for systems that exist through M4. Do not reserve empty banner, Trial, equipment, profile-level, duplicate, or progression sections for later milestones.

## Task M4A - Canonical Run Save

Save:

- run seed and difficulty;
- RNG streams;
- party snapshot;
- map graph/mutations;
- Shop state;
- run deck/card instances;
- current HP;
- Obols;
- queued bankable rewards.

## Task M4B - Profile Persistence

Persist:

- profile ID/name;
- owned production character IDs;
- saved parties;
- generic validated inventory quantities for reward families implemented through M4;
- reward ledger and run history needed to audit banking/finalization.

Later M8, M8T, and M8X profile sections require explicit save-schema changes, migrations, and compatibility tests when those systems become live.

## Task M4C - Reward Banking

Bank:

- declared profile-scoped rewards from validated M3/M4 fixture tables;
- generic inventory quantities and reward-ledger entries transactionally.

Never bank:

- Obols;
- run cards;
- run keepsakes;
- Symptoms/Curses;
- run HP/map state after finalization.

## Task M4D - Save/Resume UX

Expose only approved checkpoints from DD-04.

Until DD-04 is approved, implement safe schema support but expose:

- map movement completion;
- node consequence commit;
- Shop purchase;
- reward choice;
- stable ordinary player-action boundary if enabled by task scope.

## Task M4E - Run Finalization

Implement:

- victory;
- defeat;
- abandon confirmation;
- run history entry;
- active run clearing/finalization.

## Task M4F - Persistence E2E

E2E:

- create profile;
- start run;
- save/reload;
- complete run;
- verify profile inventory mutation;
- verify Obols did not persist.

**M4 exit criteria:**

- profile and active run survive reload with deterministic state intact;
- the save contains no speculative banner, Trial, equipment, profile-level, duplicate, or unused progression payloads;
- production reward quantities remain gated by DD-10.

---

# PHASE M5 - Full Launch Character Roster

## M5 Goal

Implement remaining launch characters and roster/party UI.

## Task M5A - Launch Roster Schema Completion and Validation

Extend the production character contract established in M2A; do not create a parallel character schema.

Validate:

- all eight characters;
- 40 starting cards;
- generated Transcend cards;
- passives;
- ultimates;
- apparent adult age band and gameplay-scale silhouette;
- costume structure, palette, concrete material/anatomy cues, horror motif, and pose language;
- content warning tags;
- portrait, combat-sprite, and ultimate-VFX references;
- placeholder/provenance/review status for presentation fields that are not release-ready.

## Task M5B - Venelis

Implement generic operations for:

- nonlethal HP-loss costs;
- scheduled Maws at ordinary player end;
- area Flesh spender payoff.

## Task M5C - Nyxalia

Implement generic operations for:

- first Abyss card discount each ordinary player action phase;
- end-phase volley damage modifier;
- Tentacle duplication cap.

## Task M5D - Kibane

Implement:

- Spirit exception that consumes Essence;
- Stun;
- ignore ordinary Shield;
- high-Essence payoff.

## Task M5E - Mira Nox

Implement:

- Vulnerable;
- Weak;
- Doom;
- removable positive status duration removal;
- unremovable/permanent trait protection.

## Task M5F - Roster and Party Screens

Implement:

- owned/unowned roster;
- character detail;
- stat/card/passive/ultimate display;
- exact-four party builder;
- party order persistence.

## Task M5G - Starter and Developer Profiles

Implement:

- production starter: Mara, Thalassa, Sephira, Azael;
- banner-eligible metadata for Venelis, Nyxalia, Kibane, and Mira Nox;
- development profile owns all eight.

M5 does not assemble a versioned banner pool. M8 consumes banner-eligible production metadata when its banner schema and decisions are locked.

**M5 exit criteria:**

- all eight launch characters load from content and play without production hardcoding.

---

# PHASE M6 - Encounters, Boss, Rewards, and Node Breadth

## M6 Goal

Broaden content systems beyond the minimal loop and replace the M3 runtime fixture catalog. DD-21 is approved: every release-quality enemy, event, Symptom, Curse, keepsake, and Boss brief must translate Bloom/Domain identity into concrete 2D materials, silhouette or anatomy changes, behavior or animation tells, and gameplay-readable consequences.

## Task M6A - Enemy and Intent Framework Expansion

Implement enemy schema fields:

- HP/Shield;
- intent deck/state machine;
- targeting;
- formulas;
- traits;
- Control Resistance;
- phase hooks;
- gameplay-scale silhouette and size class;
- stage footprint, readable target bounds, target marker anchor, HP/status/intent label anchors, and optional formation role;
- concrete surface/material and anatomy changes;
- Domain refraction cues;
- idle, attack, hit, and telegraph requirements;
- visible horror reveal and content warnings;
- optional Bloom lifecycle stage as guidance, never a required mechanic;
- asset provenance and review status.

## Task M6B - Normal and Elite Encounter Pools

Implement validated encounter tables with:

- biome/depth tags;
- anti-repeat constraints;
- reward tier;
- no secret party hard-countering;
- concrete environment/material brief and readable enemy grouping at combat scale;
- no independently targetable enemy grouping that creates ambiguous target bounds, labels, or focus targets.

## Task M6C - Launch Boss

Implement:

- boss phase thresholds;
- Control Resistance;
- telegraphed phase changes;
- no hidden instant-kill or unexplained immunity;
- concrete phase-by-phase silhouette/material changes and attack tells that remain visible without prose.

## Task M6D - Keepsake and Boon Hooks

Implement:

- run-scoped keepsake definitions;
- trigger timing;
- typed effect operations;
- UI status representation;
- concrete object/material motif and icon-readability brief.

## Task M6E - Rest, Event, Treasure, Symptom, and Curse Content

Implement:

- Rest options;
- Event choice schema;
- Treasure reward schema;
- at least four Symptoms;
- at least four Curses;
- content warning metadata;
- concrete visible material, body/object change, or environmental manifestation;
- Domain refraction and the explicit benefit/cost of Bloom contact where relevant;
- behavior, animation, or UI tell required to communicate the change;
- optional lifecycle-stage guidance without making lifecycle a gameplay system.

## Task M6F - Reward Pool Breadth

Block production quantities until DD-10 is approved. Use the DD-13-approved YAML/JSON format policy for any production content authored in this phase.

Implement:

- card reward pools;
- keepsake reward pools;
- profile reward grant hooks;
- content validation for reward references.

## Task M6G - Production Run Content Cutover and Fixture Retirement

Implement:

- replace M3 runtime combat rewards, Shop offer, keepsake, Symptom, Boss, and related run-content fixtures with production schema-authored definitions;
- remove M3 fixture definitions from app/runtime bundles, developer-facing registries, and production saves;
- explicitly migrate or invalidate development saves that reference retired fixture IDs rather than silently loading them as production state;
- retain minimal equivalents only under isolated testing content;
- replace or regenerate affected deterministic fixtures without weakening assertions;
- add release scans proving normal application startup cannot load retired M3 fixture IDs.

**M6 exit criteria:**

- multiple seeds generate varied playable runs with validated production content;
- release-quality content briefs are implementable as gameplay-scale 2D assets rather than abstract prose;
- production-facing registries and saves contain no M3 run-content fixture IDs.

---

# PHASE M7 - Labyrinth Generation Breadth

## M7 Goal

Expand map variety and route-economy reporting. M7 authors semantic presentation tags for later asset work; it does not create or require literal floral map art.

## Task M7A - Expanded Motif Library

Add validated motifs for:

- shop loops;
- recovery loops;
- elite branches;
- boss approaches;
- travel pacing;
- presentation tags for spine/loop metaphor, connector material, landmark role, symmetry treatment, and branching treatment.

The tags must use concrete values that M10 can map to tiles, connectors, landmarks, and effects. They may express stem, root, vein, tissue, devotional circuit, threshold, or other approved metaphors without changing topology or requiring every map to resemble a flower.

## Task M7B - Distribution and Difficulty Constraints

Implement generation constraints for:

- combat counts;
- Shop count;
- Rest count;
- Elite count;
- Symptom/Curse exposure;
- direct route viability.

## Task M7C - Reveal and Information Rules

Implement:

- topology reveal;
- node category reveal;
- Shop reveal on first visit;
- node consequence and lifecycle preview;
- Symptom card preview before traversal.

## Task M7D - Property and Route-Economy Reports

Reports:

- minimum/maximum combats;
- possible Obol totals;
- Shop affordability;
- Boss reachability under Collapsing Node use;
- node consequence exposure;
- side-loop integrity.

**M7 exit criteria:**

- generator passes large-seed validation and route-economy reports are produced;
- every production motif exposes validated presentation tags without embedding asset paths or rendering logic in map generation.

---

# PHASE M8 - Gacha and Meta Progression

## M8 Goal

Implement direct-pull gacha, character growth, profile cap, persistent inventory, and the extensible rarity/result-family foundation required before M8X equipment banners.

## Task M8A - Gacha and Progression Decision Lock

Block production M8 implementation until DD-05, DD-06, DD-07, DD-10, DD-11, and DD-15 are approved. Use the DD-13-approved YAML/JSON format policy for production content.

Deliver:

- exact pity table/formula;
- featured guarantee rule;
- first-acquisition protection;
- exact C1-C5 and post-cap duplicate outcomes;
- production reward quantities needed to earn and spend M8 items;
- profile-to-character cap bands;
- SSR/SR/R result model, result-family splits, and 10-pull guarantee precedence;
- pre-M8X banner composition before lower-rarity weapons enter the pool;
- explicit behavior of the 10-pull guarantee for that pre-M8X pool;
- required audit fields.

## Task M8B - Banner and Progression Content Schema Lock

Implement and validate production schemas/content for:

- versioned banner definitions and banner-eligible character references;
- rates, pity, featured guarantee, first-acquisition protection, result families, and 10-pull guarantee tables;
- character duplicate ladders and post-cap outcomes;
- character EXP items, Domain Sigils, ascension costs, and growth/cap tables;
- audit entry payloads and localization/display fields.

No production resolver may load incomplete or provisional banner behavior.

## Task M8C - Profile Save Migration and Gacha RNG State

Introduce the M8 profile-save version that adds:

- profile EXP and level;
- character levels, ascensions, and duplicate progress;
- character EXP items and Domain Sigils;
- direct-pull inventory;
- versioned banner pity/guarantee state;
- gacha audit history;
- named profile/banner RNG state required for exact continuation.

Provide migration from the M4 live-only profile, reject incompatible future versions, and prove active Run state is unchanged.

## Task M8D - Direct Pull Resolver

Implement:

- one direct pull consumed per pull;
- no intermediary conversion resource;
- pity state;
- guarantee state;
- result rarity and family categories;
- ten-pull SR-or-better guarantee state;
- audit log.

## Task M8E - Duplicate Ladder

Implement:

- C1-C5 structure;
- duplicate progress;
- C0 viability invariant;
- approved post-cap compensation.

## Task M8F - EXP Items and Sigils

Implement:

- three EXP item tiers;
- Domain Sigils;
- every-10-level ascension gate;
- correct Domain Sigil validation.

## Task M8G - Profile Level Roster Cap

Implement:

- profile EXP;
- profile level;
- profile-to-character-level cap table;
- cap enforcement during leveling.

## Task M8H - Banner UI

Implement:

- direct pull count;
- rates;
- rarity/result-family table;
- pity;
- guarantee;
- pool contents;
- recent audit/history.

## Task M8I - Meta E2E

E2E:

- earn direct pull;
- pull character or other approved banner result;
- receive duplicate or new character;
- level with EXP item;
- ascend with Sigil;
- enforce profile roster cap.

**M8 exit criteria:**

- direct-pull loop works without any intermediary conversion resource;
- all banner/progression behavior is schema-authored and decision-locked;
- M8 profile saves migrate from M4 and continue profile/banner RNG exactly.

---

# PHASE M8T - Trials

## M8T Goal

Implement direct boss challenges for targeted persistent rewards. This corresponds to the `docs/DESIGN.md` launch milestone labelled `M8B - Trials`; this plan uses `M8T` to keep task IDs unique.

## Task M8T-A - Trial Decision and Schema Lock

Block production Trial work until DD-10 and DD-12 are approved. Use the DD-13-approved YAML/JSON format policy for Trial content.

Implement and validate Trial definitions:

- Trial ID;
- reward family;
- difficulty tiers;
- boss/encounter reference;
- first-clear rewards;
- repeat rewards;
- unlock requirements.

## Task M8T-B - Production Trial Definitions

Implement schema-authored definitions for:

- Flesh, Abyss, Spirit, and Void Sigil Trials;
- EXP Trial with three-tier character EXP rewards;
- Money Trial with persistent general currency rewards;
- boss/encounter references, difficulty scaling, first-clear rewards, repeat rewards, and unlocks;
- validation that each Trial grants only its declared reward family.

## Task M8T-C - Trial Selection and Launch UI

Implement:

- Trial family list;
- difficulty selection;
- reward preview;
- first-clear indicator;
- repeat reward indicator.

## Task M8T-D - Trial Save Migration and Clear Persistence

Introduce the M8T profile-save version and migrate M8 profiles.

Persist:

- completed Trial family;
- highest completed difficulty;
- first-clear claimed flags;
- repeat clear history if needed for balancing/debugging.

Prove migration leaves banner state, inventory, character progression, active Run state, and RNG streams unchanged.

## Task M8T-E - Trial Reward Integration

Implement:

- transactional first-clear and repeat reward grants;
- reward-ledger/audit entries;
- no active Run mutation;
- rejected or duplicate first-clear claims consume no state, items, or RNG.

## Task M8T-F - Trial E2E

E2E:

- select Trial;
- choose difficulty;
- clear boss;
- receive targeted persistent reward;
- verify active Run state is not mutated.

**M8T exit criteria:**

- player can select a Trial difficulty, clear a production Trial, and receive targeted persistent rewards;
- M8 profiles migrate to M8T without losing banner, inventory, progression, Run, or RNG state.

---

# PHASE M8X - Equipment, Profile Shop, and Inventory Expansion

## M8X Goal

Implement rarity expansion, character weapons, six-slot gear sets, Profile Shop, conversion currency sinks, equipment inventory, and deterministic Run/Trial equipment snapshots after M8 and M8T are stable.

## Task M8X-A - Rarity and Equipment Schema Lock

Block implementation until DD-15 through DD-20 are approved, and verify the inherited DD-07 and DD-10 through DD-12 decisions needed by equipment duplicates, costs, cap bands, and Trial rewards remain approved.

Implement content schemas for:

- SSR/SR/R rarity and result-family tables;
- weapon definitions, signature links, growth, ascension, duplicate bonuses, and acquisition sources;
- gear slots, gear sets, main stat pools, substat pools, tier weights, reroll tables, and desynthesis yields;
- Profile Shop offers and purchase limits;
- inventory categories and lock/favorite eligibility.

## Task M8X-B - Equipment Save Migration and Profile RNG State

Introduce the M8X profile/run save versions and migrate M8T state.

Persist:

- owned weapon instances and duplicate bonuses;
- owned gear instances;
- equipment loadouts;
- Profile Shop state and purchase history;
- conversion and reroll currencies;
- named `profile.equipment` RNG state;
- versioned active Run/Trial equipment snapshots where applicable;
- snapshotted combat parameters from equipment, such as maximum hand size.

Prove migration preserves banner, Trial, inventory, progression, active Run, and all pre-existing RNG state exactly.

## Task M8X-C - Weapon Progression and Banner Integration

Implement:

- one weapon slot per character;
- weapon level and ascension commands;
- three weapon EXP item tiers;
- weapon ascension materials;
- duplicate bonus +0 through +5;
- excess/maxed duplicate conversion;
- banner result handling for SSR/SR/R weapons without introducing a pull conversion currency;
- generic weapon passive event hooks only when their owning gate is satisfied, such as DD-22 for Domain-resource or Ultimate-cast reactions and DD-23 for copied-card reactions.

## Task M8X-D - Gear Sets, Rerolls, and Desynthesis

Implement:

- six gear slots per character;
- 3-piece and 6-piece set bonus activation;
- main stat enhancement to +12;
- three substat slots;
- deterministic substat rerolls using `profile.equipment` RNG;
- desynthesis into declared reroll currency/material yields;
- lock/favorite protection.

## Task M8X-E - Profile Shop and Economy Sinks

Implement:

- Profile Shop content loading and stock state;
- targeted character dupe, weapon, gear, EXP, ascension, and reroll-material offers;
- purchases using persistent general currency and special conversion currency;
- profile-money costs for character, weapon, and gear upgrades;
- audit entries for irreversible shop purchases.

## Task M8X-F - Equipment Inventory and Loadout UI

Implement:

- inventory categories for currencies, materials, weapons, and gear;
- weapon and gear detail views;
- lock/favorite controls;
- equip/unequip flows from Roster and Inventory;
- desynthesis flow with yield preview;
- Profile Shop screen;
- banner result display for rarity and family.

## Task M8X-G - Equipment Snapshot E2E

E2E:

- acquire or grant weapon and gear fixtures through isolated validated test-content injection, not production catalogs or runtime registries;
- equip one weapon and six gear pieces;
- activate a 3-piece and 6-piece set bonus scenario;
- reroll gear substats;
- desynthesize eligible gear;
- buy a targeted Profile Shop offer;
- start a Run or Trial and verify the equipment snapshot remains stable after profile equipment changes.

**M8X exit criteria:**

- player can pull or acquire weapons, equip weapon and gear loadouts, upgrade equipment with persistent resources, reroll/desynth gear, purchase targeted Profile Shop items, and start a deterministic Run or Trial from a stable equipment snapshot;
- M8T profiles and active state migrate without losing pre-existing progression, Trial, banner, inventory, Run, or RNG data.

---

## Post-Launch Advanced Character Package Note

Characters with Ismera/Ismelda-like or Thaelia-like requirements are not normal content drops.

Timing guidance:

- simplified combat-only versions require at least M2 Domain combat, M4 persistence, and M6 production status/content breadth;
- Thaelia-like prototypes without card copy or signature weapon can wait until M2/M6 provide Flesh, status timing, typed operations, and production content breadth;
- banner-acquired advanced characters require M8 gacha/profile progression;
- full versions with signature weapon reactions require M8X equipment contracts;
- the preferred approach is a dedicated post-launch advanced-character slice under the approved DD-22 future gate, rather than embedding these rules into launch tasks.

Thaelia-like concepts are valid future Flesh candidates when they express Embryo acceleration, propagation, maternal possession, and pearl/cradle imagery without creating a second Embryo pool or replacing the base Flesh Domain engine. Implementation must use generic operations and content-authored hooks, never Thaelia-specific character-ID branches.

This note does not approve Sacrifice, Tide Aspects, Womb-Sea, Mother's Pearl, operation-tag resource amplification, or any future-character name. It only records the dependency shape for planning.

---

## Post-Launch Card-Memory Package Note

Characters with Moirenne/Noema-like or Thaelia-like temporary-copy requirements are not normal content drops.

Timing guidance:

- visible-zone or stored-snapshot copy systems may be simpler than hidden-zone selection, but both require a DD-23 follow-up implementation task before production;
- safe hand-only copies still require DD-23 because they create runtime card instances, preserve owners and upgrade IDs, apply copy discounts, and need deterministic overflow/cleanup;
- hidden Draw reveal/selection needs a dedicated accepted-command or pending-choice contract so rejected or cancelled commands do not leak hidden order;
- copy-triggered signature weapon effects require M8X equipment contracts plus DD-23 generic copy events;
- the preferred approach is a dedicated post-launch card-memory/copy slice rather than embedding these rules into launch tasks.

This note does not approve Pattern, Recollection, Woven Reprise, Wing cards, Foregone Hours, Last Measure, safe hand-copy Ultimates, or any future-character name. It only records the dependency shape for planning.

---

# PHASE M9 - Production Combat UI/UX Closure

## M9 Goal

Close and polish the combat interaction/presentation systems that already exist from M1/M2. DD-26 and DD-28 are approved. M9 must not replace the independent actor model, authoritative card-order model, drag Play Area state machine, or event-to-presentation architecture with unrelated shortcuts merely to obtain a different look.

## Task M9A - Final Combat Information Layout

Implement/finalize:

- compact four-portrait/ultimate cluster upper-left;
- Domain helper immediately below/adjacent to portraits;
- party actor stage left/lower-left;
- shared survival bar below party sprites;
- Mana lower-left;
- bottom-centred hand and pile controls;
- right/right-center enemy stage with target-readable formation constraints;
- enemy intent/status anchors;
- collapsible Event Log that does not permanently consume battlefield space;
- responsive safe zones for common desktop aspect ratios, including ultrawide.

## Task M9B - Card, Ultimate, and Target Interaction Polish

Polish the M1H system rather than reimplementing it:

- production fan spacing/rotation/overlap curves;
- hover/focus scale/raise timing;
- drag weighting/smoothing while preserving pointer fidelity;
- clear Play Area/armed indication;
- staged explicit-target state and legal enemy highlight;
- cancel/return animation;
- click fallback for every drag action;
- keyboard card selection/confirmation/cancel;
- target confirmation and Ultimate confirmation;
- no card-position drift, off-screen loss, coordinate jump, or accidental command submission under repeated interaction.

## Task M9C - Authoritative Preview Layer

Implement:

- damage previews;
- Shield previews;
- resource spend previews;
- control conversion previews;
- Safe/skull incoming damage;
- preview surfaces integrated into hover/selected/armed/target-selection states without duplicating formulas in UI code.

## Task M9D - Presentation Sequence Closure

Extend the M1 presenter:

- complete launch engine-event to presentation-token mapping;
- production actor/UI/VFX/audio binding contracts;
- ordered playback across multi-effect cards, Ultimates, Tentacle volleys, status ticks, sequential enemies, and terminal transitions;
- animation speed/skip controls;
- reduced-motion substitution rules;
- interruption/reload behaviour from committed authoritative state;
- safe fallback presentation for missing optional assets;
- no authoritative state mutation in presentation.

## Task M9E - Pile Viewers and Event Log

Implement:

- draw viewer with hidden order;
- discard viewer;
- Graveyard viewer;
- event log filters;
- formula detail view;
- keyboard/controller focus flow that does not disturb the active hand layout.

## Task M9F - Accessibility and Responsive Behaviour

Implement/validate:

- keyboard/controller navigation;
- accessible labels for interactive combat information;
- reduced motion;
- scalable card/tooltips;
- colourblind-safe indicators;
- 16:9, 16:10, ultrawide, and declared minimum-window layout checks;
- pointer/touch-safe target sizes if touch is included in the release platform set;
- drag is never required because click/keyboard paths remain equivalent.

## Task M9G - Production Combat E2E

Play Mode/built-player E2E:

- click play;
- drag below threshold -> cancel;
- drag above threshold -> target-complete cast;
- drag above threshold -> explicit-target state -> enemy select;
- target/cast cancellation;
- repeated drag/cancel/play stability;
- keyboard play;
- Ultimate;
- pile inspect;
- one, two, three, four-plus, and boss/add enemy formation readability;
- enemy target non-overlap and unambiguous focus/selection;
- reduced motion mode;
- required aspect-ratio matrix.

**M9 exit criteria:**

- combat is production-usable by click, drag, and keyboard/controller where required;
- card feel is stable across repeated use and declared resolutions/aspect ratios;
- enemy targets remain readable and unambiguous;
- the final layout still obeys the M1/M2 authoritative/presentation separation.

# PHASE M10 - Art, Audio, VFX, and Presentation Closure

## M10 Goal

Close the production presentation catalogue, animation/VFX/audio breadth, technical budgets, and visual consistency without sacrificing readability or deterministic gameplay. **Generated art is allowed before and during M10 and may already be release-quality.** M10 does not require replacing generated art merely because it is generated; it reviews assets by quality, coherence, provenance/rights, technical suitability, and gameplay readability.

DD-21, DD-26, and DD-29 directly govern character/enemy/map identity, target bounds, formation readability, and generated-art acceptance.

## Task M10A - Presentation Asset Catalogue, Import Rules, and Budgets

Finalize:

- finalize the M0F logical presentation asset catalogue/manifest and validation for full release breadth;
- Unity import presets/rules for character sprites, backgrounds, card art, icons, audio, and VFX textures;
- loading states and missing-asset fallbacks;
- texture/memory/build-size/performance budgets;
- source/provenance, human-review, and release-readiness status;
- generated-art provenance metadata without treating generation itself as a placeholder flag;
- release scan proving every required production logical asset ID resolves.

## Task M10B - Character and Enemy Assets

Complete/review:

- portrait assets framed for roster and detail views;
- independent full-body combat actor assets with gameplay-scale silhouette requirements;
- enemy actor assets with declared size class, stage footprint, readable target bounds, target marker anchors, HP/status/intent label anchors, and formation role support;
- boss/add compositions that keep independently targetable enemies in readable satellite lanes;
- concrete costume, surface/material, anatomy, Domain refraction, palette, and pose requirements from content briefs;
- idle, act/attack, hit, defeat, and telegraph presentation coverage where applicable;
- animation method chosen per asset: simple transform layers, Animator clips, frame animation, 2D Animation/SpriteSkin, or another approved technique; skeletal rigging is not mandatory when it adds cost without value;
- visible horror reveal that survives gameplay scale;
- content warning metadata;
- source/provenance, human-review, and release-readiness status.

## Task M10C - Card, Map, Shop, Symptom, Keepsake, and Environment Assets

Complete/review:

- card art and frame roles that remain legible behind rules text;
- map nodes, connectors, landmarks, and backgrounds mapped from M7 semantic presentation tags;
- Shop assets with inspectable item/space hierarchy;
- Symptom and Curse art with a concrete visible change plus benefit/cost readability;
- keepsake icons readable at inventory and combat-status sizes;
- combat environments with separated decorative layers so actors/targets remain independent;
- fallback assets for every required role;
- source/provenance, review, and release-readiness status.

## Task M10D - VFX and Audio

Implement/finalize production bindings for M9 presentation tokens:

- URP 2D/Shader Graph/Particle System effects as baseline, with VFX Graph only where useful and compatible;
- Flesh VFX using readable graft, petal, blood, tissue-growth, or propagation cues;
- Abyss VFX using readable pressure, tentacle, devotional ripple, lure-light, or depth cues;
- Spirit VFX using readable Essence, foxfire, halo, chimera-cohesion, memory, or ritual cues;
- Void VFX using readable threshold, seal, eclipse, debt, delay, or controlled-anomaly cues;
- full-motion and reduced-motion variants for camera movement, actor displacement, particles, and transitions;
- UI sound hooks;
- combat sound hooks;
- music/audio settings.

## Task M10E - Readability, Loading, and Performance Validation

Validate with Unity Profiler, Memory Profiler, Unity 6.5's 2D Profiler, and equivalent project instrumentation as appropriate. Use the 2D Profiler in particular to inspect sprite-rendering counts and sprite-atlas usage on representative combat scenes:

- effects do not hide intents/cards/HP;
- effects do not hide target rings, hitboxes, labels, or silhouettes for independently targetable enemies;
- enemy formation remains readable for one, two, three, four-plus, and boss/add encounter layouts;
- full and reduced-motion modes preserve ordered sequencing and attack tells;
- reduced motion removes large movement, camera shake, long particle travel, and nonessential parallax without removing mechanical information;
- horror cues remain visible at gameplay scale and do not depend only on subtle timing, reflection mismatch, animation, or prose;
- Domain identity remains distinguishable without relying on colour alone;
- target frame-time/memory baseline for the declared Windows reference hardware class;
- asset loading/import/catalog failures cannot corrupt deterministic state;
- generated and non-generated assets are judged by identical runtime quality/readability requirements.

**M10 exit criteria:**

- presentation pass does not reduce combat readability, targetability, accessibility, interaction stability, or deterministic behaviour;
- every required production presentation role resolves to a reviewed asset or explicit approved fallback.

# PHASE M11 - Balance, Reliability, and Release Gate

## M11 Goal

Validate the release candidate across determinism, schema discipline, saves, balance, accessibility, and performance.

## Task M11A - Determinism and Architecture Audit

Audit:

- engine dependencies;
- RNG streams;
- replay checksums;
- preview parity;
- no UI-side rule duplication.

## Task M11B - Hardcoding and Schema Audit

Audit:

- production content locations;
- engine/UI special cases;
- content registry coverage;
- schema validation completeness;
- non-production fixture leakage into release registries, profiles, saves, banners, Shops, inventories, snapshots, or asset manifests;
- advanced character concepts cannot enter production registries or UI routes before their DD-22 follow-up contracts are implemented;
- advanced card-memory/copy concepts cannot enter production registries or UI routes before their DD-23 follow-up contracts are implemented;
- production art briefs contain concrete gameplay-scale requirements rather than unresolved abstract direction.

## Task M11C - Save, Migration, and Corruption Audit

Audit:

- save roundtrip;
- invalid content version handling;
- failed write recovery;
- active run compatibility;
- M4 to M8, M8 to M8T, and M8T to M8X migration chains;
- retired M3 fixture-save migration/invalidation behavior.

## Task M11D - Seeded E2E Matrix

Run matrix over:

- starter party;
- alternate parties;
- multiple map seeds;
- victory;
- defeat;
- abandon;
- save/resume;
- direct pull;
- Trial rewards;
- equipment loadout snapshot;
- Profile Shop purchase;
- gear reroll/desynthesis.

## Task M11E - Domain-Aware Simulation and Tuning Reports

Reports:

- resource growth;
- damage/shield per Mana;
- route Obol economy;
- direct pulls per hour/mode;
- EXP/Sigil income;
- Trial reward efficiency;
- duplicate rate;
- weapon duplicate rate;
- gear stat distribution;
- equipment upgrade and reroll cost curves;
- Profile Shop affordability and conversion currency sinks;
- Domain underuse.

## Task M11F - Accessibility and Performance Audit

Audit:

- keyboard-only combat;
- reduced motion;
- colourblind-safe indicators;
- text scaling;
- Unity Player/URP/uGUI performance with representative combat and menu scenes;
- Unity 6.5 2D Profiler sprite-rendering and sprite-atlas behaviour;
- CPU/GPU frame timing and Canvas rebuild/batching hotspots where applicable;
- memory usage and asset residency.

## Task M11G - Release Candidate and Content Lock

Lock:

- content version;
- schema version;
- save version;
- known issues;
- release checklist.

**M11 exit criteria:**

- content lock candidate passes deterministic, schema, persistence, UI, accessibility, and performance gates.

---

## Appendix A - Global Risk Register

| Risk | Consequence | Mitigation |
|---|---|---|
| Schema bypass or hardcoding | Content becomes brittle and untestable | Content registry, hardcoding audit, M0 validation gates |
| RNG nondeterminism | Replays/saves diverge | Named streams in authoritative state, golden tests |
| Save corruption | Player loses progress | save envelopes, validation, fallback snapshots |
| Gacha trust failure | Ethical contract breaks | direct pulls, visible rates/pity, audit log |
| Equipment economy creep | Permanent power overwhelms roguelike balance | M8X gates, equipment snapshots, simulations, Profile Shop sink tests |
| Map softlock | Run cannot reach Boss | property tests, reachability validation |
| UI preview mismatch | Player cannot trust decisions | preview/resolution parity tests |
| Unity presentation/UI performance issues | Combat readability or input stability suffers | URP/uGUI budgets, profiling, pooled effects, reduced effects, layout tests |
| Trial reward imbalance | Meta economy collapses | reward tables, simulations, first-clear/repeat distinction |
| AI-assisted/generated content inconsistency | Tone/content quality drifts | provenance/readiness metadata, human review, schema validation; generation method alone is not a rejection criterion |
| Non-production fixture leakage | Test characters or items enter player-facing state or release content | isolated fixture registries, explicit milestone retirement, release scans |
| Production content precedes its schema or decisions | Content forces one-off engine/UI branches or unstable rewrites | schema/decision locks before M2, M6, M8, M8T, and M8X production authoring |
| Speculative persisted state | Empty future payloads become accidental compatibility contracts | persist only live systems; add versioned migrations when M8/M8T/M8X systems ship |
| Abstract art direction | Assets cannot communicate mechanics at gameplay scale | concrete 2D briefs, semantic presentation tags, readable tells, review status |
| Advanced character retrofit pressure | Post-launch selfish-cost, Domain-transforming, or resource-amplifier kits require rewrites if launch systems are too rigid | DD-22 gate, source-kind event metadata, explicit status persistence scope, generic Domain operations, resource activity ledgers, and release scans for unapproved future content |
| Unsafe card-copy semantics | Future copy kits bypass Transcend, leak hidden Draw order, or create recursive temporary cards | DD-23 gate, copy eligibility defaults, copy-lineage metadata, combat-scoped copy cleanup, hidden-zone information tests |
| Card drag/canvas instability | Cards jump, drift, leave the screen, or submit unintended plays | one runtime UI system, deterministic fan layout, dedicated drag layer, explicit coordinate conversion, Play Area state machine, Play Mode aspect-ratio tests |
| Unity scene/prefab rule leakage | Gameplay becomes coupled to GameObjects/Animator/prefab names and loses determinism | no-engine-reference core assembly, stable IDs, adapter-only Unity references, hardcoding audit |
| Experimental CLI/Pipeline churn | Agent scripts break when Unity changes syntax | project-owned wrappers/commands, installed `--help` authoritative, gameplay independent of CLI/Pipeline |

---

## Appendix B - Delegation and Parallel Work

- Work may be parallelized only after shared schemas/interfaces are stable.
- Content production can parallelize after content schemas and registry validation exist.
- Unity presentation work can parallelize after the relevant command/event/actor contracts exist; it may not invent gameplay rules.
- Trials can parallelize after profile inventory, reward grants, and encounter schemas exist.
- Generated art/reference production may occur at any time. Runtime binding as production content waits only for the relevant logical asset-reference schema/catalog contract and human review; it does not need to wait for M10.
- Audio/VFX breadth may parallelize after presentation-token/asset contracts exist, with M10 owning final closure and budgets.
- No subtask may introduce production gameplay content outside schema-validated data.
- Parallel Unity scene/prefab work must respect stable actor/presenter interfaces and may not branch on production IDs.

---

## Appendix C - Required Task-Plan Index

Required task-plan files should eventually exist under `plans/tasks/` or an equivalent directory:

- M0A through M0F
- M1A through M1J
- M2A through M2I
- M3A through M3H
- M4A through M4F
- M5A through M5G
- M6A through M6G
- M7A through M7D
- M8A through M8I
- M8T-A through M8T-F
- M8X-A through M8X-G
- M9A through M9G
- M10A through M10E
- M11A through M11G

No whole milestone should be handed to an implementation agent as one vague prompt once task-level work begins.

---

## Appendix D - Task Plan Template

```markdown
# Task <ID> - <Title>

## Objective

## In Scope

## Non-Goals

## Source Documents To Inspect

## Public Contract Changes

## Schema or Content Changes

## Implementation Steps

## Required Tests

## Validation Commands

## Exit Criteria

## Worklog Entry Requirements
```

---

## Appendix E - Pre-Implementation Approval Checklist

Before coding beyond M0 foundation work:

- `docs/DESIGN.md` is current.
- `plans/implementation_plan.md` is approved.
- Content schema policy is approved or explicitly provisional.
- Unity 6.5 Supported-line project/version policy, Windows development baseline, and DD-14 release-platform gate are recorded; the exact `6000.5.x` patch is pinned in `ProjectSettings/ProjectVersion.txt`.
- Direct pulls have no intermediary conversion resource.
- Production content hardcoding is forbidden and reviewable.
- M0 validation commands exist.
- `Bloomdrawn.Engine` No Engine References boundary is enforced.
- M1 owns the independent actor model, bottom-centred fan, Play Area threshold interaction, and initial presentation adapter.
- Generated art is permitted and readiness is tracked independently from generation method.
- Unity CLI/Pipeline is treated as experimental development tooling and cannot be required by gameplay/runtime architecture.
- Open design gates are tracked and block their dependent milestones.

---

## Appendix F - Unity Documentation Baseline

Task authors and agents must verify Unity/package behaviour against current official documentation when it affects implementation. Useful baseline references:

- Unity 6 releases/support and Update-release guidance: https://unity.com/releases/unity-6/support
- Unity 6.5 current 6000.5.x patches: https://unity.com/releases/editor/archive
- Unity 6.5 base release notes and 2D feature additions: https://unity.com/releases/editor/whats-new/6000.5.0f1
- Unity CLI: https://docs.unity.com/en-us/unity-cli/
- Unity CLI reference (`--help` is authoritative): https://docs.unity.com/en-us/unity-cli/unity-cli-reference
- Unity Pipeline package setup and Editor connection: https://docs.unity.com/en-us/unity-production-pipeline/local-tools-cli/unity-pipeline-package
- Unity CLI/Pipeline/`[CliCommand]`/live Editor evaluation overview: https://unity.com/blog/meet-the-unity-cli
- Unity Test Framework: https://docs.unity3d.com/Manual/com.unity.test-framework.html
- Unity C# compiler/language support: https://docs.unity3d.com/Manual/csharp-compiler.html
- Assembly Definition properties / No Engine References: https://docs.unity3d.com/Manual/class-AssemblyDefinitionImporter.html
- Unity UI system comparison: https://docs.unity3d.com/Manual/UI-system-compare.html
- RectTransform coordinate utilities: https://docs.unity3d.com/ScriptReference/RectTransformUtility.html
- uGUI Canvas scaling: https://docs.unity3d.com/ScriptReference/UI.CanvasScaler.html
- Input System: https://docs.unity3d.com/Manual/com.unity.inputsystem.html
- Persistent data path: https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html
- URP 2D documentation: https://docs.unity3d.com/Manual/urp/2d-index.html

Rules:

- Experimental CLI/Pipeline syntax is not frozen by this plan. Use installed help/discovery and update project wrappers when syntax changes.
- Package/API decisions in task plans should note the documentation page/version actually checked when behaviour is version-sensitive.
- Third-party packages are not architectural defaults. Add one only when a task demonstrates a concrete need and records the dependency/maintenance cost.

---
