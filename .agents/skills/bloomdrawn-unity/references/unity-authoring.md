# Unity Authoring Conventions

## Assemblies

Respect the Assembly Definition boundaries created by M0.

The authoritative engine should be capable of compiling without runtime presentation dependencies. Do not solve circular references by collapsing assembly boundaries.

Keep Editor tooling in Editor-only assemblies/folders and player/runtime code free of `UnityEditor` references.

## Scenes and prefabs

Treat scenes/prefabs as presentation/composition assets, not as hidden gameplay databases.

Prefer generic prefab roles such as:

- party actor view;
- enemy actor view;
- card view;
- intent/status anchor;
- combat HUD region;
- VFX anchor/binding.

Authored content chooses presentation assets through logical presentation references/catalogs approved by the project. Gameplay rules should not be encoded only in prefab component values.

When modifying scenes/prefabs through automation, validate:

- object/component existence;
- references are assigned;
- no missing scripts;
- no accidental duplicate EventSystems/input handlers/cameras/canvases;
- prefab overrides are intentional;
- the scene still loads in Play Mode.

## UI

Use the project's approved runtime UI system consistently. For the combat hand, HUD, and target overlays, keep Canvas/scale strategy explicit and testable.

For pointer-to-RectTransform work:

- know which Canvas owns the object;
- know its render mode;
- use the correct event camera when required;
- convert screen coordinates to local coordinates through Unity's supported RectTransform utility path;
- preserve visual screen position when reparenting into/out of drag layers;
- rebuild final resting layout from data instead of preserving temporary drag geometry.

## Input

UI gestures are presentation state until a complete gameplay command is submitted.

Avoid input paths where both a UI callback and a world/actor callback can submit the same command. One interaction controller should own a card drag/target session at a time.

Keyboard/click alternatives must converge on the same interaction state machine rather than maintain separate gameplay logic.

## Animation and VFX

Animation/VFX respond to ordered presentation tokens/events.

Do not:

- apply damage from an animation event;
- wait for an Animator state before deciding an authoritative result;
- use particle collision or frame timing to determine gameplay;
- reorder simultaneous game events because one animation is longer.

The presentation queue may serialize visuals for readability while preserving authoritative event order.

## Assets and generated art

Generated art is allowed and can be final.

Import/review concerns are technical, not provenance-based:

- correct transparency;
- Sprite import mode/mesh/pivot appropriate to use;
- readable scale;
- compression/filtering appropriate to the visual target;
- no accidental huge textures for tiny UI roles;
- atlasing/addressable strategy only when the active milestone owns it;
- independent party/enemy actors remain separable assets when individual acting/targeting is required.
