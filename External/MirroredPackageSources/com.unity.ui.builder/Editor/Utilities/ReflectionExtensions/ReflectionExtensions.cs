using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class ReflectionExtensions
    {
        internal static readonly string s_PropertyNotFoundMessage = "Property not found from Reflection";

        public static bool HasValueByReflection(this object obj, string propertyName)
        {
            var propertyInfo = obj.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);

            return propertyInfo != null;
        }

        public static object GetValueByReflection(this object obj, string propertyName)
        {
            var propertyInfo = obj.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);

            if (propertyInfo == null)
                throw new ArgumentException(s_PropertyNotFoundMessage);

            return propertyInfo.GetValue(obj, null);
        }

        public static void SetValueByReflection(this object obj, string propertyName, object value)
        {
            var propertyInfo = obj.GetType().GetProperty(propertyName);

            if (propertyInfo == null)
                throw new ArgumentException(s_PropertyNotFoundMessage);

            propertyInfo?.SetValue(obj, value, null);
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
