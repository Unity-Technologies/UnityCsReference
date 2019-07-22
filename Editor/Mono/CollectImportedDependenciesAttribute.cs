// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Scripting;
using static UnityEditor.AttributeHelper;

namespace UnityEditor.Experimental.AssetImporters
{
    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class CollectImportedDependenciesAttribute : Attribute
    {
        private Type m_ImporterType;
        private uint m_Version;

        public CollectImportedDependenciesAttribute(Type importerType, uint version)
        {
            m_ImporterType = importerType;
            m_Version = version;
        }

        public Type importerType { get { return m_ImporterType; } }
        public uint version { get { return m_Version; } }

        [RequiredSignature]
        static extern string[] CollectImportedDependenciesSignature(string assetPath);
    }

    static class ImportedDependenciesApi
    {
        static Dictionary<Type, string> s_ImportDependenciesHashStringMap = null;
        static Dictionary<Type, MethodWithAttribute[]> s_ImportDependencyCallbackTypeMap = null;

        private static IEnumerable<MethodWithAttribute> GetImportedDependenciesCallbacksAndAttributesForImporter(Type importerType)
        {
            if (s_ImportDependencyCallbackTypeMap != null && s_ImportDependencyCallbackTypeMap.ContainsKey(importerType))
                return s_ImportDependencyCallbackTypeMap[importerType];

            if (s_ImportDependencyCallbackTypeMap == null)
                s_ImportDependencyCallbackTypeMap = new Dictionary<Type, MethodWithAttribute[]>();

            Func<CollectImportedDependenciesAttribute, bool> filter = (a) => a.importerType.IsAssignableFrom(importerType);
            s_ImportDependencyCallbackTypeMap[importerType] = AttributeHelper.GetMethodsWithAttribute<CollectImportedDependenciesAttribute>(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .methodsWithAttributes
                .Where(x => filter((CollectImportedDependenciesAttribute)x.attribute))
                .ToArray();

            return s_ImportDependencyCallbackTypeMap[importerType];
        }

        [RequiredByNativeCode]
        private static MethodInfo[] GetImportedDependenciesCallbacks(Type importerType)
        {
            return GetImportedDependenciesCallbacksAndAttributesForImporter(importerType).Select(x => x.info).ToArray();
        }

        private static string BuildHashString(SortedList<string, uint> list)
        {
            var hashStr = "";
            foreach (var pair in list)
            {
                hashStr += pair.Key;
                hashStr += '.';
                hashStr += pair.Value;
                hashStr += '|';
            }

            return hashStr;
        }

        [RequiredByNativeCode]
        static string GetImportedDependenciesCallbacksHashString(Type importerType)
        {
            if (s_ImportDependenciesHashStringMap != null && s_ImportDependenciesHashStringMap.ContainsKey(importerType))
                return s_ImportDependenciesHashStringMap[importerType];

            if (s_ImportDependenciesHashStringMap == null)
                s_ImportDependenciesHashStringMap = new Dictionary<Type, string>();

            var versionsByType = new SortedList<string, uint>();

            var methodsWithAttribute = GetImportedDependenciesCallbacksAndAttributesForImporter(importerType);

            foreach (var method in methodsWithAttribute)
            {
                var attribute = (CollectImportedDependenciesAttribute)method.attribute;
                var version = attribute.version;
                string methodName = method.info.Name;
                string className = method.info.ReflectedType.FullName;

                string fullMethodName = className + "." + methodName;

                if (version != 0)
                {
                    versionsByType.Add(fullMethodName, version);
                }
            }

            s_ImportDependenciesHashStringMap[importerType] = BuildHashString(versionsByType);
            return s_ImportDependenciesHashStringMap[importerType];
        }
    }
}
