using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using TimboJimbo.Variants;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TimboJimboEditor.Variants
{
    /// <summary>What an edit to a property is to a variant being recorded.</summary>
    internal enum CaptureKind
    {
        /// <summary>An ordinary edit, made to the scene or prefab itself: nothing a variant sets.</summary>
        Pass,

        /// <summary>A value the variant records.</summary>
        Record,

        /// <summary>
        /// Follows from a recorded value and goes back with it, but is not recorded itself: the euler angles Unity keeps
        /// beside a rotation, TextMesh Pro's second copy of its colour.
        /// </summary>
        Follow,
    }

    /// <summary>
    /// Between serialized properties, as the inspector edits them, and the values variants set: which property an edit
    /// is to (a colour whole, not its red), whether a variant can set it, and how AnimationMode is told about it.
    /// </summary>
    internal static class VariantCapture
    {
        // A variant set's selection in one of its groups, and whether the group inherits, as the inspector's tabs edit them.
        private static readonly Regex s_selectionPath = new(@"^_groups\.Array\.data\[(\d+)\]\._selected$");
        private static readonly Regex s_inheritPath = new(@"^_groups\.Array\.data\[(\d+)\]\._inherit$");

        // Edits that are never a variant's: what an object is called, where it sits, and Unity's own bookkeeping.
        private static readonly HashSet<string> s_passed = new()
        {
            "m_Name", "m_TagString", "m_Layer", "m_StaticEditorFlags", "m_Icon", "m_NavMeshLayer", "m_Script",
            "m_ObjectHideFlags", "m_EditorHideFlags", "m_EditorClassIdentifier", "m_Father", "m_Children", "m_RootOrder",
            "m_ConstrainProportionsScale",
        };

        private static readonly HashSet<string> s_followed = new() { "m_LocalEulerAnglesHint", "m_fontColor32" };

        /// <summary>
        /// What an edit to <paramref name="leafPath"/> of <paramref name="target"/> is to a variant being recorded, and
        /// for one it records, the property it sets.
        /// </summary>
        public static CaptureKind Classify(Object target, string leafPath, out string property)
        {
            property = null;
            if (target is VariantSet set)
                return TrySelection(set, leafPath, out property) ? CaptureKind.Record : CaptureKind.Pass;

            int dot = leafPath.IndexOf('.');
            string root = dot < 0 ? leafPath : leafPath.Substring(0, dot);
            if (s_passed.Contains(root)) return CaptureKind.Pass;
            if (s_followed.Contains(root)) return CaptureKind.Follow;

            using var serialized = new SerializedObject(target);
            property = EntryPath(serialized, leafPath);
            return property != null && VariantAccessor.For(target, property) != null ? CaptureKind.Record : CaptureKind.Pass;
        }

        /// <summary>Whether <paramref name="leafPath"/> is <paramref name="set"/>'s selection in a group, and that group's selection property.</summary>
        public static bool TrySelection(VariantSet set, string leafPath, out string property)
        {
            property = null;
            var match = s_selectionPath.Match(leafPath);
            if (!match.Success) return false;
            int group = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            if (group >= set.Groups.Count) return false;
            property = VariantSet.SelectionPrefix + set.Groups[group].Name;
            return true;
        }

        /// <summary>Whether <paramref name="leafPath"/> is whether one of <paramref name="set"/>'s groups inherits, and which.</summary>
        public static bool TryInherit(VariantSet set, string leafPath, out int group)
        {
            group = -1;
            var match = s_inheritPath.Match(leafPath);
            if (!match.Success) return false;
            group = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            return group < set.Groups.Count;
        }

        /// <summary>
        /// The property a leaf belongs to as a variant sets it: a colour, vector, rect or rotation whole (an edit to its
        /// red is an edit to the colour), anything else the leaf itself. Null when there is no such property, or it is
        /// inside an array.
        /// </summary>
        public static string EntryPath(SerializedObject serialized, string leafPath)
        {
            if (leafPath.Contains(".Array.") || serialized.FindProperty(leafPath) == null) return null;

            string path = leafPath;
            for (int dot = path.LastIndexOf('.'); dot > 0; dot = path.LastIndexOf('.'))
            {
                var parent = serialized.FindProperty(path.Substring(0, dot));
                if (parent == null || !IsWhole(parent.propertyType)) break;
                path = parent.propertyPath;
            }
            return path;
        }

        private static bool IsWhole(SerializedPropertyType type) => type is SerializedPropertyType.Color
            or SerializedPropertyType.Vector2 or SerializedPropertyType.Vector3 or SerializedPropertyType.Vector4
            or SerializedPropertyType.Rect or SerializedPropertyType.Quaternion or SerializedPropertyType.Vector2Int
            or SerializedPropertyType.Vector3Int or SerializedPropertyType.Bounds or SerializedPropertyType.BoundsInt
            or SerializedPropertyType.RectInt;

        /// <summary>
        /// Every leaf of <paramref name="property"/> (a colour's r, g, b and a; a bool, string or reference itself), which
        /// is how AnimationMode keeps and puts back values.
        /// </summary>
        public static void ForEachLeaf(SerializedProperty property, Action<SerializedProperty> action)
        {
            if (!HasLeaves(property))
            {
                action(property);
                return;
            }

            var leaf = property.Copy();
            var end = property.GetEndProperty();
            bool enter = true;
            while (leaf.Next(enter) && !SerializedProperty.EqualContents(leaf, end))
            {
                enter = HasLeaves(leaf);
                if (!enter) action(leaf);
            }
        }

        private static bool HasLeaves(SerializedProperty property) => property.hasChildren
            && property.propertyType != SerializedPropertyType.String
            && property.propertyType != SerializedPropertyType.ObjectReference;

        /// <summary>The leaf's value now, as AnimationMode puts it back.</summary>
        public static PropertyModification Snapshot(Object target, SerializedProperty leaf)
        {
            var modification = new PropertyModification { target = target, propertyPath = leaf.propertyPath, value = "" };
            switch (leaf.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    modification.objectReference = leaf.objectReferenceValue;
                    break;
                case SerializedPropertyType.Float:
                    modification.value = leaf.floatValue.ToString("R", CultureInfo.InvariantCulture);
                    break;
                case SerializedPropertyType.Boolean:
                    modification.value = leaf.boolValue ? "1" : "0";
                    break;
                case SerializedPropertyType.String:
                    modification.value = leaf.stringValue;
                    break;
                default:
                    modification.value = leaf.longValue.ToString(CultureInfo.InvariantCulture);
                    break;
            }
            return modification;
        }

        /// <summary>The curve binding AnimationMode files a leaf under.</summary>
        public static EditorCurveBinding Binding(Object target, SerializedProperty leaf) =>
            leaf.propertyType == SerializedPropertyType.ObjectReference
                ? EditorCurveBinding.PPtrCurve("", target.GetType(), leaf.propertyPath)
                : EditorCurveBinding.FloatCurve("", target.GetType(), leaf.propertyPath);

        /// <summary>How an entry reads in the inspector: the component and the property's own label.</summary>
        public static string Describe(Object target, string property)
        {
            if (target is GameObject)
                return property == "m_IsActive" ? "Active" : property;
            if (property.StartsWith(VariantSet.SelectionPrefix, StringComparison.Ordinal))
                return $"Variant Set · {property.Substring(VariantSet.SelectionPrefix.Length)}";

            using var serialized = new SerializedObject(target);
            var found = serialized.FindProperty(property);
            string label = found != null ? found.displayName : ObjectNames.NicifyVariableName(property);
            int dot = property.LastIndexOf('.');
            if (found != null && dot > 0)
            {
                var parent = serialized.FindProperty(property.Substring(0, dot));
                if (parent != null) label = $"{parent.displayName} {label}";
            }
            return $"{ObjectNames.NicifyVariableName(target.GetType().Name)} · {label}";
        }
    }
}
