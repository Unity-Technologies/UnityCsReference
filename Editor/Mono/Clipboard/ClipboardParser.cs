// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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

        public static bool ParseEnum(string text, out int res)
        {
            res = 0;
            if (string.IsNullOrEmpty(text))
                return false;

            var match = Regex.Match(text, @"^Enum\(([\-0-9]+)\)");
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

        public static bool ParseVector2(string text, out Vector2 res)
        {
            res = Vector2.zero;
            var v = ParseFloats(text, "Vector2", 2);
            if (v == null)
                return false;
            res = new Vector2(v[0], v[1]);
            return true;
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

        public static bool ParseRect(string text, out Rect res)
        {
            res = Rect.zero;
            var v = ParseFloats(text, "Rect", 4);
            if (v == null)
                return false;
            res = new Rect(v[0], v[1], v[2], v[3]);
            return true;
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

        public static bool ParseQuaternion(string text, out Quaternion res)
        {
            res = Quaternion.identity;
            var v = ParseFloats(text, "Quaternion", 4);
            if (v == null)
                return false;
            res = new Quaternion(v[0], v[1], v[2], v[3]);
            return true;
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
