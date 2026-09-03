// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.GraphToolkit.InternalBridge;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolkit
{
    static partial class InternalTypeHelpers
    {
        static readonly Regex s_GenericTypeExtractionRegex = new(@"(?<=\[\[)(.*?)(?=\]\])");

        static readonly string s_CurrentSystemAssemblyName = ", " + typeof(int).Assembly.GetName().Name + ", ";
        const string k_CoreClrSystemAssemblyName = ", System.Private.CoreLib, ";
        const string k_MonoSystemAssemblyName = ", mscorlib, ";

        [AutoStaticsCleanupOnCodeReload]
        static Dictionary<string, Type> s_MovedFromTypes;

        [AutoStaticsCleanupOnCodeReload]
        static readonly Dictionary<Type, bool> k_IsTypeSerializableCache = new();

        static Dictionary<string, Type> GetMovedFromTypes()
        {
            if (s_MovedFromTypes == null)
            {
                s_MovedFromTypes = new Dictionary<string, Type>();
                var movedFromTypes = TypeCache.GetTypesWithAttribute<MovedFromAttribute>();
                foreach (var t in movedFromTypes)
                {
                    var attributes = Attribute.GetCustomAttributes(t, typeof(MovedFromAttribute), false);
                    foreach (var attribute in attributes)
                    {
                        var movedFromAttribute = (MovedFromAttribute)attribute;

                        movedFromAttribute.GetMovedFromData(out var className, out _,
                            out var nameSpace, out var nameSpaceHasChanged,
                            out var assembly, out _, out _);

                        var currentClassName = GetFullNameNoNamespace(t.FullName, t.Namespace);
                        var currentNamespace = t.Namespace;
                        var currentAssembly = t.Assembly.GetName().Name;

                        var oldNamespace = nameSpaceHasChanged ? nameSpace : currentNamespace;
                        var oldClassName = string.IsNullOrEmpty(className) ? currentClassName : className;
                        var oldAssembly = string.IsNullOrEmpty(assembly) ? currentAssembly : assembly;

                        var oldAssemblyQualifiedName =
                            string.IsNullOrEmpty(oldNamespace) ? $"{oldClassName}, {oldAssembly}" : $"{oldNamespace}.{oldClassName}, {oldAssembly}";

                        s_MovedFromTypes.Add(oldAssemblyQualifiedName, t);
                    }
                }
            }

            return s_MovedFromTypes;
        }

        static Type GetMovedFromType(string typeName)
        {
            int firstComma = typeName.IndexOf(',');
            if (firstComma < 0)
                return null;
            int secondComma = typeName.IndexOf(',', firstComma + 1);

            string typeNameWithoutVersion = secondComma < 0 ? typeName : typeName.Substring(0, secondComma);

            return GetMovedFromTypes().GetValueOrDefault(typeNameWithoutVersion);
        }

        /// <summary>
        /// Gets the full name of a type without the namespace.
        /// </summary>
        /// <remarks>
        /// The full name of a type nested type includes the outer class type name. The type names are normally
        /// separated by '+' but Unity serialization uses the '/' character as separator.
        ///
        /// This method returns the full type name of a class and switches the type separator to '/' to follow Unity.
        /// </remarks>
        /// <param name="typeName">The full type name, including the namespace.</param>
        /// <param name="nameSpace">The namespace to be removed.</param>
        /// <returns>Returns a string.</returns>
        static string GetFullNameNoNamespace(string typeName, string nameSpace)
        {
            if (typeName != null && nameSpace != null && typeName.Contains(nameSpace))
            {
                return typeName.Substring(nameSpace.Length + 1).Replace("+", "/");
            }
            return typeName;
        }

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
            return GetMovedFromType(assemblyQualifiedName);
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
                    var movedType = GetMovedFromType(assemblyQualifiedName);
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

        /// <summary>
        /// Check if the given type is serializable or a Unity Object reference.
        /// </summary>
        /// <remarks>
        /// Use this method to determine if a given type is valid to be used as a Constant, which requires a value type that can be serialized.
        /// </remarks>
        /// <param name="type">The type for which to check.</param>
        /// <returns>True if the given type is serializable or a Unity Object reference.</returns>
        public static bool IsTypeSerializable(Type type)
        {
            if (type == null)
                return false;

            if (!k_IsTypeSerializableCache.TryGetValue(type, out bool serializable))
            {
                serializable = type.IsSerializable || typeof(UnityEngine.Object).IsAssignableFrom(type);
                k_IsTypeSerializableCache[type] = serializable;
            }

            return serializable;
        }
    }
}
