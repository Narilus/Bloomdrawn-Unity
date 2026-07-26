# Bloomdrawn - Game Design Document

**Working Title:** Bloomdrawn  
**Genre:** Ethical gacha, party-driven roguelike deckbuilder  
**Mode:** Single-player first  
**Monetisation:** None. Pulls are earned through play only.  
**Target Build:** Unity 6.5 (`6000.5.x`) 2D project. Windows is the primary development and validation target; the deterministic engine and content layers remain platform-neutral, and the first public release platform remains gated by DD-14. The exact Unity 6.5 patch used by the repository is pinned in `ProjectSettings/ProjectVersion.txt`.  
**Inspirational Neighbourhood:** Morimens and Chaos Zero Nightmare for party/gacha/deckbuilder positioning; Slay the Spire for card legibility; Darkest Dungeon for party pressure; cosy horror media for intentional tonal dissonance.

---

## 0. Document Purpose and Authority

This document is the starting design contract for Bloomdrawn. It defines the experience we are building toward, the rules boundaries implementation must respect, and the design questions that must be resolved before deeper production work begins.

When this document, task plans, tests, and implementation disagree, the conflict must be raised and resolved. Code must not silently invent rules for combat, rewards, gacha outcomes, saves, or progression.

Design annotations:

- **[DECISION]** means the rule is accepted unless this document is revised.
- **[TUNING]** means the number is provisional and expected to move during testing.
- **[INVARIANT]** means implementation and tests must protect the rule.
- **[OPEN]** means the question is intentionally unresolved.

### 0.1 Project Contract Goals

This contract establishes:

1. The product identity: ethical gacha, roguelike deckbuilding, party ownership, calm pastel horror.
2. A transparent ethical gacha contract with no monetisation or hostile retention design.
3. A clear separation between aesthetic dissonance and player-hostile deception.
4. A conservative deterministic rules-engine architecture so implementation does not sprawl into Unity scene, prefab, or presentation-only logic.
5. A Unity 6.5 workflow with schema-driven content, deterministic commands/events, and strict source-of-truth governance.
6. Permanent combat presentation infrastructure early enough to validate battlefield layout, independent actors, card feel, and interaction stability without pulling future gameplay systems forward.
7. The major open decisions needed before combat, gacha, progression, and release packaging are implemented.

### 0.2 Source of Truth Hierarchy

In descending order of authority:

1. `docs/DESIGN.md`
2. `plans/design-decisions.md`, for approved decision records that explicitly amend `docs/DESIGN.md`
3. `plans/implementation_plan.md` and approved task plans
4. Automated rules and validation tests
5. Engine implementation
6. UI implementation and presentation

Lower layers may reveal ambiguity, but they do not override higher layers by accident.

---

## 1. Vision and Core Pillars

### 1.1 Vision Statement

Bloomdrawn is a party-driven roguelike deckbuilder about collecting soft, beautiful, deeply wrong companions and leading them through gentle-looking places where the Bloom refracts bodies, beliefs, souls, and hidden transformations into impossible horror.

The player builds a party from a growing roster, enters a run, pilots a shared tactical deck, and earns pulls through play. The game should satisfy the collection and teamcrafting appeal of gacha games without using money, pressure, or live-service manipulation.

### 1.2 Core Pillars

1. **Ethical Collection.** Pulls are exciting because they are earned, transparent, and generous, not because the player is pressured to spend.
2. **Party as Engine.** Characters are not skins. Party composition changes the deck, resource loops, defensive profile, and run plan.
3. **Pastel Wrongness.** The palette says calm, safety, and care. The events, enemies, card effects, and lore quietly prove that beautiful transformation can be dangerous without ceasing to be beautiful.
4. **Legible Tactics.** Cards, intents, resources, and consequences are clear before commitment. Horror may be strange; rules should not be.
5. **Runs as Stories.** A run is a sequence of temptations, sacrifices, discoveries, and compounding consequences.
6. **No Hostile Retention.** No paid currency, no stamina timers, no expiring paid offers, no dark-pattern login pressure, no design that punishes healthy disengagement.
7. **Rules Before Content.** Content definitions are data-driven and validated against a deterministic engine rather than implemented as one-off UI exceptions.

### 1.3 Intended Session Shape

A standard successful run should target **35-70 minutes** after onboarding. Shorter challenge modes and longer late-game acts may exist later.

Initial run shape targets:

- 1 party of 4 characters. **[DECISION]**
- 1 shared deck assembled from party cards plus run acquisitions.
- 3-5 major route branches. **[TUNING]**
- 6-10 combat encounters. **[TUNING]**
- 1 final boss or crisis encounter per run. **[TUNING]**
- Multiple non-combat nodes that alter deck, health, party state, currencies, or future choices.

---

## 2. Identity, Tone, and Aesthetic Direction

### 2.1 Tone

Bloomdrawn is not grimdark on the surface. It is soft, airy, and inviting. Its horror comes from contradiction: friendly UI around dreadful choices, delicate character art around inhuman biology, cheerful reward rhythms around transformation and loss.

The player should feel curiosity and unease more often than shock. The game may contain body horror, cosmic horror, memory loss, identity erosion, impossible anatomy, cultic ritual, and predatory environments, but it should avoid cheap cruelty and gratuitous gore.

### 2.2 Visual Direction

- Pastel palettes dominate the first read: petal pink, faded mint, powder blue, lavender, cream, warm grey, and soft yellow.
- Horror enters through form, animation, wording, negative space, and consequence.
- The majority of the roster should be adult women, reflecting the expected gacha audience interest in attractive female character forms.
- Character designs should mix attractiveness, elegance, and horror. They should be desirable collection targets without relying on pornographic framing or reducing characters to body-display alone.
- Silhouettes, apparent adult ages, body language, costume structures, and cultural references should vary meaningfully.
- Every character is visibly marked by her Domain through body language, materials, palette, anatomy, costume, aura, or impossible details. The exact marks can change, but the Domain should be readable.
- Character in-combat sprites are full-body and readable at gameplay scale.
- Enemies and environments may look toy-like or storybook-safe at first, especially early in a run, but their monstrous elements should become apparent through tentacles, maws, impossible joints, predatory blooms, wrong shadows, and other horror motifs.
- UI should feel calm and tactile: paper, vellum, enamel pins, pressed flowers, soft ink, stitched cloth, glass charms.
- The wrongness should be discoverable at gameplay scale, not hidden in tiny lore-only details.

### 2.3 Domain Colour and Material Language

The launch Domains retain the four-Domain structure. The Bloom is not a fifth Domain; it refracts through the Domains and through what characters already want, fear, worship, preserve, or hide.

Domain horror should remain art-directable for a character-collection game. It comes through material, silhouette, pose, costume, body language, animation, and implication as much as saturation.

| Domain | Accent | Material | Motif |
|---|---|---|---|
| Flesh | petal pink / rose red / warm bile cream | wet tissue, suture, blood, petals, soft membrane | visible Bloom-embrace, beauty, propagation, mouths, Embryo |
| Abyss | seafoam teal / soft blue / abyssal navy | water, scale, bioluminescence, ritual cloth, pressure glass | old-god devotion, cultic longing made physical, tentacles, lure-lights |
| Spirit | lavender / pale gold / milk white | crystal, smoke, choir-light, veil cloth, hybrid anatomy | humanoid-first chimera, soul continuity, Essence, halos, hymn-lines |
| Void | chalk white / muted obsidian / eclipse grey | matte enamel, shadowed cloth, thresholds, quiet distortions | wildcard or internalized transformation, rule-bending, uncategorized power |

### 2.4 Dissonance Rules

The pastel presentation is not an excuse to trick the player about content. Bloomdrawn can create emotional dissonance while still being honest and respectful.

**[INVARIANT]** The game must provide content warnings and settings for intensity where appropriate.

**[INVARIANT]** Mechanical consequences must be previewed clearly before the player commits to a choice.

**[INVARIANT]** Gacha presentation must not disguise odds, duplicate outcomes, pity state, or direct-pull income.

### 2.5 The Bloom and Refraction

The world did not simply suffer an invasion. It entered Bloom.

The **Bloom** is a generative cosmic pressure or event that compels bodies, ecosystems, structures, memories, beliefs, water, and hidden transformations to grow beyond their intended boundaries. It is dangerous because it offers real beauty, adaptation, healing, strength, belonging, or revelation alongside loss, hunger, replacement, and distortion.

**[DECISION]** The Bloom is Bloomdrawn's central unifying world phenomenon.

**[INVARIANT]** The Bloom is not a fifth Domain and does not replace Domain-specific material, gameplay, or visual identity.

**[INVARIANT]** The full origin, intention, and cosmic category of the Bloom remain unresolved. Writing may offer theories, cult claims, evidence, and contradictions, but should not reduce the Bloom to a fully explained alien species with a simple plan.

**[INVARIANT]** The Bloom remains beautiful enough that accepting it can seem reasonable.

The Bloom refracts differently through each Domain:

- **Flesh:** the Bloom is embraced as beauty, gift, propagation, and bodily transformation. Flesh characters are the most visibly altered, often using flower, petal, bloom, blood, flesh, mouth, and Embryo imagery.
- **Abyss:** the Bloom refracts old-god devotion, cultic longing, and imagined divinity into tangible forms. Tentacles are not merely sea-monster anatomy; they are belief, worship, and desire becoming physical. Whether the old god was real before the Bloom remains ambiguous.
- **Spirit:** the Bloom refracts bodies into humanoid-first hybrids and chimera while soul, spirit, memory, or Essence remains the unifying identity. Some Spirit beings are less physically refracted but unusually strong of spirit.
- **Void:** the Bloom is internalized, uncategorized, or only partially expressed. Void is the wildcard lane for characters whose transformations do not clearly belong to Flesh, Abyss, or Spirit.

Those substantially altered, summoned, empowered, or compelled by the Bloom may be called **Bloomdrawn**. The player characters are not ordinary people standing outside the horror; they are marked beings who can survive and use proximity to the Bloom while remaining vulnerable to further change.

### 2.6 Audio Direction

- Music begins gentle, melodic, and sparse.
- Horror is layered underneath through detuning, reversed textures, breath-like pads, music-box irregularity, and unresolved cadences.
- UI sounds should be soft and pleasant by default.
- Combat impacts should be readable without becoming abrasive.
- Rare pulls should feel celebratory, but not casino-like.

### 2.7 Accessibility Direction

Required from the beginning:

- readable card text at common desktop and handheld resolutions;
- colourblind-safe gameplay information;
- no information conveyed by colour alone;
- reduced motion option;
- screen-shake control;
- animation speed controls for repeated combat actions;
- plain-language rules tooltips;
- input support that does not rely on drag precision alone.

---

## 3. The Four Domains

Each launch Domain is a self-contained combat economy with its own resource rules, UI presentation, scaling axis, and intended tempo.

Domain resources are pooled at the **party-Domain** level: Flesh characters share one Embryo pool; Abyss characters share Tentacle count and Potency; Spirit characters share Essence. Void has no resource pool.

### 3.1 Flesh - Visceral / Body Horror

- **Resource:** **Embryo**, a non-negative integer.
- **Combat start:** 0 Embryo unless modified by a passive, relic, Symptom, or duplicate bonus.
- **Persistence:** persists across rounds within combat; resets at combat end.
- **Builders:** generate Embryo.
- **Spenders:** always resolve a useful basic effect. If the requirement is met, they consume the listed Embryo and resolve their enhanced effect instead.
- **[INVARIANT]** A Flesh spender may never consume Embryo unless its enhanced branch resolves successfully.
- **Scaling axis:** repeated build/spend rhythm, owner Attack, and effects that reward the number of Embryos consumed.
- **UI:** `Embryo: N`.

### 3.2 Abyss - Deep Sea / Eldritch

- **Resources:** **Tentacles** and **Potency**.
- **Combat start:** 0 Tentacles and base Potency 2 when at least one Abyss character is present.
- **Persistence:** both persist during combat and reset after combat.
- **Builders:** add Tentacles or Potency. Abyss has no spender taxonomy.
- **Automatic volley:** once during each ordinary player end phase, all Tentacles attack for `Tentacles x Potency` total base damage.
- **Targeting:** volleys focus the lowest-current-HP living enemy. Excess damage from a hit continues to the next lowest-current-HP enemy. Ties use stable enemy slot order.
- **Immediate triggers:** some cards cause an additional volley during the action phase. They do not consume Tentacles.
- **Launch scope guardrail:** the automatic volley is the launch Abyss rule. Post-launch characters may propose Abyss modifiers, suppression, replacement, or per-hit Tentacle triggers only through DD-22 and generic Domain modifier hooks, not character-name branches.
- **Scaling axis:** horizontal Tentacle count multiplied by vertical Potency growth.
- **UI:** `Tentacles: quantity x Potency = total`, with active volley modifiers shown beside it.

### 3.3 Spirit - Ethereal / Mystic

- **Resource:** **Essence**, a non-negative integer.
- **Combat start:** 0 Essence unless modified.
- **Persistence:** persists during combat and resets afterward.
- **Builders:** generate Essence.
- **Ritual cards:** Retain while in hand. At each ordinary player cleanup in which a Ritual remains in hand, its current cost decreases by 1 to a minimum of 0.
- **Cost reset:** when a Ritual leaves the hand for any reason, its temporary held-cost reduction resets to 0 before it next enters a hand.
- **Scaling axis:** Essence, time held, and the opportunity cost of occupying hand space.
- **General rule:** Spirit does not normally spend Essence. A character may explicitly violate this as a defining exception.
- **UI:** `Essence: N`.

### 3.4 Void - Cosmic / Entropy

- **Resource:** none.
- **Function:** economy manipulation, draw, cost changes, Weak, Vulnerable, Doom, intent reduction, and controlled Delay.
- **Scaling axis:** the value of the other Domains' engines and the encounter state it alters.
- **Control protection:** elites and bosses may have Control Resistance, converting hard control into a weaker but still useful effect rather than nullifying Void cards.
- **UI:** an `ACTIVE` Domain badge plus current rule-modifier chips such as `Borrowed Mana: -1 next round` or `Enemy damage -30% this round`.

### 3.5 Domain Helper UI

Only represented Domains appear:

- Flesh -> `Embryo: N`
- Abyss -> `Tentacles: Q x P = T`
- Spirit -> `Essence: N`
- Void -> `ACTIVE` plus current rule modifiers

Counters animate when changed but settle immediately to the authoritative value. Tooltips explain current modifiers and their expiry.

## 4. Party and Character System

### 4.1 Party Contract

**[DECISION]** A run party contains exactly four unique owned characters.

The party contributes:

- shared maximum HP;
- starting deck cards;
- passive effects;
- ultimate actions;
- Domain engines;
- owner identities for card formulas and progression hooks;
- tags used by rewards, events, routing, and future character stories.

The party should be understood as one tactical organism built from four personalities. The player is not piloting four separate hands or four separate HP bars; she is composing a shared deck and shared survival pool from four character engines.

### 4.2 Character Definition Schema

Every character definition contains:

- **Identity:** stable definition ID, name, title, Domain, character rarity/acquisition category, lore summary, profile quote, voice/text barks, portrait reference, full-body combat sprite reference, and ultimate VFX reference.
- **Presentation:** adult apparent age band, silhouette notes, costume structure, palette notes, horror motif, pose language, and content-warning tags where relevant.
- **Core stats:** HP, Attack, and Defense at each level/ascension breakpoint.
- **Passive:** combat-scoped or run-scoped and explicitly labelled.
- **Ultimate:** gauge cost, targeting rules, confirmation rules, base effect, Transcended effect, and VFX/audio identity.
- **Five-card personal set:** Strike, Shield, Transcend, Flavor A, Flavor B.
- **Generated cards:** cards created by the character's kit, including lifetime and destination.
- **Progression:** ownership state, level, EXP, ascension stage, duplicate tier, signature weapon linkage, equipment loadout eligibility, and any unlocked profile/lore entries.

**[DECISION] Stat reconciliation:** Attack is the player-facing name for Power; Defense is the player-facing name for Resolve. Engine formulas use canonical identifiers `attack`, `defense`, and `maxHp`.

**[DECISION] Character rarity:** all playable characters are SSR. Character definitions still declare rarity so banner pools, roster filters, audit logs, and future content validation can prove that no lower-rarity playable character entered the roster by accident.

**[DECISION] Advanced character mechanics:** DD-22 is approved as a future-gated extension policy. Post-launch characters may introduce run-persistent self-debt, party-level stances/aspects, Domain engine replacement, damage-to-status conversion, per-damage-instance reaction engines, or party-scoped Domain-resource reaction/amplifier systems only through explicit future task contracts, schema/save/UI/replay review, and deterministic tests. These mechanics are not launch M2 rules.

### 4.3 Shared HP Construction

- Party Max HP equals the sum of the four selected characters' current Max HP after persistent progression and run-start modifiers.
- The party has one current HP value and one ordinary Shield value.
- Effects that scale from `owner.maxHp` read the owning character's personal Max HP.
- Effects that scale from `party.maxHp` read the shared maximum.
- Damage and healing alter the shared pool unless an effect explicitly manipulates a character-specific contribution modifier.
- Current HP persists between encounters. Ordinary Shield does not.
- If Max HP changes during a run, current HP changes only when the effect explicitly grants or removes current HP as well.
- Individual character portraits may show contribution, statuses, and ultimate readiness, but they do not display separate HP bars in the launch combat model.

### 4.4 Character Presentation Rules

Characters should be emotionally inviting at first glance and unsettling on closer inspection. The player should understand why she wants to collect a character before she understands everything wrong with that character.

Launch roster presentation requirements:

- The majority of the roster, and all eight launch characters unless later revised, are adult women.
- Attractiveness is a valid collection draw and should be designed consciously.
- Sexualisation must not become the only form of desirability; elegance, confidence, melancholy, menace, tenderness, and uncanny beauty all matter.
- Silhouettes, apparent adult age ranges, body language, costume structures, and cultural references vary meaningfully.
- Every character is visibly marked by her Domain through body, costume, aura, material, palette, or impossible anatomy.
- Every character should imply a personal answer to transformation: embrace, care, devotion, adaptation, fear, negotiation, concealment, resistance, or use.
- Character in-combat sprites are full-body and readable at gameplay scale.
- Character horror should be readable in the sprite, not hidden only in card text.
- No desirable character archetype is allowed to become the default body, face, pose, or costume template for the full roster.

### 4.5 The Five-Card Personal Set

**[DECISION]** Every launch character contributes exactly one copy of each of five cards to the starting deck:

1. **Scaling Strike:** cost 1 by default; owner-Attack damage; grants owner ultimate charge.
2. **Scaling Shield:** cost 1 by default; owner-Defense Shield; grants owner ultimate charge.
3. **Transcend:** cost 1 by default; one use per combat; enters the Graveyard after resolving; upgrades the owner for the current combat and grants substantial ultimate charge.
4. **Flavor Card A:** Domain or character engine card.
5. **Flavor Card B:** a mechanically distinct second engine, payoff, defence, or utility card.

All cards have a stable definition ID and every runtime card instance has a unique instance ID and `ownerCharacterInstanceId`.

### 4.6 Owner Awareness

Every card reads stats and ultimate gauge from its owner. Domain resources remain pooled by Domain, not by owner.

- A Flesh card owned by Mara and one owned by Venelis read different Attack values but spend from the same party Flesh Embryo pool.
- Ultimate charge generated by a card goes only to that card's owner unless the effect explicitly states otherwise.
- Run-acquired Domain cards must be assigned to an eligible party member when acquired. If multiple owners are eligible, the player chooses before accepting the card.
- Neutral cards, if later introduced, must still declare an owner or use an explicitly defined party-scaling formula. There is no implicit ownerless card state.

**[INVARIANT]** No runtime card may enter a player deck without a valid ownership contract.

### 4.7 Ultimate Rules

- Each character tracks a separate gauge from 0 to the ultimate's current cost, normally 100.
- Gauge persists throughout a combat and resets at combat end.
- Charge beyond the current cost is capped unless an effect explicitly allows overcharge.
- A ready ultimate may be used only during the ordinary player action phase while no animation or command is resolving.
- Hover/focus displays the full effect and target information.
- Activation requires a deliberate confirm action.
- Ultimates do not consume Mana unless explicitly stated.
- An ultimate cannot be activated recursively while another ultimate is resolving.
- Transcend upgrades are combat-scoped and reset after combat.

### 4.8 Transcend Rules

- Each starting Transcend card may resolve at most once per combat.
- After successful resolution, it enters the Graveyard.
- If countered or invalidated before resolution, the command fails and the card remains in hand; there is no silent consumption.
- Generated cards granted by Transcend are combat-scoped unless explicitly marked otherwise.
- A generated Transcend card cannot bypass the owner's once-per-combat Transcend flag.
- Transcend cards should be mechanically exciting, but the base character must function before Transcending.

### 4.9 Launch Roster and Onboarding

The launch roster contains eight characters, two per Domain.

