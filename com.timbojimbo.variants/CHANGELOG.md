## [Unreleased]

### Added

- `VariantGroup.Animated`, on for new groups and an Animated toggle on each group in the inspector: switching the group animates wherever it is switched from, as SwiftUI's `.animation(value:)` (the switch is made inside `MotionSystem.Animate`, and one made inside a change joins it)
- `VariantGroup.Animation`: what an animated switch is made on, as SwiftUI's `.animation(_:value:)` (a hover's quick spring); Inherit (null) for the default. A layout node the switch moves with an Animation of its own keeps it. In the inspector under the group's header while it is Animated
- `VariantSet.Toggle(variant)`: on, or back to Default when it is on already; with `Set` and `Clear`, a button wires to it in the inspector
- `VariantStates`: selects a variant of a group as the pointer hovers and presses, as navigation focuses, and while disabled, on any object with a raycast target. One state at a time, Disabled, Pressed, Focused then Hover; Pressed drops when a touch drags out, as UIKit's highlight does; Disabled is its own `Interactable` off or a Selectable on it that is not interactable. Inspector with the set's own groups and variants to pick from
- `VariantBreakpoints`: selects a variant of a group by the size its object is drawn at (width, height or aspect ratio), mobile first, as container queries and Tailwind's breakpoints. The editor previews the variant for the size it is drawn at, without saving it, and the set's inspector says what selected it. Inspector with rows of breakpoints and a readout of what it measures now

- Inherit: a group on Inherit (`VariantGroup.Inherits`) shows what the nearest set above it with a group of the same name shows, as SwiftUI's environment passes down; a name it has no variant of shows Default and still passes down. An explicit selection (Default or a variant) overrides it for that set and those under it. A switch passes down inside the same change; a set made or moved under another in play mode shows what it inherits there. The editor previews it, and the inspector has an Inherit tab first and says where an inherited variant comes from. A group that `VariantStates` or `VariantBreakpoints` drives selects for itself, and passes its state down

### Changed

- New groups start on Inherit, made in the inspector or in code; groups saved before keep their selection. `Get` returns what a group shows, inherited or its own; `Clear`, `ClearAll` and a `Toggle` turned off put groups back on Inherit, which shows Default with nothing above
- Motion comes from its own package, `com.timbojimbo.motion` (`MotionSystem.Animate`, `MotionSystem.AnimateValue`), a new dependency. A value moves on the animation of the layout node it is drawn in for the change (`LayoutSystem.AnimationOf`): the node's own, the nearest above it, or the change's

## [0.1.0]

The first version, a working mockup.

### Added

- `VariantSet`: the variants of a prefab, kept inside it and switched in place. Variants come in groups set independently (a toast's Type: Success, Warning, Error; its Size: Compact), each on Default or one of its variants, a later group winning where two set the same value. A variant sets any serialized value of anything under the set: colours, sprites, text, numbers, references, whether an object is active, a field inside a struct. A variant can select a variant of a set inside it
- Runtime: `Set(group, variant)`, `Set(variant)`, `Get`, `Clear`, `ClearAll` and `Changed`. A set applies its selection when it wakes and on each change, writing through each property's own setter (so a graphic redraws) and only what differs; it notes each value's default the first time, and puts that back when nothing selected sets it
- Editor preview: every set in the open scenes, or the prefab open in prefab mode, is shown as its selection says through AnimationMode, as the Animation window previews a clip. Scenes and prefabs keep and save their defaults; previewed fields are tinted in the inspector
- Recording: with a variant selected, Record turns edits to anything under its set (in the inspector, the scene view, a tab of a set inside it) into the variant's values, with undo. An edit to a value a variant is showing, outside recording, is turned away with a note naming the variant
- Inspector for `VariantSet`: a row of tabs per group, the selected variant's values by object, each editable or removable; groups and variants added, renamed, duplicated, reordered and removed. In play mode the tabs switch variants as code would
- Right-clicking any property under a set adds its value to, or takes it out of, the variant selected there
- Motion: switched inside `LayoutSystem.Animate`, a set's values move with the change. The layout they change springs as any change's does; colours, radii and other numbers drawn as they are move on the spring of the layout node they are drawn in (`LayoutSystem.AnimateValue`), colours in OkLab in linear light; text, sprites and active states go at once. Hide something by its node's Display, not its active state, for it to animate out
