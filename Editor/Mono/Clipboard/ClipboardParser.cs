// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor.Build.Content;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace - we explicitly want UnityEditor namespace
namespace UnityEditor
{
    internal static class ClipboardParser
    {
        public static bool ParseGuid(string text, out GUID res)
        {
            res = new GUID();
            if (string.IsNullOrEmpty(text))
                return false;
            return GUID.TryParse(text, out res);
        }

        public static bool ParseBool(string text, out bool res)
        {
            res = false;
            if (string.IsNullOrEmpty(text))
                return false;
            return bool.TryParse(text, out res);
        }

        public static bool ParseLayerMask(string text, out LayerMask res)
        {
            res = 0;
            if (string.IsNullOrEmpty(text))
                return false;

            var match = Regex.Match(text, @"^LayerMask\(([\-0-9]+)\)");
            if (match.Success && match.Groups.Count > 1)
            {
                if (int.TryParse(match.Groups[1].Value, out var id))
                {
                    res = id;
                    return true;
                }
            }
            return false;
        }

        static float[] ParseFloats(string text, string prefix, int count)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            // build a regex that matches "Prefix(a,b,c,...)" at start of text
            var sb = new StringBuilder();
            sb.Append('^');
            sb.Append(prefix);
            sb.Append("\\(");
            for (var i = 0; i < count; ++i)
            {
                if (i != 0)
                    sb.Append(',');
                sb.Append("([^,]+)");
            }
            sb.Append("\\)");

            var match = Regex.Match(text, sb.ToString());
            if (!match.Success || match.Groups.Count <= count)
                return null;