**[DECISION] Production onboarding:** the profile begins with Mara, Thalassa, Sephira, and Azael, guaranteeing one character from each Domain and a valid party of four. The approved tutorial is a short sequence with one focused teaching beat per launch Domain, using curated cards and encounters, and ending in a complete starter-party combat. Venelis, Nyxalia, Kibane, and Mira Nox enter the standard gacha pool immediately after banners unlock.

**[DECISION] Development profile:** automated tests, balance tools, and the local developer profile own all eight launch characters at level 1 so the entire system can be exercised without gacha setup.

All numbers below are **[TUNING]** anchors. Costs and rule text are part of the design contract; coefficients may change during balance passes without changing the underlying identity.

### 4.10 Flesh Launch Characters

#### 4.10.1 Mara - The Graft-Sister

**Domain:** Flesh  
**Acquisition:** Starter  
**Role:** controlled Embryo builder/spender; sustain  
**Base stats:** HP 120, ATK 14, DEF 6

**Visual design:** Mara reads as a composed adult woman in nurse-sister vestments and pressed-flower surgical layers. Her silhouette is soft and caretaking until the viewer notices graft seams, petal-like scar tissue, small bone clasps, and the suggestion that her sleeves are grown rather than sewn. Her palette sits in rose pink, warm cream, and muted arterial red.

**Passive - Symbiotic Graft:** At combat start, gain 2 Embryo. Whenever a Flesh spender resolves its enhanced branch, heal the party for 2 HP.

**Ultimate - Blooming Autotomy (100):** Consume all Embryo. For each consumed Embryo, deal `8 + owner.ATK` damage to the current lowest-HP enemy, overflowing in stable order, and gain 3 Shield.

**Transcended Ultimate:** Each Embryo consumed also applies 2 Bleed to the enemy struck.

| Card | Cost | Target | Effect |
|---|---:|---|---|
| **Incise** - Strike | 1 | One enemy | Deal `6 + ATK`. Gain 8 ultimate charge. |
| **Suture** - Shield | 1 | Party | Gain `5 + DEF` Shield. Gain 8 ultimate charge. |
| **Eviscerate the Bloom** - Transcend | 1 | Self | Upgrade the Ultimate for this combat; gain 40 ultimate charge; create **Graft: Hepatic** in hand; Graveyard. |
| **Gestate** - Builder | 1 | Party | Gain 2 Embryo. If Embryo is now at least 3, draw 1. |
| **Rupture** - Spender | 2 | One enemy | Basic: deal `4 + ATK`. Enhanced at 2 Embryo: consume 2, deal `12 + 2 x ATK`, and apply 2 Bleed. |

**Generated - Graft: Hepatic:** Cost 0. Gain 2 Embryo and draw 1. Exhaust to Graveyard. Combat-scoped.

#### 4.10.2 Venelis - The Open Bloom

**Domain:** Flesh  
**Acquisition:** Standard gacha after banners unlock  
**Role:** reckless Embryo acceleration and area payoff  
**Base stats:** HP 100, ATK 16, DEF 4

**Visual design:** Venelis is a glamorous adult woman whose beauty is too open, too floral, too biological. Her dress has layered petals that read as couture at a distance and exposed inner tissue up close. Her posture is inviting and theatrical, with maw-like bloom shapes hidden in hair ornaments, bodice folds, and shadow negative space.

**Passive - Open Heart:** During each ordinary player turn start, lose 2 shared HP, then gain 1 Embryo. This cannot reduce the party below 1 HP.

**Ultimate - Crown of Mouths (100):** Consume all Embryo. For each consumed Embryo, schedule one Maw at the next ordinary player end phase. Each Maw bites the current highest-HP enemy for `10 + ATK`.

**Transcended Ultimate:** Each Maw also heals the party for 3 after dealing damage.

| Card | Cost | Target | Effect |
|---|---:|---|---|
| **Rend** - Strike | 1 | One enemy | Deal `7 + ATK`. Gain 8 ultimate charge. |
| **Carapace** - Shield | 1 | Party | Gain `4 + DEF` Shield. Gain 8 ultimate charge. |
| **Heartflower Unfold** - Transcend | 1 | Self | Upgrade the Ultimate for this combat; gain 40 ultimate charge; create **Petal Guard** in hand; Graveyard. |
| **Seep** - Builder | 1 | Party | Lose 3 shared HP, gain 3 Embryo, then draw 1. Cannot be played if the HP loss would be lethal. |
| **Detonate Bloom** - Spender | 2 | All enemies | Basic: deal `3 + ATK` to all. Enhanced at 3 Embryo: consume 3, deal `15 + 2 x ATK` to all, then gain 8 Shield. |

**Generated - Petal Guard:** Cost 1. Gain `4 + DEF + 2 x current Embryo` Shield. Exhaust to Graveyard. Combat-scoped.

### 4.11 Abyss Launch Characters

#### 4.11.1 Thalassa - Drowned Choir

**Domain:** Abyss  
**Acquisition:** Starter  
**Role:** Tentacle quantity and durable baseline scaling  
**Base stats:** HP 110, ATK 12, DEF 7

**Visual design:** Thalassa is an adult woman with the serene bearing of a drowned saint. Her silhouette uses long veils, ribboning hair, and submerged choir robes. The first read is blue-white calm; the second read reveals gill seams, pressure bruising, barnacle jewellery, and tendrils braided into her sleeves.

**Passive - Tidewake:** At combat setup, increase starting Potency by `floor(party Max HP / 100)`, minimum +1 and maximum +4. This is calculated once per combat.

**Ultimate - Leviathan's Hymn (100):** Add 3 Tentacles and 2 Potency.

**Transcended Ultimate:** After adding Tentacles and Potency, trigger one immediate Tentacle volley.

| Card | Cost | Target | Effect |
|---|---:|---|---|
| **Brine Slash** - Strike | 1 | One enemy | Deal `6 + ATK`. Gain 8 ultimate charge. |
| **Kelp Ward** - Shield | 1 | Party | Gain `5 + DEF` Shield. Gain 8 ultimate charge. |
| **Deepsong Rapture** - Transcend | 1 | Self | Upgrade the Ultimate for this combat; gain 40 ultimate charge; create **Undertow** in hand; Graveyard. |
| **Summon Tendril** - Builder | 1 | Party | Add 2 Tentacles. |
| **Crushing Depth** - DPS | 2 | Automatic | Trigger an immediate Tentacle volley. Each enemy hit gains 1 Drown. |

**Generated - Undertow:** Cost 1. Trigger an immediate Tentacle volley; if it kills an enemy, add 1 Tentacle. Exhaust to Graveyard. Combat-scoped.

#### 4.11.2 Nyxalia - The Lurelight

**Domain:** Abyss  
**Acquisition:** Standard gacha after banners unlock  
**Role:** Potency scaling and efficient Abyss sequencing  
**Base stats:** HP 95, ATK 13, DEF 5

**Visual design:** Nyxalia is an adult woman styled as a luminous deep-sea idol: elegant, playful, and predatory. She uses translucent fabrics, lurelight jewellery, fin-like sleeves, and soft teal highlights. Her horror read comes from too-wide reflection points in the eyes, floating hair that ignores gravity, and tendrils posed like stage ribbons.

**Passive - Allure:** The first Abyss-tagged card played during each ordinary player action phase costs 1 less, minimum 0. This discount refreshes once per ordinary player turn only.

**Ultimate - Abyssal Lure (100):** Gain 3 Potency and add 1 Tentacle. Until the current enemy phase ends, enemy attack damage is reduced by 25%.

**Transcended Ultimate:** Also add Tentacles equal to the current Tentacle count before the Ultimate, maximum +5.

| Card | Cost | Target | Effect |
|---|---:|---|---|
| **Lumen Strike** - Strike | 1 | One enemy | Deal `6 + ATK`. Gain 8 ultimate charge. |
| **Mariana Veil** - Shield | 1 | Party | Gain `5 + DEF` Shield. Gain 8 ultimate charge. |
| **Beckon the Below** - Transcend | 1 | Self | Upgrade the Ultimate for this combat; gain 40 ultimate charge; create **Phosphor Sting** in hand; Graveyard. |
| **Amplify** - Builder | 1 | Party | Gain 2 Potency. If Tentacles are at least 3, gain 1 additional Potency. |
| **Phosphor Volley** - DPS | 2 | Automatic | Trigger an immediate Tentacle volley. The ordinary end-phase volley this round deals 50% additional damage. |

**Generated - Phosphor Sting:** Cost 0. Deal `3 + ATK`; add 1 Tentacle if the target has Drown. Exhaust to Graveyard. Combat-scoped.

### 4.12 Spirit Launch Characters

#### 4.12.1 Sephira - Vesper Cantor

**Domain:** Spirit  
**Acquisition:** Starter  
**Role:** stable Essence generation and broad Ritual payoff  
**Base stats:** HP 105, ATK 10, DEF 8

**Visual design:** Sephira is an adult woman in chapel-singer finery, built from lavender veils, pale gold fixtures, and glass-chime ornaments. She should look safe and compassionate until her halo appears slightly off-centre, her shadow sings in another pose, and her crystal fractures line up like sheet music under the skin.

**Passive - Liturgy:** During each ordinary player turn start, if at least one Ritual was retained from the previous cleanup, gain 1 Essence.

**Ultimate - Coda of Mourning (100):** Deal `5 x Essence` damage to all enemies. Essence is not consumed.

**Transcended Ultimate:** Also grant Retain to every non-Curse card currently in hand for the next cleanup only.

| Card | Cost | Target | Effect |
|---|---:|---|---|
| **Chant Blade** - Strike | 1 | One enemy | Deal `6 + ATK`. Gain 8 ultimate charge. |
| **Warding Hymn** - Shield | 1 | Party | Gain `5 + DEF` Shield. Gain 8 ultimate charge. |
| **Ascendant Verse** - Transcend | 1 | Self | Upgrade the Ultimate for this combat; gain 40 ultimate charge; create **Kyrie** in hand; Graveyard. |
| **Gather Essence** - Builder | 1 | Party | Gain 3 Essence. Retain while unplayed. |
| **Requiem** - Ritual | 4 | One enemy | Retain; held cost decreases by 1 per ordinary cleanup, minimum 0. Deal `(12 + ATK) x (1 + 0.5 x Essence)`. |

**Generated - Kyrie:** Ritual, base cost 4. Deal `(8 + ATK) x (1 + 0.4 x Essence)` to all enemies. Combat-scoped; Exhaust after play.

#### 4.12.2 Kibane - The Foxfire Chimera

**Domain:** Spirit  
**Acquisition:** Standard gacha after banners unlock  
**Role:** fragile high-risk Ritual finisher  
**Base stats:** HP 85, ATK 11, DEF 6

**Visual design:** Kibane is an adult humanoid kitsune/dragon chimera with a readable fox-girl first impression: fox ears, a graceful tail silhouette, sharp eyes, and shrine-dancer poise. Her dragon inheritance appears as small swept horns, scale accents along shoulders and hips, clawed hands, and lacquered scale plates worked into ritual costume rather than bulky armour. Her Spirit horror is that one soul is visibly binding several inherited shapes into a single deliberate body: foxfire wisps trace sutra-like loops around her tails, scale seams glow with Essence, and her ritual ribbons look tied as much to hold her together as to adorn her.

**Passive - Ninefold Resonance:** During each ordinary player turn start, gain 1 Essence for each Ritual retained from the previous cleanup.

**Ultimate - Foxfire Cataclysm (100):** **Character exception:** consume all Essence and deal `20 x consumed Essence` damage to the highest-HP enemy. At 6 or more consumed Essence, apply Stun; Control Resistance converts this to Falter.

**Transcended Ultimate:** Damage becomes `25 x consumed Essence` and ignores ordinary Shield.

| Card | Cost | Target | Effect |
|---|---:|---|---|
| **Clawflame Strike** - Strike | 1 | One enemy | Deal `6 + ATK`. Gain 8 ultimate charge. |
| **Scaled Aegis** - Shield | 1 | Party | Gain `4 + DEF` Shield. Gain 8 ultimate charge. |
| **Unseal the Chimera** - Transcend | 1 | Self | Upgrade the Ultimate for this combat; gain 40 ultimate charge; create **Foxfire Descent** in hand; Graveyard. |
| **Kindle Essence** - Builder | 1 | Party | Gain 2 Essence and 1 Mana. Retain while unplayed. |
| **Dragon-Vow Sutra** - Ritual | 5 | All enemies | Retain; held cost decreases by 1 per ordinary cleanup. Deal `(10 + ATK) x (1 + 0.4 x Essence)` to all. At 8 or more Essence, the final result is doubled. |

**Generated - Foxfire Descent:** Ritual, base cost 3. Deal `(14 + ATK) x (1 + 0.3 x Essence)` to one enemy and apply 2 Doom. Combat-scoped; Exhaust after play.

### 4.13 Void Launch Characters

#### 4.13.1 Azael - The Between

**Domain:** Void  
**Acquisition:** Starter  
**Role:** temporary economy, cost manipulation, and controlled enemy delay  
**Base stats:** HP 100, ATK 12, DEF 6

**Visual design:** Azael is an adult woman with a poised threshold-guardian silhouette: a long coat with layered veil panels, chalk-white edging, keyhole and hinge ornaments, asymmetrical gloves, and sealed inner seams that suggest something contained rather than displayed. Her attractiveness should be austere and liminal rather than lush. Her Void horror is implementable through concrete details: a muted door-frame halo behind her portrait, cut-out hems shaped like key teeth, mismatched reflection motifs in polished accessories, and hands posed around visible hinge-charms or threadlike threshold lines.

**Passive - Threshold:** At ordinary player cleanup, if at least 2 Mana remains, store 1 Threshold Mana for the next ordinary player turn. The bonus is applied after Mana refill and Mana Debt, then removed.

**Ultimate - Still the Hinge (100):** Draw 2. Choose up to two cards in hand; until they leave hand or the current action phase ends, their costs are reduced to 0. Apply Delay to one enemy. Control Resistance converts Delay to Falter as normal.

**Transcended Ultimate:** Also gain 2 Mana and clear up to 2 Mana Debt before choosing cards.


| Card | Cost | Target | Effect |
|---|---:|---|---|
| **Void Edge** - Strike | 1 | One enemy | Deal `6 + ATK`. Gain 8 ultimate charge. |
| **Null Ward** - Shield | 1 | Party | Gain `5 + DEF` Shield. Gain 8 ultimate charge. |
| **Fold the Hour** - Transcend | 1 | Self | Upgrade the Ultimate for this combat; gain 40 ultimate charge; create **Lag** in hand; Graveyard. |
| **Borrow** - Economy | 0 | Party | Gain 2 Mana now and draw 1. Add 1 Mana Debt for the next ordinary turn. |
| **Hasten** - Control | 2 | One enemy | Apply Delay. A target already Delayed cannot be Delayed again. Control Resistance converts Delay to Falter. |

**Generated - Lag:** Cost 1. Apply Delay to one enemy; if converted to Falter, also draw 1. Exhaust to Graveyard. Combat-scoped.

#### 4.13.2 Mira Nox - Eventide

**Domain:** Void  
**Acquisition:** Standard gacha after banners unlock  
**Role:** debuff amplification and encounter-wide mitigation  
**Base stats:** HP 95, ATK 13, DEF 5

**Visual design:** Mira Nox is an adult woman dressed like a pastel eclipse: soft evening gradients, a twilight veil, crescent fasteners, veiled star pins, and shadow-lined cape panels over restrained occult formalwear. She should feel composed, romantic, and deliberately contained. Her Void horror comes from the sense that she keeps worse things sealed inside the costume language: clasped cape layers, dark inner linings, star charms pinned like wards, and a calm gaze that reads as protection as much as menace.

**Passive - Penumbra:** At combat setup, all enemies gain 1 turn of Vulnerable.

**Ultimate - Total Eclipse (100):** Apply 3 turns of Vulnerable and 2 turns of Weak to all enemies. Remove removable positive status durations from them. Permanent traits, phase rules, Control Resistance, and unremovable boss effects are unaffected.

**Transcended Ultimate:** Also apply 3 Doom to all enemies.

| Card | Cost | Target | Effect |
|---|---:|---|---|
| **Twilight Cut** - Strike | 1 | One enemy | Deal `6 + ATK`. Gain 8 ultimate charge. |
| **Shade Veil** - Shield | 1 | Party | Gain `5 + DEF` Shield. Gain 8 ultimate charge. |
| **Umbra Surge** - Transcend | 1 | Self | Upgrade the Ultimate for this combat; gain 40 ultimate charge; create **Eclipse Mark** in hand; Graveyard. |
| **Fracture** - Debuff | 1 | One enemy | Apply 2 turns of Vulnerable. If already Vulnerable, also apply 2 turns of Weak. |
| **Time Erode** - Rule bend | 2 | All enemies | Enemy attack damage is reduced by 30% for the current enemy phase. |

**Generated - Eclipse Mark:** Cost 0. Apply 1 Vulnerable and 2 Doom to one enemy. Exhaust to Graveyard. Combat-scoped.

---

## 5. Card, Deck, and Hand Contract

### 5.1 Card Instance Fields

Every runtime card instance contains at minimum:

- unique `instanceId`;
- stable `definitionId`;
- `ownerCharacterInstanceId`;
- base Mana cost;
- current cost modifiers;
- tags;
- target specification;
- current pile;
- temporary upgrade state;
- Ritual held-reduction value;
- combat-scoped/generated flags;
- run-scoped upgrade identifier, if any.

**[INVARIANT]** A card instance exists in exactly one legal location at a time.

### 5.2 Starting Deck

A party of four contributes five cards per character for a 20-card starting deck. All 20 cards begin each combat in the draw pile, except cards explicitly created at combat setup. The draw pile is shuffled by the combat's card-shuffle RNG stream.

### 5.3 Card Locations

- **Draw pile:** face-down cards available to draw.
- **Hand:** cards available to inspect and, where legal, play.
- **Discard:** played or discarded cards that may later be reshuffled.
- **Graveyard:** cards exhausted for the current combat. They cannot be drawn or recovered unless an effect explicitly names the Graveyard.
- **Resolving:** transient engine location while a card command is executing. It is not user-visible and may contain only the currently resolving card.

### 5.4 Hand Target and Maximum

- Ordinary hand target: 5 cards.
- Maximum hand size: 10 cards.
- At ordinary turn start, draw until the hand contains the target number, not five additional cards.
- Retained cards count toward the target. A hand already containing five or more cards draws nothing from the normal turn draw.
- Explicit draw effects may exceed the target but never the maximum.
- If an effect would draw above the maximum, excess cards remain in the draw pile and the Event Log records the prevented draw.

This makes Retain and Ritual powerful but not free: every retained card occupies future hand capacity.

### 5.5 Reshuffling

**[DECISION]** The discard pile is shuffled into a new draw pile only when a draw is requested and the current draw pile cannot satisfy it.

1. Draw as many cards as possible from the current draw pile.
2. If more cards are required and the discard is non-empty, shuffle the entire discard into a new draw pile.
3. Continue drawing.
4. If both piles are empty, draw ends early.

The discard pile is not automatically shuffled at the beginning of every round.

### 5.6 Playing a Card

A card command is legal only when:

- the combat is in a player action phase;
- the card is in hand;
- the player has sufficient current Mana after all cost modifiers;
- required targets exist and are legal;
- all card-specific preconditions are satisfied;
- no other command is currently resolving.

On successful play:

1. Lock the command and targets.
2. Move the card to `Resolving`.
3. Pay final Mana cost.
4. Resolve effects in printed order, including conditionals.
5. Grant listed ultimate charge.
6. Move the card to Discard or Graveyard according to its tags/effect.
7. Emit ordered combat events and presentation tokens.
8. Check victory and defeat after each atomic damage/heal/status event.

A failed validation pays no Mana and moves no card.

**[INVARIANT]** Hovering, selecting, dragging, arming, cancelling, or choosing a target is presentation state only. Those gestures consume no Mana, RNG, card movement, or authoritative state until a complete `PlayCard` command with all required targets is accepted by the engine.

### 5.7 Mana and Cost Modifiers

- Base maximum Mana: 6.
- At ordinary player turn start, current Mana refills to current maximum, then Mana Debt is deducted, then positive start bonuses are added.
- Current Mana cannot fall below 0.
- Unless explicitly stated, temporary cost discounts expire when the card leaves hand or the action phase ends.
- Ritual held reductions persist only while that instance remains in hand.
- Final card cost has a minimum of 0.
- Mana gained during an action phase may exceed maximum Mana unless the granting effect states otherwise; unspent Mana is normally lost at the next refill.
- **Mana Debt:** each stack reduces next ordinary turn's post-refill Mana by 1, then all stacks are removed.

### 5.8 Target Types

Canonical targets are:

- `selfOwner`;
- `party`;
- `oneEnemy`;
- `allEnemies`;
- `lowestHpEnemy`;
- `highestHpEnemy`;
- `randomLivingEnemy` using the combat target RNG stream;
- `automaticSequence` for Tentacle overflow and other rule-defined targeting;
- `oneCardInHand`, `oneCardInDiscard`, or other explicit card-zone targets if introduced later.

