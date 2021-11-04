// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Search
{
    static class PropertySelectors
    {
        internal static object GetSerializedPropertyValue(SearchItem item, string propertyPath)
        {
            object value = null;
            var property = GetSerializedProperty(item, propertyPath, out var so);
            if (property != null)
                value = GetSerializedPropertyValue(property);

            property?.Dispose();
            so?.Dispose();

            return value;
        }

        internal static SerializedProperty GetSerializedProperty(SearchItem item, string propertyPath, out SerializedObject so)
        {
            var go = item.ToObject<GameObject>();
            if (go)
            {
                foreach (var c in go.GetComponents<Component>())
                {
                    var v = FindProperty(c, propertyPath, out so);
                    if (v != null)
                        return v;
                }
            }

            return FindProperty(item.ToObject<AssetImporter>(), propertyPath, out so) ??
                FindProperty(item.ToObject(), propertyPath, out so);
        }

        public static SerializedProperty FindProperty(UnityEngine.Object obj, string propertyPath, out SerializedObject so)
        {
            if (!obj)
            {
                so = null;
                return null;
            }

            so = new SerializedObject(obj);
            var property = so.FindProperty(propertyPath);
            if (property != null)
                return property;

            {

                property = so.FindProperty($"m_{propertyPath}");
                if (property != null)
                {
                    return property;
                }

                property = so.GetIterator();
                var next = property.NextVisible(true);
                while (next)
                {
                    if (property.name.EndsWith(propertyPath, StringComparison.OrdinalIgnoreCase) ||
                        (property.name.Contains(" ") && property.name.Replace(" ", "").EndsWith(propertyPath, StringComparison.OrdinalIgnoreCase)))
                    {
                        return property;
                    }
                    next = property.NextVisible(property.hasChildren);
                }

                so?.Dispose();
                so = null;
                return null;
            }
        }

        internal static object GetSerializedPropertyValue(SerializedProperty p)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.Character:
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.Integer:
                    return p.intValue;

                case SerializedPropertyType.Boolean: return p.boolValue;
                case SerializedPropertyType.Float: return p.floatValue;
                case SerializedPropertyType.String: return p.stringValue;
                case SerializedPropertyType.Enum: return p.enumNames[p.enumValueIndex];
                case SerializedPropertyType.Bounds: return p.boundsValue.size.magnitude;
                case SerializedPropertyType.BoundsInt: return p.boundsIntValue.size.magnitude;
                case SerializedPropertyType.Color: return p.colorValue;
                case SerializedPropertyType.FixedBufferSize: return p.fixedBufferSize;

                case SerializedPropertyType.Rect: return p.rectValue.ToString();
                case SerializedPropertyType.RectInt: return p.rectIntValue.ToString();

                case SerializedPropertyType.Vector2: return Utils.ToString(p.vector2Value, 2);
                case SerializedPropertyType.Vector3: return Utils.ToString(p.vector3Value, 3);
                case SerializedPropertyType.Vector4: return Utils.ToString(p.vector4Value, 4);
                case SerializedPropertyType.Vector2Int: return Utils.ToString(p.vector2IntValue);
                case SerializedPropertyType.Vector3Int: return Utils.ToString(p.vector3IntValue);

                case SerializedPropertyType.AnimationCurve: return p.animationCurveValue.ToString();
                case SerializedPropertyType.Quaternion: return p.quaternionValue.eulerAngles.ToString();

                case SerializedPropertyType.ObjectReference: return p.objectReferenceValue;
                case SerializedPropertyType.ExposedReference: return p.exposedReferenceValue;

                case SerializedPropertyType.Gradient: return p.gradientValue.ToString();
                case SerializedPropertyType.LayerMask: return p.layerMaskBits;
                case SerializedPropertyType.Hash128: return p.hash128Value.ToString();

                case SerializedPropertyType.ManagedReference:
                case SerializedPropertyType.Generic:
                    break;
            }

            if (p.isArray)
                return p.arraySize;

            return null;
        }

        static void DrawObjectField(Rect rect, UnityEngine.Object obj)
        {
            var mouseInRect = rect.Contains(Event.current.mousePosition);
            if (Event.current.type == EventType.Repaint)
            {
                var temp = EditorGUIUtility.ObjectContent(obj, obj.GetType());
                Styles.readOnlyObjectField.Draw(rect, temp, -1, false, mouseInRect);
            }
            else if (Event.current.type == EventType.MouseDown && mouseInRect)
            {
                Utils.SelectObject(obj, ping: true);
                Event.current.Use();
            }
        }

        internal static object DrawObjectReference(Rect rect, object value)
        {
            if (value is UnityEngine.Object obj)
            {
                DrawObjectField(rect, obj);
            }
            else if (value is string s && GlobalObjectId.TryParse(s, out var gid))
            {
                obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
                if (obj)
                    DrawObjectField(rect, obj);
            }

            return value?.ToString();
        }
    }
}
