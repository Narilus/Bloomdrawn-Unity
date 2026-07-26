# Bloomdrawn Design Decision Register

This file is the audit trail for design-level choices that must not emerge accidentally during implementation.

`docs/DESIGN.md` remains the gameplay, product, UX, content, and economy source of truth. When an approved decision changes the rules contract, update `docs/DESIGN.md`, update affected implementation/task plans, then mark the decision as mirrored. This file does not permanently substitute for the design document.

## Decision Workflow

1. Open the decision before its dependent task begins.
2. State the exact question and affected systems.
3. Record viable options and consequences.
4. Obtain approval from the project owner.
5. Record the approved answer, rationale, and date.
6. Update `docs/DESIGN.md` where the rules contract changes.
7. Update `plans/implementation_plan.md` or active task plans if sequencing/contracts change.
8. Assess save, schema, migration, content-version, test, and balance impact.
9. Mark the decision `approved` only after required mirrors are complete.

## Status Values

- **open** - no approved answer; dependent work must not begin.
- **proposed** - a candidate exists but is not approved.
- **approved** - decision accepted and required mirrors complete.
- **superseded** - replaced by a later decision; retain history.

---

## DD-01 - Combat Terminal Timing

**Status:** approved
**Opened:** 2026-06-22  
**Approved:** 2026-06-30
**Required before:** M1 combat finalization  
**Owner/approver:** Project owner

### Question

When a sub-effect creates a victory or defeat condition, does combat finalize immediately between atomic effects, or does the currently resolving accepted action finish first?

### Context and Constraints

This affects multi-hit cards, post-hit healing, resource gain, generated cards, scheduled effects, ultimate charge, enemy phase transitions, and replay determinism.

### Options Considered

1. **Action-boundary finalization.** Detect terminal conditions immediately, but normally finish the currently accepted action before finalizing combat unless an explicit interrupt/death rule applies.
2. **Atomic finalization.** Finalize immediately after the atomic event that caused victory or defeat.
3. **Fully delayed finalization.** Resolve all queued timing-window effects before finalization.

### Proposed Baseline

Option 2. Atomic finalization is the approved baseline.

### Approved Decision

Option 2: **Atomic Stop**.

Combat checks victory and defeat after each atomic damage, healing, status, or effect event that can change terminal state. If that check reaches `VICTORY` or `DEFEAT`, combat enters the terminal phase immediately. Remaining non-terminal sub-effects of the accepted action do not resolve.

An effect may continue through terminal only when it explicitly declares a death, replacement, or phase-transition exception that is part of the same terminal-causing atomic event. Future content that needs death throes, boss replacement, or phase transition must declare that exception through a typed rule rather than relying on ordinary action completion.

### Rationale

Atomic Stop gives clearer player feedback, simpler deterministic replay, and avoids ambiguous outcomes such as winning and then dying from the unfinished tail of the same action, or dying while still receiving later resource/reward-like effects from that action. It also matches the combat design direction that victory/defeat is checked after every atomic effect while preserving a narrow explicit exception lane for authored terminal transitions.

### Required Document Mirrors

- `docs/DESIGN.md`: combat victory/defeat and terminal-condition rules.
- `plans/implementation_plan.md`: M1E/M1H requirements and DD-01 gate status.

### Tests and Acceptance Evidence

- Multi-part card that kills an enemy before a later non-terminal sub-effect; later non-terminal sub-effects are skipped.
- Shared HP reaches zero during enemy, status, HP-loss, or other atomic effect resolution; combat enters `DEFEAT` immediately.
- If shared HP reaches 0 at the same terminal checkpoint as all enemies being defeated, defeat has precedence unless an explicit encounter rule says otherwise.
- Explicit terminal exception example if boss replacement, death throes, or phase transition is introduced.
- Golden replay encodes Atomic Stop event order and final checksum.

### Save/Migration/Content-Version Impact

No save impact before combat saves exist. Later changes are engine and content-version affecting.

---

## DD-02 - Domain Tuning Lock

**Status:** approved
**Opened:** 2026-06-22  
**Approved:** 2026-06-30
**Required before:** M2A production character schema/content lock<br>
**Owner/approver:** Project owner

### Question

Are the launch Domain values, resource names, UI strings, and edge-case rules in `docs/DESIGN.md` section 3 approved for first implementation?

### Context and Constraints

M2 implements Flesh, Abyss, Spirit, and Void. Resource rules must be stable before schema and engine operations are built around them.

### Options Considered

1. Approve section 3 as written for implementation.
2. Approve section 3 with tuning-only numeric changes.
3. Revise resource identities or UI strings before M2.

### Proposed Baseline

Approve section 3 as written, with numeric values marked tuning where applicable.

### Approved Decision

Option 1: approve `docs/DESIGN.md` section 3 as written for first implementation.

Flesh, Abyss, Spirit, and Void keep their launch resource identities, UI labels, reset/persistence rules, and edge-case invariants for M2. Numeric values explicitly marked as tuning remain adjustable during balance passes, but the underlying Domain economies and resource semantics are approved implementation anchors.

### Rationale

Approving the Domain rules before M2A gives production schemas and M2 engine tasks stable targets. It keeps the launch scope focused on the four established Domain engines while still allowing numeric balance changes where the design marks values as tuning.

### Required Document Mirrors

- `docs/DESIGN.md`: section 3 remains the approved launch Domain contract.
- `plans/implementation_plan.md`: DD-02 is resolved for M2A.
- M2 task files: Domain resource implementation tasks use section 3 as the contract.

### Tests and Acceptance Evidence

- Resource initialization/reset tests.
- Flesh enhanced-spend invariant.
- Abyss volley targeting and overflow.
- Spirit Ritual held-cost behavior.
- Void Delay/Falter conversion.

### Save/Migration/Content-Version Impact

No immediate save, schema, engine-version, or content-version impact from this decision record. M2B and later Domain implementation work will affect combat state shape, tests, and content semantics through their own tasks.

---

## DD-03 - Launch Character Tuning Lock

**Status:** approved
**Opened:** 2026-06-22  
**Approved:** 2026-06-30
**Required before:** M2A/M5A production character content lock<br>
**Owner/approver:** Project owner

### Question

Are the launch character stats, cards, passives, ultimates, generated cards, and visual notes in `docs/DESIGN.md` section 4 approved for first implementation?

### Context and Constraints

M2 implements Mara, Thalassa, Sephira, and Azael. M5 implements Venelis, Nyxalia, Kibane, and Mira Nox. Schema and engine operation needs depend on these kits.

### Options Considered

1. Approve all kits as implementation anchors, with numeric tuning allowed.
2. Approve starter party only, defer non-starter tuning to M5.
3. Revise any character kit before implementation.

### Proposed Baseline

Approve all launch kits as implementation anchors. Treat numbers as tuning anchors unless `docs/DESIGN.md` marks them invariant.

### Approved Decision

Option 1: approve all eight launch kits as implementation anchors.

Mara, Venelis, Thalassa, Nyxalia, Sephira, Kibane, Azael, and Mira Nox are approved for schema and implementation planning. M2 implements the starter party first: Mara, Thalassa, Sephira, and Azael. M5 implements the remaining four launch characters. Numeric values remain tuning anchors unless `docs/DESIGN.md` marks them invariant; kit identities, card roles, passives, ultimates, generated cards, and presentation notes are approved as the first implementation target.

### Rationale

Approving all eight launch kits now lets the production character schema support the full launch roster rather than overfitting to the starter party. It also keeps M2 and M5 aligned: M2 proves one character per Domain, while M5 expands within the same approved schema and design language.

### Required Document Mirrors

- `docs/DESIGN.md`: section 4 remains the approved launch character contract.
- `plans/implementation_plan.md`: DD-03 is resolved for M2A and M5A.
- M2 and M5 task files: character/card content tasks use section 4 as implementation anchors.

### Tests and Acceptance Evidence

- One schema-authored content fixture per character.
- Golden combat for starter party.
- No production character-specific engine/UI hardcoding.

### Save/Migration/Content-Version Impact

No immediate save, schema, engine-version, or content-version impact from this decision record. Character schema and content implementation in M2A/M2C-M2F/M5 will be content-version affecting. Changing production character definition IDs after persistence exists is content-version and migration affecting.

---

## DD-04 - Save Checkpoints

**Status:** approved
**Opened:** 2026-06-22  
**Approved:** 2026-07-26
**Required before:** M4 save/resume UX  
**Owner/approver:** Project owner

### Question

Which authoritative checkpoints are exposed to the player for release?

### Context and Constraints

The save schema may represent complete state earlier than the UI exposes all possible checkpoints. Presentation state is not authoritative.

### Options Considered

1. Map/node boundaries only.
2. Map/node boundaries plus stable combat action boundaries.
3. Broader mid-combat recovery, excluding mid-animation state.
4. Arbitrary mid-resolution/mid-animation state.

### Proposed Baseline

Expose map/node boundaries plus stable fully resolved combat-action boundaries. Do not expose arbitrary mid-resolution, mid-animation, or interaction-state recovery.

### Approved Decision

Option 2 is approved. Player-visible save/recovery checkpoints are map/node boundaries and stable, fully resolved combat-action boundaries. Mid-resolution, mid-animation, and interaction-state recovery are excluded.

### Rationale

This preserves deterministic recovery at meaningful committed state boundaries without requiring presentation or in-progress command state to become save data.

### Required Document Mirrors

- `docs/DESIGN.md`: save contract and UX.
- `plans/implementation_plan.md`: M4D save/resume UX and M4F persistence E2E.
- E2E save/resume matrix.