Random targeting is deterministic but still labelled as random in the UI. Where a target can be known at command time, the preview shows it.

### 5.9 Retain and Ritual

- Retain prevents ordinary cleanup discard.
- Ritual inherently includes Retain.
- Each Ritual that remains in hand during ordinary cleanup gains one held-reduction counter.
- A Ritual's held-reduction counter resets when played, discarded, exhausted, transformed, or otherwise removed from hand.
- Effects that Retain a non-Ritual card do not reduce its cost unless separately stated.

### 5.10 Generated Cards

Generated cards declare:

- destination on creation;
- owner;
- combat-scoped or run-scoped lifetime;
- whether they Exhaust;
- whether they may be generated more than once.

If generated directly to a full hand, they are placed on top of the draw pile unless the effect explicitly says to discard or lose them. Combat-scoped cards are removed when combat ends regardless of pile.

### 5.11 Future Card Memory and Copy Safety

Advanced card-memory mechanics, copied cards, source snapshots, and hidden-zone selection are approved only as future-gated extension points under DD-23. Launch M2 may reserve schema/state guardrails, but it must not implement runtime copy effects, hidden-zone reveal/selection, replay UI, save behaviour, or card-memory mechanics.

Default copy rules:

- copied cards preserve the source definition and source owner unless the approved effect says otherwise;
- copied cards are combat-scoped unless directly specified otherwise;
- combat-scoped copies are removed from Hand, Draw, Discard, Graveyard, and Resolving cleanup at combat end;
- run-scoped copies require a future DD-23 implementation task, save serialization, migration rules, and content validation;
- copied cards must carry copy-lineage metadata and cannot recursively copy, record, or snapshot themselves unless an approved exception says otherwise.

Visible hand-only temporary copies are still DD-23-gated runtime copy effects even when they do not reveal hidden zones. A future safe copy may restrict its source to an eligible starting-deck card in Hand, preserve source owner and upgrade ID, calculate discounts from printed base cost, reject temporary modifiers and spent once-per-instance flags, gain Exhaust and `copyProhibited`, and overflow to Draw in deterministic order.

Default eligible copy sources are:

- Hand;
- Draw;
- Discard;
- approved stored snapshots such as a future Pattern-like structure.

Default blocked copy sources and card categories are:

- Transcend;
- Graveyard;
- Exhaust or already-exhausted cards;
- generated cards;
- combat-scoped cards;
- Curse;
- Symptom;
- X-cost cards;
- existing copy, Recollection, Reprise, Wing, or equivalent copied-card instances;
- consumed once-per-instance cards or card instances with already-spent unique flags;
- cards tagged `copyProhibited`.

Any exception to these defaults must be approved through a future DD-23 follow-up task and covered by explicit deterministic tests. In particular, no copy effect may bypass an owner's once-per-combat Transcend flag.

Hidden-zone reveal and selection effects, such as choosing from the top of Draw before those cards would normally be visible, are also future-gated by DD-23. They must not leak hidden card order through preview, rejected commands, or cancelled selections.

### 5.12 Run-Acquired Cards

Card rewards are drawn from pools associated with the current party and represented Domains.

- Character-specific cards automatically belong to that character.
- Generic Domain cards require the player to assign an eligible owner from that Domain before accepting them.
- A reward preview shows the formula using the selected owner's current stats.
- Added cards are run-scoped and remain in the run deck until removed, transformed, or the run ends.
- Duplicate card definitions are legal unless a card explicitly has a copy limit.
- The launch scope contains no ownerless neutral cards.

### 5.13 Card Upgrades

**[DECISION]** Generic Rest-site card upgrading is outside the launch scope. Launch Rest nodes offer healing, Symptom/Curse treatment, or another authored recovery choice. Transcend remains the launch game's principal in-combat upgrade system. A later card-upgrade feature must define stable upgrade IDs and save migration before inclusion.

---

## 6. Combat Rules Contract

### 6.1 Combat Goals

Combat should be readable, tactical, and engine-driven. The fun is in making party resources interlock under visible enemy pressure, not in guessing hidden outcomes.

### 6.2 Combat State Machine

The engine uses explicit phases. Commands that are legal in one phase are rejected in every other phase.

1. `COMBAT_SETUP`
2. `PLAYER_TURN_START`
3. `PLAYER_ACTION`
4. `PLAYER_CLEANUP`
5. `PLAYER_END`
6. `ENEMY_PHASE_START`
7. `ENEMY_ACTION[n]` sequentially for each living enemy
8. `ENEMY_END`
9. `ROUND_END`
10. back to `PLAYER_TURN_START`, or `VICTORY` / `DEFEAT`


### 6.3 Combat Setup

In stable order:

1. Instantiate the party snapshot, enemies, and 20-card starting deck plus run additions.
2. Set shared current HP from the run state and ordinary Shield to 0.
3. Initialize Domain resources.
4. Reset ultimate gauges and Transcend state.
5. Apply encounter traits.
6. Apply party combat-start passives in party slot order.
7. Apply relic, Symptom, and boon combat-start effects in stable acquisition order.
8. Generate and reveal first enemy intents.
9. Shuffle the draw pile.
10. Enter the first ordinary player turn start.

### 6.4 Ordinary Player Turn Start

In order:

1. Increment round number.
2. Remove ordinary party Shield unless a modifier explicitly preserves it.
3. Refill Mana to current maximum.
4. Deduct all Mana Debt and clear it.
5. Apply stored next-turn Mana bonuses such as Threshold and clear them.
6. Resolve player-turn-start statuses and passives in stable order.
7. Draw until hand target.
8. Refresh once-per-ordinary-turn card discounts and flags.
9. Recompute previews and enter `PLAYER_ACTION`.

Enemy intents shown during this phase are the actions enemies will attempt in the upcoming enemy phase unless altered by the player.

### 6.5 Player Action Phase

The player may:

- inspect all cards, piles, characters, statuses, and intents;
- play legal cards;
- cast ready ultimates;
- inspect and collapse/expand the Event Log;
- end the turn;
- abandon through a confirmation flow.

Atomic commands resolve fully before another command is accepted.

### 6.6 Ordinary Player Cleanup

When End Turn is confirmed:

1. Record remaining Mana for effects such as Threshold.
2. Discard every non-Retain card remaining in hand.
3. Keep Retain and Ritual cards.
4. Apply one held-reduction counter to each Ritual retained through this cleanup.
5. Expire action-phase-only cost modifiers and player-action flags.
6. Enter `PLAYER_END`.

### 6.7 Ordinary Player End

Resolve in this order:

1. Scheduled "at end of player turn" effects in creation order, including Venelis's Maws.
2. One automatic Tentacle volley if Abyss is represented and at least one Tentacle exists.
3. Party-afflicting end-of-turn damage statuses.
4. Other player-end passives and relic effects in stable order.
5. Duration expiry tied to player end.
6. Enter `ENEMY_PHASE_START`.

### 6.8 Enemy Phase Start

- Apply enemy-phase-wide modifiers such as Time Erode or Abyssal Lure.
- Establish living enemy action order by stable slot.
- No enemy calculations are resolved simultaneously.

### 6.9 Sequential Enemy Actions

For each living enemy in slot order:

1. Remove that enemy's ordinary Shield if its shield-decay rule is `startOfOwnAction`.
2. If Stunned, consume one Stun and skip the intent.
3. Else if Delayed, consume Delay, preserve the current intent for next round, and skip the action.
4. Otherwise resolve the displayed intent exactly as previewed, subject to player-applied modifiers.
5. Play its ordered animation/event sequence before the next enemy begins.
6. Check victory/defeat after every atomic effect.

An enemy created during the enemy phase acts only if its summoning effect explicitly grants an immediate action; otherwise it joins stable order next round.

### 6.10 Enemy End and Next Intents

After all living enemies have had an action opportunity:

1. Resolve enemy-afflicting end-of-turn damage statuses in stable enemy order.
2. Resolve enemy end-passives and phase transitions.
3. Decrement durations tied to enemy end.
4. Remove enemy-phase-wide player modifiers.
5. Generate and reveal next intents for enemies that did not preserve a Delayed intent.
6. Enter `ROUND_END`, then the next ordinary player turn unless combat has ended.

### 6.11 Victory and Defeat

- Victory occurs immediately when no hostile enemy remains and no pending enemy replacement/phase transition exists.
- Defeat occurs immediately when shared HP reaches 0.
- **Atomic Stop:** combat checks terminal state after each atomic damage, healing, status, or effect event that can create victory or defeat. If terminal state is reached, combat enters `VICTORY` or `DEFEAT` immediately.
- After terminal state, remaining non-terminal sub-effects of the accepted action do not resolve.
- Explicitly declared death, replacement, or phase-transition effects may continue only when they are part of the same terminal-causing atomic event.
- If shared HP reaches 0 at the same terminal checkpoint as all enemies being defeated, defeat takes precedence unless an explicit encounter rule says otherwise.
- Victory returns current shared HP to the run state; ordinary Shield, gauges, combat resources, generated cards, temporary upgrades, and combat statuses are cleared.

### 6.12 Abandon

Abandon requires confirmation and ends the run rather than merely the encounter. The player receives only rewards explicitly banked on abandon under the run-reward rules.

---

## 7. Stats, Damage, Shield, Status, and Modifier Contract

### 7.1 Formula Evaluation

Card and effect formulas are typed expressions. They may read approved state such as owner stats, party stats, Domain resources, status stacks, cards in hand, or consumed-resource amount.

Calculation order:

1. Evaluate base expression and stat coefficients.
2. Floor to an integer, minimum 0 unless negative values are explicitly legal.
3. Apply outgoing additive modifiers.
4. Apply outgoing multiplicative modifiers and floor.
5. Apply target incoming multiplicative modifiers and floor.
6. Apply ignore-Shield or piercing rules.
7. Subtract ordinary Shield.
8. Subtract remaining damage from HP.

Multiple multiplicative modifiers multiply together. The UI preview uses the same engine function as resolution.

### 7.2 Healing

- Healing cannot raise current shared HP above party Max HP.
- Prevented overheal is logged only when relevant to an effect or passive.
- HP-loss costs are not damage and ignore Shield, Weak, Vulnerable, and damage reduction.
- A card with a lethal HP-loss cost is unplayable unless its text explicitly permits lethal payment.

### 7.3 Ordinary Shield

- Party Shield is shared and absorbs incoming party damage before HP.
- Ordinary party Shield is removed at the next ordinary player turn start.
- Enemy ordinary Shield is removed at the start of that enemy's own action unless its definition states another duration.
- Shield gain has no default cap.
- "Ignore Shield" bypasses ordinary Shield but does not remove it.
- Persistent barriers, phase armour, and other exceptional defences must use separate named statuses rather than silently changing ordinary Shield.

### 7.4 Incoming Damage Preview

The shared HP bar computes expected attack damage from currently displayed enemy intents using current visible modifiers.

- `Safe` appears when current Shield covers all currently previewable incoming HP damage.
- A skull appears when expected incoming damage after Shield is at least current HP.
- Conditional, random, delayed, summoned, or non-damage intent components are shown separately and may mark the preview as a range.
- The preview updates through the authoritative engine after every command.

### 7.5 Status Classification

Every status declares:

- owner side;
- target scope: party, character owner, enemy, or encounter;
- persistence scope: combat, run, or profile;
- magnitude and/or duration;
- stacking method;
- trigger timing;
- decrement timing;
- cleanse category;
- maximum stacks if any;
- whether it is removable;
- whether Control Resistance converts it.

Statuses fall into:

1. **Timed modifier:** fixed magnitude, duration stacks.
2. **Stacking damage status:** magnitude stacks, rule-defined decay.
3. **Control:** skips or postpones action.
4. **Removable positive status:** buff that can be dispelled.
5. **Permanent trait:** not removable.
6. **Phase rule:** encounter state, not a normal status.

Launch status implementations are combat-scoped unless a status explicitly says otherwise. Run-persistent harmful party statuses, delayed self-debt, and profile-persistent combat modifiers are reserved for DD-22 or a later approved feature gate.

### 7.6 Launch Status Glossary

| Status | Class | Rule |
|---|---|---|
| **Vulnerable** | Timed modifier | Target takes 50% more damage. Applications add duration turns, maximum 9. Decrease by 1 at the end of the afflicted side's end phase. Magnitude does not stack. |
| **Weak** | Timed modifier | Target deals 25% less damage. Applications add duration turns, maximum 9. Decrease by 1 at the end of the afflicted side's end phase. |
| **Bleed** | Stacking damage | At the afflicted side's end phase, take damage equal to current Bleed, then remove 1 Bleed. Maximum 99. |
| **Drown** | Stacking damage | At the afflicted side's end phase, take `2 x Drown` damage. Drown does not naturally decay. Maximum 20. Cleansable by specified effects. |
| **Doom** | Stacking damage | At the afflicted side's end phase, take `4 x Doom` damage, then remove 1 Doom. Maximum 20. |
| **Stun** | Control | Skip the next action opportunity, then remove one Stun. Standard enemies can hold at most 1. |
| **Delay** | Control | Skip the next enemy action opportunity while preserving its displayed intent for the following round. Cannot be reapplied while active. |
| **Falter** | Timed modifier | Control-resistance conversion: the enemy still acts, but its attack/heal/shield numerical output is reduced by 25% for that action. |
| **Retain** | Card keyword | Card stays in hand during cleanup. |
| **Ritual** | Card keyword | Retain plus held-cost reduction plus an Essence-aware formula. |
| **Curse** | Run card class | Primarily harmful run-scoped card. Removal requires a named cleanse/removal service. |
| **Symptom** | Run card class | A card with deliberately commingled positive and negative effects, gained from a one-time Symptom node. |

### 7.7 Control Resistance

- Standard enemies normally have no Control Resistance.
- Elites and bosses may declare it.
- When a resisted Stun or Delay is applied, it becomes Falter for the next action rather than doing nothing.
- UI previews show the conversion before confirmation.
- Permanent immunity without conversion is reserved for exceptional phase rules and must be clearly telegraphed.

### 7.8 Stable Ordering

When multiple effects share a timing window, resolve in this order unless a rule explicitly overrides it:

1. effect that created the window;
2. active character/card owner slot order;
3. relic/Symptom acquisition order;
4. enemy slot order;
5. card instance creation order.

The engine records enough identifiers in events to reproduce the ordering.

### 7.9 Future Advanced Character Extension Points

Future characters may be allowed to transform a Domain engine or create selfish cost loops, but those mechanics must remain generic and auditable.

Reserved extension points:

- damage events record command ID, owner, source kind, direct/status/Tentacle/HP-loss classification, Shield absorbed, and HP damage dealt;
- reaction systems declare per-command caps and recursion prevention;
- triggered effects can exclude source kinds such as status damage, Tentacle damage, or HP-loss costs;
- Domain modifiers can augment, suppress, or replace a Domain automatic effect through typed operations;
- party-scoped combat statuses can react to successful Domain-resource generation or consumption through generic resource events;
- operation-tag amplifiers can target declared categories such as damage, Shield, healing, and fixed offensive status stacks without modifying unrelated operations;
- resource activity ledgers can track per-turn and per-combat facts such as first builder, first consumer, first Ultimate, first Transcended Ultimate, and total resource consumed;
- run-persistent party effects must be saved, previewed, and migrated explicitly;
- UI must provide a distinct lane for dangerous future debt or carry-over harm rather than hiding it inside ordinary Shield or HP text.

These hooks do not approve Sacrifice, Tide Aspects, Womb-Sea, Mother's Pearl, or any specific future character kit. They exist so an approved post-launch character can be added without hardcoded engine or UI branches.

---

## 8. Combat UI/UX Specification

Combat is a single responsive stage. The visual layout may adapt to aspect ratio and accessibility settings, but the information hierarchy, interaction states, and target readability are fixed.

The combat presentation is layered conceptually as:

```text
background / environment
  -> independent party and enemy actors
    -> actor-local VFX and telegraphs
      -> combat HUD and targeting overlays
        -> card hand and interaction layer
```

Gameplay-critical information must never be baked only into a background illustration or a composite party/enemy image. Decorative art may be richly layered, but authoritative targets, actors, intents, HP, and cards remain individually addressable.

### 8.1 Portraits and Ultimates

- Four portraits appear as a compact cluster in the upper-left in party slot order.
- Each shows ultimate gauge, readiness, relevant owner statuses, and Transcended state.
- Hover/focus displays the exact current Ultimate and upgraded differences.
- Casting requires confirmation and target selection where applicable.
- A portrait may subtly show the character's Max HP contribution, but individual HP bars are not displayed because survival is shared.
- Portrait presentation is a view of the underlying party slot; it does not become a second owner/state model.

### 8.2 Domain Helper

- The Domain helper sits directly below or immediately adjacent to the portrait cluster so party identity and party resources read as one unit.
- It shows only represented Domains.
- Counter values update immediately when authoritative state changes.
- Hover/focus explains resource persistence, current modifiers, and the next automatic Abyss volley.
- Potential resource spend is previewed when hovering or selecting a spender.
- Counter change animation may anticipate neither success nor failure; it settles to the authoritative post-command value.

### 8.3 Party Stage and Battlefield Composition

- Four full-body party actors are arranged diagonally on the left/lower-left and remain individually legible.
- Each party member is an independent presentation actor with its own transform, visual root, selection/target anchor where needed, status/UI anchor, and VFX anchor.
- A production combat scene may not represent the whole party as one flattened composite render. Shared source artwork may be used for reference or background composition only; the four playable actors must remain separately addressable.
- Owner reactions identify whose card or Ultimate acted even though damage is shared.
- Shared damage reactions must not imply that an individual character was separately targeted unless an enemy effect truly targets an owner-specific property.
- The party formation reserves a clear lane beneath the actors for the shared survival bar and does not compete with the bottom-centred card hand.
- Backgrounds, foreground dressing, and large decorative limbs may overlap the stage only when they do not obscure actor silhouettes, target anchors, intents, or the card play area.
- Battlefield layout is anchor/constraint driven rather than authored as one fixed screenshot. A normal desktop reference composition should remain stable at common 16:9, 16:10, and ultrawide aspect ratios before final M9 responsive closure.

### 8.4 Shared Survival Bar

The shared survival bar is visually associated with the party and normally sits below the party sprites rather than in a distant HUD corner.

It displays:

- current and maximum shared HP;
- ordinary Shield;
- expected incoming HP damage;
- Safe state;
- lethal skull;
- a breakdown by enemy intent;
- uncertainty/range markers for conditional effects.

### 8.5 Hand Layout and Card Interaction

The hand is anchored to the bottom-centre of the combat stage and fans outward around a stable centre point. Five-card hands should read immediately as a conventional roguelike-deckbuilder hand rather than as a row of unrelated buttons.

Resting-hand requirements:

- card order follows authoritative hand order;
- the fan derives each resting position, rotation, overlap, and depth from the current hand order every time layout is refreshed, so repeated hover/drag cycles cannot accumulate positional drift;
- the hand remains centred as cards enter or leave it and expands/contracts within declared safe bounds;
- hovered/focused cards rise above the fan and may scale slightly without reordering the authoritative hand;
- selected/dragged cards render above neighbouring cards and remain sufficiently on-screen for title, cost, and pointer relationship to stay readable;
- hand layout and drag coordinates must be resolution/aspect-ratio safe and may not depend on raw desktop pixel coordinates from one reference screenshot.

The canonical drag interaction is:

1. Pointer-down on a legal card begins a UI-only drag session and visually detaches that card from its resting fan position.
2. The card follows the pointer upward within the combat interaction canvas.
3. Crossing the defined **Play Area** threshold arms the card. The play field and/or card receives a clear non-colour-only armed indicator.
4. Moving the card back below the threshold disarms it.
5. Releasing while disarmed or back in the hand cancels the gesture and returns the card smoothly to the current fan layout with no authoritative mutation.
6. Releasing while armed behaves by target type:
   - `selfOwner`, `party`, `allEnemies`, deterministic automatic targets, and other target-complete effects submit the complete play command immediately;
   - `oneEnemy` and any other explicit-choice target enters a target-selection state. The card remains visibly committed/staged above the hand, legal targets highlight, and the next valid target selection submits the complete play command;
   - cancelling target selection with Escape/right-click or the platform-equivalent cancel action returns the card to the hand without cost or mutation.
7. A rejected engine command returns the presentation cleanly to the authoritative hand state and displays the rejection reason where useful.

The threshold is a responsive presentation region/anchor, not a hardcoded screen-space Y value. The drag implementation must use one coherent coordinate space or explicit coordinate conversion when moving between hand and drag layers; reparenting must preserve the intended visual position. A card must not jump, scale unexpectedly, or be forced off-screen merely because its parent or canvas changes during drag.

Click/keyboard interaction reaches the same underlying selection/arming/target-confirmation states:

- clicking a card selects/raises it for inspection;
- clicking the play area arms/confirms target-complete cards;
- explicit target cards then require a legal target selection;
- number keys may select cards; Enter/Space confirms where appropriate; Escape/right-click cancels.

