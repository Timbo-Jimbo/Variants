# Timbo Jimbo - Variants

Variants that live inside a prefab and switch in place: prefab variants, if one instance could be any of them.

🎛️ **Variant Set**

A component on a prefab's root. Its variants come in groups that are set independently: a toast's **Type** (Success, Warning, Error) and its **Size** (Compact). Each group is on **Default**, the prefab as it is, or one of its variants. Where two groups set the same value, the later one wins.

🧩 **Anything Serialized**

A variant is a set of values on objects under the set: colours, sprites, text, numbers, references, whether an object is active, even a field inside a struct (a button's normal colour). Values are written through each component's own property (`m_Color` through `color`, `_cornerRadii` through `CornerRadii`), so graphics redraw and layouts update as they would from code.

⏺️ **Record by Editing**

Select a variant and press **Record**. Edits to anything under the set, in the inspector or the scene view, go into the variant instead of the prefab. Undo works. Right-click any property to add it to the selected variant, or take it out.

👁️ **Preview Without Saving**

The selected variants show in the scene and in prefab mode through AnimationMode, the way the Animation window previews a clip. Scenes and prefabs keep and save their default values, so switching variants never dirties a prefab or leaves overrides behind. An instance can start on a variant: select it on the instance, and it shows that way in the editor.

🌀 **Animated**

A group marked **Animated** (as new groups are) animates wherever it's switched from: code, a button, a state, a breakpoint. It's SwiftUI's `.animation(value:)`. The layout a switch changes springs, and colours and other numbers move on the spring of the layout node they're drawn in. Text, sprites and active states change at once. To hide something animatedly, set its node's Display rather than its active state. Any switch made inside `LayoutSystem.Animate` animates too, and joins that change.

🖱️ **Interaction States**

**Variant States** selects a variant as the pointer hovers and presses, as keyboard or gamepad navigation focuses, and while it's disabled. It works on any object with a raycast target, so custom hover effects and custom buttons work as well as UGUI controls. One state shows at a time, Disabled first, then Pressed, Focused and Hover, as UIKit's and Selectable's do. Pressed drops when a touch drags out. A Selectable on the same object that isn't interactable disables it, and its own transition is left alone (set it to None).

📐 **Breakpoints**

**Variant Breakpoints** selects a variant by the size its object is drawn at, like CSS container queries and Tailwind's breakpoints, mobile first: Default below every breakpoint. It measures width, height or aspect ratio; orientation is an aspect breakpoint at 1. The editor previews the variant for the size it's drawn at, without saving it.

🪆 **Nesting**

A variant can select a variant of a set inside it: a toast's Error selects Danger on its close button. The outer set wins where both set a value, as an outer prefab's overrides do.

# Usage

```csharp
toast.Set("Type", "Error");   // or toast.Set("Error"): the first group with a variant of that name
toast.Clear("Type");          // back to Default
card.Toggle("Expanded");      // on, or back to Default when it's on already
toast.Changed += set => Debug.Log(set.Get("Type"));
```

# Limits

- A variant sets values; it does not add, remove or move objects.
- Array elements (a list's items, an event's listeners) can't be recorded.
- Runtime writes go through reflection, cached per type and property. With aggressive managed code stripping, a property only a variant uses could be stripped; keep it with a `link.xml`.
- The preview shares AnimationMode with the Animation window and Timeline, and waits while either is previewing.