            var res = new float[count];
            for (var i = 0; i < count; ++i)
            {
                if (float.TryParse(match.Groups[i + 1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
                    res[i] = f;
                else
                    return null;
            }
            return res;
        }

        public static bool ParseVector3(string text, out Vector3 res)
        {
            res = Vector3.zero;
            var v = ParseFloats(text, "Vector3", 3);
            if (v == null)
                return false;
            res = new Vector3(v[0], v[1], v[2]);
            return true;
        }

        public static string WriteVector3(Vector3 value)
        {
            return string.Format(CultureInfo.InvariantCulture, "Vector3({0:g9},{1:g9},{2:g9})", value.x, value.y, value.z);
        }

        public static bool ParseVector2(string text, out Vector2 res)
        {
            res = Vector2.zero;
            var v = ParseFloats(text, "Vector2", 2);
            if (v == null)
                return false;
            res = new Vector2(v[0], v[1]);
            return true;
        }

        public static string WriteVector2(Vector2 value)
        {
            return string.Format(CultureInfo.InvariantCulture, "Vector2({0:g9},{1:g9})", value.x, value.y);
        }

        public static bool ParseVector4(string text, out Vector4 res)
        {
            res = Vector4.zero;
            var v = ParseFloats(text, "Vector4", 4);
            if (v == null)
                return false;
            res = new Vector4(v[0], v[1], v[2], v[3]);
            return true;
        }

        public static string WriteVector4(Vector4 value)
        {
            return string.Format(CultureInfo.InvariantCulture, "Vector4({0:g9},{1:g9},{2:g9},{3:g9})", value.x, value.y, value.z, value.w);
        }

        public static bool ParseRect(string text, out Rect res)
        {
            res = Rect.zero;
            var v = ParseFloats(text, "Rect", 4);
            if (v == null)
                return false;
            res = new Rect(v[0], v[1], v[2], v[3]);
            return true;
        }

        public static string WriteRect(Rect value)
        {
            return string.Format(CultureInfo.InvariantCulture, "Rect({0:g9},{1:g9},{2:g9},{3:g9})", value.x, value.y, value.width, value.height);
        }

        public static bool ParseBounds(string text, out Bounds res)
        {
            res = new Bounds();
            var v = ParseFloats(text, "Bounds", 6);
            if (v == null)
                return false;
            res = new Bounds(new Vector3(v[0], v[1], v[2]), new Vector3(v[3] * 2, v[4] * 2, v[5] * 2));
            return true;
        }

        public static string WriteBounds(Bounds value)
        {
            return string.Format(CultureInfo.InvariantCulture,
                "Bounds({0:g9},{1:g9},{2:g9},{3:g9},{4:g9},{5:g9})",
                value.center.x, value.center.y, value.center.z, value.extents.x, value.extents.y, value.extents.z);
        }

        public static bool ParseQuaternion(string text, out Quaternion res)
        {
            res = Quaternion.identity;
            var v = ParseFloats(text, "Quaternion", 4);
            if (v == null)
                return false;
            res = new Quaternion(v[0], v[1], v[2], v[3]);
            return true;
        }

        public static string WriteQuaternion(Quaternion value)
        {
            return string.Format(CultureInfo.InvariantCulture, "Quaternion({0:g9},{1:g9},{2:g9},{3:g9})", value.x, value.y, value.z, value.w);
        }

        public static bool ParseColor(string text, out Color res)
        {
            res = Color.black;
            if (string.IsNullOrEmpty(text))
                return false;

            if (ColorUtility.TryParseHtmlString(text, out res))
                return true;

            var v = ParseFloats(text, "Color", 4);
            if (v == null)
                return false;
            res = new Color(v[0], v[1], v[2], v[3]);
            return true;
        }

        public static string WriteColor(Color val)
        {
            // check if this is a color that can be represented in LDR fine
            Color32 ldr = val;
            Color hdrFromLdr = ldr;
            if (((Vector4)val - (Vector4)hdrFromLdr).sqrMagnitude < 0.0001f)
            {
                if (ldr.a == 255)
                    return '#' + ColorUtility.ToHtmlStringRGB(val);
                return '#' + ColorUtility.ToHtmlStringRGBA(val);
            }

            return string.Format(CultureInfo.InvariantCulture, "Color({0:g9},{1:g9},{2:g9},{3:g9})", val.r, val.g, val.b, val.a);
        }

        internal const string kEnumPrefix = "Enum:";

        public static int ParseEnumPropertyIndex(string text, SerializedProperty prop)
        {
            if (!text.StartsWith(kEnumPrefix))
                return -1;
            var val = text.Substring(kEnumPrefix.Length);
            if (string.IsNullOrEmpty(val))
                return -1;
            var names = prop.enumDisplayNames;
            var idx = Array.IndexOf(names, val);
            if (idx < 0)
                return -1;
            return idx;
        }

        public static void ParseEnumProperty(string text, SerializedProperty prop)
        {
            var idx = ParseEnumPropertyIndex(text, prop);
            if (idx >= 0)
                prop.enumValueIndex = idx;
        }

        public static string WriteEnumProperty(SerializedProperty prop)
        {
            if (prop.propertyType != SerializedPropertyType.Enum)
                return string.Empty;
            var idx = prop.enumValueIndex;
            var names = prop.enumDisplayNames;
            if (idx < 0 || idx >= names.Length)
                return string.Empty;
            return $"{kEnumPrefix}{names[idx]}";
        }

        public static bool ParseCustom<T>(string text, out T res) where T : new()
        {
            res = new T();
            if (string.IsNullOrEmpty(text))
                return false;
            var prefix = CustomPrefix<T>();
            if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;
            try
            {
                EditorJsonUtility.FromJsonOverwrite(text.Substring(prefix.Length), res);
            }
            catch (ArgumentException)
            {
                return false;
            }
            return true;
        }

        public static string WriteCustom<T>(T val)
        {
            return CustomPrefix<T>() + EditorJsonUtility.ToJson(val);
        }

        static string CustomPrefix<T>()
        {
            return typeof(T).FullName + "JSON:";
        }

        // Given a serialized property, produce an object structure for JSON encoding of it.
        internal static Dictionary<string, object> WriteGenericSerializedProperty(SerializedProperty p)
        {
            var res = new Dictionary<string, object>
            {
                ["name"] = p.name,
                ["type"] = (int)p.propertyType
            };
            switch (p.propertyType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Character:
                    res["val"] = p.intValue; break;
                case SerializedPropertyType.Boolean: res["val"] = p.boolValue; break;
                case SerializedPropertyType.Float: res["val"] = p.floatValue; break;
                case SerializedPropertyType.String: res["val"] = p.stringValue; break;
                case SerializedPropertyType.ObjectReference: res["val"] = WriteCustom(new ObjectWrapper(p.objectReferenceValue)); break;
                case SerializedPropertyType.ArraySize: res["val"] = p.intValue; break;
                case SerializedPropertyType.AnimationCurve: res["val"] = WriteCustom(new AnimationCurveWrapper(p.animationCurveValue)); break;
                case SerializedPropertyType.Enum: res["val"] = WriteEnumProperty(p); break;
                case SerializedPropertyType.Bounds: res["val"] = WriteBounds(p.boundsValue); break;
                case SerializedPropertyType.Gradient: res["val"] = WriteCustom(new GradientWrapper(p.gradientValue)); break;
                case SerializedPropertyType.Quaternion: res["val"] = WriteQuaternion(p.quaternionValue); break;
                case SerializedPropertyType.Vector2Int: res["val"] = WriteVector2(p.vector2IntValue); break;
                case SerializedPropertyType.Vector3Int: res["val"] = WriteVector3(p.vector3IntValue); break;
                case SerializedPropertyType.RectInt:
                    var ri = p.rectIntValue;
                    res["val"] = WriteRect(new Rect(ri.x, ri.y, ri.width, ri.height));
                    break;
                case SerializedPropertyType.BoundsInt:
                    var bi = p.boundsIntValue;
                    res["val"] = WriteBounds(new Bounds(bi.center, bi.size));
                    break;

                // Copy/Paste of these for generic serialized properties is not implemented yet.
                case SerializedPropertyType.ExposedReference: break;
                case SerializedPropertyType.FixedBufferSize: break;
                case SerializedPropertyType.ManagedReference: break;

                default:
                    if (p.isArray)
                    {
                        res["arraySize"] = p.arraySize;
                        res["arrayType"] = p.arrayElementType;
                    }
                    if (p.hasChildren)
                    {
                        var children = new List<object>();
                        SerializedProperty chit = p.Copy();
                        var end = chit.GetEndProperty();
                        chit.Next(true);
                        while (!SerializedProperty.EqualContents(chit, end))
                        {
                            children.Add(WriteGenericSerializedProperty(chit));
                            if (!chit.Next(false))
                                break;
                        }
                        res["children"] = children;
                    }
                    break;
            }
            return res;
        }

        // Given an object structure from JSON encoding, apply that to a serialized property.
        internal static void ParseGenericSerializedProperty(SerializedProperty prop, Dictionary<string, object> obj)
        {
            if (prop == null)
                return;
            if (!obj.TryGetValue("name", out var oName)) return;
            var name = oName as string;
            if (string.IsNullOrEmpty(name)) return;

            if (!obj.TryGetValue("type", out var oType)) return;
            if (!(oType is long)) return;
            try
            {
                var propertyType = (SerializedPropertyType)Convert.ToInt32(oType);
                if (propertyType != prop.propertyType)
                    return;
                obj.TryGetValue("val", out var oval);

                switch (propertyType)
                {
                    case SerializedPropertyType.Integer:
                    case SerializedPropertyType.LayerMask:
                    case SerializedPropertyType.Character:
                        prop.intValue = Convert.ToInt32(oval);
                        break;
                    case SerializedPropertyType.Boolean:
                        prop.boolValue = Convert.ToBoolean(oval);
                        break;
                    case SerializedPropertyType.Float:
                        prop.floatValue = Convert.ToSingle(oval);
                        break;
                    case SerializedPropertyType.String:
                        prop.stringValue = Convert.ToString(oval);
                        break;
                    case SerializedPropertyType.ObjectReference:
                        if (ParseCustom<ObjectWrapper>(Convert.ToString(oval), out var objectWrapper))
                            prop.objectReferenceValue = objectWrapper.ToObject();
                        break;
                    case SerializedPropertyType.ArraySize:
                        prop.arraySize = Convert.ToInt32(oval);
                        break;
                    case SerializedPropertyType.AnimationCurve:
                        if (ParseCustom<AnimationCurveWrapper>(Convert.ToString(oval), out var animWrapper))
                            prop.animationCurveValue = animWrapper.curve;
                        break;
                    case SerializedPropertyType.Enum:
                        ParseEnumProperty(Convert.ToString(oval), prop);
                        break;
                    case SerializedPropertyType.Bounds:
                        if (ParseBounds(Convert.ToString(oval), out var boundsValue))
                            prop.boundsValue = boundsValue;
                        break;
                    case SerializedPropertyType.Gradient:
                        if (ParseCustom<GradientWrapper>(Convert.ToString(oval), out var gradientWrapper))
                        {
                            prop.gradientValue = gradientWrapper.gradient;
                            UnityEditorInternal.GradientPreviewCache.ClearCache();
                        }
                        break;
                    case SerializedPropertyType.Quaternion:
                        if (ParseQuaternion(Convert.ToString(oval), out var quaternionValue))
                            prop.quaternionValue = quaternionValue;
                        break;
                    case SerializedPropertyType.Vector2Int:
                        if (ParseVector2(Convert.ToString(oval), out var v2Value))
                            prop.vector2IntValue = new Vector2Int((int)v2Value.x, (int)v2Value.y);
                        break;
                    case SerializedPropertyType.Vector3Int:
                        if (ParseVector3(Convert.ToString(oval), out var v3Value))
                            prop.vector3IntValue = new Vector3Int((int)v3Value.x, (int)v3Value.y, (int)v3Value.z);
                        break;
                    case SerializedPropertyType.RectInt:
                        if (ParseRect(Convert.ToString(oval), out var rectValue))
                            prop.rectIntValue = new RectInt((int)rectValue.x, (int)rectValue.y, (int)rectValue.width, (int)rectValue.height);
                        break;
                    case SerializedPropertyType.BoundsInt:
                        if (ParseBounds(Convert.ToString(oval), out var biValue))
                            prop.boundsIntValue = new BoundsInt(
                                new Vector3Int((int)biValue.center.x, (int)biValue.center.y, (int)biValue.center.z),
                                new Vector3Int((int)biValue.size.x, (int)biValue.size.y, (int)biValue.size.z));
                        break;

                    // Copy/Paste of these for generic serialized properties is not implemented yet.
                    case SerializedPropertyType.FixedBufferSize: break;
                    case SerializedPropertyType.ExposedReference: break;
                    case SerializedPropertyType.ManagedReference: break;

                    default:
                        if (prop.isArray)
                        {
                            if (!obj.TryGetValue("arraySize", out var oArraySize))
                                return;
                            if (!obj.TryGetValue("arrayType", out var oArrayType))
                                return;
                            if (Convert.ToString(oArrayType) != prop.arrayElementType)
                                return;
                            prop.arraySize = Convert.ToInt32(oArraySize);
                        }

                        if (prop.hasChildren)
                        {
                            if (!obj.TryGetValue("children", out var oChildren))
                                return;
                            if (!(oChildren is List<object> children))
                                return;

                            SerializedProperty chit = prop.Copy();
                            var end = chit.GetEndProperty();
                            chit.Next(true);
                            var index = 0;
                            while (!SerializedProperty.EqualContents(chit, end) && index < children.Count)
                            {
                                if (!(children[index] is Dictionary<string, object> ch))
                                    return;
                                ParseGenericSerializedProperty(chit, ch);
                                if (!chit.Next(false))
                                    break;
                                ++index;
                            }
                        }

                        break;
                }
            }
            catch (InvalidCastException)
            {
            }
        }
    }

    [Serializable]
    internal class ObjectWrapper
    {
        public ObjectWrapper() {}

        public ObjectWrapper(Object o)
        {
            if (o == null)
                return;
            instanceID = o.GetInstanceID();
            if (!ObjectIdentifier.TryGetObjectIdentifier(o, out var id))
                return;
            guid = id.guid.ToString();
            localId = id.localIdentifierInFile;
            type = id.fileType;
        }

        public Object ToObject()
        {
            var id = new ObjectIdentifier
            {
                guid = new GUID(guid),
                localIdentifierInFile = localId,
                fileType = type
            };
            if (!id.guid.Empty())
            {
                var o = ObjectIdentifier.ToObject(id);
                if (o != null)
                    return o;
            }
            return EditorUtility.InstanceIDToObject(instanceID);
        }

        // guid/localId/type are for asset references; these are used if present
        public string guid = "";
        public long localId = 0;
        public FileType type = FileType.NonAssetType;
        // instanceIDs are for in-scene references, not stable across sessions but fine for local copy/paste
        public int instanceID = 0;
    }

    [Serializable]
    internal class GradientWrapper
    {
        public GradientWrapper() { gradient = new Gradient(); }
        public GradientWrapper(Gradient g) { gradient = g; }
        public Gradient gradient;
    }

    [Serializable]
    internal class AnimationCurveWrapper
    {
        public AnimationCurveWrapper() { curve = new AnimationCurve(); }
        public AnimationCurveWrapper(AnimationCurve g) { curve = g; }
        public AnimationCurve curve;
    }
}
