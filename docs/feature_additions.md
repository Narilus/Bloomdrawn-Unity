# Bloomdrawn Feature Additions - Rarity, Equipment, Profile Shop, and Inventory

This addendum captures the approved planning direction for rarity, character weapons, gear sets, banner fill, Profile Shop, economy sinks, and inventory expansion.

`docs/DESIGN.md` remains the source of truth for gameplay and product rules. This file is a focused planning companion so future task authors can see the whole feature cluster in one place before splitting implementation into M8X task files.

## Summary

These systems are planned as **M8X - Equipment, Profile Shop, and Inventory Expansion**, after M4 persistence, M8 gacha/meta progression, and M8T Trials are stable.

Default assumptions:

- Player-facing rarity is `SSR > SR > R`.
- All playable characters are SSR.
- All character signature weapons are SSR.
- Non-signature weapons can be SSR, SR, or R.
- Banner results use a hybrid model: rarity plus exact result family.
- Every 10-pull guarantees at least one SR-or-better item.
- Special conversion currency is spent in the Profile Shop and never converts into direct pulls.
- `Gear` is the public term for persistent six-slot equipment; `relic` remains avoided because run-scoped keepsakes/boons already occupy that design space.

## Baseline Rules

### Rarity and Banner Fill

- `Rarity = SSR | SR | R`.
- Every banner result shows rarity and result family, such as `SSR character`, `SSR signature weapon`, `SR weapon`, `R weapon`, or `SR weapon EXP item`.
- Banner pools are schema-validated content data.
- Rates, pity, featured guarantee, result-family splits, 10-pull guarantee precedence, and rounding are blocked on DD-05 and DD-15.
- Optional auto-discard may convert eligible low-rarity banner results into special Profile Shop currency.
- Auto-discard cannot affect new characters, SSR signature weapons, locked/favorited weapons, protected gear, or item categories not explicitly eligible in content data.

### Character Weapons

- Each character may equip one persistent weapon.
- Weapons are acquired primarily from banner pulls, with targeted Profile Shop or Trial paths allowed after decision approval.
- Weapon definitions include rarity, stat contribution, growth, level cap, ascension requirements, duplicate bonus table, acquisition sources, and optional signature character link.
- Weapons level with three tiers of weapon EXP items and profile-scoped money.
- Weapon ascension materials and profile-scoped money are required every 10 weapon levels.
- Duplicate weapons increase weapon bonus from +0 through +5.
- Excess/maxed duplicate weapons convert into approved special currency or materials.
- +0 weapons must remain useful; +5 cannot be required for standard content clearability.
- Signature weapons may react only to approved generic engine events. Future stance/aspect-entry or Domain-transforming signature hooks require DD-22 before they enter production weapon content.
- Future Domain-resource generation/consumption or Ultimate-cast signature hooks require DD-22 before they enter production weapon content.
- Future Pattern, Recollection, Wing, pursuit, generated-copy, or copy-lineage signature hooks require DD-23 before they enter production weapon content.
- Combat parameter changes from weapons, such as maximum hand size, are captured in the Run/Trial equipment snapshot and cannot mutate active state after a profile equipment change.

Draft signature weapon seed names, pending DD-20:

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

### Gear Sets

- Each character has six persistent gear slots.
- Every gear instance belongs to exactly one slot and one gear set.
- Equipping 3 matching set pieces activates the set's 3-piece bonus.
- Equipping 6 matching set pieces activates the set's 6-piece bonus.
- Main stat enhancement goes from +0 through +12.
- Each gear piece has three substat slots.
- Substat rerolls spend gear reroll currency and use the profile equipment RNG stream.
- Gear can be desynthesized into declared reroll currency or materials.
- Locked/favorited gear cannot be desynthesized or auto-converted.
- Gear is gained from targeted Trials and the Profile Shop by default.

### Profile Shop and Economy Sinks

- The Profile Shop is a persistent account shop, separate from Labyrinth Shops.
- Profile Shop purchases never spend Obols.
- Profile Shop stock, prices, refresh rules, purchase limits, and unlock requirements are content-authored.
- The shop may sell targeted character duplicate progress, weapons, gear, character EXP, weapon EXP, Sigils, weapon ascension materials, and gear reroll materials.
- Profile-scoped money pays for character leveling, character ascension, weapon leveling, weapon ascension, gear main stat enhancement, and other approved upgrades.
- Special conversion currency may buy targeted Profile Shop items, but never direct pulls.

### Inventory and Snapshots

- Inventory gains categories for currencies, materials, weapons, gear, and desynthesis flows.
- Inventory UI must support category filtering, details, lock/favorite protection, desynthesis previews, and clear mutation confirmation.
- Starting a Run or Trial snapshots selected characters, levels, duplicates, equipped weapons, equipped gear, calculated stats, set bonuses, and content versions.
- Profile equipment changes never mutate an active Run or Trial already in progress.
- Equipment stat calculation is authoritative engine/profile logic, not UI logic.

### Foundation Fixture Compatibility

- M8X equipment definitions, signature links, banner pools, Profile Shop offers, loadouts, and equipment snapshots may reference only production character definition IDs.
- Character, weapon, and gear fixture IDs use explicit non-production namespaces and are loaded only through isolated test content sources.
- Unit and E2E tests may inject validated fixture weapons or gear without adding them to production catalogs or normal runtime registries.
- Production inventories, gacha audit entries, saves, banner catalogs, Profile Shop stock, loadouts, and Run/Trial snapshots must reject fixture character or equipment IDs.
- M8X fixture grants prove generic equipment behavior; tests must not depend on production character tuning or quietly promote fixture items into shipping content.

