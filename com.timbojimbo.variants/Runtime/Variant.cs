using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TimboJimbo.Variants
{
    /// <summary>One value a variant sets: a property of an object in its variant set's hierarchy, and what it is set to.</summary>
    [Serializable]
    public struct VariantEntry
    {
        [Tooltip("The GameObject or component the value is set on.")]
        [SerializeField] private Object _target;

        [Tooltip("The property set, by its serialized path (m_Color, _cornerRadii, m_IsActive).")]
        [SerializeField] private string _property;

        [SerializeField] private VariantValue _value;

        public VariantEntry(Object target, string property, VariantValue value)
        {
            _target = target;
            _property = property;
            _value = value;
        }

        public Object Target => _target;
        public string Property => _property;
        public VariantValue Value => _value;

        public bool Sets(Object target, string property) => _target == target && _property == property;
    }

    /// <summary>One variant of a group: a name, and the values it sets.</summary>
    [Serializable]
    public sealed class Variant
    {
        [SerializeField] private string _name;
        [SerializeField] private List<VariantEntry> _entries = new();

        public Variant() : this("Variant") { }

        public Variant(string name) => _name = name;

        /// <summary>A variant named <paramref name="name"/> setting <paramref name="entries"/>, for building one in code.</summary>
        public Variant(string name, params VariantEntry[] entries)
        {
            _name = name;
            _entries.AddRange(entries);
        }

        public string Name { get => _name; internal set => _name = value; }
        public IReadOnlyList<VariantEntry> Entries => _entries;
        internal List<VariantEntry> EntryList => _entries;

        /// <summary>The index of the entry setting <paramref name="property"/> of <paramref name="target"/>, or -1.</summary>
        public int IndexOf(Object target, string property)
        {
            for (int i = 0; i < _entries.Count; i++)
                if (_entries[i].Sets(target, property)) return i;
            return -1;
        }
    }

    /// <summary>
    /// Variants that exclude one another (a toast's Type: Success, Warning, Error), and which of them is selected: one
    /// of them by name, or none (empty), which is Default, the prefab as it is.
    /// </summary>
    [Serializable]
    public sealed class VariantGroup
    {
        [SerializeField] private string _name;
        [SerializeField] private string _selected = "";
        [SerializeField] private List<Variant> _variants = new();

        public VariantGroup() : this("Group") { }

        public VariantGroup(string name) => _name = name;

        /// <summary>A group named <paramref name="name"/> of <paramref name="variants"/>, on Default, for building one in code.</summary>
        public VariantGroup(string name, params Variant[] variants)
        {
            _name = name;
            _variants.AddRange(variants);
        }

        public string Name { get => _name; internal set => _name = value; }

        /// <summary>The selected variant's name; empty for Default.</summary>
        public string Selected { get => _selected ?? ""; internal set => _selected = value ?? ""; }

        public IReadOnlyList<Variant> Variants => _variants;
        internal List<Variant> VariantList => _variants;

        /// <summary>The variant named <paramref name="name"/>; null for none, or for Default (empty).</summary>
        public Variant Find(string name)
        {
            int index = IndexOf(name);
            return index < 0 ? null : _variants[index];
        }

        /// <summary>The index of the variant named <paramref name="name"/>; -1 for none, or for Default (empty).</summary>
        public int IndexOf(string name)
        {
            if (string.IsNullOrEmpty(name)) return -1;
            for (int i = 0; i < _variants.Count; i++)
                if (_variants[i].Name == name) return i;
            return -1;
        }
    }
}
