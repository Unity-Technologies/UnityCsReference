// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

// ReSharper disable PossibleInvalidOperationException -- we are accessing bool?
// values in this file many times, but ensuring they are set before.

// ReSharper disable once CheckNamespace - we explicitly want UnityEditor namespace
namespace UnityEditor
{
    internal static class Clipboard
    {
        static ClipboardState m_State = new ClipboardState();

        public static bool hasString
        {
            get
            {
                FetchState();
                return !string.IsNullOrEmpty(m_State.m_RawContents);
            }
        }

        public static string stringValue
        {
            get
            {
                FetchState();
                return m_State.m_RawContents;
            }
            set => EditorGUIUtility.systemCopyBuffer = value;
        }

        public static bool hasLayerMask
        {
            get
            {
                FetchState();
                m_State.FetchLayerMask();
                return m_State.m_HasLayerMask.Value;
            }
        }

        public static LayerMask layerMaskValue
        {
            get
            {
                FetchState();
                m_State.FetchLayerMask();
                return m_State.m_ValueLayerMask;
            }
            set => EditorGUIUtility.systemCopyBuffer = $"LayerMask({value.value})";
        }

        public static bool hasBool
        {
            get
            {
                FetchState();
                m_State.FetchBool();
                return m_State.m_HasBool.Value;
            }
        }

        public static bool boolValue
        {
            get
            {
                FetchState();
                m_State.FetchBool();
                return m_State.m_ValueBool;
            }
            set => EditorGUIUtility.systemCopyBuffer = value.ToString();
        }


        public static bool hasGuid
        {
            get
            {
                FetchState();
                m_State.FetchGuid();
                return m_State.m_HasGuid.Value;
            }
        }

        public static GUID guidValue
        {
            get
            {
                FetchState();
                m_State.FetchGuid();
                return m_State.m_ValueGuid;
            }
            set => EditorGUIUtility.systemCopyBuffer = value.ToString();
        }

        public static bool hasVector3
        {
            get
            {
                FetchState();
                m_State.FetchVector3();
                return m_State.m_HasVector3.Value;
            }
        }

        public static Vector3 vector3Value
        {
            get
            {
                FetchState();
                m_State.FetchVector3();
                return m_State.m_ValueVector3;
            }
            set => EditorGUIUtility.systemCopyBuffer = ClipboardParser.WriteVector3(value);
        }

        public static bool hasVector2
        {
            get
            {
                FetchState();
                m_State.FetchVector2();
                return m_State.m_HasVector2.Value;
            }
        }

        public static Vector2 vector2Value
        {
            get
            {
                FetchState();
                m_State.FetchVector2();
                return m_State.m_ValueVector2;
            }
            set => EditorGUIUtility.systemCopyBuffer = ClipboardParser.WriteVector2(value);
        }

        public static bool hasVector4
        {
            get
            {
                FetchState();
                m_State.FetchVector4();
                return m_State.m_HasVector4.Value;
            }
        }

        public static Vector4 vector4Value
        {
            get
            {
                FetchState();
                m_State.FetchVector4();
                return m_State.m_ValueVector4;
            }
            set => EditorGUIUtility.systemCopyBuffer = ClipboardParser.WriteVector4(value);
        }

        public static bool hasQuaternion
        {
            get
            {
                FetchState();
                m_State.FetchQuaternion();
                return m_State.m_HasQuaternion.Value;
            }
        }

        public static Quaternion quaternionValue
        {
            get
            {
                FetchState();
                m_State.FetchQuaternion();
                return m_State.m_ValueQuaternion;
            }
            set => EditorGUIUtility.systemCopyBuffer = ClipboardParser.WriteQuaternion(value);
        }

        public static bool hasRect
        {
            get
            {
                FetchState();
                m_State.FetchRect();
                return m_State.m_HasRect.Value;
            }
        }

        public static Rect rectValue
        {
            get
            {
                FetchState();
                m_State.FetchRect();
                return m_State.m_ValueRect;
            }
            set => EditorGUIUtility.systemCopyBuffer = ClipboardParser.WriteRect(value);
        }

        public static bool hasBounds
        {
            get
            {
                FetchState();
                m_State.FetchBounds();
                return m_State.m_HasBounds.Value;
            }
        }