### Tests and Acceptance Evidence

- Save after map movement.
- Save after node consequence.
- Save after purchase/reward choice.
- Save after a fully resolved player action.
- No save/recovery path exposes a mid-resolution, mid-animation, or interaction-state snapshot.
- Reload preserves RNG and authoritative state.

### Save/Migration/Content-Version Impact

Defines the exposed active-run checkpoint contract. Persisted combat snapshots must represent only fully resolved command boundaries; transient UI interaction and presentation playback state remain non-authoritative and unsaved.

---

## DD-05 - Gacha Rates and Pity

**Status:** approved
**Opened:** 2026-06-22  
**Approved:** 2026-07-26
**Required before:** M8A decision lock and M8X banner equipment expansion<br>
**Owner/approver:** Project owner

### Question

What exact rates, soft-pity table or formula, hard pity, featured guarantee, 10-pull SR-or-better guarantee, result-family splits, and rounding rules apply to direct pulls?

### Context and Constraints

Bloomdrawn uses direct stored pulls, no intermediary conversion resource. Rates, rarity, pity, result family, 10-pull guarantees, and equipment outcomes must be visible, exact, deterministic, and auditable.

### Options Considered

1. Exact table for pulls 60 through 90.
2. Exact integer/rational formula with specified rounding.
3. Revised pity threshold or hard pity value.
4. Separate rarity and result-family tables for characters, weapons, and materials.

### Proposed Baseline

Use current design anchors until this decision is approved: top-rarity equivalent 3%, mid-rarity equivalent 15%, low-rarity/material equivalent 82%, soft pity begins at pull 60, hard pity at pull 90, featured 50/50 with guarantee after loss, and every 10-pull guarantees at least one SR-or-better result.

### Approved Decision

Option 2 is approved. Onboarding is a short sequence with one focused teaching beat per launch Domain, ending in a complete starter-party combat.

### Rationale

This introduces all four starter engines without requiring a single overloaded tutorial encounter or deferring essential guidance into an otherwise uncurated first Run.

### Required Document Mirrors

- `docs/DESIGN.md`: gacha rates/pity.
- Banner content data.
- M8 resolver and M8X banner-equipment task files.

### Tests and Acceptance Evidence

- Exact boundary tests for pity increments.
- Hard pity cannot fail.
- Featured guarantee persists across banner rotation.
- 10-pull SR-or-better guarantee cannot fail.
- Banner pools expose exact rarity and result-family rates.
- Audit log records pity before/after.

### Save/Migration/Content-Version Impact

Banner state and pity persistence are profile-save affecting.

---

## DD-06 - First-Acquisition Protection

**Status:** open  
**Opened:** 2026-06-22  
**Approved:** pending  
**Required before:** M8A decision lock<br>
**Owner/approver:** Project owner

### Question

What rule prevents early duplicate frustration after the player already owns the four-character starter party?

### Context and Constraints

The player starts with Mara, Thalassa, Sephira, and Azael. A rule such as "force unowned while owning fewer than four" does not help normal onboarding.

### Options Considered

1. First one or two Bloom results after banner unlock are guaranteed unowned launch characters.
2. Bloom results prefer unowned launch characters until the player owns a specified number.
3. Duplicate Bloom results are impossible until the first non-starter character is acquired.
4. No special protection beyond pity/featured guarantee; compensate through direct-pull income.

### Proposed Baseline

Option 1 for first implementation: guarantee the first Bloom result after banner unlock is an unowned launch character.

### Approved Decision

Pending project-owner approval.

### Rationale

Pending.

### Required Document Mirrors

- `docs/DESIGN.md`: onboarding and gacha acquisition rules.
- Banner/gacha content data.
- M8 resolver tests.

### Tests and Acceptance Evidence

- Starter profile's first protected Bloom is unowned.
- Protection state persists through save/load.
- Protection does not break featured guarantee rules.

### Save/Migration/Content-Version Impact

Profile/banner state affecting.

---

## DD-07 - Duplicate Ladder

**Status:** open  
**Opened:** 2026-06-22  
**Approved:** pending  
**Required before:** M8A decision lock and M8X weapon duplicate handling<br>
**Owner/approver:** Project owner

### Question

What exact C1-C5 character duplicate benefits, +1 through +5 weapon duplicate bonuses, and post-cap/maxed compensation apply?

### Context and Constraints

C0 characters and +0 weapons must be complete and viable. Duplicate tiers must not become mandatory for standard content. Post-cap compensation must not become an intermediary pull conversion resource.

### Options Considered

1. Use the current C1-C5 structure from `docs/DESIGN.md` with character-specific content data.
2. Use generic duplicate benefits per Domain.
3. Reduce duplicate progression to cosmetic/profile rewards only.
4. Add separate weapon duplicate bonus tables from +1 through +5 with overflow conversion after +5.

### Proposed Baseline

Use current C1-C5 character structure and add content-authored weapon duplicate bonus tables from +1 through +5, with overflow converting to approved Profile Shop currency or materials.

### Approved Decision

Pending project-owner approval.

### Rationale

Pending.

### Required Document Mirrors

- `docs/DESIGN.md`: duplicate ladder.
- Character and weapon progression schemas.
- M8 progression and M8X weapon tasks.

### Tests and Acceptance Evidence

- C0 viability checks.
- Duplicate tier application tests.
- Post-cap duplicate behaviour if approved.
- +0 weapon viability checks.
- Weapon duplicate +5 cap and overflow conversion tests.

### Save/Migration/Content-Version Impact

Character ownership, weapon ownership, inventory, and profile state affecting.

---

## DD-08 - Content Intensity Settings

**Status:** open  
**Opened:** 2026-06-22  
**Approved:** pending  
**Required before:** M10/M11 content lock  
**Owner/approver:** Project owner

### Question

What content warning taxonomy and player-facing intensity/accessibility settings are required for release?

### Context and Constraints

Bloomdrawn uses pastel horror dissonance but must remain honest about content. Content definitions must carry warning metadata where relevant.

### Options Considered

1. Content warnings plus archive/filter metadata only.
2. Content warnings plus intensity toggles for selected visual/audio effects.
3. Full content filtering by warning category where feasible.

### Proposed Baseline

Use warnings plus intensity settings for motion, shake, gore/body-horror emphasis, and audio harshness where content supports it.

### Approved Decision

Pending project-owner approval.

### Rationale

Pending.

### Required Document Mirrors

- `docs/DESIGN.md`: accessibility/content warning sections.
- Content schemas.
- Settings UI.

### Tests and Acceptance Evidence

- Content missing required warning metadata fails validation.
- Reduced intensity settings affect presentation without changing authoritative state.

### Save/Migration/Content-Version Impact

Settings/profile state affecting.

---

## DD-09 - Starter Onboarding

**Status:** open  
**Opened:** 2026-06-22  
**Approved:** pending  
**Required before:** tutorial implementation  
**Owner/approver:** Project owner

### Question

What tutorial order, curated encounters, and first-run party guidance introduce Mara, Thalassa, Sephira, and Azael?

### Context and Constraints

The starter profile owns one character per Domain. Tutorial pacing must teach all four engines without overwhelming the player.

### Options Considered

1. One curated tutorial combat that introduces all four Domains.
2. Short tutorial sequence with one focused beat per Domain.
3. First Run teaches systems organically through curated early nodes.

### Proposed Baseline

Use option 2: a short sequence with one focused beat per Domain, ending in a complete starter-party combat.

### Approved Decision

Pending project-owner approval.

### Rationale

Pending.

### Required Document Mirrors

- `docs/DESIGN.md`: onboarding.
- Tutorial content data.
- `plans/implementation_plan.md`: M5H onboarding implementation after starter-profile and persistence prerequisites exist.

### Tests and Acceptance Evidence

- Starter profile owns the correct four characters.
- Tutorial content references valid starter cards, curated encounters, and the four focused Domain teaching beats.
- The final tutorial encounter uses the complete starter party.
- First run can begin without gacha setup.

### Save/Migration/Content-Version Impact

M5H introduces explicit profile tutorial-completion/one-time-reward state with compatible persistence handling; it must not reserve future gacha, Trial, or equipment payloads.

---

## DD-10 - Reward Economy

**Status:** open  
**Opened:** 2026-06-22  
**Approved:** pending  
**Required before:** production M6/M8/M8T/M8X reward tables<br>
**Owner/approver:** Project owner

### Question

How many direct pulls, character EXP items, weapon EXP items, Sigils, weapon ascension materials, gear/reroll rewards, persistent currency rewards, conversion rewards, and Obols are earned per mode, difficulty, and milestone?

### Context and Constraints

Run rewards, Trial rewards, Profile Shop stock, and equipment progression rewards must support collection, catch-up leveling, targeted farming, and economy sinks without collapsing balance.

M3 and M4 may prove deterministic reward selection, grant, banking, purchase, and persistence transactions with isolated validated non-production tables. Those fixtures do not approve production quantities and must be retired from runtime content during M6.

### Options Considered

1. Conservative direct-pull income with generous EXP/Sigil Trials.
2. Generous direct-pull income with slower character progression resources.
3. Broadly generous economy tuned around no monetisation and longer-term completion.
4. Separate equipment progression pacing from character collection pacing.

### Proposed Baseline

Use option 3 as the design direction, but require exact tables before implementation.

### Approved Decision

Pending project-owner approval.

### Rationale

Pending.

### Required Document Mirrors