Additional requirements:

- only one card interaction session may own targeting/drag focus at a time;
- selected or armed cards clearly highlight legal targets;
- invalid targets explain why they are invalid;
- formula previews use current owner stats and current modifiers;
- cards expose owner portrait, Domain, final cost, base cost where modified, tags, and pile destination;
- drag, hover, targeting, and cancellation never mutate authoritative combat state or consume RNG before command acceptance.

### 8.6 Pile Viewers

- Draw pile viewer shows known composition but not hidden order.
- Discard and Graveyard viewers show exact contents.
- Ritual held reductions and temporary/generated state remain visible in viewers.
- Pile counts are always visible along the bottom edge without displacing the centred hand.

### 8.7 Enemies and Intents

Enemies occupy the right or right-center side of the combat stage. Enemy formation is not a mirrored version of the party's compact diagonal: it prioritizes target selection, readable silhouettes, HP/status/intent anchors, and inspection clarity over symmetry.

Every independently targetable enemy is an independent presentation actor with its own transform, target bounds, status/UI anchor, intent anchor, and VFX anchor. A production encounter may not flatten multiple targetable enemies into one composite render.

Enemy placement rules:

- one enemy uses a prominent right-side anchor;
- two enemies use separated staggered anchors;
- three enemies use a loose triangle or arc;
- four or more enemies use rows, lanes, or an arc with scale and spacing constraints;
- bosses use a large/boss anchor, with adds placed in satellite lanes instead of squeezed into the boss footprint.

Independently targetable enemies must not overlap in a way that hides silhouettes, target rings, hitboxes, HP/status labels, or intent labels. Overlap is allowed only for authored non-targetable body parts, background limbs, or boss composition elements where target anchors remain unambiguous.

Each enemy displays:

- exact current HP and Shield;
- intent icon and numerical values;
- status stacks/durations;
- control-resistance conversion preview;
- verbose tooltip explaining every component in resolution order.

Enemy actions animate one at a time. The presentation queue may speed up repeated hits, but it may not collapse multiple enemy turns into an unreadable simultaneous result.

### 8.8 Event Log

The log is an ordered rendering of engine events, not a second source of combat logic. It includes:

- commands;
- card owner and final cost;
- damage before/after modifiers;
- Shield absorbed;
- status applications and decay;
- resource changes;
- Tentacle volleys;
- intent changes;
- resisted control conversions;
- victory/defeat transitions.

It is collapsible, filterable, and retains the full current-combat history. It should not permanently consume a large portion of the battlefield when closed or minimally displayed.

### 8.9 Accessibility

- Full keyboard navigation and accessible labels for interactive combat controls.
- Text alternatives for colour, icon, animation, and intent shape.
- Reduced-motion mode that preserves ordered sequencing without large movement or shake.
- Scalable card text and tooltip text.
- No essential information is conveyed only through Domain colour.
- Drag is never the only way to play a card; click/keyboard paths remain first-class.

### 8.10 Reference Battlefield Composition and Safe Zones