## Schema and Save Impact

M8X requires schema/content additions for:

- rarity tables;
- banner result-family tables;
- weapon definitions, growth, ascension, duplicate bonuses, and acquisition sources;
- weapon EXP item tiers;
- weapon ascension materials;
- gear sets, gear slots, main stats, substats, tier weights, reroll tables, and desynthesis tables;
- Profile Shop offers;
- inventory categories;
- equipment snapshot payloads.

M8X is save-affecting:

- M8X must introduce explicit versioned profile and active-run save migrations before equipment commands or snapshots become player-facing.
- `saveSchemaVersion` changes when equipment inventory, loadouts, Profile Shop state, equipment RNG, and equipment snapshots enter saves.
- `contentVersion` changes when weapon, gear, banner, or shop content is added.
- Profile save data must persist owned weapons, owned gear, equipment loadouts, Profile Shop state, conversion currency, reroll currency, and audit entries.
- Gacha audit entries must include rarity, result family, duplicate/overflow handling, and auto-discard conversions.
- Gear rerolls use a named persisted `profile.equipment` RNG stream.
- Migration from the M8T profile must preserve banner, Trial, character progression, inventory, active Run, and all pre-existing RNG state exactly.
- Active Run/Trial equipment snapshots are added only with their versioned save contract; no earlier milestone reserves speculative empty equipment payloads.

## Decision Gates

- DD-05: exact rates, pity, 10-pull guarantee, and banner rounding.
- DD-07: character duplicate overflow and weapon duplicate +5 behaviour.
- DD-10: reward economy for equipment materials and conversion sinks.
- DD-11: profile roster cap and possible equipment cap bands.
- DD-12: Trial reward tables, including equipment-expansion Trial families.
- DD-15: rarity and banner result model.
- DD-16: player stats, scalers, stacking order, caps, and equipment snapshot rules.
- DD-17: weapon progression, signature restrictions, ascension, and duplicate bonuses.
- DD-18: gear slots, main stats, substats, set bonuses, reroll costs, and desynthesis yields.
- DD-19: Profile Shop stock, targeted dupe rules, conversion currency policy, prices, and profile-money sinks.
- DD-20: final economy and item names.
- DD-22: future stance/aspect, Domain-transforming, Domain-resource, Ultimate-cast, or resource-amplifier hooks, if any.
- DD-23: future card-memory, generated-copy, safe hand-copy, Pattern, Recollection, or Wing signature hooks, if any.

## Future Validation

Content validation should prove:

- rarity values are only `SSR`, `SR`, or `R`;
- all playable characters are SSR;
- signature weapons are SSR;
- banner pools satisfy the 10-pull SR-or-better guarantee;
- gear sets define valid 3-piece and 6-piece bonuses;
- gear instances reference valid slots, stat pools, and set IDs;
- Profile Shop offers do not sell direct pulls for conversion currency;
- desynthesis and auto-discard tables cannot affect protected items;
- production equipment, banner, Shop, and snapshot content cannot reference non-production fixture character or equipment IDs.

Engine/profile tests should prove:

- rejected equipment upgrade, reroll, desynthesis, auto-discard, or Profile Shop commands consume no currency, items, or RNG;
- gear rerolls advance only the profile equipment RNG stream;
- maxed duplicate characters and weapons convert transactionally;
- active Run and Trial equipment snapshots remain stable after profile equipment changes;
- weapon-modified combat parameters, including maximum hand size, come only from the active Run/Trial equipment snapshot;
- M8T saves migrate to M8X without losing banner, Trial, character progression, inventory, active Run, or RNG state.

UI/E2E scenarios should cover:

- equipping a weapon;
- equipping six gear pieces;
- activating 3-piece and 6-piece set bonuses;
- running a 10-pull guarantee;
- excluding protected items from auto-discard/desynthesis;
- desynthesizing gear or eligible weapons;
- buying a targeted Profile Shop item;
- granting equipment through isolated validated test content without exposing it in production catalogs.


## Unity Data and Presentation Notes

This addendum inherits the Unity architecture defined by `docs/DESIGN.md` and `plans/implementation_plan.md`.

- Weapon, gear, banner, shop, and equipment rules remain canonical validated content/engine data. ScriptableObjects, prefabs, scene objects, Animator states, or UI components may bind presentation but are not the sole gameplay source of truth.
- Runtime equipment/profile UI submits typed application/engine commands and displays accepted state; it does not calculate authoritative upgrade costs, reroll outcomes, snapshot stats, or conversion results independently.
- Logical presentation asset IDs map weapons, gear sets, currencies, and item families to Unity sprites/VFX/audio through the presentation catalogue.
- AI-generated weapon art, gear icons, item art, banners, backgrounds, and other presentation assets are permitted at every stage and may be final after human review; generation method is provenance rather than placeholder status.
- Equipment presentation work may begin once the relevant M8X schemas/contracts exist. It does not need to wait for M10, which owns final presentation closure, performance, and release-readiness audit.

## Non-Goals

- This addendum does not implement code, schemas, generated files, or content data.
- Exact rates, costs, stat pools, item names, currency names, reward quantities, and shop prices remain open until their decision gates are approved.
- Special conversion currency must not become a direct-pull conversion layer.