- `docs/DESIGN.md`: reward economy.
- Run reward tables.
- Trial reward tables.
- Profile Shop offer tables.
- M6/M8/M8T/M8X tests.

### Tests and Acceptance Evidence

- Direct-pull grants are deterministic.
- Obols never persist.
- Trial rewards match declared tables.
- Weapon EXP, weapon ascension material, gear, reroll currency, and conversion currency rewards match declared tables.
- Profile Shop purchases are atomic and never spend Obols.
- Reward simulations report expected income by mode/difficulty.

### Save/Migration/Content-Version Impact

Profile inventory, run finalization, Trial rewards, Profile Shop state, equipment progression, and reward content affecting.

---

## DD-11 - Profile Roster Cap Table

**Status:** open  
**Opened:** 2026-06-22  
**Approved:** pending  
**Required before:** M8A decision lock and M8X equipment cap rules<br>
**Owner/approver:** Project owner

### Question

What profile levels unlock which maximum character levels, and do profile levels also gate weapon or gear progression bands?

### Context and Constraints

Profile level gates roster level, but not 1:1. Characters out-level the profile. EXP items allow catch-up for newly pulled characters. M8X may also need equipment progression bands to prevent runaway weapon or gear scaling.

### Options Considered

1. Broad profile bands, such as profile level 1 allowing character level 20.
2. More frequent smaller cap increases.
3. Difficulty-tier based caps rather than profile-level caps.
4. Separate character, weapon, and gear progression cap bands.

### Proposed Baseline

Use broad profile bands stored in validated content data. Defer equipment cap bands until DD-16/DD-17/DD-18 unless M8X balancing requires them.

### Approved Decision

Pending project-owner approval.

### Rationale

Pending.

### Required Document Mirrors

- `docs/DESIGN.md`: profile progression.
- Profile cap and any equipment cap content tables.
- M8 leveling and M8X equipment tests.

### Tests and Acceptance Evidence

- Character level cannot exceed cap.
- Cap table validation rejects decreasing or skipped invalid bands.
- Newly pulled character can use stored EXP up to cap.
- Weapon and gear cap tables validate if approved.

### Save/Migration/Content-Version Impact

Profile, character progression, and possible equipment progression affecting.

---

## DD-12 - Trial Difficulty and Reward Table

**Status:** open  
**Opened:** 2026-06-22  
**Approved:** pending  
**Required before:** M8T-A decision/schema lock and M8X equipment Trial rewards<br>
**Owner/approver:** Project owner

### Question

What boss levels, encounter scaling, first-clear rewards, and repeat rewards apply to each Trial, including equipment-expansion reward families?

### Context and Constraints

Trials are direct boss challenges for Flesh, Abyss, Spirit, Void, EXP, and persistent general currency rewards. M8X adds targeted Trial reward families for weapon EXP, weapon ascension materials, gear sets, and gear reroll currency.

### Options Considered

1. Uniform difficulty ladder shared by all Trials.
2. Domain-specific ladders with unique boss mechanics.
3. Shared early ladder, unique high-tier Trial mechanics later.
4. Equipment-expansion Trials share the early ladder but have distinct reward tables.

### Proposed Baseline

Use option 3: shared early ladder for implementation simplicity, room for unique high-tier mechanics later. Apply the same ladder to early equipment-expansion Trials unless DD-18/DD-19 require a different gate.

### Approved Decision

Pending project-owner approval.

### Rationale

Pending.

### Required Document Mirrors

- `docs/DESIGN.md`: Trials.
- Trial content schemas.
- M8T and M8X tasks and tests.

### Tests and Acceptance Evidence

- Trial reward preview matches awarded inventory.
- First-clear and repeat rewards are distinct.
- Trials do not mutate active Run state.
- Equipment Trial rewards match declared weapon/gear/reroll tables.

### Save/Migration/Content-Version Impact

Trial clear state, profile inventory, equipment inventory, and reward content affecting.

---

## DD-13 - Content Format Policy

**Status:** approved
**Opened:** 2026-06-22  
**Approved:** 2026-06-30
**Required before:** M2A and all later production content authoring  
**Owner/approver:** Project owner

### Question

Which content families use YAML, which use JSON, and what generated runtime artifacts may Unity tooling produce from those canonical sources?

### Context and Constraints

Schema-driven content is mandatory from M0. YAML is preferred for hand-authored gameplay and narrative definitions because it remains readable and reviewable. JSON is preferred for generated or machine-written tables, manifests, registries, hashes, audit output, and save-like structures.

Unity Editor tooling may validate, import, index, or compile canonical content into generated runtime representations, but generated Unity/C# artifacts are derivatives rather than a second authored source of truth.

### Options Considered

1. JSON for all content.
2. YAML for hand-authored content and JSON for generated/machine-written content.
3. YAML for all content.

### Proposed Baseline

Use option 2. Hand-authored narrative/gameplay definitions use YAML by default; generated or machine-written artifacts use JSON by default.

### Approved Decision

Option 2 is approved.

Hand-authored production definitions use YAML by default, including characters, cards, generated cards, enemies, encounters, lineups, statuses, Trials, rewards, map motifs, keepsakes/boons, Symptoms, Curses, tutorials, and presentation metadata unless a later decision records a content-family exception.

Generated tables, lockfiles, content hashes/manifests, generated registries, audit outputs, machine-written lookup tables, and deeply nested save-like snapshots use JSON by default.

Unity import/build tooling may emit generated C# data, serialized runtime assets, lookup tables, caches, or other derived artifacts when useful, provided that:

- the canonical authored source remains unambiguous;
- generated output can be reproduced from canonical content;
- generated output is not manually edited as competing gameplay content;
- validation runs before generated data is accepted for runtime use.

Each content family has exactly one canonical authored source format at a time.

### Rationale

YAML keeps authored gameplay and narrative content easy to review and edit. JSON keeps generated data deterministic and tooling-friendly. Allowing Unity to compile or import validated source data gives runtime convenience without moving gameplay truth into scenes, prefabs, or manually edited generated assets.

### Required Document Mirrors

- `docs/DESIGN.md`: content-authoring and validation policy.
- `plans/implementation_plan.md`: DD-13 is resolved for production content authoring.
- Repository content loader/importer, schema documentation, validation commands, and task files follow one canonical source format per content family.

### Tests and Acceptance Evidence

- Loader/import tooling validates both approved source formats where required.
- Each content family has exactly one canonical source format.
- M2A schema tests prove hand-authored production definitions use YAML unless an explicit family exception is recorded.
- Generated or machine-written tables and audit outputs use JSON.
- Any generated Unity runtime representation can be rebuilt from validated canonical content.
- Runtime/editor generated artifacts do not become a second hand-authored gameplay source of truth.

### Save/Migration/Content-Version Impact

No immediate save or migration impact from this decision. M2A and later production content work is content-version and tooling affecting when schemas, importers, generated registries, or runtime representations are added.

---

## DD-14 - Release Platform Lock

**Status:** open  
**Opened:** 2026-07-26  
**Approved:** pending  
**Required before:** release packaging  
**Owner/approver:** Project owner

### Question

Which Unity platforms are part of Bloomdrawn's first public release?

### Context and Constraints

Windows is the primary development and validation environment. The project uses Unity 6.5 and should keep the authoritative engine, content definitions, and non-presentation rules platform-neutral where practical.

The first public release does not need to target every platform Unity can build. Additional platforms increase input, resolution, filesystem, packaging, performance, certification, and QA scope and should be selected deliberately.

### Options Considered

1. Windows-only first public release.
2. Windows plus Linux.
3. Windows plus Linux and macOS.
4. Broader desktop/mobile release after platform-specific validation.
5. Defer additional-platform commitment until the core game and production UI are stable.

### Proposed Baseline

Use Windows as the guaranteed development/validation target and defer commitment to additional release platforms until later production evidence exists.

### Approved Decision

Pending project-owner approval.

### Rationale

Pending.

### Required Document Mirrors

- `docs/DESIGN.md`: target build/platform language.
- `plans/implementation_plan.md`: packaging, performance, and release gates.
- Build profiles, platform-specific settings, and release checklist once approved.

### Tests and Acceptance Evidence

Once release platforms are approved:

- supported platform builds complete successfully;
- save/load and content loading work on each supported platform;
- card input, targeting, text scaling, aspect-ratio handling, and accessibility controls are validated on each supported platform;
- platform-specific filesystem paths and settings do not change deterministic gameplay results;
- unsupported platforms are not implied by release metadata.

### Save/Migration/Content-Version Impact

Platform selection is primarily packaging, input, persistence-adapter, and QA affecting. It must not redefine authoritative gameplay rules.

---

## DD-15 - Rarity and Banner Result Model

**Status:** open  
**Opened:** 2026-06-23  
**Approved:** pending  
**Required before:** M8A decision lock and M8X banner equipment expansion<br>
**Owner/approver:** Project owner

### Question

What exact SSR/SR/R rates, result-family splits, 10-pull SR-or-better guarantee rules, guarantee precedence, and banner pool structures apply?

### Context and Constraints

All playable characters are SSR. Character signature weapons are SSR. Non-signature weapons may be SSR, SR, or R. Banner results must show both rarity and exact result family without creating paid currency, paid pulls, or an intermediary pull conversion resource.

### Options Considered

1. Hybrid model: rarity tier plus result family for every result.
2. Replace Bloom/Echo/Material language entirely with rarity-only categories.
3. Keep legacy result categories as primary and use rarity as metadata only.

### Proposed Baseline