The implementation must use anchors/constraints rather than reproducing one screenshot at fixed coordinates, but the normal desktop composition has a fixed **information relationship**. At a 16:9 reference view it should read approximately as:

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│ [4 portraits / ultimates]              turn / phase                [log]    │
│ [represented Domain resources]                                                │
│                                                                              │
│        PARTY ACTORS                               ENEMY ACTORS                │
│      compact diagonal                         separated target lanes          │
│                                                                              │
│        [shared HP / Shield / incoming]                                      │
│                                                                              │
│ [Mana]                    PLAY / TARGET CORRIDOR                 [End Turn]   │
│ [piles]              \   \   [FANNED HAND]   /   /                           │
└──────────────────────────────────────────────────────────────────────────────┘
```

Reference constraints:

- The card-hand anchor is bottom-centre. It must never migrate to a left-aligned row because of available space or implementation convenience.
- Portraits and Domain resources form a compact upper-left cluster; they must not grow into a large panel that competes with the battlefield.
- Party actors occupy the left/lower-left battlefield and enemies the right/right-centre. They are not displayed inside large opaque image panels; the environment remains visually continuous behind them unless a deliberate localized readability treatment is required.
- Shared HP/Shield/incoming is visually associated with the party, normally below the actor cluster and above the resting hand.
- Mana remains near the lower-left and uses current/max text rather than a fixed number of permanent slots.
- The central/lower-middle area above the resting hand is the primary Play/Target corridor. An armed card should visually enter this corridor without needing to reach an enemy sprite.
- End Turn and other high-frequency controls stay outside the hand fan and enemy target bounds.
- The Combat Log defaults to collapsed/minimal presentation or an overlay that can be dismissed; it may not permanently remove a large right-side rectangle from combat composition.
- A five-card default hand should leave a meaningful portion of the battlefield visible. Larger hands may increase overlap/fan compression before they are allowed to consume the whole lower half of the screen.
- Large artwork may extend decoratively beyond its logical actor bounds, but target bounds, label anchors, and neighbouring targets remain unambiguous.

For the initial Unity implementation, a screen-space uGUI combat canvas should use a consistent `CanvasScaler`/anchor strategy and explicit drag-layer coordinate conversion. `RectTransformUtility` or an equivalent tested conversion path should be used when converting pointer screen positions to UI-local positions; raw pointer pixels must not be assigned directly to unrelated local/world transforms.

## 9. The Labyrinth - Structure, Movement, and Opportunity Cost

### 9.1 Design Intent

The Labyrinth is not a one-directional ladder and not a sequence of mandatory fights. It is a freely traversable board of connected hexes with a stable route to the boss and optional stitched loops.

The player may discover an expensive, high-value keepsake in a Shop before she can afford it, deliberately leave the Shop, seek additional finite combat rewards elsewhere, and later return. The strategic question is not merely "which next node?" but: what lasting cost am I willing to accept to open, revisit, or escape this profitable branch?

In world terms, the Labyrinth is land and memory reorganized by the Bloom into traversable growth geometry. The main spine may read as a stem, root, vein, or pilgrimage path; side loops may read as petals, branching tissue, or devotional circuits; boss regions may read as hearts, fruiting bodies, seed chambers, or thresholds. This should guide motif art and event writing without requiring every map to become a literal flower diagram.

Travel-only hexes give the map physical shape, route readability, and return distance without forcing a combat on every step.

### 9.2 Free Traversal

- The player may move freely between the current hex and any adjacent hex connected by a currently traversable edge.
- Traversal itself has no universal stamina or time cost in the launch scope.
- Previously resolved nodes remain on the map and may be crossed again.
- Cleared combat, treasure, event, Rest, and Symptom rewards do not respawn.
- Shops remain open and retain generated stock, prices, sold-out state, and reveal state for the entire run.
- Backtracking never respawns enemies or creates infinite currency.

### 9.2.1 Nodes Are Places; Edges Are Connections

The Labyrinth is node-primary and edge-supported.

- **Nodes are places.** They hold player-facing content, costs, hazards, consequences, lifecycle state, previews, one-time resolution rules, and revisit state. Travel, Combat, Shop, Symptom, Boss, Rest, Event, Treasure, Collapsing Node, and future Corrupted/Curse-style nodes are node types or node-owned modifiers.
- **Edges are connections.** They hold graph topology: connected or unconnected, traversable or blocked, motif anchors, adjacency, walls, pathfinding, and validation structure.
- If a rule changes the run, it belongs on a node. If a rule changes reachability, it belongs on an edge.

Edge removal or blocking is allowed only for connectivity changes, not hidden rewards, costs, Symptoms, Shops, events, or other authored consequences. If a future design truly needs a transition-scoped mechanic, it requires an explicit design decision explaining why node ownership is insufficient.

### 9.3 Main Spine and Side Loops

Every generated run has two structural layers.

**Main Spine:**

- A guaranteed connected route from Start to Boss.
- Contains required progression beats and enough baseline combat/recovery to make a direct run viable.
- Initial-scope main-spine traversal never depends on an intact Collapsing Node that could permanently block the boss.
- May have branches and junctions, but boss reachability is always preserved.

**Side Loops:**

- Optional motifs attached to the spine at two distinct traversal anchors whenever topology allows.
- Usually contain a desirable node such as a Shop, Elite, Treasure, Rest, character Event, or rare reward.
- Designed as loops rather than simple dead ends so entry, exit, and later return can differ.
- Commonly use asymmetric gates: one Collapsing Node route and one Symptom-bearing route.

A side loop is an authored opportunity-cost structure, not arbitrary edge noise.

### 9.4 The Canonical Temptation Loop

A common side-loop motif works as follows:

1. The player sees or discovers a desirable interior node, often a Shop with a premium keepsake.
2. The loop connects to the spine at two gates.
3. One gate uses a **Collapsing Node** that is safe to enter but collapses after the player successfully leaves it.
4. The other gate passes through a **Symptom node** that triggers once and adds a mixed-benefit card to the run deck.
5. The player chooses how to enter, whether to spend the Collapsing Node bypass, whether to accept the Symptom, and whether preserving a route back to the Shop is worth the deck cost.

Example routing consequences:

- Enter through the Collapsing Node gate: the bypass remains intact while the party stands on it, then collapses when the party successfully moves away. Exiting through the other side requires accepting the Symptom. Later Shop returns use the now-cleared Symptom route.
- Enter through the Symptom gate: the cost is paid immediately, but the Collapsing Node remains available as a one-time shortcut or emergency exit.
- Ignore the loop: preserve deck purity and HP, but forgo its reward and possible future purchase.

After a Symptom node triggers once, it remains traversable and does not add further copies unless its authored rule explicitly says otherwise.

Collapsing Nodes are one-use bypasses around another cost, usually a Symptom route. They are safe now and costly later, not permanently safe paths. A player who uses the Collapsing Node first can avoid the Symptom for the moment but may force that Symptom route when returning; a player who accepts the Symptom first preserves the Collapsing Node as a later shortcut.

**Canonical fixture contract:** the deterministic test fixture has a Start-to-Boss spine with two distinct junctions connected by a safe spine route. A side loop joins those junctions through exactly two distinct degree-2 gates: one travel-only Collapsing Node and one Symptom node. The loop interior contains the revisitable Shop and premium offer, and the two gates are the only loop gates between the spine junctions and that interior. The Collapsing-first route enters the Shop without mutation, then collapses on accepted departure so a later return uses the once-triggered Symptom route. The Symptom-first route commits the Symptom once and preserves the Collapsing Node until it is later departed. Preview, cancellation, rejection, and destination confirmation preserve the DD-24 lifecycle/RNG contract; save/reload preserves current position, node lifecycle, spent Symptom state, and Shop state. The canonical image remains a required visual topology comparison when available. If it is absent, only that image-dependent comparison is blocked; its visual composition must not be inferred or recreated.

### 9.5 Travel-Only Hexes

Travel hexes have no encounter or reward. Their functions are:

- give loops and branches physical length;
- prevent every route decision from requiring another combat;
- space high-intensity nodes for pacing;
- create visible junctions and return paths;
- support terrain art, foreshadowing, and map landmarks;
- make collapsing shortcuts and alternate exits spatially meaningful.

A travel node is not considered content merely because it consumes a click. Motifs must use travel hexes to clarify topology, pacing, or route commitment.

### 9.6 Node Resolution and Revisit Rules

| Node | First visit | Later visits |
|---|---|---|
| **Travel** | No event | Freely traversable |
| **Normal Combat** | Fight; gain rewards on victory | Cleared, no new fight or reward |
| **Elite Combat** | Hard fight; higher reward on victory | Cleared, no new fight or reward |
| **Boss** | Fight when entered and confirmed | Run ends on victory; defeat follows run rules |
| **Shop** | Reveal fixed seeded stock and prices | Re-enter and buy remaining stock |
| **Rest** | Choose one recovery option | Becomes spent travel node |
| **Event** | Resolve authored choice | Becomes spent travel node unless event says otherwise |
| **Treasure** | Claim reward | Becomes spent travel node |
| **Symptom** | Preview and accept the exact mixed card on traversal | Freely traversable; no repeat trigger |
| **Collapsing Node** | Travel-only; visually distinct and no effect on entry | Traversable while intact; collapses after accepted movement away and then becomes impassable/disabled |
| **Corrupted Node** | Future/expanded node type; may apply an authored run cost on accepted entry | Repeated-trigger behavior must be explicitly authored |

### 9.7 Symptom Nodes

A **Symptom** is a run-scoped card with intentionally commingled positive and negative effects. It is neither a conventional reward nor a pure Curse. In world terms, a Symptom is concentrated Bloom contact that offers a real advantage while changing the party's run plan in a costly or unstable way.

Symptom-node rules:

- The exact card and full effect are shown before the first committed traversal.
- Trigger occurs once per node, not once per direction.
- The card is added directly to the run deck and assigned according to its definition.
- Symptoms persist for the run unless treated, removed, transformed, or consumed by their own text.
- Rest nodes, Shops, Events, keepsakes, and character passives may interact with Symptoms.
- A Symptom can be sufficiently useful that some parties deliberately seek it.
- No Symptom may be a strictly better zero-cost reward with cosmetic downside; the drawback must matter in ordinary play.

Launch example templates:

- **Fever Bloom:** Cost 0. Draw 2 and gain 1 Mana; lose 5 shared HP; Exhaust.
- **Salt Lung:** Retain. At ordinary cleanup, add 1 Tentacle and gain 1 Drown on the party; Cost 1 to play and Exhaust for 6 Shield.
- **Glass Hymn:** Ritual, base cost 3. Gain 3 Essence and 8 Shield; when played, add a Fragile Curse to Discard.
- **Missing Moment:** Cost 0. Apply 2 Weak to one enemy and gain 2 Mana Debt; Exhaust.

Final Symptoms require balance and explicit owner-assignment rules before content lock.

### 9.8 Node Consequences and Edge Connectivity

- **Traversal edge:** stable graph connection between adjacent hexes unless blocked by a connectivity-only rule.
- **Collapsing Node:** node-scoped one-use bypass. It is visibly marked before entry, does not collapse on entry, and collapses only after accepted movement away from it. Collapse happens regardless of destination and makes the node impassable as a future destination while leaving a visible disabled/ruined state.
- **Corrupted Node:** future/expanded node type that may apply an authored run cost on first accepted entry, such as adding a Curse. Repeated-trigger behaviour must be explicitly authored.
- **Locked/Keyed edge:** future connectivity-only feature, excluded from initial scope unless required by a specific event. Any cost, reward, event, or unlock consequence associated with a lock belongs on a node or event, not on the edge itself.

M3 includes Travel, Collapsing Node, Normal Combat, Shop, Symptom, and Boss node resolution. Corrupted Nodes, Rest, Event, Treasure, and other expanded node types are excluded from M3 unless a later approved task explicitly scopes them. M3 Collapsing Nodes are travel-only. They cannot also be combat, Shop, Symptom, Boss, Rest, Event, or Treasure nodes. Degree-2 gate/bypass usage is the launch default; higher-degree Collapsing Nodes require later validation work.

Collapsing Node lifecycle:

1. **Intact:** visible, reachable, and safe to enter.
2. **Occupied/armed:** the party is standing on the node; save/reload preserves that it has not collapsed.
3. **Collapsed:** after accepted departure, the node remains visually indicated but is no longer reachable.

Movement preview from an occupied Collapsing Node must show that departure will collapse it, but preview alone changes nothing. If the destination requires confirmation, such as Symptom or Boss entry, collapse occurs only after the movement is accepted. Rejected or cancelled movement leaves the node intact and consumes no RNG or state. Collapse resolves before or alongside the destination consequence commit, but never strands the player on a collapsed node.

### 9.9 Motif Definition

A Motif is a hand-authored small axial-coordinate subgraph containing:

- stable motif ID and version;
- normalized hex footprint;
- internal traversal edges;
- entry/exit anchors with direction;
- optional role tags such as `spine`, `junction`, `sideLoop`, `shopLoop`, `recoveryLoop`, or `bossApproach`;
- content slots with allowed weighted node types;
- fixed or allowed node content and node-modifier constraints;
- depth band and difficulty band;
- minimum/maximum reuse count;
- symmetry/rotation permissions;
- topology invariants such as "two distinct loop gates."

### 9.10 Seeded Stitching Algorithm

Map construction is motif placement with backtracking, not breadth-first graph generation.

1. Place a Start/root spine motif at the origin.
2. Reserve a target spine depth and boss-attachment requirement.
3. Extend the main spine by selecting compatible spine motifs from open anchors.
4. Place and validate the Boss approach/motif at the intended terminal depth.
5. Attach optional side-loop motifs to compatible pairs of spine or junction anchors.
6. Fill remaining optional anchors with small branches or close them.
7. Assign allowed node content from seeded tables while meeting distribution constraints.
8. Apply authored or constrained node content and node-modifier variants.
9. Run full topology, reachability, economy, and one-time-trigger validation.
10. Reject and backtrack on invalid layouts.

BFS/DFS/Dijkstra may be used for validation, reachability, path length, and analysis. They must not be used as the primary procedural construction method.

### 9.11 Map Validation Invariants

Every generated map must satisfy:

- no overlapping hex footprints;
- every traversal edge joins physically adjacent axial coordinates;
- touching hexes without an edge are blocked walls;
- no reward, cost, Shop, Symptom, Event, combat, Corrupted, Curse, or other authored run consequence is owned by an edge;
- Start can reach Boss before any optional choices;
- Boss remains reachable after every legal sequence of optional Collapsing Node use in the initial scope;
- each declared side loop has its required distinct gates and interior route;
- an intact Collapsing Node is never the only connection on the required main spine;
- one-time nodes cannot trigger more than once;
- every Shop can be revisited if its authored loop claims revisitability after every legal sequence of Collapsing Node use and Symptom resolution;
- no revisit regenerates combat, rewards, inventory, or currency;
- required content distribution and combat-count bounds are met;
- the map serializes and reloads with identical Collapsing Node lifecycle state and spent-node state.

### 9.12 Map Information and Fog

**[DECISION]** The launch map shows topology and node categories once a motif is revealed, while some exact event outcomes remain hidden. Shops display their icon before entry; stock is revealed on first visit and then remains inspectable. Collapsing Node, future Corrupted Node, and Symptom gates are visible before commitment, and Symptom card text is previewable at the gate.

### 9.13 Map UI

- Flat-top SVG or equivalent boardgame-style hex rendering.
- Current node, directly reachable nodes, collapsed Collapsing Nodes, spent nodes, and unresolved node consequences are distinct.
- Selecting a reachable node previews the destination node consequence, prior-node lifecycle consequence, and connectivity state before movement.
- Collapsed nodes must remain visually readable as disabled, ruined, faded, locked-out, or equivalent; colour alone is not sufficient, so icon, shape, tooltip, label, or texture support is required.
- Shop icons indicate affordable stock, unaffordable desirable stock, and sold-out state without revealing hidden inventory before first visit.
- A route-history layer may be toggled for debugging and accessibility.

---

## 10. Game Modes, Rewards, and Economy

### 10.1 Launch Game Modes

Bloomdrawn launches with two persistent game modes:

1. **Run** - name pending. This is the default roguelike mode: select a party, traverse the Labyrinth map, meet encounters, build the run deck, manage Obols, and clear the boss.
2. **Trials** - direct boss challenges for targeted profile rewards. Trials do not use Labyrinth traversal, Obols, Shops, side loops, or run-scoped map economy.

Both modes use the same core combat engine, character kits, statuses, ownership rules, RNG discipline, and accessibility requirements.

### 10.2 Run Flow

1. Select or load a party of exactly four.
2. Choose or generate seed and difficulty.
3. Enter the Labyrinth with starting HP, deck, and no run-scoped keepsakes/currency unless a profile boon states otherwise.
4. Traverse freely, resolving nodes and revisiting persistent Shops.
5. Gain finite currency and build the run deck through combat and events.
6. Defeat the Boss, abandon, or be defeated.
7. Bank eligible meta rewards and record the run.

### 10.3 Trials

Trials are selectable direct boss challenges that reward targeted profile items. They exist so players can deliberately pursue progression resources instead of waiting for the default Run mode to surface them.

Launch Trial families:

- **Flesh Trial:** rewards Flesh Sigils.
- **Abyss Trial:** rewards Abyss Sigils.
- **Spirit Trial:** rewards Spirit Sigils.
- **Void Trial:** rewards Void Sigils.
- **EXP Trial:** rewards character EXP items across the three item tiers.
- **Money Trial:** rewards persistent general currency, name pending.

Trial requirements:

- Each Trial has clearly selectable difficulty/level tiers before entry.
- Difficulty tiers scale encounter level, boss stats, intent patterns, reward quantity, and possible reward tier.
- Trial rewards are previewed before entry.
- Trials award persistent profile inventory items only.
- Trials do not award Obols and do not mutate the active Labyrinth run.
- Trial encounters may reuse bosses, but a Trial boss must declare a stable Trial ruleset and reward table separate from map Boss encounters.
- Trial completion records should support first-clear rewards, repeat rewards, and difficulty unlock checks.

M8X equipment expansion adds additional targeted Trial reward families for weapon EXP items, weapon ascension materials, gear sets, and gear reroll currency. These are persistent profile rewards and must follow the same preview, deterministic reward table, first-clear, repeat-clear, and active-run isolation rules as launch Trials.

### 10.4 Run Currency - Obols

**[DECISION]** Run-scoped shop currency is called **Obols**. It is distinct from persistent profile currencies and disappears at run end.

Obols are earned primarily from:

- first victory at Normal combat nodes;
- first victory at Elite combat nodes;
- Events and Treasures;
- selected keepsakes, passives, and Symptoms;
- boss victory.

Cleared combats never respawn, so Obol generation is finite. Routing tension comes from deciding whether additional combat risk and branch costs justify returning to an earlier Shop.

### 10.5 Shop Contract

This section describes Labyrinth Shops inside a run. Profile Shop rules are separate and live in section 12.8.

- Shop stock, prices, and rarity are generated from the run seed and Shop ID on first reveal.
- Stock persists unchanged except for purchases, discounts, or explicit restock effects.
- Shops may contain keepsakes, card rewards, card removal/treatment, healing, and other authored services.
- Premium keepsakes may intentionally be unaffordable on first discovery.
- The UI may allow pinning a desired item and showing current Obols versus price from the map.
- Selling or infinite currency loops are excluded from launch unless individually proven safe.

### 10.6 Combat Rewards

A victory may grant:

- Obols;
- a choice of run-scoped cards;
- keepsakes/boons according to encounter tier;
- profile reward items queued for banking;
- direct pulls;
- EXP items;
- domain Sigils;
- weapon EXP items;
- weapon ascension materials;
- gear reroll currency;
- persistent general currency;
- healing or Domain resources only when explicitly authored.

Normal encounters provide modest Obols and a chance/choice of cards. Elites provide higher Obols and a guaranteed premium reward category. Boss rewards close the run or transition to a later act if acts are added.

### 10.7 Rest Nodes

Launch Rest choices are drawn from authored options such as:

- heal a percentage of party Max HP;
- remove one eligible Curse;
- treat or transform one eligible Symptom;
- gain a temporary run boon at a cost.

Generic card upgrading is deferred as stated in section 5.13.

### 10.8 Run-End Banking

On victory, bank all marked profile rewards. On defeat or abandon, bank a reduced but non-zero portion according to difficulty and progress so experimentation is not entirely wasted.

Never bank:

- Obols;
- run-scoped cards;
- run keepsakes/boons;
- run Symptoms/Curses;
- current run HP or map state after finalization.

Always bank if awarded and not explicitly blocked by mode rules:

- direct pulls;
- EXP items;
- domain Sigils;
- weapon EXP items;
- weapon ascension materials;
- gear reroll currency;
- persistent general currency;
- owned character/profile progression rewards.

### 10.9 Encounter Count and Fatigue Guardrail

Generation must not equate map size with fight count. A launch run should remain viable on a relatively direct route, while optional loops offer additional fights for Obols and rewards. Content validation reports both total hex count and minimum/maximum reachable combat counts under legal routing.

**[INVARIANT]** Direct-pull rewards must be deterministic, auditable, and not tied to paid boosts.

---

## 11. Gacha System - Ethical Collection Details

### 11.1 Principles

- No real-world purchases or premium currency sales.
- Pulls are earned directly through play and profile progression.
- Rates, pity, guarantee, and pool contents are always visible.
- No paid FOMO, paid stamina, paid rerolls, or monetised inventory pressure.
- No character requires duplicates to function in her intended role.
- Banner scheduling may rotate for variety, but characters do not become permanently unobtainable.
- The gacha is a collection and teamcrafting toy, not a revenue machine.

**[INVARIANT]** Bloomdrawn has no real-money purchases. This includes no paid pulls, no paid premium currency, no battle pass, no stamina refills, no paid cosmetics, no limited paid bundles, and no ads-for-rewards economy.

If the project ever accepts external funding or releases paid content, it must be outside the gacha economy and must not compromise this contract.

### 11.2 Direct Pull Rewards

**[DECISION]** Gacha uses direct stored pulls, not an intermediary currency.

Because Bloomdrawn has no monetisation, there is no reason to obscure conversion rates behind premium currency. A reward that says "1 pull" means the profile gains one pull that can be spent directly on a banner. There is no gem-to-ticket-to-pull chain.

Direct pulls may come from:

- first-time tutorial completion;
- run milestones;
- boss victories;
- Trial first-clears or milestone rewards where appropriate;
- profile levels;
- achievements without daily-chore pressure;
- collection milestones;
- event-style content that remains available or returns predictably without paid urgency.

The final display name for direct pulls is **[OPEN]**. Until then, design and implementation should call them direct pulls or pull grants.

Exact direct-pull income rates are **[OPEN]** until DD-10.

### 11.3 Rarity Model

Player-facing rarity uses three tiers:

```text
SSR > SR > R
```

Rarity is a content field and an audit field, not a substitute for exact result descriptions.

- All playable characters are SSR.
- All character signature weapons are SSR.
- Non-signature weapons may be SSR, SR, or R.
- Upgrade materials, currencies, selectors, and other banner fill may have a rarity only when it improves player understanding.
- Every banner result preview must show both rarity and result family, such as `SSR character`, `SSR signature weapon`, `SR weapon`, `R weapon`, or `SR weapon EXP item`.

### 11.4 Banners

- **Standard Bloom:** permanent pool containing all generally released characters and eligible non-limited weapons.
- **Featured Domain/Character:** rotating rate-up using earned direct pulls only.
- **Weapon or equipment-focused banner variants:** allowed only after DD-15 and DD-17 approve exact pool structure, rates, guarantees, and compensation.
- Each banner family declares whether pity and guarantee are shared or separate.
- The launch design uses a persistent featured-banner pity family and a separate Standard pity family.
- Featured banner pity and guarantee do not expire when a featured banner rotates.
- Banner pools are authored content. No production banner may be assembled through hardcoded code branches.

### 11.5 Rates, Pity, and Ten-Pull Guarantee

Launch **[TUNING]** anchors before DD-15:

- Top-rarity result equivalent: 3%.
- Mid-rarity result equivalent: 15%.
- Low-rarity or material result equivalent: 82%.
- Soft pity begins at pull 60.
- Hard pity at pull 90 guarantees an SSR top-rarity result according to the banner family.
- A featured SSR character result uses a 50/50 rule; losing it guarantees the next eligible SSR character result is featured.
- Pity and guarantee never expire when a featured banner rotates.
- Every 10-pull guarantees at least one SR or better item.

Exact rarity rates, result-family rates, soft-pity increments, guarantee precedence, 10-pull SR guarantee behaviour, and rounding must be specified in banner data and tested exactly before release. Do not implement a guessed pity or guarantee shortcut.

### 11.6 Result Families

Banner results use a hybrid model: player-facing rarity plus exact result family.

- **Character:** SSR only; unlocks a new character or grants duplicate progress/compensation for an owned character.
- **Weapon:** SSR, SR, or R; unlocks or duplicates a persistent weapon instance or duplicate bonus, according to weapon rules.
- **Signature weapon:** SSR weapon linked to a specific character, but still usable only according to approved equip restrictions.
- **Upgrade material:** EXP items, ascension materials, gear reroll currency, or other declared profile items.
- **Conversion currency:** special profile-shop currency from auto-discarded low-rarity items or maxed duplicate overflow.

The launch implementation must avoid misleading rarity names: every result preview explains exactly what is awarded, whether it can be equipped, whether it is a duplicate, and what it can purchase or convert into.

### 11.7 Character Duplicate Ladder

Each character has a fixed five-tier positive ladder:

- C1: passive augment;
- C2: modest stat increase;
- C3: one Flavor card augment;
- C4: Ultimate cost reduction or additional effect;
- C5: small capstone run bonus.

C0 remains fully viable. No duplicate tier removes choices, adds a drawback, or becomes necessary to clear standard content.

After C5, additional copies convert into special profile-shop currency or another approved overflow item. Overflow compensation is a profile reward, not a direct pull and not an intermediary pull conversion resource.

### 11.8 Weapon Duplicate Ladder

Weapons use duplicate bonus tiers from +0 through +5. A duplicate weapon either unlocks the weapon if unowned, increases that weapon's duplicate bonus if below +5, or converts into approved overflow compensation if already maxed.

- Signature weapons are SSR and may have character-specific presentation or effects, but their rules must be content-authored and schema-validated.
- Signature weapon hooks that react to Domain-resource activity, Ultimate casts, or other advanced character events require the relevant approved generic event contract, such as DD-22 for resource/Ultimate reactions or DD-23 for copied-card reactions.
- SR and R weapons exist to fill banner pulls with useful equipment and conversion value.
- No weapon duplicate bonus may be required for standard content clearability.
- Exact +1 through +5 bonuses are open until DD-17.

### 11.9 Auto-Discard and Conversion

The player may enable optional auto-discard rules for eligible low-rarity banner results. Auto-discard converts eligible results into special profile-shop currency according to content-authored tables.

Auto-discard must never affect:

- new characters;
- SSR character signature weapons;
- locked or favorited weapons;
- gear marked protected;
- any item category not explicitly eligible in content data.

Special conversion currency may be spent in the Profile Shop. It must not convert into direct pulls.

### 11.10 Pull Auditability

Gacha resolution uses a profile/banner RNG stream, never the active run RNG. Each pull records:

- pull index;
- banner ID and version;
- pity before and after;
- guarantee before and after;
- ten-pull guarantee state where applicable;
- RNG roll or deterministic audit token;
- result rarity;
- result family;
- specific result;
- duplicate/overflow handling;
- direct pull count before/after;
- inventory mutation;
- auto-discard or conversion result if applicable;
- timestamp or local sequence number.

The UI exposes a recent pull history. Development tools may export a full audit log for deterministic reproduction.

### 11.11 Onboarding Protection

- The player receives the four-character starter party before banners unlock.
- Tutorial rewards grant enough direct pulls for an early multi-pull.
- A first-acquisition protection rule should strongly prefer an unowned launch character until the player has broader roster choice.
- Exact implementation is a decision-lock item before the gacha resolver ships.

---

## 12. Party, Roster, Equipment, and Meta Progression

### 12.1 Party Rules

- A run party contains exactly four unique owned characters.
- Duplicate copies of the same character cannot occupy multiple slots.
- Any Domain composition is legal.
- Each selected character contributes five starting cards, personal stats, equipped weapon stats, equipped gear stats, and active equipment bonuses to shared Max HP and owner formulas.
- Party order determines stable owner ordering for simultaneous effects and sprite placement.
- Saved lineups store character IDs, order, and equipment loadout references, not mutable character snapshots.
- Starting a run or Trial creates a versioned party and equipment snapshot so later meta changes cannot alter active deterministic state.

### 12.2 Composition Trade-offs

- Same-Domain stacking increases engine density and pooled-resource synergy.
- Mixed parties gain wider tactical coverage and improve the value of Void rule manipulation.
- The design must not assume one-of-each Domain is mandatory after the tutorial.
- Encounters may pressure different strategies but cannot make an entire legal Domain composition categorically nonfunctional without warning.
- Equipment should create build pressure and recovery paths without replacing character identity as the primary party engine.

### 12.3 Player Profile

The profile tracks:

- stable profile ID and display name;
- level and EXP;
- persistent direct pull count;
- persistent currencies, names pending;
- owned characters and progression;
- owned weapons and duplicate bonuses;
- owned gear instances;
- equipment loadouts;
- inventory categories;
- banner pity/guarantee states;
- Profile Shop state and purchase history;
- saved parties;
- run saves and run history;
- settings and accessibility preferences;
- one-time reward and tutorial flags.

### 12.4 Inventory

The player profile stores persistent inventory across modes. Persistent inventory is intentionally separate from run-scoped Obols so meta progression cannot distort Shop balance inside a run.

Profile-scoped inventory categories include:

- direct pulls;
- persistent general currencies, names pending;
- special profile-shop conversion currency, name pending;
- gear reroll currency, name pending;
- small character EXP items;
- medium character EXP items;
- large character EXP items;
- small weapon EXP items;
- medium weapon EXP items;
- large weapon EXP items;
- Abyss Sigils;
- Flesh Sigils;
- Spirit Sigils;
- Void Sigils;
- weapon ascension materials;
- owned weapon instances;
- owned gear instances;
- dust/selector materials;
- future cosmetic or codex unlock tokens.

Obols, run cards, run keepsakes, Symptoms, and Curses are never stored in the profile inventory.

The Inventory screen must support category filtering, lock/favorite protection where relevant, weapon and gear detail inspection, desynthesis actions, and clear previews of resulting currencies or materials before commitment.

### 12.5 Character Weapons

Each character may equip one persistent weapon. Weapons are profile-owned items acquired primarily from banner pulls and, where approved, Profile Shop offers or targeted rewards.

Weapon definition fields include:

- stable weapon definition ID and content version;
- display name and rarity;
- weapon family/type tags;
- signature character ID when applicable;
- base stat contribution and growth table;
- duplicate bonus tiers from +0 through +5;
- level cap and ascension requirements;
- passive/effect operation references if the weapon has more than flat stats;
- acquisition sources;
- lock/favorite default rules;
- content warning or presentation metadata where relevant.

Weapon progression rules:

- Weapon EXP items have three tiers: small, medium, and large. Final names are pending.
- Weapons level by consuming weapon EXP items and profile-scoped money.
- Every 10 weapon levels is capped by weapon ascension materials and profile-scoped money.
- Signature weapons are SSR.
- Non-signature weapons may be SSR, SR, or R.
- Duplicate weapons increase duplicate bonus up to +5.
- Excess/maxed duplicate weapons convert into approved special currency or materials.
- Upgrade costs, ascension costs, duplicate bonuses, and resulting stats are previewed before commitment.
- Combat parameter changes from weapons, such as maximum hand size, are captured in the Run/Trial equipment snapshot and cannot mutate an active deterministic state when profile equipment changes later.

Draft signature weapon seed names, pending DD-20 and content review:

| Character | Draft signature weapon |
|---|---|
| Mara | Suture-Knife of the Kindly Bloom |
| Venelis | Mawpetal Sceptre |
| Thalassa | Leviathan Tuning Fork |
| Nyxalia | Lurelight Trident |
| Sephira | Vesper Glass Baton |
| Kibane | Foxfire Reliquary Fang |
| Azael | Hinge-Key of the Between |
| Mira Nox | Eventide Eclipse Needle |

### 12.6 Gear Sets

Gear is the public term for persistent six-slot equipment. It is not called relics, because run-scoped keepsakes already occupy the relic/boon design space.

Each character has six gear slots. Slot names are pending DD-20, but every gear definition and gear instance must declare exactly one legal slot.

Gear instance fields include:

- unique instance ID;
- gear set ID;
- slot;
- rarity or quality tier if approved;
- main stat type and main stat enhancement level from +0 through +12;
- three substat slots;
- substat roll tiers;
- lock/favorite protection;
- acquisition source;
- content version and roll provenance for debugging.

Gear set rules:

- Equipping 3 matching set pieces activates that set's 3-piece bonus.
- Equipping 6 matching set pieces activates that set's 6-piece bonus.
- Mixed sets may activate multiple 3-piece bonuses if slot counts support it.
- Set bonuses must be content-authored and schema-validated.
- Gear is gained from targeted Trials and the Profile Shop by default.
- Gear may also appear in other persistent reward tables only after DD-18 and DD-19 define the economy impact.

Enhancement and reroll rules:

- Main stat enhancement increases from +0 to +12 by spending profile-scoped money and approved materials.
- Substats are rerolled using gear reroll currency.
- Reroll results use a named profile equipment RNG stream and declared stat pools/tier weights.
- Reroll previews show cost, eligible stat pool, locked values if any, and the commitment boundary; they do not reveal hidden random outcomes unless the content explicitly grants a selector-style result.
- Gear can be desynthesized into reroll currency or approved materials, with previewed yields.
- Locked/favorited gear cannot be desynthesized or auto-converted.

Exact main stats, substats, tier weights, set bonuses, reroll costs, desynthesis yields, and slot names are open until DD-18 and DD-20.

### 12.7 Equipment Snapshot and Scaling

Persistent equipment modifies the character stat contribution used by party Max HP, owner Attack, owner Defense, and any approved future scaler.

Rules:

- Active Run and Trial state stores a snapshot of selected characters, levels, duplicates, equipped weapons, equipped gear, stat contributions, set bonuses, and content versions.
- Changing equipment in the profile never mutates an active Run or active Trial already in progress.
- Equipment stat calculation is authoritative engine/profile logic, never UI logic.
- Equipment may modify combat through generic typed operations, not production item-specific code branches.
- Exact stat stacking, scaling order, caps, and formula keys are open until DD-16.

### 12.8 Profile Shop and Economy Sinks

The Profile Shop is a persistent account/meta shop. It is distinct from Labyrinth Shops and never uses Obols.

Profile Shop stock may include:

- targeted character duplicate progress or selectors;
- specific weapons or weapon selectors;
- gear set pieces or targeted gear selectors;
- character EXP items;
- weapon EXP items;
- character Sigils;
- weapon ascension materials;
- gear reroll currency;
- other approved persistent materials.

Profile Shop rules:

- Stock, prices, refresh rules, purchase limits, and unlock requirements are content-authored.
- Purchases are atomic and cannot create negative currencies.
- The shop may spend persistent general currency and special conversion currency.
- Special conversion currency must not buy direct pulls and must not become an intermediary pull currency.
- Profile-scoped money is a general economy sink for character leveling, character ascension, weapon leveling, weapon ascension, gear main stat enhancement, and other approved upgrades.
- Every purchase and irreversible upgrade has a preview or clear committed action.

Exact currency names, prices, refresh rules, and targeted dupe policy are open until DD-19 and DD-20.

### 12.9 Character Leveling and Ascension

- Characters gain persistent levels by consuming EXP items and profile-scoped money.
- EXP items have three tiers: small, medium, and large. Final names are pending.
- Higher-tier EXP items grant progressively more EXP.
- EXP items exist so a newly pulled character can be raised quickly in the later game instead of forcing the player to replay early content with an underlevelled roster member.
- Every 10 character levels is capped by a Domain-specific Sigil requirement and profile-scoped money.
- Abyss characters require Abyss Sigils.
- Flesh characters require Flesh Sigils.
- Spirit characters require Spirit Sigils.
- Void characters require Void Sigils.
- Sigils can be awarded by default Run rewards, but Trials are the targeted acquisition path.
- Level and Ascension modify base HP, Attack, and Defense through content-defined growth tables.
- Ascension, level, duplicate ladders, weapons, and gear are separate systems.
- Progression costs and resulting stats are previewed before commitment.

### 12.10 Profile Level

Profile EXP comes from Run milestones, Trial clears, victories, and other non-paywalled account accomplishments. Profile levels unlock features, difficulty tiers, and non-paywalled account bonuses. Unlock pacing must never prevent the initial four-character party or the core run loop.

The player profile level also gates the maximum roster character level and may gate equipment progression bands if DD-11 approves that extension.

- The cap is not 1:1 with profile level.
- Roster characters out-level the player profile.
- Example tuning shape: a low profile level may permit character level 20, while later profile bands raise the roster cap by larger steps.
- The cap prevents runaway character progression before the account has engaged with enough content, while still allowing newly pulled characters to catch up quickly using stored EXP items and Sigils.

Exact profile-to-roster cap bands and any equipment cap bands are **[OPEN]** tuning data.

### 12.11 Run Persistence

One active run may be saved and resumed in the initial scope. The save contains the complete deterministic run state, including map mutations, Shop stock, node resolution, run deck/card instances, current HP, rewards queued for banking, party/equipment snapshots, RNG substream states, and content/schema versions.

---

## 13. Main Menu and Screen Map

### 13.1 Primary Screens

- **Continue Run:** shown when a compatible active save exists.
- **Start Run:** party selection, difficulty, and optional seed input for the default map-based mode.
- **Trials:** selectable direct boss challenges with difficulty tiers and targeted reward previews.
- **Roster:** owned and unowned characters, stats, cards, Ultimate, passive, progression, duplicate ladder, weapon slot, and gear loadout summary.
- **Party Lineup:** construct and save exact-four parties plus selected equipment loadout references.
- **Inventory:** profile item categories, currencies, weapons, gear, lock/favorite tools, and desynthesis.
- **Banners:** pool contents, rates, pity, guarantee, pull history, and pull actions.
- **Profile Shop:** persistent account shop for targeted dupes, weapons, gear, upgrade materials, and reroll resources.
- **Profile:** level, EXP, run history, achievements/tasks, and statistics.
- **Codex:** enemies, statuses, Symptoms, keepsakes, and lore as unlocked.
- **Settings:** audio, display, controls, accessibility, data management, and seed tools.

### 13.2 In-Run Screens

- Labyrinth map;
- Combat;
- Trial setup and Trial result;
- Shop;
- Rest;
- Event;
- Treasure/reward choice;
- Run summary;
- pause/settings overlay.

### 13.3 Navigation Contract

- Leaving a non-combat screen does not silently commit purchases or choices.
- Combat cannot be exited except by completing it, explicit abandon, or application close/save.
- Every irreversible profile or run mutation has a confirmation or an immediately clear committed action.
- A compatible active run is never silently overwritten by Start Run.

### 13.4 Gacha UI Priorities

Gacha UI must show:

- available direct pulls;
- that each pull consumes exactly one direct pull;
- rate table;
- pity state;
- guarantee state;
- duplicate rules;
- possible results;
- recent pull audit/history.

Celebration is welcome. Obfuscation is not.

### 13.5 Tone in UI Text

UI text should be gentle, precise, and slightly uncanny. It should avoid both generic fantasy bombast and exploitative sales language.

Examples of desired flavour:

- "The garden has remembered your shape."
- "Choose what the party carries forward."
- "A soft door opens where the wall was breathing."

Examples to avoid:

- "Limited time mega value!"
- "You will miss out!"
- "Buy now!"
- "Insane DPS meta queen!"

---

## 14. Content Model

### 14.1 Schema-Driven Authored Content

**[INVARIANT]** Authored gameplay content must be data-first and schema-validated. Characters, cards, generated cards, enemies, encounters, statuses, keepsakes, Symptoms, Curses, Trials, rewards, banners, rarity tables, weapons, weapon growth, gear sets, gear stat pools, Profile Shop offers, map motifs, growth tables, and tutorials should live in version-controlled JSON or YAML files wherever practical.

The engine and Unity presentation layer may contain generic systems, renderers, validators, importers, prefabs, scene composition, and test fixtures. They must not hardcode production characters, cards, enemies, encounters, Trial rewards, gacha pools, weapon pools, gear sets, Profile Shop stock, or progression tables.

Allowed exceptions:

- tiny smoke-test fixtures inside test files;
- temporary prototype content explicitly marked as throwaway/non-production;
- engine constants that are true rules rather than authored content, such as legal pile names or command phase names.

Schema requirements:

- Every content family has a preset schema represented by strongly typed C# DTO/contracts plus explicit validators.
- Schemas are validated at build/test time and during developer content import.
- Every definition has a stable ID, content version, display name key, and content-family discriminator.
- Cross-references use stable IDs and are validated.
- Presentation asset references are explicit logical IDs, not implicit filename guesses, scene searches, or Resources-path conventions.
- Balance values live in content data unless they are universal rules.
- Content files are imported/compiled into a versioned runtime registry consumed by the engine/application layer.
- Engine logic references definitions by ID and typed fields, not by filename, GameObject name, prefab name, or UI label.
- Generated Unity assets or caches are derived artifacts and never silently become the gameplay source of truth.

**[DECISION] Content format policy:** production hand-authored gameplay content uses YAML by default. Generated or machine-written content uses JSON by default. Each content family has exactly one canonical source format at a time.

For the Unity build, YAML is an authoring format, not a runtime dependency. Editor/build tooling parses and validates canonical source files and emits deterministic generated runtime data/registries. The runtime consumes the generated validated representation and does not need to parse YAML. Generated JSON, manifests, hashes, registries, lookup tables, and save-like snapshots remain machine-written artifacts rather than competing sources of truth.

### 14.2 Content Registry and Modularity

The content import/registry pipeline builds a versioned registry before gameplay begins. The registry should expose:

- lookup by stable ID;
- family-specific typed indexes;
- validation errors with source path and definition ID;
- content version/hash information for saves and deterministic replay;
- dependency reports showing which definitions reference each other;
- validated logical presentation-asset IDs. Unity Editor/presentation validation cross-checks those IDs against the presentation asset catalogue; the pure content registry never stores `UnityEngine.Object` references.

Modularity requirements:

- Adding a new character should not require editing combat engine code.
- Adding a new card should not require editing Unity UI/presentation code except for genuinely new generic renderer/operation features.
- Adding a new enemy should not require editing the combat state machine unless it introduces a new generic operation.
- Adding a new Trial should mean adding a Trial definition and reward table, not adding a bespoke scene path.
- Adding a new weapon, gear set, gear stat table, or Profile Shop offer should primarily be a content-data change.
- New effect behaviour must be added as typed reusable engine operations, then used by content.

### 14.3 Static Content

Version-controlled definitions include:

- characters;
- cards;
- generated cards;
- enemies and intents;
- encounters;
- keepsakes/boons;
- Symptoms and Curses;
- events;
- map motifs and map-content tables;
- banners and gacha pools;
- rarity tables;
- weapon definitions;
- weapon growth, ascension, duplicate bonus, and EXP tables;
- gear set definitions;
- gear slot, main stat, substat, tier weight, and reroll tables;
- gear desynthesis tables;
- Profile Shop offer tables;
- rewards;
- direct pull grant tables;
- Trial definitions and difficulty tiers;
- EXP item tiers;
- domain Sigils;
- profile-to-roster level cap tables;
- statuses;
- tutorials;
- growth tables.

Each definition has a stable ID and content version. Validation runs during content import, automated tests, and release builds.

### 14.4 Content Validation

Validation must catch:

- missing IDs;
- duplicate IDs;
- invalid owner references;
- invalid card zones;
- impossible targeting;
- missing localisation/display strings;
- invalid rarity/rate tables;
- invalid ten-pull guarantee tables;
- weapon definitions with invalid rarity, signature links, duplicate tiers, growth, or ascension references;
- gear definitions with invalid slot, set, main stat, substat, tier, reroll, or desynthesis references;
- Profile Shop offers that reference unknown items or forbidden direct-pull conversion paths;
- unreachable map nodes;
- unbounded resource loops;
- missing content warnings where needed;
- missing logical portrait/sprite/VFX/audio references once the relevant presentation contract requires them;
- logical asset references that cannot be resolved by the production presentation catalog at the milestone that requires them;
- cards that can enter a deck without ownership;
- formulas that read unavailable state;
- invalid status timing;
- Trial reward tables that reference unknown items;
- character ascension requirements that do not match the character's Domain;
- profile-level cap tables that decrease or skip invalidly.

### 14.5 Hardcoding Guardrails

Implementation reviews should reject production gameplay content when it appears in:

- `MonoBehaviour`, presenter, scene, prefab, or UI conditionals keyed to a specific production character/card/enemy ID;
- engine switch statements that special-case a named character rather than a generic effect operation;
- hand-written encounter construction outside validated content data;
- hardcoded gacha pool contents;
- hardcoded Trial reward tables;
- hardcoded Profile Shop stock;
- hardcoded weapon or gear set definitions outside validated content data;
- hardcoded level cap, EXP, or Sigil tables outside validated content data;
- scene hierarchy names or prefab names being treated as gameplay IDs.

Some switch statements are acceptable when they dispatch over stable operation kinds, target kinds, status classes, node types, presentation-token kinds, or renderer primitives. The difference is that the switch belongs to a reusable system, not to a particular authored piece of content.

### 14.6 Writing Direction

Writing should imply rather than overexplain. Bloomdrawn's horror works best when the player can understand the immediate consequence but not the full cosmic reason.

The game should maintain compassion toward its characters. Horror may happen to them or through them, but they should not be reduced to props.

The Bloom should be written through immediate consequences: what grew, what it offered, what was lost, what remains recognizable, and what choice follows. Avoid flattening it into a simple plague, evil corruption, or fully explained invader. Different cultures and characters may describe the Bloom as a gift, illness, god, ecology, miracle, disaster, reproductive event, or judgement without any single explanation becoming final.

### 14.7 AI-Assisted and Generated Content

AI-assisted writing, data scaffolding, concept work, and generated art are permitted project tools. **Generated art is explicitly permitted at every production stage, including release-quality content.** The method of generation is provenance metadata, not an automatic placeholder classification.

Requirements:

- AI-assisted gameplay/data content must still pass the same schemas, deterministic rules, and content validation as hand-authored content.
- A human review pass is required before generated writing, gameplay content, or art is treated as release-quality.
- Generated art may be used directly as production art when it meets the project's quality, readability, consistency, provenance/rights, content-warning, and technical-import requirements.
- No milestone, validator, or release gate may reject an otherwise acceptable asset solely because it was AI-generated.
- Generated character, enemy, card, environment, UI, and event art must be checked against Bloomdrawn's visual direction, Domain language, gameplay-scale readability, and existing character continuity.
- Generation should target usable production assets where practical: transparent actor art separated from backgrounds, clean silhouettes, sufficient resolution, and separable secondary-motion layers when the intended animation benefits from them.
- AI-generated art or writing should not be accepted merely because it exists; there must be an attempt at high quality, coherence, and fit.
- Placeholder/provisional status is explicit metadata based on readiness, not generation method.
- Source/provenance records should distinguish prompt/reference/source material, generated derivative/export, human edits, review state, and release-readiness where applicable.

AI assistance is a production accelerator, not an authority layer. `docs/DESIGN.md`, approved decision records, schemas, validated content, and human art/design review remain authoritative.

### 14.8 Content Warning Metadata

Content definitions that include significant horror motifs must declare content warning tags. These tags support settings, archive filtering, and future platform requirements. Tags should be descriptive rather than moralising: body horror, drowning, eye imagery, memory loss, identity erosion, ritual harm, confinement, and similar.

## 15. Technical Architecture

### 15.1 Architectural Goal

Presentation, persistence, deterministic rules, and Unity scene state are separate layers.

```text
pointer / keyboard / controller input
  -> UI interaction state (hover, drag, armed, targeting)
    -> validated Game Command
      -> pure deterministic Engine transition
        -> next authoritative Game State + ordered Game Events
          -> application/session adapter
            -> presentation-token sequence
              -> Unity actor / UI / VFX / audio rendering
                -> persistence snapshot when required
