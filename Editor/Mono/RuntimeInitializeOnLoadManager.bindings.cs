// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [StructLayout(LayoutKind.Sequential)]
    internal sealed partial class RuntimeInitializeMethodInfo
    {
        string      m_FullClassName;
        string      m_MethodName;
        int         m_OrderNumber = 0;
        bool            m_IsUnityClass = false;

        internal string fullClassName { get { return m_FullClassName; } set { m_FullClassName = value; } }
        internal string methodName { get { return m_MethodName; } set { m_MethodName = value; } }
        internal int orderNumber { get { return m_OrderNumber; } set { m_OrderNumber = value; } }
        internal bool isUnityClass { get { return m_IsUnityClass; } set { m_IsUnityClass = value; } }
    }

    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    internal class RuntimeInitializeClassInfo
    {
        public string assemblyName;
        public string nameSpace;
        public string className;
        public string methodName;
        public int loadTypes;
    }

    [NativeHeader("Runtime/Misc/RuntimeInitializeOnLoadManager.h")]
    [StaticAccessorAttribute("GetRuntimeInitializeOnLoadManager()")]
    internal sealed partial class RuntimeInitializeOnLoadManager
    {
        extern internal static string[] dontStripClassNames { get; }

        internal static bool ValidateRuntimeInitializeOnLoadMethod(MethodInfo methodInfo)
        {
            if (!methodInfo.IsStatic)
            {
                return false;
            }
            if (methodInfo.IsSpecialName)
            {
                Debug.LogError($"Method '{methodInfo.DeclaringType?.FullName ?? "Global"}.{methodInfo.Name}' is a property or event accessor method and cannot be marked with [RuntimeInitializeOnLoadMethod]");
                return false;
            }
            if (methodInfo.GetParameters().Length != 0)
            {
                Debug.LogError($"Method '{methodInfo.DeclaringType?.FullName ?? "Global"}.{methodInfo.Name}' has arguments, but [RuntimeInitializeOnLoadMethod] methods cannot have arguments");
                return false;
            }
            if (methodInfo.DeclaringType?.IsGenericType == true)
            {
                Debug.LogError($"Method '{methodInfo.DeclaringType?.FullName ?? "Global"}.{methodInfo.Name}' is in a generic type, but [RuntimeInitializeOnLoadMethod] methods cannot be in generic types");
                return false;
            }
            if (methodInfo.IsGenericMethod)
            {
                Debug.LogError($"Method '{methodInfo.DeclaringType?.FullName ?? "Global"}.{methodInfo.Name}' is a generic method, but [RuntimeInitializeOnLoadMethod] methods cannot be generic");
                return false;
            }

            return true;
        }

        [RequiredByNativeCode]
        internal static MethodInfo[] GetAllValidRuntimeInitializeOnLoadMethods()
        {
            var methods = TypeCache.GetMethodsWithAttribute<RuntimeInitializeOnLoadMethodAttribute>();
            var validMethods = new MethodInfo[methods.Count];

            int validCount = 0;
            foreach (var method in methods)
            {
                if (ValidateRuntimeInitializeOnLoadMethod(method))
                {
                    validMethods[validCount++] = method;
                }
            }

            if (validCount != methods.Count)
            {
                Array.Resize(ref validMethods, validCount);
            }

            return validMethods;
        }
    }
}