Use the hybrid model. Every 10-pull guarantees at least one SR-or-better result. Exact rates, guarantee precedence, and pool splits remain pending.

### Approved Decision

Pending project-owner approval.

### Rationale

Pending.

### Required Document Mirrors

- `docs/DESIGN.md`: gacha rarity, result family, and 10-pull guarantee rules.
- Banner content schemas and pool data.
- M8 and M8X task files.

### Tests and Acceptance Evidence

- Rarity enum validation rejects invalid tiers.
- All playable characters validate as SSR.
- Signature weapons validate as SSR.
- 10-pull SR-or-better guarantee cannot fail.
- Audit entries record rarity and result family.

### Save/Migration/Content-Version Impact

Banner state, gacha audit entries, content version, and profile inventory affecting.

---

## DD-16 - Stats and Equipment Scaling

**Status:** open  
**Opened:** 2026-06-23  
**Approved:** pending  
**Required before:** M8X-A schema lock and M8X-B save/snapshot contract<br>
**Owner/approver:** Project owner

### Question

What player stat keys, equipment scalers, formula inputs, stacking order, caps, and snapshot rules are approved?

### Context and Constraints

Equipment affects persistent character power and active Run/Trial snapshots. UI must not calculate authoritative stats. Existing character formulas use `attack`, `defense`, and `maxHp`; new stats or scalers must be explicit content/schema fields.

### Options Considered

1. Keep launch stats limited to `attack`, `defense`, and `maxHp`.
2. Add a small approved stat extension for equipment only.
3. Add a broader stat system before M8X.

### Proposed Baseline

Use option 1 for first M8X implementation unless equipment design proves a small extension is required.

### Approved Decision

Pending project-owner approval.

### Rationale

Pending.

### Required Document Mirrors

- `docs/DESIGN.md`: equipment snapshot, stat, and scaling rules.
- Gear and weapon stat schemas.
- M8X task files and simulator reports.

### Tests and Acceptance Evidence

- Equipment stat calculation is deterministic.
- UI and store code do not duplicate authoritative stat formulas.
- Starting a Run or Trial snapshots equipment stats and bonuses.
- Changing profile equipment after start does not mutate active Run or Trial state.

### Save/Migration/Content-Version Impact

Run save, Trial state, profile equipment, content version, and possible save-schema affecting.

---

## DD-17 - Weapon Progression

**Status:** open  
**Opened:** 2026-06-23  
**Approved:** pending  
**Required before:** M8X-A schema lock and M8X-C weapon implementation<br>
**Owner/approver:** Project owner

### Question

What weapon level caps, EXP item values, ascension material requirements, profile-money costs, signature restrictions, equip restrictions, and +1 through +5 duplicate bonuses apply?

### Context and Constraints

Weapons are persistent character equipment acquired from banner pulls and approved targeted sources. Signature weapons are SSR. SR and R weapons fill banner pulls and should remain useful as equipment, desynthesis fodder, or conversion value.

### Options Considered

1. Weapons are universal within broad weapon families.
2. Weapons are Domain-restricted.
3. Signature weapons have character-specific bonuses, non-signatures are broadly usable.

### Proposed Baseline

Use option 3. Exact restrictions, growth values, and duplicate bonuses remain pending.

### Approved Decision

Pending project-owner approval.

### Rationale

Pending.

### Required Document Mirrors

- `docs/DESIGN.md`: weapon rules and draft signature names.
- Weapon definition, growth, ascension, and duplicate schemas.
- Banner pool data and Profile Shop offer data.

### Tests and Acceptance Evidence

- Signature weapons validate as SSR and reference valid characters.
- Weapon duplicate bonus cannot exceed +5.
- Level and ascension costs are previewed and consumed atomically.
- Excess/maxed duplicate conversion is transactional.

### Save/Migration/Content-Version Impact

Profile inventory, owned weapon instances, equipment loadouts, gacha audit entries, and content version affecting.

---

## DD-18 - Gear Set and Stat System

**Status:** open  
**Opened:** 2026-06-23  
**Approved:** pending  
**Required before:** M8X-A schema lock and M8X-D gear implementation<br>
**Owner/approver:** Project owner

### Question

What six gear slots, main stat pools, substat pools, substat tier weights, set bonuses, reroll rules, reroll costs, main stat enhancement costs, and desynthesis yields apply?

### Context and Constraints

Gear is persistent six-slot character equipment with 3-piece and 6-piece set bonuses. Main stat enhancement goes to +12. Each gear piece has three substat slots. Rerolls must use a deterministic profile equipment RNG stream.

### Options Considered

1. Fully fixed stat pools per slot.
2. Slot-specific main stats with shared substat pools.
3. Set-specific stat pools and slot rules.

### Proposed Baseline

Use option 2 for first implementation: slot-specific main stat pools with shared, tiered substat pools unless a set explicitly narrows eligibility through schema data.

### Approved Decision

Pending project-owner approval.

### Rationale

Pending.

### Required Document Mirrors

- `docs/DESIGN.md`: gear slots, set bonuses, reroll, and desynthesis rules.
- Gear set, stat pool, tier, reroll, and desynthesis schemas.
- M8X tasks and tests.

### Tests and Acceptance Evidence

- Gear definitions declare exactly one valid slot.
- Gear sets validate 3-piece and 6-piece bonuses.
- Main stat enhancement cannot exceed +12.
- Gear rerolls use `profile.equipment` RNG only.
- Locked/favorited gear cannot be desynthesized.

### Save/Migration/Content-Version Impact

Owned gear instances, equipment loadouts, profile inventory, RNG state, and content version affecting.

---

## DD-19 - Profile Shop and Conversion Economy

**Status:** open  
**Opened:** 2026-06-23  
**Approved:** pending  
**Required before:** M8X-A schema lock and M8X-E Profile Shop implementation<br>
**Owner/approver:** Project owner

### Question

What Profile Shop stock, targeted character dupe policy, prices, purchase limits, refresh rules, conversion currency rules, and profile-money upgrade sink tables apply?

### Context and Constraints

The Profile Shop is distinct from Labyrinth Shops and never uses Obols. Special conversion currency may come from auto-discarded low-rarity items or maxed duplicate overflow. It must not buy direct pulls or become an intermediary pull currency.

### Options Considered

1. Static permanent Profile Shop stock.
2. Rotating stock with persistent purchase limits.
3. Hybrid permanent essentials plus rotating targeted offers.

### Proposed Baseline

Use option 3. Conversion currency buys targeted growth items, not direct pulls.

### Approved Decision

Pending project-owner approval.

### Rationale

Pending.

### Required Document Mirrors

- `docs/DESIGN.md`: Profile Shop and economy sink rules.
- Profile Shop offer schemas.
- Reward, desynthesis, and duplicate overflow tables.
- M8X tasks and tests.

### Tests and Acceptance Evidence

- Purchases are atomic and cannot create negative currency.
- Profile Shop never spends Obols.
- Special conversion currency cannot buy direct pulls.
- Targeted dupe purchases respect limits and ownership rules.
- Profile-money costs apply to approved upgrade flows.

### Save/Migration/Content-Version Impact

Profile inventory, shop state, audit history, purchase flags, and content version affecting.

---

## DD-20 - Economy and Item Naming

**Status:** open  
**Opened:** 2026-06-23  
**Approved:** pending  
**Required before:** M8X-A content lock and M8X-F UI text lock<br>
**Owner/approver:** Project owner

### Question

What are the final display names for direct pulls, profile money, special conversion currency, gear reroll currency, character EXP tiers, weapon EXP tiers, weapon ascension materials, gear slots, and public equipment terminology?

### Context and Constraints

Display names are player-facing and affect UI tone, content schemas, localization keys, inventory categories, and audit logs. `Gear` is the baseline public term for six-slot persistent equipment because `relic` overlaps with run-scoped keepsakes/boons.

### Options Considered

1. Keep practical placeholder names until content lock.
2. Approve final names before any M8X UI work.
3. Approve system terms early and item flavour names later.

### Proposed Baseline

Use option 3: approve system terms before M8X UI and allow individual item flavour names to remain content-review work.

### Approved Decision

Pending project-owner approval.

### Rationale

Pending.

### Required Document Mirrors

- `docs/DESIGN.md`: glossary, inventory, Profile Shop, gacha, and UI text sections.
- Content schemas/localization keys.
- M8X task files.

### Tests and Acceptance Evidence

- Inventory categories have stable display keys.
- Banner, audit, and Profile Shop UI use approved terms.
- Content validation rejects missing display strings.

### Save/Migration/Content-Version Impact

Content version, localization keys, inventory categories, and audit display affecting. Stable IDs should not depend on final display names.

---

## DD-21 - Bloom Identity and Refraction Premise

**Status:** approved
**Opened:** 2026-06-23
**Approved:** 2026-06-23
**Required before:** release-quality enemy, Symptom, Labyrinth art, major event, and character-writing content locks
**Owner/approver:** Project owner

### Question

What Bloom premise, Domain refraction rules, lifecycle language, and art/writing constraints guide release-quality content?

### Context and Constraints

Bloomdrawn already used Pastel Wrongness, Domain-specific horror, predatory blooms, Symptoms, Transcend, and Labyrinth temptation loops. The project needed a tighter world premise connecting those elements without turning the Bloom into a fifth Domain or pushing character art into unreadable abstraction.

### Options Considered

1. Make the Bloom the core unifying phenomenon with concise `docs/DESIGN.md` rules and richer guidance in a concept note.
2. Keep the Bloom as soft interpretive framing only.
3. Keep the Bloom entirely in a concept note until a later lore pass.

