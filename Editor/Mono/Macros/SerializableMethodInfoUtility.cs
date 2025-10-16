// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace UnityEditor.Macros.Utilities
{
    [DataContract]
    // Helper to serialize MethodInfo
    internal class SerializableMethodInfo
    {
        [DataMember] public string DeclaringType { get; set; }
        [DataMember] public string MethodName { get; set; }
        [DataMember] public string[] ParameterTypes { get; set; }

        public static SerializableMethodInfo FromMethodInfo(MethodInfo methodInfo)
        {
            var parameterInfos = methodInfo.GetParameters();
            var parameterTypes = new string[parameterInfos.Length];

            for (int i = 0; i < parameterInfos.Length; i++)
            {
                parameterTypes[i] = parameterInfos[i].ParameterType.AssemblyQualifiedName!;
            }

            return new SerializableMethodInfo
            {
                DeclaringType = methodInfo.DeclaringType!.AssemblyQualifiedName!,
                MethodName = methodInfo.Name,
                ParameterTypes = parameterTypes
            };
        }

        public MethodInfo GetMethodInfo()
        {
            var type = Type.GetType(DeclaringType, throwOnError: true);

            var parameterTypesArray = new Type[ParameterTypes.Length];
            for (int i = 0; i < ParameterTypes.Length; i++)
            {
                parameterTypesArray[i] = Type.GetType(ParameterTypes[i], throwOnError: true)!;
            }

            return type.GetMethod(
                MethodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
                null,
                parameterTypesArray,
                null
            );
        }
    }
}
