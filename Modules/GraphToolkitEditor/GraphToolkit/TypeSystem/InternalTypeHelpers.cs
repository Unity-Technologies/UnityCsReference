// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace Unity.GraphToolkit
{
    static class InternalTypeHelpers
    {
        static InternalTypeHelpers()
        {
            TypeSerializerHelper.EnsureStaticConstructorIsCalled();
        }

        public static Func<string, Type> GetMovedFromType { private get; set; }

        static Regex s_GenericTypeExtractionRegex = new(@"(?<=\[\[)(.*?)(?=\]\])");

        static string s_CurrentSystemAssemblyName = ", " + typeof(int).Assembly.GetName().Name + ", ";
        const string k_CoreClrSystemAssemblyName = ", System.Private.CoreLib, ";
        const string k_MonoSystemAssemblyName = ", mscorlib, ";

        static string ExtractAssemblyQualifiedName(string fullTypeName, out bool isList)
        {
            isList = false;
            if (fullTypeName.StartsWith("System.Collections.Generic.List"))
            {
                fullTypeName = s_GenericTypeExtractionRegex.Match(fullTypeName).Value;
                isList = true;
            }

            // remove the assembly version string
            var versionIdx = fullTypeName.IndexOf(", Version=");
            if (versionIdx > 0)
                fullTypeName = fullTypeName.Substring(0, versionIdx);

            // replace all '+' with '/' to follow the Unity serialization convention for nested types
            fullTypeName = fullTypeName.Replace("+", "/");
            return fullTypeName;
        }

        public static Type ResolveMovedFromType(string assemblyQualifiedName)
        {
            var movedType = GetMovedFromType?.Invoke(assemblyQualifiedName);
            return movedType;
        }

        public static Type GetTypeFromTypeName(string assemblyQualifiedName)
        {
            Type type = null;
            if (!string.IsNullOrEmpty(assemblyQualifiedName))
            {
                assemblyQualifiedName = ConvertTypeNameFromCoreClrToCurrentSystemLib(assemblyQualifiedName);
                type = Type.GetType(assemblyQualifiedName);
                if (type == null)
                {
                    // Check if the type has moved
                    assemblyQualifiedName = ExtractAssemblyQualifiedName(assemblyQualifiedName, out var isList);
                    var movedType = GetMovedFromType?.Invoke(assemblyQualifiedName);
                    if (movedType != null)
                    {
                        type = movedType;
                        if (isList)
                        {
                            type = typeof(List<>).MakeGenericType(type);
                        }
                    }
                }
            }

            return type;
        }

        public static string ConvertTypeNameFromMonoToCoreClr(string asmQualifiedTypeName)
        {
            if (asmQualifiedTypeName == null || asmQualifiedTypeName.Length < k_MonoSystemAssemblyName.Length)
                return asmQualifiedTypeName;

            return asmQualifiedTypeName.Replace(k_MonoSystemAssemblyName, k_CoreClrSystemAssemblyName);
        }

        static string ConvertTypeNameFromCoreClrToCurrentSystemLib(string asmQualifiedTypeName)
        {
            if (asmQualifiedTypeName == null || asmQualifiedTypeName.Length < k_CoreClrSystemAssemblyName.Length)
                return asmQualifiedTypeName;

            if (s_CurrentSystemAssemblyName == k_CoreClrSystemAssemblyName)
            {
                return asmQualifiedTypeName;
            }

            return asmQualifiedTypeName.Replace(k_CoreClrSystemAssemblyName, s_CurrentSystemAssemblyName);
        }
    }
}
