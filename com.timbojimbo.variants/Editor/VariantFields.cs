using System;
using System.Collections.Generic;
using System.Linq;
using TimboJimbo.Variants;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TimboJimboEditor.Variants
{
    /// <summary>Draws a variant's value with the field its property's type calls for: a colour picker, an enum popup, an object field.</summary>
    internal static class VariantFields
    {
        public static void Draw(GUIContent label, SerializedProperty value, VariantAccessor accessor, Object target)
        {
            var vector = value.FindPropertyRelative("_vector");
            var integer = value.FindPropertyRelative("_integer");
            var text = value.FindPropertyRelative("_string");
            var reference = value.FindPropertyRelative("_object");
            var type = accessor.ValueType;
            var v = vector.vector4Value;

            EditorGUI.BeginChangeCheck();
            switch (accessor.Kind)
            {
                case VariantValueKind.Bool:
                {
                    bool on = EditorGUILayout.Toggle(label, integer.longValue != 0);
                    if (EditorGUI.EndChangeCheck()) integer.longValue = on ? 1 : 0;
                    return;
                }
                case VariantValueKind.Int:
                {
                    long chosen;
                    if (type.IsEnum)
                    {
                        var current = (Enum)Enum.ToObject(type, integer.longValue);
                        var picked = type.IsDefined(typeof(FlagsAttribute), false)
                            ? EditorGUILayout.EnumFlagsField(label, current)
                            : EditorGUILayout.EnumPopup(label, current);
                        chosen = Convert.ToInt64(picked);
                    }
                    else
                    {
                        chosen = EditorGUILayout.LongField(label, integer.longValue);
                    }
                    if (EditorGUI.EndChangeCheck()) integer.longValue = chosen;
                    return;
                }
                case VariantValueKind.Float:
                {
                    float chosen = EditorGUILayout.FloatField(label, v.x);
                    if (EditorGUI.EndChangeCheck()) vector.vector4Value = new Vector4(chosen, 0f, 0f, 0f);
                    return;
                }
                case VariantValueKind.Color:
                {
                    Color chosen = EditorGUILayout.ColorField(label, v);
                    if (EditorGUI.EndChangeCheck()) vector.vector4Value = chosen;
                    return;
                }
                case VariantValueKind.Vector2:
                {
                    Vector2 chosen = EditorGUILayout.Vector2Field(label, v);
                    if (EditorGUI.EndChangeCheck()) vector.vector4Value = chosen;
                    return;
                }
                case VariantValueKind.Vector3:
                {
                    Vector3 chosen = EditorGUILayout.Vector3Field(label, v);
                    if (EditorGUI.EndChangeCheck()) vector.vector4Value = chosen;
                    return;
                }
                case VariantValueKind.Vector4:
                {
                    var chosen = EditorGUILayout.Vector4Field(label, v);
                    if (EditorGUI.EndChangeCheck()) vector.vector4Value = chosen;
                    return;
                }
                case VariantValueKind.Rect:
                {
                    var chosen = EditorGUILayout.RectField(label, new Rect(v.x, v.y, v.z, v.w));
                    if (EditorGUI.EndChangeCheck()) vector.vector4Value = new Vector4(chosen.x, chosen.y, chosen.width, chosen.height);
                    return;
                }
                case VariantValueKind.Quaternion:
                {
                    var euler = EditorGUILayout.Vector3Field(label, new Quaternion(v.x, v.y, v.z, v.w).eulerAngles);
                    if (EditorGUI.EndChangeCheck())
                    {
                        var rotation = Quaternion.Euler(euler);
                        vector.vector4Value = new Vector4(rotation.x, rotation.y, rotation.z, rotation.w);
                    }
                    return;
                }
                case VariantValueKind.Vector2Int:
                {
                    var chosen = EditorGUILayout.Vector2IntField(label, new Vector2Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y)));
                    if (EditorGUI.EndChangeCheck()) vector.vector4Value = new Vector4(chosen.x, chosen.y, 0f, 0f);
                    return;
                }
                case VariantValueKind.Vector3Int:
                {
                    var chosen = EditorGUILayout.Vector3IntField(label, new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z)));
                    if (EditorGUI.EndChangeCheck()) vector.vector4Value = new Vector4(chosen.x, chosen.y, chosen.z, 0f);
                    return;
                }
                case VariantValueKind.String:
                {
                    string chosen = accessor is SelectionAccessor selection && target is VariantSet inner
                        ? SelectionPopup(label, text.stringValue, inner, selection.Group)
                        : EditorGUILayout.TextField(label, text.stringValue);
                    if (EditorGUI.EndChangeCheck()) text.stringValue = chosen;
                    return;
                }
                case VariantValueKind.Object:
                {
                    var chosen = EditorGUILayout.ObjectField(label, reference.objectReferenceValue, type, true);
                    if (EditorGUI.EndChangeCheck()) reference.objectReferenceValue = chosen;
                    return;
                }
                default:
                    EditorGUI.EndChangeCheck();
                    return;
            }
        }

        // Which of another set's variants a variant selects there: Default or one of its names.
        private static string SelectionPopup(GUIContent label, string current, VariantSet inner, string groupName)
        {
            int g = inner.IndexOfGroup(groupName);
            var options = new List<string> { "Default" };
            if (g >= 0) options.AddRange(inner.Groups[g].Variants.Select(v => v.Name));

            int index = string.IsNullOrEmpty(current) ? 0 : options.IndexOf(current);
            if (index < 0)
            {
                options.Add($"{current} (missing)");
                index = options.Count - 1;
            }
            int chosen = EditorGUILayout.Popup(label, index, options.ToArray());
            if (chosen == index) return current;
            return chosen == 0 ? "" : options[chosen];
        }
    }
}