### Proposed Baseline

Use option 1. The Bloom is a generative cosmic pressure or event that refracts through existing Domains and desires. Its origin and intention remain unresolved.

### Approved Decision

The Bloom is Bloomdrawn's central unifying world phenomenon. It is not a fifth Domain. It refracts through the four Domains and through what beings already want, fear, worship, preserve, or hide.

Domain framing:

- Flesh visibly embraces the Bloom as beauty, gift, propagation, and bodily transformation.
- Abyss reflects old-god devotion and cultic belief made tangible; whether the old god was real before the Bloom remains ambiguous.
- Spirit centers humanoid-first hybrid/chimera forms unified by soul, memory, or Essence.
- Void is the wildcard/internalized lane for domain-less, uncategorized, or contained transformation, not primarily literal negative-space character art.

Lifecycle terms are approved as content guidance: Dormant, Seeded, Rooted, Flowering, Fruiting. The enemy relationship labels in `docs/bloom_identity_pass.md` remain internal brainstorming buckets, not canon taxonomy.

### Rationale

This keeps the title, Bloom imagery, Domains, Labyrinth, Symptoms, and transformation themes connected while preserving art-directable gacha character design and mystery around the Bloom's origin.

### Required Document Mirrors

- `docs/DESIGN.md`: Bloom/refraction premise, Domain framing, Labyrinth/Symptom framing, glossary, open questions, and design lock checklist.
- `plans/implementation_plan.md`: design gate and content/art milestone references.
- `docs/bloom_identity_pass.md`: richer concept guidance.

### Tests and Acceptance Evidence

- Documentation scan confirms the Bloom is not described as a fifth Domain.
- Documentation scan confirms Abyss old-god reality remains ambiguous.
- Documentation scan confirms Void is not framed primarily as literal negative-space character design.
- Documentation scan confirms lifecycle terms are guidance, not required mechanics.
- Documentation scan confirms no formal external media dependency was added to source-of-truth docs.

### Save/Migration/Content-Version Impact

No direct save, migration, schema, engine-version, or content-version impact. Future release-quality content authored under this guidance will affect content version through normal content workflows.

---

## DD-22 - Advanced Character Mechanics and Selfish Costs

**Status:** approved<br>
**Opened:** 2026-06-28<br>
**Approved:** 2026-07-01<br>
**Required before:** post-launch characters that alter Domain engines, add run-persistent self-debt, add party-scoped Domain-resource reaction/amplifier systems, or require advanced signature-weapon reaction hooks<br>
**Owner/approver:** Project owner

### Question

What generic engine, save, UI, schema, and test contracts allow future characters to use selfish costs, Domain engine transformations, party-level stances/aspects, damage-to-status conversion, per-hit reaction loops, and Domain-resource reaction/amplifier systems without hardcoded character branches?

### Context and Constraints

A future Abyss concept kit explored during planning under the unresolved Ismelda/Ismera name proposes run-persistent party self-debt, delayed debt conversion, Abyss engine replacement, party-level Tide Aspect state, per-hit Tentacle reactions, damage-to-status conversion, and signature-weapon stance-entry hooks.

The concept fits the Abyss identity of belief made tangible, but its mechanics are far beyond the launch character contract. Approving this gate must not make Sacrifice, Tide Aspects, Womb-Sea, or that exact kit part of launch scope.

A future Flesh concept kit explored under the Thaelia name proposes Embryo acceleration, party-scoped resource-consumption amplification, per-turn/per-combat resource activity ledgers, first-builder and first-consumer triggers, and resource/Ultimate-triggered signature weapon hooks. The concept fits Flesh identity, but Mother's Pearl and related exact rules are future content, not launch scope.

### Options Considered

1. Reserve generic extension points now and implement the actual mechanics in a post-launch advanced-character slice.
2. Implement broad advanced-character support during launch foundations.
3. Defer all consideration until the character is ready for production.

### Proposed Baseline

Use option 1. Launch keeps Abyss as Tentacles, Potency, automatic volley, immediate volley, and Drown. Foundation tasks avoid brittle assumptions by preserving source-kind damage metadata, explicit status persistence scope, generic Domain operations, and reaction-guard concepts for later approval.

### Approved Decision

Approved option 1. Bloomdrawn reserves generic extension points for future advanced-character mechanics now, but the actual mechanics belong to a post-launch advanced-character slice.

Launch and M2 implementation must not add selfish-cost mechanics, run-persistent self-debt, delayed debt conversion, damage-to-status conversion, party stances/aspects, Domain engine replacement, per-hit reaction loops, party-scoped Domain-resource amplifiers, resource activity ledgers, or advanced signature-weapon reaction hooks. Those systems require explicit future task contracts, save/replay/UI review, deterministic tests, and migration assessment.

DD-22 covers Thaelia-like resource-consumption amplifiers; no separate Thaelia-specific design decision is required for that mechanic family. DD-23 still governs any copied-card runtime effects in the same future kit.

### Rationale

This keeps release scope clean while preventing the launch engine from hardening around a single Abyss shape that would make future selfish-cost or Domain-transforming characters expensive to retrofit.

### Required Document Mirrors

- `docs/DESIGN.md`: launch-scope guardrails, approved future-gated extension points, glossary, and design-lock checklist.
- `plans/implementation_plan.md`: approved future gate, M2 launch Abyss guardrail, future advanced-character engineering guardrails, and risk register.
- `docs/feature_additions.md`: signature weapon compatibility note for advanced stance/aspect, Domain-transforming, resource-triggered, or Ultimate-triggered hooks.

### Tests and Acceptance Evidence

When implemented, future tests must cover:

- run-persistent debt save/load and migration;
- rejected commands consuming no resources or RNG;
- status timing and Shield interaction;
- per-hit triggers, source-kind exclusions, and recursion prevention;
- deterministic retargeting and per-command trigger caps;
- UI preview of current and future self-debt;
- party-scoped resource statuses consume exactly one stack only from accepted qualifying commands;
- resource-triggered amplifiers apply only to declared operation categories and match preview;
- first-builder, first-consumer, first-Ultimate, and first-Transcended-Ultimate ledgers refresh at declared timing windows;
- signature weapon hooks that trigger only from approved generic events.

### Save/Migration/Content-Version Impact

No current save, schema, engine-version, or content-version impact. Future implementation is likely save-affecting, content-version affecting, and UI-affecting because run-persistent statuses, combat event metadata, resource activity ledgers, active-run payloads, previews, and signature weapon triggers may need explicit versioned contracts.

---

## DD-23 - Advanced Card Memory, Copy, and Hidden-Zone Selection

**Status:** approved<br>
**Opened:** 2026-06-29<br>
**Approved:** 2026-07-01<br>
**Required before:** post-launch characters that copy cards, store card snapshots, reveal hidden Draw cards for selection, or require copy-triggered signature weapon hooks<br>
**Owner/approver:** Project owner

### Question

What generic engine, save, UI, schema, and test contracts allow future characters to create owner-preserving temporary card copies, store source-card snapshots, select from hidden zones, and react to copy events without bypassing Transcend, Exhaust, Graveyard, or copy-recursion safety?

### Context and Constraints

Future Void concept kits explored during planning under the Moirenne and Noema names propose delayed card replication, owner-preserving temporary copies, Pattern/Recollection/Wing-style generated cards, copy-triggered support, once-per-run choice ledgers, and hidden Draw reveal/selection.

The concepts fit the Void identity and avoid extra turns, bonus action phases, and hidden card-play windows. Their risk is different from DD-22: they pressure card-instance lifetime, copy eligibility, hidden-information handling, and copy-chain prevention.

A future Flesh concept under the Thaelia name proposes a safer hand-only copy case: selecting an eligible starting-deck card in Hand, preserving source owner and upgrade ID, discounting from printed base cost, applying Exhaust and `copyProhibited`, and using deterministic overflow to Draw. Even without hidden-zone reveal, this remains a DD-23 runtime copy effect.

### Options Considered

1. Gate card-memory/copy mechanics behind a future DD-23 contract while preserving current concepts.
2. Add broad card-copy support to launch foundations.
3. Prune future kits so they may copy only visible cards from hand or discard.

### Proposed Baseline

Use option 1. Future copied cards are combat-scoped by default, preserve source owner, and cannot recursively copy. Hidden-zone reveal/selection remains future-gated. Unsafe sources and card categories are blocked by default.

Default blocked categories:

- Transcend;
- Graveyard;
- Exhaust or already-exhausted cards;
- generated or combat-scoped cards;
- Curse and Symptom;
- X-cost cards;
- existing copy/Recollection/Reprise/Wing-style cards;
- consumed once-per-instance cards;
- cards tagged `copyProhibited`.

Default allowed sources:

- Hand;
- Draw;
- Discard;
- approved stored snapshots.

### Approved Decision

Approved option 1 as a launch guardrail. Bloomdrawn reserves generic card-memory/copy extension points and safety rules now, but runtime copy mechanics, hidden-zone selection, replay UI, save behaviour, and card-memory mechanics are not part of launch M2.

Current M2A schema reservations for generated/combat-scoped/source-referenced cards are acceptable. They are not approval for engine, UI, persistence, save, or replay behaviour. Future implementations must keep copied cards owner-preserving, source-identifiable, and combat-scoped by default unless a later approved task explicitly upgrades the lifetime and migration contract.

Launch-safe guardrails:

