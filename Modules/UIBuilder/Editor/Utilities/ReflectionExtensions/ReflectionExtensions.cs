// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine;

namespace Unity.UI.Builder
{
    internal static class ReflectionExtensions
    {
        internal static readonly string s_PropertyNotFoundMessage = "Property not found from Reflection";
        const BindingFlags k_BindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;

        public static bool HasValueByReflection(this object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName, k_BindingFlags) != null ||
                obj.GetType().GetField(propertyName, k_BindingFlags) != null;
        }

        public static object GetValueByReflection(this object obj, string propertyName)
        {
            var propertyInfo = obj.GetType().GetProperty(propertyName, k_BindingFlags);

            if (propertyInfo == null)
            {
                var fieldInfo = obj.GetType().GetField(propertyName, k_BindingFlags);
                if (fieldInfo != null)
                {
                    return fieldInfo.GetValue(obj);
                }

                throw new ArgumentException(s_PropertyNotFoundMessage);
            }

            return propertyInfo.GetValue(obj, null);
        }

        public static void SetValueByReflection(this object obj, string propertyName, object value)
        {
            var propertyInfo = obj.GetType().GetProperty(propertyName, k_BindingFlags);

            if (propertyInfo == null)
            {
                var fieldInfo = obj.GetType().GetField(propertyName, k_BindingFlags);
                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(obj, value);
                    return;
                }

                throw new ArgumentException(s_PropertyNotFoundMessage);
            }

            propertyInfo.SetValue(obj, value, null);
        }

        public static bool CallBoolMethodByReflection(this object obj, string methodName)
        {
            var methodInfo = obj.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (methodInfo == null)
                return false;

            var result = methodInfo.Invoke(obj, new object[] { });
            return (bool)result;
        }

        public static MethodInfo GetStaticMethodByReflection(Type type, string methodName)
        {
            var methodInfo = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            return methodInfo;
        }
    }
}