```

The Unity scene never computes authoritative damage, targeting legality, resource spend, map reachability, gacha results, rewards, or progression independently. UI interaction state may decide what the player is pointing at or whether a card is visually armed, but only the engine accepts gameplay commands.

### 15.2 Unity Runtime and Tooling Baseline

**[DECISION]** Bloomdrawn targets the **Unity 6.5 Supported release line (`6000.5.x`)** for initial production. Unity recommends Update/Supported releases for new and mid-cycle productions because they carry the latest fixes, platform support, performance improvements, and features. The repository pins the exact 6.5 patch in `ProjectSettings/ProjectVersion.txt`; changing that pinned patch or moving to another Unity release line requires an explicit upgrade task with package, import, test, build, and representative-scene validation. Windows is the primary development and validation target. The public release platform remains a DD-14 packaging decision.

Recommended launch stack:

- **Language:** C# for engine, application services, Unity presentation, import tooling, validation, tests, and simulations. Code targets Unity 6's documented C# 9.0 support and must not casually introduce C# 10+ syntax or C# 9 features Unity documents as unsupported/caveated.
- **Assembly boundaries:** `Bloomdrawn.Engine` is a dedicated Assembly Definition with **No Engine References** enabled so it cannot reference `UnityEngine` or `UnityEditor`. The deterministic/runtime content-contract assembly is likewise kept free of Unity engine/editor references; Unity-specific importers and asset bindings live in Editor/Application/Presentation assemblies.
- **Rendering:** Universal Render Pipeline with the 2D Renderer for sprite-based battlefield, backgrounds, 2D lighting, Shader Graph effects, and optional 2D-compatible VFX Graph work.
- **Actor presentation:** independent `GameObject` actor roots using `SpriteRenderer`/sorting, presentation anchors, and animation/VFX components. Party members and targetable enemies are never represented as one production composite actor.
- **Runtime UI:** Unity UI (uGUI) with TextMesh Pro for the combat HUD, cards, hand, target overlays, menus, roster, inventory, banners, Trials, settings, tooltips, and modals. One runtime UI system is preferred initially to reduce cross-system focus, raycast, and drag-coordinate complexity.
- **Editor tooling:** UI Toolkit is appropriate for custom Editor windows, content-validation views, and developer tools. Runtime UI Toolkit adoption later requires an explicit plan-level decision rather than an accidental mixed-UI migration.
- **Input:** Unity Input System for mouse, keyboard, controller, and future touch/pointer support. Card drag and target selection are input presentation; they dispatch engine commands only when complete.
- **Animation:** Animator/Animation Clips and small presentation-only interpolation/tween helpers for actor movement and UI motion. Unity 2D Animation/SpriteSkin is optional for art that is deliberately prepared for deformation; rigging is not required for every character.
- **VFX:** Particle System and Shader Graph are baseline tools; VFX Graph is optional when it materially improves an effect and remains compatible with the 2D Renderer/readability budget.
- **Testing:** Unity Test Framework for Edit Mode and Play Mode tests, supplemented by deterministic batch simulations and project validation scripts.
- **Persistence:** versioned local save envelopes through repository interfaces, stored under `Application.persistentDataPath` for local builds. Authoritative saves contain data IDs/state, not Unity object references, scene instance IDs, or transient GameObject state.
- **Agent/editor automation:** Unity CLI plus the experimental `com.unity.pipeline` package is a development surface, not a gameplay dependency. Repeated project operations should be exposed as project-specific CLI commands; `unity command eval` is reserved for ad-hoc inspection/debugging.
- **Future account service, optional:** only if cloud sync, accounts, leaderboards, or shared online features become real requirements.

Unity's current documentation notes that Unity 6 runtime UI can use both Unity UI and UI Toolkit; Unity UI remains the general runtime recommendation and is specifically suited to custom materials/world-space integration, while UI Toolkit is strong for dense multi-resolution menus/HUD. Bloomdrawn intentionally starts with uGUI for one coherent runtime interaction system because the card hand, drag layer, targeting, custom visual treatment, and actor overlays are unusually interaction-sensitive.

Official reference baseline is mirrored in `plans/implementation_plan.md` Appendix F. Exact Unity CLI syntax is not a design contract because the CLI/Pipeline is experimental; the installed `unity --help` / `unity command --help` output is authoritative for the development machine.

### 15.3 Backend Stance

The launch game should not pretend to be a live-service architecture. A local profile/save is authoritative in the local build, and deterministic audit logs plus save validation provide trust and debuggability.

A remote backend becomes appropriate only for features such as:

- cloud saves;
- cross-device profiles;
- account identity;
- leaderboards;
- shared events;
- anti-tamper requirements for public competitive features.

If an account service is introduced later, profile mutations, direct pulls, inventory changes, Trial rewards, and run-finalization rewards must become transactional service operations. Combat can still run client-side responsively, but submitted summaries or command/event proofs need validation according to the chosen service model.

### 15.4 Pure Engine Modules

`Bloomdrawn.Engine` contains framework-independent C# and must compile with Unity engine references disabled. Deterministic content DTOs/registry interfaces supplied to it must also be Unity-object-free. It contains:

- combat state machine;
- card/effect resolution;
- formula evaluator;
- status and timing system;
- target selection;
- deck/pile operations;
- enemy intent generation and resolution;
- map motif stitching and validation;
- traversal and node-resolution rules;
- reward generation;
- gacha/pity resolution;
- save-model serialization/migration helpers that do not depend on Unity object state.

Core transition shape:

```csharp
public readonly struct EngineResult<TState, TEvent>
{
    public bool Accepted { get; }
    public TState State { get; }
    public IReadOnlyList<TEvent> Events { get; }
    public CommandRejection Rejection { get; }
}

public static EngineResult<CombatState, GameEvent>
    ApplyCombatCommand(CombatState state, CombatCommand command, CombatRng rng);