- no runtime copy command or copy effect in launch M2;
- no hidden-zone selection or reveal behaviour in launch M2;
- no recursive copy, copy-of-copy, or self-snapshot support;
- future copies must preserve owner and source identity;
- future temporary copies must be combat-scoped unless a later approved task explicitly upgrades them;
- safe hand-copy effects may additionally restrict sources to eligible starting-deck cards in Hand, preserve source upgrade ID, use printed base cost for discounts, ignore temporary modifiers and spent flags, gain Exhaust and `copyProhibited`, and overflow to Draw in deterministic order;
- future blocked categories remain blocked by default: Transcend, Graveyard-only, exhausted, generated, combat-scoped, Curse, Symptom, X-cost, existing copy/Recollection/Reprise/Wing-style, consumed once-per-instance, and `copyProhibited`.

### Rationale

This supports future Moirenne/Noema-style designs without letting copied Transcend cards bypass the once-per-combat invariant, without allowing infinite copy chains, and without leaking hidden Draw order through preview or rejected commands.

### Required Document Mirrors

- `docs/DESIGN.md`: card-memory/copy safety defaults, approved future gate, glossary, and design-lock checklist.
- `plans/implementation_plan.md`: approved future gate, future guardrails, post-launch package note, and risk register.
- `docs/feature_additions.md`: signature weapon compatibility note for Pattern, Recollection, Wing, pursuit, or generated-copy events.

### Tests and Acceptance Evidence

When implemented, future tests must prove:

- copies preserve owners and printed source identity;
- copies preserve source upgrade ID where applicable;
- copies do not inherit temporary source discounts, temporary Retain, Ritual held-cost reductions, or spent once-per-instance flags;
- copied cards cannot recursively copy, record, or snapshot themselves;
- copied cards cannot bypass Transcend once-per-combat flags;
- Graveyard, Exhaust, Transcend, generated, combat-scoped, Curse, Symptom, X-cost, and `copyProhibited` cards are rejected by default;
- combat-scoped copies are removed from every pile at combat end;
- hand-overflow copies enter Draw in stable order;
- hidden-zone reveal/selection commands reveal no hidden information and consume no RNG when rejected or cancelled;
- copy-triggered weapon hooks fire only from approved generic events.

### Save/Migration/Content-Version Impact

No current save, schema, engine-version, or content-version impact. Future implementation is likely save-affecting and content-version affecting because copied-card lifetime, source snapshots, copy-lineage metadata, hidden-zone pending selections, once-per-run ledgers, and active-run payloads may need explicit versioned contracts.

---

## DD-24 - Collapsing Node and Safe-Path Topology

**Status:** approved<br>
**Opened:** 2026-07-04<br>
**Approved:** 2026-07-04<br>
**Required before:** M3B map state, M3D validation, M3E node resolution, M3G map UI, and M4 run serialization<br>
**Owner/approver:** Project owner

### Question

Should the launch Labyrinth's one-use safe-path hazard be modeled as a Fragile traversal edge or as a player-facing Collapsing Node, and when exactly does collapse commit?

### Context and Constraints

The intended safe-path hazard is a visually distinct Collapsing Node that the player can stand on. It remains usable while occupied and collapses only after the player successfully departs from it. This creates a reversible-looking shortcut that is safe immediately but can make later Shop return paths more expensive. The topology reference is stored at `plans/reference/collapsing-node-safe-path-reference.png`.

The image remains the canonical visual comparison when present. The required fixture topology and behavior are also defined textually below, so a missing image blocks only image-dependent visual comparison; it does not authorize invention of the missing visual composition or weaken the deterministic topology contract.

The map contract must remain deterministic, previewable, accessible, and saveable. Rejected or cancelled movement must not mutate map state or consume RNG.

### Options Considered

1. Model the safe-path hazard as an authoritative node-scoped Collapsing Node.
2. Model the hazard as edge state while drawing a node-like visual proxy.
3. Defer one-use safe paths until later and keep M3 traversal fully stable.

### Proposed Baseline

Use option 1. Collapsing Nodes are travel-only M3 nodes that are visibly marked, safe to enter, and collapse after accepted departure.

### Approved Decision

Approved option 1. M3 uses player-facing and authoritative **Collapsing Nodes**, not Fragile edge/path collapse, for the launch one-use safe-path hazard.

Rules:

- Collapsing Nodes are visibly distinct before entry.
- Entry causes no collapse and no immediate map mutation.
- The node collapses only after accepted movement away from it.
- Collapse happens regardless of which destination node is chosen.
- Preview, cancellation, or rejected movement does not collapse the node and consumes no RNG or state.
- If destination entry requires confirmation, such as Symptom or Boss entry, collapse commits only after movement is accepted.
- Saving/reloading while the party stands on an intact Collapsing Node preserves it as occupied/armed, not collapsed.
- Collapsed nodes remain visually indicated but are no longer valid destinations.
- M3 Collapsing Nodes are travel-only, degree-2 gate/bypass nodes by default.

Canonical safe-path fixture contract:

- A Start-to-Boss spine contains two distinct junctions connected by a safe spine route; collapsing the bypass cannot orphan Boss reachability.
- A side loop joins those junctions through exactly two distinct degree-2 gates: one travel-only Collapsing Node and one Symptom node.
- The side-loop interior contains the revisitable Shop and its premium offer; the Collapsing Node and Symptom are the only loop gates between the spine junctions and the Shop interior.
- The Collapsing-first route enters the Shop through the intact Collapsing Node without immediate mutation. Accepted departure collapses that node, and a later Shop return from the spine must use the once-triggered Symptom route.
- The Symptom-first route commits the Symptom once, leaves the Collapsing Node intact until it is later departed, and preserves the same Boss-safe spine route.
- Route preview and any destination confirmation show the pending prior-node collapse and destination consequence without mutating state. Cancellation or rejection preserves topology, node lifecycle, consequences, and RNG state.
- Save/reload preserves current position, intact/occupied/collapsed lifecycle state, spent Symptom state, Shop stock/purchases, and the route behavior above.

### Rationale

The node-scoped model matches the intended safe-path fantasy: the player can step onto a visibly risky shortcut without paying immediately, but leaving it spends that shortcut and may force a future Symptom route. It is easier for players to reason about than an invisible edge-state rule and gives UI/accessibility a concrete disabled node to present after collapse.

### Required Document Mirrors

- `docs/DESIGN.md`: Labyrinth section, node revisit table, hazard timing, map invariants, map UI, milestone mirrors, glossary, and design-lock checklist.
- `plans/implementation_plan.md`: DD gate table, M3B/M3D/M3E/M3G/M3H tasks, and M7 route-economy reports.
- `plans/reference/collapsing-node-safe-path-reference.png`: required topology visual-comparison reference only, not production UI art or a renderer target. If absent, record only the image-dependent comparison as blocked; do not infer or recreate its visual composition.

### Tests and Acceptance Evidence

When implemented, tests must prove:

- entering a Collapsing Node does not mutate collapse state;
- accepted departure collapses the prior node exactly once;
- rejected or cancelled movement leaves collapse state, RNG, and node consequences unchanged;
- destination confirmation resolves before departure collapse commits;
- save/reload while occupied preserves the armed Collapsing Node;
- collapsed nodes cannot be selected as destinations;
- Boss reachability and claimed Shop revisitability survive every legal Collapsing Node/Symptom sequence;
- collapsed nodes are visually and accessibly distinct from current, reachable, unresolved, and spent nodes.
- the canonical textual safe-path fixture contract passes for both Collapsing-first and Symptom-first routes.
- when the reference image is available, the fixture topology matches it visually; when unavailable, this comparison is reported as blocked separately from the required textual behavior tests.

### Save/Migration/Content-Version Impact

No current save, schema, engine-version, or content-version impact because this is documentation-only. M3/M4 implementation will be save-affecting: active runs must serialize Collapsing Node lifecycle state, current position, spent-node state, and any fixture-development compatibility or invalidation rules.

---

## DD-25 - Node-Primary Labyrinth Topology

**Status:** approved<br>
**Opened:** 2026-07-04<br>
**Approved:** 2026-07-04<br>
**Required before:** M3B/M3C/M3D/M3E/M3G map work, M4 run serialization, and M7 map breadth<br>
**Owner/approver:** Project owner

### Question

Should Labyrinth implementation treat nodes or edges as the owner of player-facing content, costs, hazards, consequences, previews, and one-time resolution rules?

### Context and Constraints

Bloomdrawn's Labyrinth model is built around stepping into visible places: encounters, events, Symptoms, Shops, Boss nodes, and one-use bypass nodes. Edge-owned consequences make player preview, accessibility, save state, and route reasoning harder because meaningful effects can become hidden in a transition rather than attached to a visible destination or prior node.

Edges are still necessary for topology: adjacency, walls, motif anchors, pathfinding, and validation.

### Options Considered

1. Use a node-primary, edge-supported model where nodes own consequences and edges own connectivity.
2. Keep an edge-first model with hazards and costs authored on traversal edges.
3. Remove explicit edges and infer all connectivity from adjacent nodes.

### Proposed Baseline

Use option 1. Nodes are places. Edges are connections.

### Approved Decision

Approved option 1. Labyrinth implementation is **node-primary and edge-supported**.

Rules:

