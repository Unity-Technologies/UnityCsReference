// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Bindings
{
    internal static class SerializedPropertyHelper
    {
        /// Property getters
        public static int GetIntPropertyValue(SerializedProperty p)
        {
            return p.intValue;
        }

        public static uint GetUIntPropertyValue(SerializedProperty p)
        {
            return p.uintValue;
        }

        public static object GetManagedReferenceValue(SerializedProperty p)
        {
            return p.managedReferenceValue;
        }

        public static long GetLongPropertyValue(SerializedProperty p)
        {
            return p.longValue;
        }

        public static ulong GetULongPropertyValue(SerializedProperty p)
        {
            return p.ulongValue;
        }

        public static bool GetBoolPropertyValue(SerializedProperty p)
        {
            return p.boolValue;
        }

        public static float GetFloatPropertyValue(SerializedProperty p)
        {
            return p.floatValue;
        }

        public static double GetDoublePropertyValue(SerializedProperty p)
        {
            return p.doubleValue;
        }

        public static string GetStringPropertyValue(SerializedProperty p)
        {
            return p.stringValue;
        }

        public static Color GetColorPropertyValue(SerializedProperty p)
        {
            return p.colorValue;
        }

        public static T GetObjectRefPropertyValue<T>(SerializedProperty p) where T: UnityEngine.Object
        {
            return p.objectReferenceValue as T;
        }

        public static int GetLayerMaskPropertyValue(SerializedProperty p)
        {
            return p.intValue;
        }

        public static uint GetRenderingLayerMaskPropertyValue(SerializedProperty p)
        {
            return p.uintValue;
        }


        public static Vector2 GetVector2PropertyValue(SerializedProperty p)
        {
            return p.vector2Value;
        }

        public static Vector3 GetVector3PropertyValue(SerializedProperty p)
        {
            return p.vector3Value;
        }

        public static Vector4 GetVector4PropertyValue(SerializedProperty p)
        {
            return p.vector4Value;
        }

        public static Vector2Int GetVector2IntPropertyValue(SerializedProperty p)
        {
            return p.vector2IntValue;
        }

        public static Vector3Int GetVector3IntPropertyValue(SerializedProperty p)
        {
            return p.vector3IntValue;
        }

        public static Rect GetRectPropertyValue(SerializedProperty p)
        {
            return p.rectValue;
        }

        public static RectInt GetRectIntPropertyValue(SerializedProperty p)
        {
            return p.rectIntValue;
        }

        public static AnimationCurve GetAnimationCurvePropertyValue(SerializedProperty p)
        {
            return p.animationCurveValue;
        }

        public static Bounds GetBoundsPropertyValue(SerializedProperty p)
        {
            return p.boundsValue;
        }

        public static BoundsInt GetBoundsIntPropertyValue(SerializedProperty p)
        {
            return p.boundsIntValue;
        }

        public static Hash128 GetHash128PropertyValue(SerializedProperty p)
        {
            return p.hash128Value;
        }

        public static Gradient GetGradientPropertyValue(SerializedProperty p)
        {
            return p.gradientValue;
        }

        public static Quaternion GetQuaternionPropertyValue(SerializedProperty p)
        {
            return p.quaternionValue;
        }

        public static char GetCharacterPropertyValue(SerializedProperty p)
        {
            return (char)p.intValue;
        }

        // Basic conversions
        public static float GetDoublePropertyValueAsFloat(SerializedProperty p)
        {
            return (float)p.doubleValue;
        }

        public static double GetFloatPropertyValueAsDouble(SerializedProperty p)
        {
            return (double)p.floatValue;
        }

        public static string GetCharacterPropertyValueAsString(SerializedProperty p)
        {
            return new string((char)p.intValue, 1);
        }

        public static float GetIntPropertyValueAsFloat(SerializedProperty p)
        {
            return p.intValue;
        }

        public static float GetLongPropertyValueAsFloat(SerializedProperty p)
        {
            return p.longValue;
        }

        public static ToggleButtonGroupState GetToggleStatePropertyValue(SerializedProperty p)
        {
            return (ToggleButtonGroupState)p.structValue;
        }

        //this one is a bit more tricky
        public static string GetEnumPropertyValueAsString(SerializedProperty p)
        {
            return p.enumDisplayNames[p.enumValueIndex];
        }

        /// Property setters
        public static void SetIntPropertyValue(SerializedProperty p, int v)
        {
            p.intValue = v;
        }

        public static void SetUIntPropertyValue(SerializedProperty p, uint v)
        {
            p.uintValue = v;
        }

        public static void SetLongPropertyValue(SerializedProperty p, long v)
        {
            p.longValue = v;
        }

        public static void SetULongPropertyValue(SerializedProperty p, ulong v)
        {
            p.ulongValue = v;
        }

        public static void SetBoolPropertyValue(SerializedProperty p, bool v)
        {
            p.boolValue = v;
        }

        public static void SetFloatPropertyValue(SerializedProperty p, float v)
        {
            p.floatValue = v;
        }

        public static void SetDoublePropertyValue(SerializedProperty p, double v)
        {
            p.doubleValue = v;
        }

        public static void SetStringPropertyValue(SerializedProperty p, string v)
        {
            p.stringValue = v;
        }

        public static void SetColorPropertyValue(SerializedProperty p, Color v)
        {
            p.colorValue = v;
        }

        public static void SetObjectRefPropertyValue<T>(SerializedProperty p, T v) where T : UnityEngine.Object
        {
            p.objectReferenceValue = v;
        }

        public static void SetLayerMaskPropertyValue(SerializedProperty p, int v)
        {
            p.intValue = v;
        }

        public static void SetRenderingLayerMaskPropertyValue(SerializedProperty p, uint v)
        {
            p.uintValue = v;
        }

        public static void SetVector2PropertyValue(SerializedProperty p, Vector2 v)
        {
            // only set the value changed
            if (p.hasMultipleDifferentValues)
            {
                if (!p.vector2Value.x.Equals(v.x))
                    p.FindPropertyRelative("x").doubleValue = v.x;
                if (!p.vector2Value.y.Equals(v.y))
                    p.FindPropertyRelative("y").doubleValue = v.y;
            }
            else
                p.vector2Value = v;
        }

        public static void SetVector3PropertyValue(SerializedProperty p, Vector3 v)
        {
            // only set the value changed
            if (p.hasMultipleDifferentValues)
            {
                if (!p.vector3Value.x.Equals(v.x))
                    p.FindPropertyRelative("x").doubleValue = v.x;
                if (!p.vector3Value.y.Equals(v.y))
                    p.FindPropertyRelative("y").doubleValue = v.y;
                if (!p.vector3Value.z.Equals(v.z))
                    p.FindPropertyRelative("z").doubleValue = v.z;
            }
            else
                p.vector3Value = v;
        }

        public static void SetVector4PropertyValue(SerializedProperty p, Vector4 v)
        {
            // only set the value changed
            if (p.hasMultipleDifferentValues)
            {
                if (!p.vector4Value.x.Equals(v.x))
                    p.FindPropertyRelative("x").doubleValue = v.x;
                if (!p.vector4Value.y.Equals(v.y))
                    p.FindPropertyRelative("y").doubleValue = v.y;
                if (!p.vector4Value.z.Equals(v.z))
                    p.FindPropertyRelative("z").doubleValue = v.z;
                if (!p.vector4Value.z.Equals(v.w))
                    p.FindPropertyRelative("w").doubleValue = v.w;
            }
            else
                p.vector4Value = v;
        }

        public static void SetVector2IntPropertyValue(SerializedProperty p, Vector2Int v)
        {
            // only set the value changed
            if (p.hasMultipleDifferentValues)
            {
                if (!p.vector2IntValue.x.Equals(v.x))
                    p.FindPropertyRelative("x").intValue = v.x;
                if (!p.vector2IntValue.y.Equals(v.y))
                    p.FindPropertyRelative("y").intValue = v.y;
            }
            else
                p.vector2IntValue = v;
        }

        public static void SetVector3IntPropertyValue(SerializedProperty p, Vector3Int v)
        {
            // only set the value changed
            if (p.hasMultipleDifferentValues)
            {
                if (!p.vector3IntValue.x.Equals(v.x))
                    p.FindPropertyRelative("x").intValue = v.x;
                if (!p.vector3IntValue.y.Equals(v.y))
                    p.FindPropertyRelative("y").intValue = v.y;
                if (!p.vector3IntValue.z.Equals(v.z))
                    p.FindPropertyRelative("z").intValue = v.z;
            }
            else
                p.vector3IntValue = v;
        }

        public static void SetRectPropertyValue(SerializedProperty p, Rect v)
        {
            p.rectValue = v;
        }

        public static void SetRectIntPropertyValue(SerializedProperty p, RectInt v)
        {
            p.rectIntValue = v;
        }

        public static void SetAnimationCurvePropertyValue(SerializedProperty p, AnimationCurve v)
        {
            p.animationCurveValue = v;
        }

        public static void SetBoundsPropertyValue(SerializedProperty p, Bounds v)
        {
            p.boundsValue = v;
        }

        public static void SetBoundsIntPropertyValue(SerializedProperty p, BoundsInt v)
        {
            p.boundsIntValue = v;
        }

        public static void SetHash128PropertyValue(SerializedProperty p, Hash128 v)
        {
            p.hash128Value = v;
        }

        public static void SetGradientPropertyValue(SerializedProperty p, Gradient v)
        {
            p.gradientValue = v;
        }

        public static void SetQuaternionPropertyValue(SerializedProperty p, Quaternion v)
        {
            p.quaternionValue = v;
        }

        public static void SetCharacterPropertyValue(SerializedProperty p, char v)
        {
            p.intValue = v;
        }

        // Conversions
        public static void SetDoublePropertyValueFromFloat(SerializedProperty p, float v)
        {
            p.doubleValue = v;
        }

        public static void SetFloatPropertyValueFromDouble(SerializedProperty p, double v)
        {
            p.floatValue = (float)v;
        }

        public static void SetCharacterPropertyValueFromString(SerializedProperty p, string v)
        {
            if (v.Length > 0)
            {
                p.intValue = v[0];
            }
        }

        public static void SetToggleStatePropertyValue(SerializedProperty p, ToggleButtonGroupState v)
        {
            p.structValue = v;
        }

        //this one is a bit more tricky
        public static void SetEnumPropertyValueFromString(SerializedProperty p, string v)
        {
            p.enumValueIndex = FindStringIndex(p.enumDisplayNames, v);
        }

        // A No Linq implementation to avoid allocations
        public static int FindStringIndex(string[] values, string v)
        {
            for (var i = 0; i < values.Length; ++i)
            {
                if (values[i] == v)
                    return i;
            }

            return -1;
        }

        // Equality comparers
        public static bool ValueEquals<TValue>(TValue value, SerializedProperty p,
            Func<SerializedProperty, TValue> propertyReadFunc)
        {
            var propVal = propertyReadFunc(p);
            return EqualityComparer<TValue>.Default.Equals(value, propVal);
        }

        public static bool ValueEquals(string value, SerializedProperty p,
            Func<SerializedProperty, string> propertyReadFunc)
        {
            switch(p.propertyType)
            {
                case SerializedPropertyType.Enum:
                    return p.enumDisplayNames[p.enumValueIndex].Equals(value, StringComparison.OrdinalIgnoreCase);
                case SerializedPropertyType.Character:
                    return !String.IsNullOrEmpty(value) &&
                           value.Length == 1 &&
                           GetCharacterPropertyValue(p) == value[0];
                default:
                    // Will always return false if propertyType is not String
                    return p.ValueEquals(value);
            }
        }

        public static bool ValueEquals(AnimationCurve value, SerializedProperty p,
            Func<SerializedProperty, AnimationCurve> propertyReadFunc)
        {
            return p.ValueEquals(value);
        }

        public static bool ValueEquals(Gradient value, SerializedProperty p,
            Func<SerializedProperty, Gradient> propertyReadFunc)
        {
            return p.ValueEquals(value);
        }

        public static bool SlowEnumValueEquals(string value, SerializedProperty p,
            Func<SerializedProperty, string> propertyReadFunc)
        {
            var propVal = propertyReadFunc(p);
            return EqualityComparer<string>.Default.Equals(value, propVal);
        }

    }
}