```

The engine may use controlled internal mutation/copy-on-write for performance if tests prove deterministic public behaviour, but its public contract behaves as a command transition. It must not use `UnityEngine.Random`, `Time`, scene queries, `MonoBehaviour`, `ScriptableObject`, GameObject names, frame rate, animation state, or input state as authoritative inputs.

### 15.5 Application and Presentation Adapters

Unity-facing application services bridge authoritative state to scenes without duplicating rules.

Expected responsibilities:

- `ProfileSession` loads profile state and submits profile/equipment/gacha commands;
- `RunSession` owns the active run snapshot and submits map/run commands;
- `CombatSession` owns the current authoritative combat state, submits combat commands, and publishes accepted events/rejections;
- `CombatPresenter` maps engine events/state changes to presentation tokens and actor/UI bindings;
- `UiState` contains hover, selected card, drag session, target-selection state, modal state, log filters, animation speed, and reduced-motion state only.

The Unity presentation layer also owns a `PresentationAssetCatalog` (or equivalent typed catalogue) that maps logical presentation IDs from validated content to `Sprite`, prefab, material, audio, VFX, and other Unity assets. The catalogue is presentation-only: deterministic state stores the logical ID, never the resolved Unity object. A minimal catalogue exists before production starter art is bound in M2; M2A expands its validation and bindings for starter portrait, combat-sprite, and Ultimate-VFX references while retaining generic fallbacks. M10 closes completeness, import rules, budgets, and release validation.

M2H establishes the first minimal authoritative, side-effect-free evaluator required for starter target/resource previews. It reuses command validation and calculation helpers without mutation or live RNG consumption; M9 expands that evaluator and closes production preview UX rather than introducing UI-side rule duplication.

A Unity component may submit a command but may not reproduce the engine rule in parallel. Scene/prefab references live in the presentation layer and may not leak into deterministic state.

### 15.6 Typed Effect Model

Card, status, weapon, and keepsake effects use a typed, serializable operation tree rather than arbitrary callbacks or unparsed code/formula strings.

Requirements:

- exhaustive operation kinds and validated payloads;
- schema validation for static content;
- no runtime code evaluation for authored gameplay effects;
- formula preview and resolution share the same evaluator;
- explicit conditional and iteration bounds;
- escape hatches require named generic engine operations and tests, not inline `MonoBehaviour` callbacks or character-ID branches.

Unity CLI `eval` is a developer inspection tool only and is never an authored gameplay-effect mechanism.

### 15.7 Persistent Data

Persistent data is accessed through repository interfaces rather than directly through UI components, `MonoBehaviour`s, or engine modules.

```csharp
public interface IProfileRepository
{
    PlayerProfile LoadProfile();
    void SaveProfile(PlayerProfile profile);
    RunSave? LoadRunSave();
    void SaveRunSave(RunSave save);
    void AppendGachaAudit(GachaAuditEntry entry);
    void AppendProfileShopAudit(ProfileShopAuditEntry entry);
    void RecordTrialClear(TrialClearEntry entry);
}
```

Local implementation:

- stores versioned data under `Application.persistentDataPath`;
- writes through a temporary file/transactional step and preserves the last valid snapshot on failure;
- validates schema/content versions and checksum before accepting a load;
- keeps a recoverable previous snapshot where practical;
- serializes stable content IDs and data state, never `UnityEngine.Object` references, scene hierarchy paths, instance IDs, or current animation/UI state.

Logical persistent entities include:

- `Profile`;
- `CharacterOwnership`;
- `InventoryItem`;
- `OwnedWeapon`;
- `OwnedGear`;
- `EquipmentLoadout`;
- `BannerState`;
- `ProfileShopState`;
- direct-pull/audit records;
- `TrialClearState`;
- `SavedParty`;
- `RunSave`;
- `RunHistory`;
- `GachaAuditEntry`;
- `ProfileShopAuditEntry`;
- `OneTimeRewardFlag`.

Deeply nested active-run state may be stored as versioned JSON snapshots as long as validation and migration rules are preserved.

### 15.8 Authority Modes

**Local mode:** the local installation/profile is authoritative. Tamper resistance is not a design goal, but save corruption detection and deterministic auditability are.

**Account service mode:** if an account-backed version ever exists, gacha pulls, inventory mutation, profile rewards, and run-finalization rewards become server-authoritative and transactional. The client may simulate combat responsively, but the service validates accepted run summaries or command/event proofs according to the eventual deployment model.

The initial project must not falsely imply that a client-only local economy is secure enough for a competitive account service.

### 15.9 Presentation Sequence and Actor Contract

Engine events are authoritative and immediate. Presentation translates them to ordered presentation tokens from M1 onward.

- Tokens identify relevant source/target runtime instance IDs and semantic action kind; they do not own gameplay results.
- `CombatActorView` instances bind runtime character/enemy IDs to independent Unity actor roots.
- The presenter may accelerate, skip, or reduce animation while preserving event order and final authoritative state.
- Skipping animation cannot skip engine events.
- Input remains locked only where simultaneous commands would violate state consistency or where an accepted sequence is intentionally resolving.
- UI-only card drag/target selection may remain active only before command acceptance; accepted command playback owns the presentation sequence until the interaction lock releases.
- Reloading from a committed state may omit already-consumed presentation tokens without altering results.
- Every required action has a safe fallback presentation so missing optional animation/VFX cannot block gameplay.

### 15.10 Map Rendering

- Flat-top axial coordinates remain authoritative engine data.
- The Unity map is a derived 2D presentation, normally using individual node views plus connector/edge views in a world-space or camera-space map scene.
- Unity Grid/Tilemap, SpriteRenderer, LineRenderer, generated meshes, or equivalent primitives may be used when useful, but no renderer primitive owns traversability.
- Current node, reachable state, spent/collapsed state, fog/reveal state, and edge state come from engine output.
- Geometry utilities are pure and separately tested where they affect layout math.
- Visual adjacency never creates traversability without an engine edge.

### 15.11 Agent and Unity CLI Workflow

Unity CLI and `com.unity.pipeline` exist to close the edit/observe/verify loop for Codex or other agents.

Rules:

- The project remains buildable and testable without an agent or Pipeline connection.
- Unity CLI/Pipeline is experimental; never make authoritative game architecture depend on undocumented CLI syntax.
- `unity --help`, `unity command --help`, and the connected Editor's `unity command` discovery output take precedence over remembered or stale syntax.
- Repeated operations should become project-owned `[CliCommand]` tools with stable Bloomdrawn names rather than large copied `eval` snippets.
- `unity command eval`/`eval_file` is appropriate for ad-hoc Editor inspection, scene queries, and one-off debugging, not for permanent project behaviour.
- Pipeline runtime control is allowed only in Editor/development/QA contexts and must not be exposed in production release builds.
- Project validation scripts should emit clear exit codes and machine-readable summaries where practical.
- Agents should verify scene/runtime consequences through tests, project commands, or live inspection rather than reporting success solely because source files compiled.

Recommended project commands grow with the relevant milestone, for example:

```text
bloom.health
bloom.validate-content
bloom.scene-summary
bloom.load-combat-fixture
bloom.reset-combat-fixture
bloom.dump-combat-state
bloom.validate-combat-layout
```

The names above are project contracts once implemented; the transport syntax used to invoke them may be updated when the experimental Unity CLI changes.

### 15.12 Official Unity Reference Baseline

Implementation tasks should prefer current official Unity documentation over model memory for engine/package behaviour. The baseline references are:

- Unity 6 release/support policy: https://unity.com/releases/unity-6/support
- Unity CLI introduction/reference: https://docs.unity.com/en-us/unity-cli/ and https://docs.unity.com/en-us/unity-cli/unity-cli-reference
- Unity CLI + Pipeline agent workflow and `[CliCommand]`: https://unity.com/blog/meet-the-unity-cli
- Unity Test Framework: https://docs.unity3d.com/Manual/com.unity.test-framework.html
- Unity C# compiler/language support: https://docs.unity3d.com/Manual/csharp-compiler.html
- Assembly Definitions / No Engine References: https://docs.unity3d.com/Manual/class-AssemblyDefinitionImporter.html
- Unity UI system comparison: https://docs.unity3d.com/Manual/UI-system-compare.html
- RectTransform coordinate utilities: https://docs.unity3d.com/ScriptReference/RectTransformUtility.html
- uGUI Canvas scaling: https://docs.unity3d.com/ScriptReference/UI.CanvasScaler.html
- Input System: https://docs.unity3d.com/Manual/com.unity.inputsystem.html
- Persistent data path: https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html
- URP 2D rendering/Shader Graph documentation: https://docs.unity3d.com/Manual/urp/2d-index.html

Documentation can change within the Unity 6 family. Task plans that depend on a specific package/API should link the current page they verified.

## 16. Determinism, RNG, and Save Contract

### 16.1 Determinism Definition

Given the same:

- engine version;
- content version;
- initial state;
- named RNG substream states;
- ordered player commands;

the engine must produce the same state transitions and ordered game events.

Visual particles, idle motion, and non-gameplay audio variation are excluded and use non-authoritative randomness.

### 16.2 Named RNG Substreams

At minimum:

- `map.layout`;
- `map.content`;
- `map.nodeModifiers`;
- `combat.shuffle` per encounter;
- `combat.targeting` per encounter;
- `enemy.intent` per encounter/enemy;
- `reward` per node;
- `shop` per Shop;
- `gacha` per profile/banner family;
- `profile.equipment` for gear stat rolls and rerolls.

Presentation-only randomness is explicitly **not** an authoritative named substream. Particles, idle offsets, harmless audio variation, and other cosmetic choices may use a separate presentation RNG because their outputs are excluded from saves, replays, and authoritative checksums. Adding a cosmetic random call must not perturb card shuffles, map topology, gacha results, or any other authoritative stream.

### 16.3 Save Contents

An active run save contains:

- `saveSchemaVersion`;
- `engineVersion`;
- `contentVersion`;
- run seed and difficulty;
- all authoritative RNG substream states/counters;
- party and progression snapshot;
- equipment snapshot, including equipped weapon, gear pieces, calculated stat contribution, and active set bonuses;
- map graph, node content, spent state, Collapsing Node lifecycle state, current position;
- Shop stock, prices, and purchases;
- run deck with unique card instances and owners;
- current shared HP and run modifiers;
- Obols and queued bankable rewards;
- active encounter/combat state if mid-combat saves are supported;
- command/event checkpoint metadata and checksum.

### 16.4 Save Timing

Save atomically after:

- movement completes;
- a node consequence commits;
- a Shop purchase;
- a reward choice;
- combat victory/defeat transition;
- an explicit quit/save action;
- each stable, fully resolved player command at the approved combat-action checkpoint.

Player-visible recovery is limited to map/node boundaries and stable fully resolved combat-action boundaries. Mid-resolution, mid-animation, and interaction-state recovery are excluded; drag, target-selection, and presentation playback state remain non-authoritative and unsaved.

A failed write does not replace the last valid snapshot.

### 16.5 Migration and Compatibility

- Migrations are explicit functions between adjacent schema versions.
- Content changes either provide stable compatibility or invalidate affected active runs with a clear explanation and compensation policy.
- Definition IDs are never casually reused.
- A checksum mismatch or validation failure falls back to the last valid snapshot where possible.

### 16.6 Replay and Debugging

Development builds can export:

- initial state;
- seed/substream state;
- command list;
- engine events;
- final checksum.

This enables deterministic reproduction of combat and map bugs without depending on animation timing.

---

## 17. Testing, Validation, and Invariants

### 17.1 Required Test Layers

1. **Edit Mode unit tests:** formula evaluation, piles, statuses, resource changes, targeting, pity, hex math, repositories, import validation, and other code that does not require a running scene.
2. **State-machine tests:** legal/illegal commands, phase transitions, command rejection, and terminal states against the pure engine assembly.
3. **Content/import validation:** IDs, owner references, formulas, targets, costs, motif topology, enemy intents, banner pools, presentation asset references, and generated runtime registry hashes.
4. **Seed/property stress tests:** map generation and combat invariants across large deterministic seed samples; a third-party property-test library is optional rather than architectural.
5. **Golden deterministic tests:** fixed content + seed + commands produces a fixed semantic event trace/state checksum independent of frame rate or presentation.
6. **Play Mode integration tests:** card hand layout, drag threshold/return behaviour, target selection, actor binding, previews, sequential enemy presentation, scene transitions, and accessibility interaction paths.
7. **End-to-end validation:** profile creation through completed run, rewards, save/reload, gacha persistence, and representative player flow in a built/development Player where useful.
8. **Visual/layout regression checks:** milestone screenshots or automated captures may support review of battlefield composition and aspect ratios, but screenshots never replace behavioural assertions.

### 17.2 Combat Invariants

- A card instance occupies exactly one location.
- Mana and Domain resources never become negative.
- A Transcend resolves no more than once per owner per combat.
- Ultimate gauge never exceeds its allowed cap.
- Enemy actions resolve in stable visible order.
- The damage preview uses the same modifier functions as resolution.
- A failed command leaves authoritative state unchanged.
- A terminal combat state accepts no normal combat commands.
- Generated combat-scoped cards cannot leak into the run deck after combat.
- Card hover/drag/arming/target-selection state cannot mutate authoritative combat state before an accepted command.
- Every targetable party/enemy runtime instance has at most one active presentation actor binding, and actor destruction/rebinding cannot alter engine slot order.

### 17.3 Map Invariants

- Motifs never overlap.
- All traversal edges connect adjacent hexes.
- Undeclared adjacency is blocked.
- Boss is reachable and cannot be permanently lost through optional Collapsing Node use.
- Node-scoped one-time consequences trigger at most once.
- Shops preserve stock and sold state.
- Revisit does not regenerate rewards.
- Generated maps meet content and combat-count bounds.
- Saving/reloading preserves topology and mutations exactly.

### 17.4 Economy and Gacha Invariants

- Obols cannot be generated by repeatedly traversing resolved nodes.
- Purchases are atomic and cannot create negative currency.
- Pity increments and resets exactly once per pull result.
- Hard pity cannot fail.
- A lost featured 50/50 correctly guarantees the next Bloom result in the same pity family.
- Pull results and inventory mutations commit transactionally in authoritative service mode.
- No paid currency or paid pull path exists.
- Pulls are awarded and consumed directly; there is no intermediary conversion resource.
- Special profile-shop conversion currency cannot be converted into direct pulls.
- Persistent profile currencies cannot be spent as Obols and Obols cannot leave the active run.
- Character level cannot exceed the current profile-level roster cap.
- Every 10-level character ascension gate consumes the correct Domain Sigil.
- Weapon duplicate bonuses cannot exceed +5.
- Gear rerolls use only the profile equipment RNG stream and consume no active-run RNG.
- Rejected equipment upgrade, reroll, desynthesis, auto-discard, or Profile Shop commands consume no currency, items, or RNG.
- Active Run and Trial equipment snapshots do not change when profile equipment changes later.
- Trials award only their declared persistent reward categories and never mutate active Run state.

### 17.5 Content and Schema Invariants

- Production authored content lives in validated JSON/YAML content files rather than hardcoded engine/UI branches.
- Every content definition has a stable ID and content version.
- Content registry construction fails on duplicate IDs, invalid references, invalid formulas, or invalid asset references once required.
- Engine and UI code may dispatch on generic operation kinds, but not on production character/card/enemy IDs for gameplay behaviour.
- Saves record content/schema versions and reject incompatible content without an explicit migration or invalidation path.
- Adding a new character, card, enemy, Trial, reward, banner pool, weapon, gear set, Profile Shop offer, or equipment stat table should be primarily a content-data change.
- Generated/runtime Unity registry artifacts must be reproducible from canonical validated content and must not become a second gameplay source of truth.
- No production gameplay rule may depend on a GameObject name, prefab name, scene hierarchy path, Animator state name, or Unity instance ID.

### 17.6 Balance Simulation Hooks

The headless engine should support batch simulation or scripted policies for:

- average cards played per turn;
- resource growth curves;
- damage/shield per Mana;
- time-to-kill by encounter tier;
- Abyss scaling over round count;
- Ritual hand congestion and time-to-cast;
- Flesh build/spend cadence;
- control-lock frequency;
- minimum/maximum route combats and Obol totals;
- likelihood of first-visit versus return-Shop affordability;
- expected pull income per hour and per completed run;
- direct pulls earned per mode and difficulty;
- EXP item income by tier;
- Sigil income by Domain;
- Trial reward efficiency by difficulty;
- collection completion timelines;
- duplicate frequency;
- weapon acquisition and duplicate frequency;
- gear stat distribution and reroll cost curves;
- Profile Shop affordability and conversion currency sinks;
- equipment stat contribution by profile stage;
- card/keepsake pick rates;
- Domain underuse.

Simulation informs tuning but does not replace human playtesting.

---

## 18. Encounters, Enemies, Keepsakes, and Content Scope

### 18.1 Encounter Types

- **Normal:** 1-3 standard enemies; moderate Obols and card reward chance.
- **Elite:** one elite or a coordinated elite group; higher Obols and premium keepsake/boon reward.
- **Boss:** capstone encounter with phase mechanics and Control Resistance; run-ending reward/finalization.

Encounters are selected from seeded tables using biome, map depth, party history where allowed, and anti-repeat constraints. Encounter selection may respond to difficulty but may not secretly hard-counter the chosen party.

### 18.2 Enemy Definition

Every enemy declares:

- stable ID and content version;
- base/current HP and ordinary Shield rules;
- sprite and animation references;
- presentation metadata for size class, stage footprint, readable target bounds, target marker anchor, HP/status/intent label anchors, and optional formation role;
- intent deck or state machine;
- targeting and numerical formulas;
- statuses, traits, immunities/conversions;
- reward tier;
- phase transitions and summon rules;
- tooltip text for every intent component.

Enemy placement metadata is presentation-only. It does not alter authoritative enemy slot order, targeting rules, deterministic stable ordering, or combat resolution.

### 18.3 Intent Generation

- Intents are selected through a deterministic enemy-specific RNG substream or deterministic state machine.
- The exact next action is revealed before the player acts.
- Multi-hit attacks show hit count and per-hit/total damage.
- Conditional branches show the condition and outcome range.
- Delayed intents remain visibly preserved.
- Boss phase transitions may conceal future phases but not the currently committed action.

### 18.4 Enemy Visual Direction

Enemies may begin toy-like, plush, ceramic, paper, floral, or storybook-soft, especially early in a run. Their horror must become apparent through gameplay-scale design details:

- tentacles, maws, wrong joints, or predatory blooms;
- soft bodies behaving like traps;
- cheerful masks hiding hungry interior forms;
- pastel surfaces interrupted by Abyss pressure, Flesh grafts, Spirit fractures, or Void absence;
- animation tells that reveal the unsafe underlying nature before or during attacks.

### 18.5 Boss Rules

Bosses may use:

- multi-phase HP thresholds;
- permanent traits;
- phase armour;
- summons;
- resource pressure;
- Control Resistance;
- telegraphed enrage clocks.

They may not rely on unexplained immunity, hidden instant-kill rules, or untelegraphed intent replacement after the player commits.

### 18.6 Keepsakes and Boons

Run-scoped keepsakes/boons are static definitions with:

- acquisition rarity and source;
- trigger timing;
- typed effect nodes;
- stacking rule;
- owner scope (party, Domain, or character);
- UI status representation;
- save/version compatibility.

High-value keepsakes are a central Shop temptation. Keepsakes should create build direction or interaction rather than only flat stats, while a limited number of straightforward defensive options maintain readability.

### 18.7 Initial Content Targets

Initial complete-game scope includes:

- 8 playable characters and their 40 starting cards;
- all generated Transcend cards;
- a small but sufficient pool of run-acquired Domain cards;
- at least 4 Symptoms and 4 Curses;
- enough motifs to generate visibly different spine/side-loop layouts;
- Normal, Elite, and Boss encounter pools;
- Trial boss challenge definitions for Flesh, Abyss, Spirit, Void, EXP, and Money reward families;
- selectable Trial difficulty tiers;
- Shops, Rest, Event, Treasure, Travel, Symptom, and combat nodes;
- a keepsake/boon pool large enough to offer meaningful Shop and Elite choices;
- direct pull reward grants;
- three EXP item tiers;
- four Domain Sigils;
- persistent general currency reward tables;
- all primary menu/meta screens;
- persistent profile, roster, inventory, banner, party, run save, and history state.

M8X equipment expansion content targets add:

- character weapon definitions, including eight draft signature weapons;
- SR and R non-signature weapon pools sufficient to fill banner pulls;
- three weapon EXP item tiers;
- weapon ascension material definitions and reward tables;
- six-slot gear set definitions with 3-piece and 6-piece bonuses;
- gear main stat, substat, tier, reroll, and desynthesis tables;
- Profile Shop offer tables for targeted dupes, weapons, gear, and upgrade materials;
- inventory category definitions for weapons, gear, and desynthesis flows.

Exact counts beyond the eight characters are content-production targets set during milestone planning after the vertical slice proves throughput.

---

## 19. Implementation Milestones

Milestones are ordered to retire design and technical risk early. The project uses deterministic, schema-first governance while establishing **permanent presentation infrastructure** in the first combat slice. M1 must prove the real actor model, battlefield composition, card hand, drag threshold, targeting flow, and event-to-presentation sequence; M9/M10 extend, harden, and polish those same systems rather than replacing them with a separate presentation architecture.

### M0 - Unity Repository Contract and Foundations

- Unity 6.5 Supported-line project/version lock and Windows development baseline;
- repository conventions, agent instructions, Unity skill references, and Git/LFS rules;
- Assembly Definition boundaries, including a pure `Bloomdrawn.Engine` assembly with No Engine References;
- YAML/JSON canonical content source structure, C# schema/contracts, import validation, generated runtime registry/hash;
- stable IDs and content validation;
- named serializable RNG substreams;
- pure command/event engine interfaces;
- local save envelope/repository interfaces;
- Unity Test Framework Edit Mode/Play Mode setup and PowerShell validation wrappers;
- Unity CLI/Pipeline smoke test plus initial project health/content-validation commands;
- minimal typed presentation asset catalogue and Editor validation for logical asset bindings;
- minimal bootstrap/dev scene with no production gameplay content.

**Exit:** fixed-seed golden test passes, sample content imports through validators, the project opens/builds under the pinned Unity version, the engine assembly has no UnityEngine/UnityEditor dependency, and an agent can query project health through the documented development tooling.

### M1 - Minimal Combat Vertical Slice and Presentation Foundation

- one schema-authored non-production fixture party of four;
- one Strike and Shield per owner;
- one fixture enemy with visible attack intent;
- shared HP/Shield, Mana, draw-to-five, discard, exhaustion reshuffle;
- player action, cleanup, player end, sequential enemy action, enemy end;
- permanent independent party/enemy actor view architecture;
- anchored battlefield layout with party left, enemy right/right-center, shared survival below party, and bottom-centred hand;
- production-shaped uGUI card prefab and deterministic fan layout;
- hover/select plus upward drag, responsive Play Area threshold, cancel/return behaviour, and explicit-target selection state;
- event-to-presentation token adapter with fixture fallback animations;
- basic event log, intent/target overlays, and Play Mode interaction coverage.

**Exit:** a complete deterministic combat can be built from the validated fixture registry, played through the real card-hand/actor/presentation path, reproduced headlessly, and verified in Play Mode without production character or Domain content. Repeated drag/cancel/play cycles do not drift or force cards off-screen at required test aspect ratios.

### M2 - One Character per Domain

- production character/card schema and content-format lock before authoring;
- Mara, Thalassa, Sephira, and Azael;
- production starter content replaces the runtime fixture party;
- Embryo, Tentacles/Potency, Essence/Ritual, Void economy/control;
- Domain-specific owner effects and ultimate gauges;
- Transcend and Graveyard;
- status timing and Control Resistance prototype;
- generated/reviewed production-capable portraits and combat art may bind through the real presentation catalog immediately when available;
- basic idle/act/hit/return presentation uses the M1 independent actor contract;

**Exit:** all four core engines interact in one playable combat without bespoke Unity-side rules, every starter party member is independently presented/addressable, and non-production M1 characters/cards remain only in isolated test sources.

### M3 - Minimal Labyrinth and Run Loop

- isolated schema-authored non-production run fixtures for reward, Shop, keepsake, Symptom, and Boss flow;
- axial hex/world-space 2D map rendering and traversal;
- main-spine and canonical side-loop motifs;
- Travel, Combat, Shop, Symptom, and Boss nodes;
- Collapsing Node lifecycle and collapse-on-departure behaviour;
- revisitable persistent Shop;
- finite Obols and cleared-node behaviour;
- deterministic canonical serialization of map, Shop, reward, and node mutations for M4.

**Exit:** the player can discover an unaffordable fixture keepsake, leave, win optional combat, pay a loop cost, return, purchase it, and reach the Boss without hardcoded fixture IDs or production reward quantities.

### M4 - Run Completion and Persistence

- reward choices and generic validated reward-ledger banking;
- victory, defeat, abandon, and banking;
- active run save/resume through local repository implementation;
- persistence for profile, roster, saved parties, run history, and inventory systems implemented through M4;
- atomic purchases and finalization;
- save corruption/recovery behaviour appropriate to the local file repository.

Save/resume exposes map/node boundaries and stable fully resolved combat-action boundaries only; it excludes mid-resolution, mid-animation, and interaction-state recovery.

M4 does not reserve empty banner, Trial, equipment, profile-level, duplicate, or future progression sections. M8, M8B/M8T, and M8X add those persisted contracts through explicit save-version changes and migrations.

**Exit:** a run can be started, closed, resumed, completed, and reflected correctly in the profile without speculative future-state payloads.

### M5 - Full Launch Character Roster

- Venelis, Nyxalia, Kibane, and Mira Nox;
- all 40 starting cards and generated Transcend cards;
- completed character presentation briefs covering apparent age band, gameplay-scale silhouette, costume, palette, concrete material/anatomy cues, horror reveal, pose language, warnings, and logical asset references;
- character/party screens;
- starting roster/tutorial profile rules, including a short focused teaching beat per launch Domain and a complete starter-party tutorial combat;
- content and deterministic golden tests;
- production actor bindings remain generic and use the same M1/M2 presentation contracts.

### M6 - Encounters, Boss, and Reward Breadth

- Normal and Elite encounter pools;
- launch Boss with phases and Control Resistance;
- keepsake/boon pool;
- card reward pools;
- Rest, Event, and Treasure nodes;
- initial Symptoms and Curses;
- concrete gameplay-scale 2D briefs for materials, silhouette/anatomy changes, behavior/animation tells, Domain refraction, Bloom benefit/cost, and warnings;
- production run content replaces M3 runtime fixtures, which remain only in isolated tests.

### M7 - Labyrinth Generation Breadth

- expanded motif library;
- depth/content constraints;
- shop/recovery/elite side-loop variants;
- semantic presentation tags for spine/loop metaphor, connector material, landmark role, symmetry, and branching;
- fog/reveal rules;
- property/stress testing across large seed samples;
- route economy reports.

### M8 - Gacha and Meta Progression

- complete gacha/progression decision and schema lock before resolver work;
- versioned profile-save migration and profile/banner RNG continuation;
- banners, pool display, pull animation;
- direct pull grants and one-pull-per-pull consumption;
- SSR/SR/R result display model;
- pity/guarantee and audit history;
- duplicate ladder;
- profile EXP/levels;
- profile-level roster cap;
- three-tier character EXP items;
- Domain Sigil Ascension gates;
- inventory and reward item flow.

### M8B - Trials

- Trial decision/schema lock and production definitions before selection UI;
- Trial selection screen;
- Trial difficulty/level tiers;
- Flesh, Abyss, Spirit, Void, EXP, and Money Trial definitions;
- targeted reward tables;
- Trial clear history;
- versioned Trial save migration and reward persistence.

### M8X - Equipment, Profile Shop, and Inventory Expansion

- character rarity lock and weapon rarity schema;
- versioned profile/run save migration and named profile equipment RNG state;
- character weapon definitions, signature weapons, duplicate bonus +0 through +5, leveling, ascension, and banner acquisition;
- six-slot gear sets with 3-piece and 6-piece bonuses, main stat enhancement to +12, substat rerolls, and desynthesis;
- Profile Shop for targeted character dupes, weapons, gear, upgrade items, and reroll materials;
- inventory category, lock/favorite, auto-discard, and desynthesis flows;
- equipment snapshotting for Runs and Trials;
- profile-money upgrade costs and conversion-currency sinks.

**Exit:** player can pull or acquire weapons, equip weapon and gear loadouts, upgrade equipment with persistent resources, reroll/desynth gear, purchase targeted Profile Shop items, and start a deterministic Run or Trial from a stable equipment snapshot.

### M9 - Production Combat UI/UX Closure

M9 closes and polishes combat systems already proven in M1/M2; it does not introduce the first real card hand, actor separation, drag system, or presentation queue.

- final combat information hierarchy and responsive battlefield constraints;
- production-quality card fan/hover/drag feel, animation tuning, and click/keyboard parity;
- formula, target, and incoming-damage previews;
- one/two/three/four-plus/boss enemy formation readability and target focus mapping;
- pile viewers and Event Log filters;
- presentation-sequence speed/skip/reduced-motion closure;
- keyboard/accessibility labels and required assistive-navigation support;
- Play Mode/built-player E2E across required aspect ratios.

### M10 - Art, Audio, VFX, and Presentation Closure

Art generation is permitted before M10 and generated art may already be production quality. M10 is the **closure/audit** milestone for presentation breadth, asset readiness, performance, and consistency rather than the first point at which real art is allowed.

- complete character portraits and gameplay-scale combat sprites built from concrete material, anatomy, costume, pose, silhouette, and horror-reveal briefs;
- complete enemy assets with size class, stage footprint, target bounds, anchor metadata, and validated formation readability;
- card, map, connector, landmark, Shop, Symptom, Curse, keepsake, and environment assets mapped through validated logical presentation IDs;
- production idle/act/hit/defeat/telegraph animation coverage using transform animation, Animator, 2D Animation, or other approved presentation techniques as appropriate to each asset;
- Domain VFX families through URP 2D, Particle System, Shader Graph, and optional VFX Graph;
- music/SFX pipeline;
- performance/memory/loading budgets;
- provenance, review, and release-readiness status;
- no readability regressions under full or reduced effects.

### M11 - Balance, Reliability, and Release Gate

- headless balance reports and human playtests;
- save migration/corruption testing;
- end-to-end seeded playthroughs;
- accessibility audit;
- Unity Profiler/Player performance and memory profiling;
- architecture/platform checkpoint for packaging;
- content lock and versioned release candidate.

## 20. Design Decision Register Seeds

These decisions are tracked in `plans/design-decisions.md` and must be approved before their dependent implementation work begins:

1. **DD-01 Combat terminal timing:** Approved Atomic Stop; terminal state is checked after each atomic terminal-capable effect, and remaining non-terminal sub-effects are skipped once victory or defeat is reached.
2. **DD-02 Domain tuning lock:** Approved. Section 3 launch Domain identities, resource names, UI strings, reset/persistence rules, and edge-case invariants are approved for first implementation; values marked tuning remain adjustable.
3. **DD-03 Launch character tuning lock:** Approved. All eight section 4 launch kits are implementation anchors; M2 implements the starter four first, M5 implements the remaining four, and numeric tuning remains allowed unless marked invariant.
4. **DD-04 Save checkpoints:** Approved Option 2. Expose map/node boundaries and stable fully resolved combat-action boundaries; exclude mid-resolution, mid-animation, and interaction-state recovery.
5. **DD-05 Gacha rates and pity:** Exact rates, soft pity, hard pity, featured guarantees, and rounding.
6. **DD-06 First-acquisition protection:** How does the game prevent early duplicate frustration?
7. **DD-07 Duplicate ladder:** What do duplicates grant, and what compensation exists after completion?
8. **DD-08 Content intensity settings:** What content warnings and intensity controls are required for release?
9. **DD-09 Starter roster onboarding:** Approved Option 2. Use a short sequence with one focused teaching beat per launch Domain, ending in a complete starter-party combat.
10. **DD-10 Reward economy:** How many direct pulls, EXP items, Sigils, and persistent currency rewards are earned per mode, difficulty, and milestone?
11. **DD-11 Profile roster cap table:** What profile levels unlock which maximum character levels?
12. **DD-12 Trial difficulty and reward table:** What boss levels, reward quantities, and first-clear/repeat rewards apply to each Trial?
13. **DD-13 Content format policy:** Approved. YAML is canonical for hand-authored content by default; JSON is canonical for generated or machine-written content by default; each content family has one canonical source format at a time.
14. **DD-14 Release platform lock:** Windows is the primary development/validation target. Which additional Unity platforms, if any, belong to the first public release?
15. **DD-15 Rarity and banner result model:** What exact SSR/SR/R rates, result-family splits, 10-pull guarantee rules, and banner pool structures apply?
16. **DD-16 Stats and equipment scaling:** What stat keys, scaler formulas, stacking order, caps, and equipment snapshot rules are approved?
17. **DD-17 Weapon progression:** What weapon level caps, EXP/ascension costs, signature restrictions, and +1 through +5 duplicate bonuses apply?
18. **DD-18 Gear set and stat system:** What six slots, main stats, substats, set bonuses, reroll rules, and desynthesis yields apply?
19. **DD-19 Profile Shop and conversion economy:** What stock, targeted dupe policy, prices, refresh rules, conversion currency rules, and profile-money sinks apply?
20. **DD-20 Economy and item naming:** What are the final display names for direct pulls, profile money, conversion currency, reroll currency, weapon EXP tiers, weapon ascension materials, and gear terminology?
21. **DD-21 Bloom identity and refraction premise:** What Bloom premise, Domain refraction rules, and art/writing constraints guide release-quality content?
22. **DD-22 Advanced character mechanics and selfish costs:** Approved. Reserve generic extension points for post-launch run-persistent self-debt, Domain engine replacement, per-hit reactions, party-level stances, and Domain-resource reaction/amplifier systems; launch M2 must not implement those mechanics.
23. **DD-23 Advanced card memory, copy, and hidden-zone selection:** Approved. Reserve card-copy/card-memory guardrails for owner-preserving temporary copies, safe hand-copy eligibility, copy lifetime, hidden-zone reveal/selection, and copy-triggered weapon hooks; launch M2 must not implement those mechanics.
24. **DD-24 Collapsing Node lifecycle:** Approved. Collapsing Nodes are player-facing node-scoped travel nodes that collapse only after accepted departure; the canonical textual safe-path fixture contract is authoritative for behavior, while the image remains a separate required visual comparison when available.
25. **DD-25 Node-primary Labyrinth topology:** Approved. Nodes own authored consequences and lifecycle state; edges own connectivity/reachability.
26. **DD-26 Combat enemy placement and target readability:** Approved. Targetable enemies occupy the right/right-center stage with readable formation, target bounds, anchors, and focus mapping without changing authoritative enemy slot order.
27. **DD-27 Unity runtime/presentation architecture:** Approved: Unity 6.5 Supported line (`6000.5.x`), C#, pure no-Unity engine assembly, URP 2D, uGUI runtime UI, Input System, independent actor views, and experimental CLI/Pipeline as development-only automation.
28. **DD-28 Card hand and play-threshold interaction:** Approved: bottom-centred fan, hover rise, upward drag, responsive Play Area threshold, disarm on return below threshold, release-to-cast for target-complete cards, explicit target-selection state for single-target cards, and first-class click/keyboard cancellation paths.
29. **DD-29 Generated art policy:** Approved: AI-generated art is permitted at every milestone and may be release-quality after human review; generation method is provenance, not placeholder status.
31. **DD-31 Windows performance acceptance baseline:** Before M10E, approve the Windows hardware class, display resolution, frame-time target, memory budget, representative scenarios, and acceptance tolerances. No numerical baseline is currently selected.

---

## 21. Glossary

- **Abyss:** Domain that builds Tentacles and Potency.
- **Advanced character mechanics:** post-launch character systems that alter base Domain engines, create run-persistent self-costs, add reaction loops beyond the launch character contract, or react to Domain-resource activity with party-scoped amplifiers.
- **Ascension item:** Domain-specific profile item required at character level breakpoints.
- **Banner:** A gacha pool with defined rates, featured characters, pity, and guarantee state.
- **Bloom:** generative cosmic pressure that refracts bodies, beliefs, souls, environments, and hidden transformations without being a fifth Domain.
- **Bloomdrawn:** a being substantially altered, summoned, empowered, or compelled by the Bloom.
- **Bloom result:** Top gacha result category; unlocks a character or grants duplicate progress for an owned character.
- **Card memory/copy:** future advanced-card mechanic family for owner-preserving temporary copies, stored card snapshots, and hidden-zone selection; future-gated by approved DD-23.
- **Control Resistance:** converts Stun/Delay into Falter instead of nullifying the card.
- **Content Registry:** validated runtime index of all authored content definitions used by the engine and UI.
- **Curse:** primarily harmful run-scoped card.
- **Delay:** control status that postpones one enemy action and preserves its intent.
- **Direct pull:** persistent profile entitlement consumed one-for-one to perform a banner pull; not an intermediary currency.
- **Dormant:** Bloom lifecycle stage in which influence is present but easily mistaken for ordinary beauty.
- **Domain:** One of the four launch mechanical and aesthetic families: Flesh, Abyss, Spirit, or Void.
- **Drown:** persistent Abyss damage stack that deals `2 x stacks` at enemy end.
- **Duplicate Tier:** progression gained from acquiring an already owned character.
- **Equipment snapshot:** versioned copy of equipped weapons, gear, stat contributions, and set bonuses captured when a Run or Trial starts.
- **Embryo:** pooled Flesh build/spend resource.
- **Ethical Gacha:** a collection system using earned pulls, transparent odds, and no monetisation.
- **Essence:** pooled Spirit multiplier resource.
- **Falter:** reduced-output conversion applied to control-resistant enemies.
- **Flowering:** Bloom lifecycle stage in which the host openly expresses Domain-shaped transformation.
- **Collapsing Node:** travel-only Labyrinth node that is safe to enter, collapses after accepted movement away from it, and then remains visible but impassable.
- **Fruiting:** Bloom lifecycle stage in which the host or place becomes a reproductive structure, environmental hazard, elite organism, or source of spread.
- **Gear:** persistent six-slot character equipment with set bonuses, main stats, substats, rerolls, and desynthesis.
- **Gear reroll currency:** profile item spent to reroll gear substats; final name pending.
- **Graveyard:** combat exhaust pile that does not reshuffle.
- **Intent:** a preview of an enemy's planned action.
- **Keepsake:** run-scoped relic/boon-style item.
- **Mana Debt:** reduction applied after the next ordinary Mana refill.
- **Main Spine:** guaranteed Start-to-Boss route.
- **Money Trial:** launch Trial family that rewards persistent general currency; final name pending.
- **Motif:** pre-authored axial-coordinate subgraph stitched into the Labyrinth.
- **Node:** Labyrinth place that owns player-facing content, consequences, lifecycle state, previews, and revisit/spent state.
- **Obols:** run-scoped Shop currency.
- **Owner:** character instance whose stats, gauge, and identity a card uses.
- **Profile level roster cap:** account-level gate that sets maximum character level; not a 1:1 level mapping.
- **Profile Shop:** persistent account shop that spends profile currencies and conversion currency, never Obols.
- **Potency:** damage dealt by each Tentacle hit.
- **Rarity:** player-facing item tier `SSR`, `SR`, or `R`.
- **Refraction:** the way the Bloom expresses through an existing Domain, desire, belief, body, memory, or hidden transformation rather than replacing it.
- **Retain:** card remains in hand during cleanup.
- **Ritual:** Retain card that becomes cheaper while held and usually scales with Essence.
- **Rooted:** Bloom lifecycle stage in which growth has integrated into the host's systems and serves a function.
- **Run:** one roguelike attempt from party selection to victory, defeat, or abandon.
- **Schema:** validation contract for a content family, save shape, or persistent entity.
- **Seeded:** Bloom lifecycle stage in which the host has begun changing while mostly retaining original form and behaviour.
- **Shared Deck:** combat deck assembled from party-owned cards and run acquisitions.
- **Side Loop:** optional two-gate branch attached to the main spine.
- **Symptom:** one-time-node card with meaningful positive and negative effects.
- **Tentacle:** persistent Abyss attack unit; automatic ordinary end-phase volley.
- **Trial:** direct boss challenge mode with selectable difficulty and targeted persistent rewards.
- **Transcend:** once-per-combat owner card that enters Graveyard and upgrades the owner for that combat.
- **Traversal edge:** topology connection between adjacent map hexes; edges define reachability and blocked walls, not run rewards, costs, Shops, Symptoms, Events, or other authored consequences.
- **Weapon:** persistent character equipment item with rarity, level, ascension, duplicate bonus, and optional signature link.
- **Weapon ascension material:** profile item required at weapon level breakpoints; final names pending.
- **Weapon EXP item:** profile item used to level weapons; final tier names pending.
- **Wrongness:** the intentional sense that Bloomdrawn's calm surface is concealing something impossible or harmful.

---

## 22. Open Questions

1. What are the exact soft-pity increments between pull 60 and pull 90?
2. What exact first-acquisition protection prevents early duplicate frustration after the four-character starter party?
3. What are the final display names for direct pulls, EXP item tiers, and persistent general currency?
4. What profile-level bands define the roster level cap?
5. What are the exact Trial difficulty tiers, boss scaling rules, and first-clear/repeat reward tables?
6. How dark can event outcomes become while preserving the friendly pastel wrapper?
7. How much permanent character power should exist outside runs before difficulty tiers become distorted?
8. Which non-starter characters enter the pool immediately versus through run/story unlocks after launch?
9. How long should it take an engaged player to complete the launch roster?
10. What exact UI language distinguishes earned gacha excitement from monetised gacha pressure?
11. What content warning taxonomy and intensity settings are required for release?
12. What should the first vertical slice prove emotionally, not just mechanically?
13. Approved: hand-authored content uses YAML by default, generated or machine-written content uses JSON by default, and each content family has one canonical source format at a time.
14. What Unity platform set should the first public release target beyond the Windows development/validation baseline (Windows-only, additional desktop platforms, Web, or later mobile)?
15. What exact SSR/SR/R rate table, result-family split, and 10-pull guarantee precedence should banners use?
16. What are the final player stat keys, equipment scalers, stacking order, and caps?
17. What weapon level caps, signature weapon restrictions, ascension materials, and duplicate +1 through +5 bonuses apply?
18. What are the six gear slot names, main stat pools, substat pools, tier weights, set bonuses, reroll costs, and desynthesis yields?
19. What are the final names for profile money, conversion currency, gear reroll currency, weapon EXP tiers, weapon ascension materials, and direct pulls?
20. What Profile Shop stock, targeted dupe limits, refresh cadence, and prices keep duplicate compensation useful without becoming a pull conversion loop?
21. How much of the Bloom's origin and intention can be revealed without weakening its mystery?
22. Approved DD-22 future gate: generic limits, UI treatment, save migration rules, and content schema contracts must be defined before implementing advanced post-launch characters with selfish costs, Domain-transforming mechanics, or Domain-resource reaction/amplifier systems.
23. Approved DD-23 future gate: copy eligibility, lifetime, hidden-information, and recursion rules must be honoured before implementing future card-memory characters like Moirenne, Noema, or safe hand-copy support characters.
24. Approved DD-24 map gate: M3 uses player-facing, node-scoped Collapsing Nodes that collapse only after accepted departure; traversal edges remain connectivity-only and do not carry collapse state.
25. Approved DD-25 map gate: Labyrinth implementation is node-primary and edge-supported; nodes own consequences and edges own connectivity.
26. Approved DD-26 combat presentation gate: targetable enemies occupy the right/right-center stage with readable formation, target bounds, anchors, and focus mapping; enemy layout does not redefine authoritative slot order.
27. Approved DD-27 Unity architecture gate: Unity 6.5 Supported line (`6000.5.x`), C#, pure engine assembly, URP 2D, uGUI runtime UI, Input System, independent actor presentation, and development-only CLI/Pipeline automation.
28. Approved DD-28 card interaction gate: the bottom-centred hand fans from stable authoritative order; upward drag crosses a responsive Play Area threshold; releasing armed target-complete cards casts, while explicit-target cards enter target selection; lowering/cancelling returns the card without mutation.
29. Approved DD-29 generated-art gate: AI-generated art is permitted throughout production and can be release-quality after human review; it is not automatically placeholder content.
30. What Windows hardware class, display resolution, frame-time target, memory budget, representative scenarios, and acceptance tolerances define M10E performance acceptance?

---

## 23. Design Lock Checklist Before Major Coding

Before implementing beyond foundation work:

- `docs/DESIGN.md` has been reviewed after this initial draft.
- Decision register exists and links back to this document.
- Combat phase timing is approved.
- Launch Domain combat contracts are represented in task plans and tests.
- Launch character stats, cards, passives, generated cards, and visual notes are approved as implementation anchors or explicitly marked for prototype tuning.
- Starter party tutorial sequence is approved.
- Gacha ethical contract remains unchanged.
- Exact gacha rates/pity are either deferred from implementation or approved.
- SSR/SR/R rarity, result-family pools, and 10-pull guarantee rules are approved before banner resolver expansion.
- Direct pulls are implemented without an intermediary currency conversion.
- EXP item tiers, Domain Sigils, and profile-level roster caps are represented in task plans and tests.
- Trial reward tables and difficulty scaling are approved before Trial implementation.
- Weapon progression, gear stats, Profile Shop stock, conversion currency, and profile-money sinks are approved before M8X implementation.
- Equipment snapshot rules are represented in task plans and deterministic tests before equipment affects Runs or Trials.
- Bloom identity and Domain refraction guidance are approved before release-quality enemy, Symptom, Labyrinth art, major event, and character-writing content locks.
- Node-primary Labyrinth ownership rules are approved before M3 map schemas, traversal, node resolution, map validation, save serialization, and map UI work.
- Collapsing Node and safe-path topology rules are approved before M3 Labyrinth movement, map validation, save serialization, and map UI work.
- Enemy placement and target-readability rules are approved before production enemy content metadata, final combat layout, target interaction, and enemy asset validation.
- Unity runtime architecture and assembly boundaries are mirrored in the repository before M1 gameplay work.
- The M1 combat slice owns independent actor views, bottom-centred fan layout, drag Play Area threshold, target-selection presentation state, and initial event-to-presentation sequencing; M9/M10 may polish but may not replace these with unrelated architecture without a plan revision.
- Generated art is permitted at every milestone; placeholder/release-readiness is determined by review status rather than generation method.
- Advanced character mechanics and selfish-cost rules are approved as a future gate before post-launch characters alter Domain engines, add run-persistent self-debt, react to Domain-resource activity with party-scoped amplifiers, or require signature weapons that react to resource activity, Ultimate casts, or stance/aspect entry.
- Advanced card-memory/copy rules are approved as a future gate before post-launch characters copy cards, store card snapshots, reveal hidden Draw cards for selection, or require copy-triggered signature weapon hooks.
- Save checkpoint scope is approved before save UI work.
- Windows performance acceptance baseline is approved before M10E validates performance.
- Content warning and accessibility requirements are represented in task plans.
- Tests are planned for all stated invariants.
- Authored production content is schema-driven and loaded through the content registry.
- Unity version, packages, Assembly Definition boundaries, CLI/Pipeline development workflow, runtime UI choice, and persistence interfaces are mirrored in repository setup and task plans.