- Nodes own player-facing content, costs, hazards, consequences, lifecycle state, previews, one-time resolution rules, and revisit state.
- Edges own connectivity only: connected/unconnected, traversable/blocked, motif anchors, adjacency, walls, pathfinding, and validation graph structure.
- If a rule changes the run, it belongs on a node.
- If a rule changes reachability, it belongs on an edge.
- Missing edges between adjacent hexes are blocked walls; visual adjacency never creates traversal.
- Edge removal or blocking cannot hide rewards, costs, Symptoms, Shops, Events, combat, Corrupted effects, Curse effects, or other authored run consequences.
- Future toll, curse, corrupted, hazard, or gate mechanics are nodes unless they purely alter reachability.
- A future transition-scoped mechanic requires a new design decision explaining why node ownership is insufficient.

DD-24 remains valid as the specific Collapsing Node timing decision under this broader model.

### Rationale

This keeps the player's mental model simple: move between visible places, preview the destination and any prior-node lifecycle consequence, then commit. It also keeps save state and validation cleaner: graph edges describe possible movement, while node state describes what happens to the run.

### Required Document Mirrors

- `docs/DESIGN.md`: Labyrinth principle, motif definitions, seeded stitching, map validation, map UI, RNG stream names, save contents, invariants, milestone mirrors, glossary, open questions, and design-lock checklist.
- `plans/implementation_plan.md`: DD gate table, M3B/M3C/M3D/M3E/M3G tasks, and M7 reveal/reporting tasks.

### Tests and Acceptance Evidence

When implemented, tests must prove:

- edge definitions cannot author rewards, costs, Shops, Symptoms, Events, combat, Corrupted effects, Curse effects, or other run consequences;
- missing edges block traversal even when hexes visually touch;
- destination node consequences and prior-node lifecycle consequences preview before accepted movement;
- rejected or cancelled movement consumes no node consequence, lifecycle state, currency, item, or RNG;
- route validation evaluates both edge connectivity and legal node consequence/lifecycle sequences;
- save/reload preserves node content, node spent state, node lifecycle state, current position, and edge topology exactly.

### Save/Migration/Content-Version Impact

No current save, schema, engine-version, or content-version impact because this is documentation-only. M3/M4 implementation will be save-affecting because active runs must serialize node content, node spent state, node lifecycle state, current position, and edges as topology.

---

## DD-26 - Combat Enemy Placement and Target Readability

**Status:** approved<br>
**Opened:** 2026-07-04<br>
**Approved:** 2026-07-04<br>
**Required before:** M6 enemy content metadata, M9 final combat layout and target interaction, and M10 enemy asset validation<br>
**Owner/approver:** Project owner

### Question

How should enemy sprites be positioned on the combat stage so target selection and readability remain clear?

### Context and Constraints

`docs/DESIGN.md` already placed the four-character party diagonally on the left, but enemy placement was only implied. Mirroring the party's compact diagonal on the right could make targetable enemies overlap, obscure intent/status labels, or create ambiguous mouse, touch, and keyboard focus targets.

Enemy placement is presentation-facing. It must not redefine authoritative enemy slot order, targeting rules, deterministic stable ordering, or combat resolution.

### Options Considered

1. Use a dedicated right/right-center enemy formation model optimized for target readability.
2. Mirror the party's tight diagonal on the enemy side.
3. Leave placement entirely to individual art assets and renderer heuristics.

### Proposed Baseline

Use option 1. Enemies are right/right-center stage occupants with formation, target bounds, anchors, and spacing metadata.

### Approved Decision

Approved option 1. Targetable enemies occupy the right or right-center combat stage and are not required to mirror the party's compact diagonal.

Rules:

- Enemy formations prioritize target selection, readable silhouettes, HP/status/intent anchors, and inspection clarity over symmetry.
- One enemy uses a prominent right-side anchor.
- Two enemies use separated staggered anchors.
- Three enemies use a loose triangle or arc.
- Four or more enemies use rows, lanes, or an arc with scale and spacing constraints.
- Bosses use a large/boss anchor, with adds placed in satellite lanes rather than squeezed into the boss footprint.
- Independently targetable enemies must not overlap in a way that hides silhouettes, target rings, hitboxes, HP/status labels, or intent labels.
- Overlap is allowed only for authored non-targetable body parts, background limbs, or boss composition elements where target anchors remain unambiguous.
- Logical enemy slot order remains engine-owned and must be visibly and focusably mapped by presentation.

### Rationale

Enemies are primary targets and intent carriers. The player must be able to distinguish, inspect, focus, and select each target under combat pressure, including on touch-sized screens and with keyboard navigation. A separate enemy formation model prevents attractive combat composition from undermining targeting clarity.

### Required Document Mirrors

- `docs/DESIGN.md`: combat UI/UX, enemy definition metadata, M9/M10 milestone mirrors, open questions, and design-lock checklist.
- `plans/implementation_plan.md`: DD gate table, M6 enemy schema/content planning, M9 layout/interaction/accessibility/E2E tasks, and M10 enemy asset/readability validation.

### Tests and Acceptance Evidence

When implemented, tests or visual QA must prove:

- enemy-side placement remains readable for one, two, three, four-plus, and boss/add encounters;
- mouse, touch, keyboard, and focus navigation can select the intended living enemy unambiguously;
- HP/status/intent labels and target rings do not obscure or merge independently targetable enemies;
- VFX and reduced-motion variants preserve target readability;
- presentation formation metadata does not alter authoritative enemy slot order, targeting rules, RNG, saves, or combat resolution.

### Save/Migration/Content-Version Impact

No current save, schema, engine-version, or content-version impact because this is documentation-only. Future M6/M9/M10 implementation may add validated presentation metadata for enemies, but that metadata is presentation-only and must not change authoritative combat state.

---

## DD-27 - Unity Runtime and Presentation Architecture

**Status:** approved  
**Opened:** 2026-07-26  
**Approved:** 2026-07-26  
**Required before:** M0 foundation and M1 combat presentation  
**Owner/approver:** Project owner

### Question

What Unity runtime architecture separates authoritative game rules from scenes, presentation, assets, and agent/editor automation?

### Context and Constraints

Bloomdrawn is a deterministic card-driven game with rich 2D presentation. Unity provides scenes, GameObjects, UI, animation, rendering, asset import, and editor automation, but those systems must not become the authority for combat, map, economy, gacha, or persistence rules.

The project also uses Unity CLI and `com.unity.pipeline` for agent/editor automation. Those tools are development infrastructure, not runtime game dependencies.

### Options Considered

1. Pure C# authoritative engine plus Unity presentation/application assemblies.
2. MonoBehaviour/GameObject-driven gameplay state with tests around scene behaviour.
3. Hybrid authority where some rules live in the pure engine and others live in presentation components.

### Proposed Baseline

Use option 1.

### Approved Decision

Bloomdrawn uses Unity 6.5 (`6000.5.x`) with the exact patch pinned by `ProjectSettings/ProjectVersion.txt`.

The runtime architecture is:

- C# authoritative rules;
- a pure engine assembly with no UnityEngine or UnityEditor dependency;
- deterministic command -> state transition -> ordered event flow;
- Unity application/session adapters that submit commands and expose authoritative state;
- independent Unity presentation actors for party members and enemies;
- URP 2D for game rendering;
- uGUI + TextMesh Pro for the initial runtime UI baseline;
- Unity Input System for runtime input;
- Unity CLI and `com.unity.pipeline` as development/editor automation only.

Scenes, prefabs, MonoBehaviours, Animator state, frame time, UnityEngine.Random, and presentation assets may not become authoritative gameplay state.

### Rationale

Unity is most valuable here as a presentation, authoring, asset, build, and tooling host. Keeping game truth in a deterministic C# engine preserves replayability, testing, save integrity, content portability, and agent auditability while still gaining Unity's rendering and production systems.

### Required Document Mirrors

- `docs/DESIGN.md`: technical architecture and presentation sections.
- `plans/implementation_plan.md`: M0/M1 assembly, testing, actor, UI, and automation tasks.
- Root `AGENTS.md` and `.agents/skills/bloomdrawn-unity/`: implementation guardrails and Unity operating workflow.

### Tests and Acceptance Evidence

- Engine assembly compiles with Unity engine references disabled or otherwise mechanically excluded.
- Engine tests run without loading a Unity scene.
- No authoritative rule reads `Time`, `UnityEngine.Random`, GameObject state, Animator state, or rendered transforms.
- Runtime input produces validated commands rather than directly mutating game state.
- Presentation can be rebuilt from authoritative state/events after reload or fixture reset.
- CLI/Pipeline unavailability does not become a runtime dependency.

### Save/Migration/Content-Version Impact

This decision defines architecture rather than current save shape. Violating the boundary later could affect deterministic replay, saves, tests, and migrations, so architecture audits must protect it.

---

## DD-28 - Card Hand and Play-Threshold Interaction

**Status:** approved  
**Opened:** 2026-07-26  
**Approved:** 2026-07-26  
**Required before:** M1H card interaction and M9 final combat UX  
**Owner/approver:** Project owner

### Question

What is the canonical desktop card-hand, drag, play-threshold, and explicit-target interaction?

### Context and Constraints

The hand is a primary tactile surface. The expected roguelike-deckbuilder behaviour is a stable bottom-centred fan: the player raises/drags a card upward, crossing a visible Play Area threshold arms the play, lowering it back into the hand cancels the intent, and release above the threshold either casts immediately or proceeds to explicit target selection.

The interaction must remain stable across repeated drags and common desktop aspect ratios. Presentation gestures must not consume Mana, mutate piles, or change RNG before a complete engine command is accepted.

### Options Considered

1. Bottom-centred fan with upward drag and a responsive Play Area threshold.
2. Click-only card buttons.
3. Free drag directly onto targets with no intermediate play threshold.
4. Fixed screen-space threshold and hand positions authored for one reference resolution.

