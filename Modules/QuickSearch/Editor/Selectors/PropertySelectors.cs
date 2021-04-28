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
                    if (property.name.EndsWith(propertyPath, StringComparison.OrdinalIgnoreCase))
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
                case SerializedPropertyType.Integer: return p.intValue;
                case SerializedPropertyType.Boolean: return p.boolValue;
                case SerializedPropertyType.Float: return p.floatValue;
                case SerializedPropertyType.String: return p.stringValue;
                case SerializedPropertyType.Enum: return p.enumNames[p.enumValueIndex];
                case SerializedPropertyType.Bounds: return p.boundsValue.size.magnitude;
                case SerializedPropertyType.BoundsInt: return p.boundsIntValue.size.magnitude;
                case SerializedPropertyType.Color: return p.colorValue;
                case SerializedPropertyType.FixedBufferSize: return p.fixedBufferSize;
                //case SerializedPropertyType.ArraySize: return p.arraySize;

                case SerializedPropertyType.Rect: return p.rectValue.ToString();
                case SerializedPropertyType.RectInt: return p.rectIntValue.ToString();

                case SerializedPropertyType.Vector2: return p.vector2Value.ToString();
                case SerializedPropertyType.Vector3: return p.vector3Value.ToString();
                case SerializedPropertyType.Vector4: return p.vector4Value.ToString();
                case SerializedPropertyType.AnimationCurve: return p.animationCurveValue.ToString();
                case SerializedPropertyType.Quaternion: return p.quaternionValue.eulerAngles.ToString();
                case SerializedPropertyType.Vector2Int: return p.vector2IntValue.ToString();
                case SerializedPropertyType.Vector3Int: return p.vector3IntValue.ToString();

                case SerializedPropertyType.ObjectReference: return p.objectReferenceValue;
                case SerializedPropertyType.ExposedReference: return p.exposedReferenceValue;

                case SerializedPropertyType.Gradient: return p.gradientValue.ToString();
                case SerializedPropertyType.LayerMask: return p.layerMaskBits;
                case SerializedPropertyType.Hash128: return p.hash128Value.ToString();

                case SerializedPropertyType.ManagedReference:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.Generic:
                    break;
            }

            if (p.isArray)
                return p.arraySize;

            return null;
        }

        static bool IsPropertyTypeSupported(SerializedProperty p)
        {
            if (p.propertyType == SerializedPropertyType.Generic)
            {
                if (string.Equals(p.type, "map", StringComparison.Ordinal))
                    return false;
                if (string.Equals(p.type, "Matrix4x4f", StringComparison.Ordinal))
                    return false;
            }

            return p.propertyType != SerializedPropertyType.LayerMask &&
                p.propertyType != SerializedPropertyType.Character &&
                p.propertyType != SerializedPropertyType.ArraySize &&
                !p.isArray && !p.isFixedBuffer && p.propertyPath.LastIndexOf('[') == -1;
        }

        static IEnumerable<UnityEngine.Object> GetTemplates(IEnumerable<UnityEngine.Object> objects)
        {
            var seenTypes = new HashSet<Type>();
            foreach (var obj in objects)
            {
                var ct = obj.GetType();
                if (!seenTypes.Contains(ct))
                {
                    seenTypes.Add(ct);
                    yield return obj;
                }

                if (obj is GameObject go)
                {
                    foreach (var comp in go.GetComponents<Component>())
                    {
                        if (!comp)
                            continue;
                        ct = comp.GetType();
                        if (!seenTypes.Contains(ct))
                        {
                            seenTypes.Add(ct);
                            yield return comp;
                        }
                    }
                }

                var path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path))
                {
                    var importer = AssetImporter.GetAtPath(path);
                    if (importer)
                    {
                        var it = importer.GetType();
                        if (it != typeof(AssetImporter) && !seenTypes.Contains(it))
                        {
                            seenTypes.Add(it);
                            yield return importer;
                        }
                    }
                }
            }
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
