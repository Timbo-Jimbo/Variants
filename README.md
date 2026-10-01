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

Switch inside `LayoutSystem.Animate(() => toast.Set("Error"))` and the change animates: the layout it changes springs, and colours and other numbers move on the spring of the layout node they're drawn in. Text, sprites and active states change at once. To hide something animatedly, set its node's Display rather than its active state.

🪆 **Nesting**

A variant can select a variant of a set inside it: a toast's Error selects Danger on its close button. The outer set wins where both set a value, as an outer prefab's overrides do.

# Usage

```csharp
toast.Set("Type", "Error");   // or toast.Set("Error"): the first group with a variant of that name
toast.Clear("Type");          // back to Default
toast.Changed += set => Debug.Log(set.Get("Type"));
```

# Limits

- A variant sets values; it does not add, remove or move objects.
- Array elements (a list's items, an event's listeners) can't be recorded.
- Runtime writes go through reflection, cached per type and property. With aggressive managed code stripping, a property only a variant uses could be stripped; keep it with a `link.xml`.
- The preview shares AnimationMode with the Animation window and Timeline, and waits while either is previewing.
