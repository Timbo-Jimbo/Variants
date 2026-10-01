## [Unreleased]

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
