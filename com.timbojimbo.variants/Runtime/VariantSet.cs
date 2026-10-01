using System;
using System.Collections.Generic;
using TimboJimbo.UI.Layout;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TimboJimbo.Variants
{
    /// <summary>
    /// The variants of a prefab, kept inside it. As prefab variants, each is a set of values (colours, sprites, text,
    /// whether an object is active, any serialized field) on objects in its hierarchy, but one instance switches between
    /// them in place rather than being swapped for another prefab. Variants come in groups that are set independently (a
    /// toast's Type: Success, Warning or Error, and its Size: Compact), each on Default (the prefab as it is) or one of its
    /// variants; where two groups set the same value, the later group wins. A variant can select a variant of another set
    /// inside this one, as a toast's Error selects Danger on its button.
    /// In the editor the selection is previewed and never saved: scenes and prefabs keep their default values, and the
    /// inspector records a variant by editing the objects while it is selected. In play mode a variant set applies its
    /// selection when it wakes and whenever <see cref="Set(string, string)"/> changes it. It notes each value's default the
    /// first time it applies, and puts that back when nothing selected sets it. A group that is
    /// <see cref="VariantGroup.Animated"/> (as groups are, unless turned off) switches inside
    /// <see cref="TimboJimbo.UI.Layout.LayoutSystem.Animate(Action, string[])"/> wherever it is switched from, as
    /// SwiftUI's .animation(value:), so what the switch changes moves (see <see cref="VariantMotion"/>); any switch made
    /// inside a change joins it. <see cref="VariantStates"/> and <see cref="VariantBreakpoints"/> switch a group from
    /// the pointer and from the size of what they are on.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Timbo Jimbo/Variants/Variant Set")]
    public sealed class VariantSet : MonoBehaviour
    {
        // How an entry names another variant set's selection in one of its groups, as its property: "group:Type".
        internal const string SelectionPrefix = "group:";

        [SerializeField] private List<VariantGroup> _groups = new();

        // What it has set at runtime, built the first time it applies: a slot for each object and property any variant
        // sets, with its default (read then) and what was last written; and, per group and variant, the slot each entry
        // sets (-1 for one that sets nothing it can reach).
        [NonSerialized] private Slot[] _slots;
        [NonSerialized] private int[][][] _entrySlots;
        [NonSerialized] private VariantValue[] _resolved;

        private struct Slot
        {
            public Object Target;
            public string Property;
            public VariantAccessor Accessor;
            public VariantValue Default;
            public VariantValue Last;
            public bool Selection;

            // Whether it moves on a spring with a change, and the layout node whose spring it moves on.
            public bool Springs;
            public LayoutNode Node;
        }

        /// <summary>Raised once its selection has changed; in play mode, once the values it gives are applied.</summary>
        public event Action<VariantSet> Changed;

        public IReadOnlyList<VariantGroup> Groups => _groups;
        internal List<VariantGroup> GroupList => _groups;

        /// <summary>The variant selected in <paramref name="group"/>: its name, or empty for Default (and for no such group).</summary>
        public string Get(string group)
        {
            int index = IndexOfGroup(group);
            return index < 0 ? "" : _groups[index].Selected;
        }

        /// <summary>Selects <paramref name="variant"/> in <paramref name="group"/> (null or empty for Default).</summary>
        public void Set(string group, string variant) => Select(group, variant, Application.isPlaying);

        /// <summary>
        /// Selects <paramref name="variant"/> in whichever group has a variant of that name (the first that does), as
        /// toast.Set("Error"); a button's click can call it with the name. Warns when no group has one.
        /// </summary>
        public void Set(string variant)
        {
            for (int g = 0; g < _groups.Count; g++)
            {
                if (_groups[g].IndexOf(variant) < 0) continue;
                Select(g, variant, Application.isPlaying);
                return;
            }
            Debug.LogWarning($"{name} has no variant named '{variant}'.", this);
        }

        /// <summary>Puts <paramref name="group"/> back on Default.</summary>
        public void Clear(string group) => Set(group, null);

        /// <summary>
        /// Selects <paramref name="variant"/> in whichever group has it, or puts that group back on Default when it is
        /// selected already: a switch turning on and off, a card opening and closing. Warns when no group has one.
        /// </summary>
        public void Toggle(string variant)
        {
            for (int g = 0; g < _groups.Count; g++)
            {
                if (_groups[g].IndexOf(variant) < 0) continue;
                Select(g, _groups[g].Selected == variant ? null : variant, Application.isPlaying);
                return;
            }
            Debug.LogWarning($"{name} has no variant named '{variant}'.", this);
        }

        /// <summary>Puts every group back on Default, animated if any group it changes is.</summary>
        public void ClearAll()
        {
            bool changed = false, animated = false;
            foreach (var group in _groups)
            {
                if (group.Selected.Length == 0) continue;
                changed = true;
                animated |= group.Animated;
                group.Selected = "";
            }
            if (!changed) return;
            if (Application.isPlaying)
                Switch(animated);
            Notify();
        }

        /// <summary>
        /// The property an entry names to select a variant of another set's <paramref name="group"/>, its value the
        /// variant's name (empty for Default): how a toast's Error selects Danger on the button inside it.
        /// </summary>
        public static string GroupProperty(string group) => SelectionPrefix + group;

        /// <summary>Adds <paramref name="group"/> after its groups, for building a set in code (an editor script making a prefab).</summary>
        public void AddGroup(VariantGroup group)
        {
            _groups.Add(group);
            _slots = null;
        }

        public int IndexOfGroup(string group)
        {
            for (int g = 0; g < _groups.Count; g++)
                if (_groups[g].Name == group) return g;
            return -1;
        }

        internal void Select(string group, string variant, bool apply)
        {
            int index = IndexOfGroup(group);
            if (index < 0)
            {
                Debug.LogWarning($"{name} has no variant group named '{group}'.", this);
                return;
            }
            Select(index, variant, apply);
        }

        internal void Select(int group, string variant, bool apply)
        {
            variant ??= "";
            var target = _groups[group];
            if (variant.Length > 0 && target.IndexOf(variant) < 0)
            {
                Debug.LogWarning($"{name}'s {target.Name} has no variant named '{variant}'.", this);
                return;
            }
            if (target.Selected == variant) return;

            target.Selected = variant;
            if (apply)
                Switch(target.Animated);
            Notify();
        }

        // Applies a change of selection: inside LayoutSystem.Animate for an animated group, so what it changes moves; a
        // switch made inside a change already joins that one, either way.
        private void Switch(bool animated)
        {
            if (animated)
                LayoutSystem.Animate(Apply);
            else
                Apply();
        }

        // Tells the editor's preview, which shows every change outside play mode, then anyone listening.
        private void Notify()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                s_editorChanged?.Invoke(this);
#endif
            Changed?.Invoke(this);
        }

        private void Awake()
        {
            foreach (var group in _groups)
            {
                if (group.Selected.Length == 0) continue;
                Apply();
                return;
            }
        }

        /// <summary>
        /// Writes every value any variant sets as the selection gives it: the selected variant's value from the last group
        /// whose selection sets it, else its default. Only what differs from what it last wrote is written. Other variant
        /// sets' selections go first, so what they set settles before this one's own values land over it.
        /// </summary>
        internal void Apply()
        {
            Build();
            for (int s = 0; s < _slots.Length; s++)
                _resolved[s] = _slots[s].Default;

            for (int g = 0; g < _groups.Count; g++)
            {
                int v = _groups[g].IndexOf(_groups[g].Selected);
                if (v < 0) continue;
                var entries = _groups[g].VariantList[v].EntryList;
                var slots = _entrySlots[g][v];
                for (int e = 0; e < slots.Length; e++)
                    if (slots[e] >= 0) _resolved[slots[e]] = entries[e].Value;
            }

            Write(selections: true);
            Write(selections: false);
        }

        private void Write(bool selections)
        {
            for (int s = 0; s < _slots.Length; s++)
            {
                ref var slot = ref _slots[s];
                if (slot.Selection != selections || slot.Target == null || slot.Last.Equals(_resolved[s])) continue;
                Put(slot, _resolved[s]);
                slot.Last = _resolved[s];
            }
        }

        private static void Put(in Slot slot, VariantValue value)
        {
            if (slot.Springs)
                VariantMotion.Move(slot.Target, slot.Property, slot.Accessor, slot.Node, value);
            else
                slot.Accessor.Write(slot.Target, value);
        }

        // Finds a slot for every object and property its variants set, reading each one's default as it is now.
        private void Build()
        {
            if (_slots != null) return;

            var slots = new List<Slot>();
            var found = new Dictionary<(Object, string), int>();
            _entrySlots = new int[_groups.Count][][];
            for (int g = 0; g < _groups.Count; g++)
            {
                var variants = _groups[g].VariantList;
                _entrySlots[g] = new int[variants.Count][];
                for (int v = 0; v < variants.Count; v++)
                {
                    var entries = variants[v].EntryList;
                    var map = _entrySlots[g][v] = new int[entries.Count];
                    for (int e = 0; e < entries.Count; e++)
                    {
                        map[e] = -1;
                        var entry = entries[e];
                        if (entry.Target == null) continue;

                        var key = (entry.Target, entry.Property);
                        if (!found.TryGetValue(key, out int s))
                        {
                            var accessor = VariantAccessor.For(entry.Target, entry.Property);
                            if (accessor == null || accessor.Kind != entry.Value.Kind) continue;
                            var current = accessor.Read(entry.Target);
                            s = slots.Count;
                            slots.Add(new Slot
                            {
                                Target = entry.Target,
                                Property = entry.Property,
                                Accessor = accessor,
                                Default = current,
                                Last = current,
                                Selection = accessor is SelectionAccessor,
                                Springs = VariantMotion.Springs(entry.Target, accessor.Kind),
                                Node = VariantMotion.NodeOf(entry.Target),
                            });
                            found.Add(key, s);
                        }
                        map[e] = s;
                    }
                }
            }
            _slots = slots.ToArray();
            _resolved = new VariantValue[_slots.Length];
        }

        // Its variants changed while it is applied (edited in the inspector in play mode): put back every default it has
        // changed, forget them, and apply again from what the variants now say.
        private void Rebuild()
        {
            if (_slots == null) return;
            foreach (var slot in _slots)
            {
                if (slot.Target != null && !slot.Last.Equals(slot.Default))
                    Put(slot, slot.Default);
            }
            _slots = null;
            Apply();
        }

#if UNITY_EDITOR
        // The editor's preview listens here, to show a change to its variants or selection outside play mode.
        internal static Action<VariantSet> s_editorChanged;

        private void OnValidate()
        {
            if (Application.isPlaying)
                Rebuild();
            else
                s_editorChanged?.Invoke(this);
        }
#endif
    }
}