### Proposed Baseline

Use option 1, with click/keyboard paths reaching the same underlying interaction states.

### Approved Decision

The canonical interaction is:

1. Cards rest in authoritative hand order in a deterministic fan anchored to the bottom-centre.
2. Hover/focus raises the card for inspection without changing authoritative order.
3. Pointer-down begins one UI-only drag session and renders the card above the resting hand.
4. Dragging upward across the responsive **Play Area** threshold arms the card and shows a clear non-colour-only ready indicator.
5. Returning below the threshold disarms the card.
6. Releasing while disarmed cancels and returns the card to the current calculated fan layout.
7. Releasing while armed:
   - immediately submits target-complete effects such as self, party, all-enemy, or deterministic automatic targets;
   - for explicit targets such as `oneEnemy`, stages the card above the hand and enters target-selection mode. The player then clicks/selects a legal target to submit the complete command.
8. Cancelling explicit target selection returns the card to the authoritative hand with no cost or mutation.
9. A rejected command resynchronizes presentation to authoritative state and reports the rejection where useful.

Additional invariants:

- resting transforms are recalculated from hand order; drag/hover transforms never accumulate into future layout;
- one card interaction session owns drag/target focus at a time;
- the drag implementation uses a coherent UI coordinate space or explicit conversion when changing layers;
- reparenting must preserve intended visual position and must not force the card off-screen;
- no pointer release below the Play Area may submit a play command;
- legal/invalid target feedback is explicit;
- click and keyboard interaction use the same command and targeting semantics rather than separate gameplay logic.

### Rationale

This is familiar deckbuilder interaction, keeps the hand visually stable, gives the player a clear commitment threshold, separates card commitment from target choice, and directly protects against the coordinate drift/off-screen failures that card-heavy Unity UI can otherwise develop.

### Required Document Mirrors

- `docs/DESIGN.md`: combat UI/UX and hand interaction contract.
- `plans/implementation_plan.md`: M1 permanent hand/drag/targeting architecture and M9 polish/closure.
- Unity skill/verification guidance: aspect-ratio and drag-state validation.

### Tests and Acceptance Evidence

Play Mode and interaction tests must prove at minimum:

- five-card and variable-size hands remain bottom-centred and correctly fanned;
- repeated hover/drag/cancel cycles produce no cumulative positional or rotational drift;
- cards remain usable at supported 16:9, 16:10, and ultrawide validation resolutions;
- crossing the Play Area arms and returning below disarms;
- release below threshold never submits a play;
- target-complete release above threshold submits exactly one command;
- explicit-target release above threshold enters target selection without spending Mana or mutating piles;
- legal target selection submits exactly one complete command;
- cancellation from drag or target mode returns to the authoritative hand state;
- rejected commands consume no Mana, card movement, resource, or RNG;
- enemy target selection maps to the intended independent enemy actor.

### Save/Migration/Content-Version Impact

The drag/arming state is presentation-only and is not persisted as authoritative combat state. Stable combat action boundaries may later be save checkpoints under DD-04, but mid-gesture drag or target-hover state is not a save requirement.

---

## DD-29 - Generated Art Policy

**Status:** approved  
**Opened:** 2026-07-26  
**Approved:** 2026-07-26  
**Required before:** all art and presentation milestones  
**Owner/approver:** Project owner

### Question

May generated artwork be used as prototype, production, and release-quality Bloomdrawn art?

### Context and Constraints

Bloomdrawn is an art-heavy 2D project. Generated art is part of the intended production workflow rather than merely a temporary prototyping aid.

Quality, visual continuity, technical suitability, gameplay readability, content warnings, and provenance still require human review. Generation method alone does not determine whether an asset is temporary.

### Options Considered

1. Permit generated art at every stage, including final release after review.
2. Permit generated art only for concepts/placeholders and replace it before release.
3. Restrict generated art to non-character assets.

### Proposed Baseline

Use option 1.

### Approved Decision

AI-generated or otherwise model-generated artwork is permitted at every development stage and may be treated as production or release-quality artwork after human review.

Generation method is provenance metadata, not placeholder status.

Generated assets must meet the same project requirements as other assets for:

- visual quality and coherence;
- Domain/character identity;
- target and silhouette readability;
- composition and UI compatibility;
- technical import requirements;
- content-warning suitability;
- rights/provenance records required by the project;
- human approval before release lock.

There is no requirement to replace an otherwise approved asset merely because it was generated.

### Rationale

Generated art is a deliberate part of Bloomdrawn's production strategy and can materially accelerate a character-collection game with substantial illustration needs. The useful gate is quality and fit, not the tool used to create the image.

### Required Document Mirrors

- `docs/DESIGN.md`: AI-assisted content and art policy.
- `plans/implementation_plan.md`: all art/presentation milestones, especially M1 fixture visuals and M10 presentation closure.
- `docs/bloom_identity_pass.md`: art direction.
- Asset catalogue/provenance metadata once implemented.

### Tests and Acceptance Evidence

- Asset validation distinguishes review/readiness status from generation provenance.
- Generated assets may pass production/release gates when quality and technical requirements pass.
- Placeholder assets remain explicitly marked because they are placeholders, not because they are generated.
- Character/enemy assets remain individually usable at gameplay scale and preserve target readability.
- Release review includes provenance/rights and human approval.

### Save/Migration/Content-Version Impact

No gameplay save or migration impact. Replacing an asset may affect content/presentation versioning or asset references but generation method itself does not alter game-state compatibility.

---

## DD-30 - Save-Facing JSON Serializer

**Status:** approved  
**Opened:** 2026-07-26  
**Approved:** 2026-07-26
**Required before:** M0C RNG serialization evidence and M0E persistence implementation
**Owner/approver:** Project owner

### Question

Which JSON serializer represents Bloomdrawn save-facing data?

### Context and Constraints

M0C must prove authoritative RNG state can roundtrip through the serializer later used by M0E. The Engine must remain serializer-library agnostic; selecting JSON representation does not authorize M0E repository, envelope, checksum, migration, or filesystem work early.

### Options Considered

1. The already-installed Unity Newtonsoft JSON package.
2. System.Text.Json.
3. A new third-party serializer dependency.

### Proposed Baseline

Use the already-installed Unity Newtonsoft JSON package from application-facing serialization code and tests.

### Approved Decision

Option 1 is approved. `com.unity.nuget.newtonsoft-json` is Bloomdrawn's save-facing JSON serializer. Engine DTOs remain ordinary pure C# types without Newtonsoft attributes or serializer dependencies. M0E owns all persistence behavior beyond JSON representation.

### Rationale

The package is already installed by Unity and is available to Editor/application-facing code, avoiding an additional dependency while preserving the deterministic engine boundary.

### Required Document Mirrors

- `plans/tasks/M0C-deterministic-rng.md`: serializer roundtrip is executed outside the Engine.
- M0E implementation/worklog: use the selected serializer for save-facing JSON while retaining M0E's assigned envelope/repository responsibilities.

### Tests and Acceptance Evidence

- M0C test serializes an authoritative RNG state with Newtonsoft JSON, deserializes it, and verifies identical subsequent output.
- Engine source and asmdef remain free of Newtonsoft references.

### Save/Migration/Content-Version Impact

This selects JSON representation only. M0E defines save schema/version, envelope, checksum, validation, atomic write, fallback, and migrations.

---

## DD-31 - Windows Performance Acceptance Baseline

**Status:** open
**Opened:** 2026-07-26
**Approved:** pending
**Required before:** M10E readability, loading, and performance validation
**Owner/approver:** Project owner

### Question

Which Windows hardware class, display resolution, frame-time target, memory budget, representative scenarios, and acceptance tolerances define release performance validation?

### Context and Constraints

Windows is the primary development and validation platform, but no authority document currently defines numerical performance acceptance. M10E must validate an approved baseline rather than silently selecting hardware or budgets during implementation.

### Options Considered

1. One named Windows reference configuration with a fixed display resolution and unified frame-time and memory budgets.
2. Separate minimum and recommended Windows configurations with tier-specific display, frame-time, and memory budgets.
3. A Windows-only release baseline with the hardware tier and numerical targets selected in a later performance-lock decision.

### Proposed Baseline

Pending project-owner approval. No hardware, resolution, frame-time, memory, or tolerance values are selected by this record while it remains open.

### Approved Decision

Pending project-owner approval.

### Rationale

Pending.

### Required Document Mirrors

- `docs/DESIGN.md`: performance and release-validation language.
- `plans/implementation_plan.md`: design-gate table, M10E, M11F, and release acceptance criteria.
- Performance test matrix, profiler captures, and release checklist once approved.

### Tests and Acceptance Evidence

After approval, M10E must measure the declared representative combat/menu/loading scenarios against the approved hardware, display, frame-time, memory, and tolerance contract. Undeclared numbers are not valid acceptance criteria.

### Save/Migration/Content-Version Impact

No save, migration, engine-version, or content-version impact.

---

## Decision Entry Template

```markdown
## DD-XX - Title

**Status:** open | proposed | approved | superseded
**Opened:** YYYY-MM-DD
**Approved:** YYYY-MM-DD or pending
**Required before:** Task ID or milestone
**Owner/approver:**

### Question

### Context and Constraints

### Options Considered

### Proposed Baseline

### Approved Decision

### Rationale

### Required Document Mirrors

### Tests and Acceptance Evidence

### Save/Migration/Content-Version Impact

### Supersedes / Superseded By
```