        public static Bounds boundsValue
        {
            get
            {
                FetchState();
                m_State.FetchBounds();
                return m_State.m_ValueBounds;
            }
            set => EditorGUIUtility.systemCopyBuffer = ClipboardParser.WriteBounds(value);
        }

        public static bool hasColor
        {
            get
            {
                FetchState();
                m_State.FetchColor();
                return m_State.m_HasColor.Value;
            }
        }

        public static Color colorValue
        {
            get
            {
                FetchState();
                m_State.FetchColor();
                return m_State.m_ValueColor;
            }
            set => EditorGUIUtility.systemCopyBuffer = ClipboardParser.WriteColor(value);
        }

        public static bool hasObject
        {
            get
            {
                FetchState();
                m_State.FetchObject();
                return m_State.m_HasObject.Value;
            }
        }

        public static Object objectValue
        {
            get
            {
                FetchState();
                m_State.FetchObject();
                return m_State.m_ValueObject;
            }
            set => EditorGUIUtility.systemCopyBuffer = ClipboardParser.WriteCustom(new ObjectWrapper(value));
        }

        public static bool hasGradient
        {
            get
            {
                FetchState();
                m_State.FetchGradient();
                return m_State.m_HasGradient.Value;
            }
        }

        public static Gradient gradientValue
        {
            get
            {
                FetchState();
                m_State.FetchGradient();
                return m_State.m_ValueGradient;
            }
            set => EditorGUIUtility.systemCopyBuffer = ClipboardParser.WriteCustom(new GradientWrapper(value));
        }

        public static bool hasAnimationCurve
        {
            get
            {
                FetchState();
                m_State.FetchAnimationCurve();
                return m_State.m_HasAnimationCurve.Value;
            }
        }

        public static AnimationCurve animationCurveValue
        {
            get
            {
                FetchState();
                m_State.FetchAnimationCurve();
                return m_State.m_ValueAnimationCurve;
            }
            set => EditorGUIUtility.systemCopyBuffer = ClipboardParser.WriteCustom(new AnimationCurveWrapper(value));
        }

        public static bool HasCustomValue<T>() where T : new()
        {
            FetchState();
            return m_State.FetchCustom<T>(out _);
        }

        public static T GetCustomValue<T>() where T : new()
        {
            FetchState();
            m_State.FetchCustom<T>(out var res);
            return res;
        }

        public static void SetCustomValue<T>(T val) where T : new()
        {
            EditorGUIUtility.systemCopyBuffer = ClipboardParser.WriteCustom<T>(val);
        }

        const string kGenericPrefix = "GenericPropertyJSON:";

        public static bool HasSerializedProperty()
        {
            return stringValue?.StartsWith(kGenericPrefix) ?? false;
        }

        public static void SetSerializedProperty(SerializedProperty src)
        {
            var encoded = ClipboardParser.WriteGenericSerializedProperty(src);
            var json = Json.Serialize(encoded);
            stringValue = kGenericPrefix + json;
        }

        public static void GetSerializedProperty(SerializedProperty dst)
        {
            var clip = stringValue;
            if (!clip.StartsWith(kGenericPrefix))
                return;
            if (!(Json.Deserialize(clip.Substring(kGenericPrefix.Length)) is Dictionary<string, object> jsonObjects))
                return;
            ClipboardParser.ParseGenericSerializedProperty(dst, jsonObjects);
        }

        public static bool HasEnumProperty(SerializedProperty prop)
        {
            var idx = ClipboardParser.ParseEnumPropertyIndex(stringValue, prop);
            return idx >= 0;
        }

        public static void GetEnumProperty(SerializedProperty prop)
        {
            ClipboardParser.ParseEnumProperty(stringValue, prop);
        }

        public static void SetEnumProperty(SerializedProperty prop)
        {
            EditorGUIUtility.systemCopyBuffer = ClipboardParser.WriteEnumProperty(prop);
        }

        static void FetchState()
        {
            var systemClipboard = EditorGUIUtility.systemCopyBuffer;
            if (systemClipboard == m_State.m_RawContents)
                return;
            m_State = new ClipboardState {m_RawContents = systemClipboard};
        }
    }
}
