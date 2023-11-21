// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Mono.Cecil;
using UnityEditor.Modules;
using UnityEditorInternal;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using Debug = UnityEngine.Debug;
using Unity.Profiling;
using UnityEditor.AssetImporters;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor
{
    internal class AssemblyHelper
    {
        static Dictionary<string, bool> managedToDllType = new Dictionary<string, bool>();
        static BuildPlayerDataExtractor m_BuildPlayerDataExtractor = new BuildPlayerDataExtractor();

        static public bool IsUnityEngineModule(AssemblyDefinition assembly)
        {
            return assembly.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(UnityEngineModuleAssembly).FullName);
        }

        static public bool IsUnityEngineModule(Assembly assembly)
        {
            return assembly.GetCustomAttributes(typeof(UnityEngineModuleAssembly), false).Length > 0;
        }

        public static string[] GetDefaultAssemblySearchPaths()
        {
            // Add the path to all available precompiled assemblies
            var target = EditorUserBuildSettings.activeBuildTarget;
            var precompiledAssemblyPaths = InternalEditorUtility.GetPrecompiledAssemblyPaths();

            HashSet<string> searchPaths = new HashSet<string>(precompiledAssemblyPaths);

            var precompiledAssemblies = InternalEditorUtility.GetUnityAssemblies(true, target);
            foreach (var asm in precompiledAssemblies)
                searchPaths.Add(Path.GetDirectoryName(asm.Path));

            // Add Unity compiled assembly output directory.
            // Required for MonoBehaviour derived types like UIBehaviour that
            // were previous in a precompiled UnityEngine.UI.dll, but are now
            // compiled in a package.
            searchPaths.Add(InternalEditorUtility.GetEditorScriptAssembliesPath());

            return searchPaths.ToArray();
        }

        [RequiredByNativeCode]
        public static bool ExtractAllClassesThatAreUserExtendedScripts(string path, out string[] classNamesArray, out string[] classNameSpacesArray, out string[] movedFromNamespacesArray)
        {
            var typesDerivedFromMonoBehaviour = TypeCache.GetTypesDerivedFrom<MonoBehaviour>();
            var typesDerivedFromScriptableObject = TypeCache.GetTypesDerivedFrom<ScriptableObject>();
            var typesDerivedFromScriptedImporter = TypeCache.GetTypesDerivedFrom<ScriptedImporter>();

            var fileName = Path.GetFileName(path);
            IEnumerable<Type> userTypes = typesDerivedFromMonoBehaviour.Where(x => Path.GetFileName(x.Assembly.Location) == fileName);
            userTypes = userTypes
                .Concat(typesDerivedFromScriptableObject.Where(x => Path.GetFileName(x.Assembly.Location) == fileName))
                .Concat(typesDerivedFromScriptedImporter.Where(x => Path.GetFileName(x.Assembly.Location) == fileName)).ToList();

            List<string> classNames = new List<string>(userTypes.Count());
            List<string> nameSpaces = new List<string>(userTypes.Count());
            List<string> originalNamespaces = new List<string>(userTypes.Count());

            string pathToAssembly = null;
            foreach (var userType in userTypes)
            {
                if (string.IsNullOrEmpty(pathToAssembly))
                    pathToAssembly = Path.GetFullPath(userType.Assembly.Location);

                classNames.Add(userType.Name);
                nameSpaces.Add(userType.Namespace);

                var movedFromAttribute = userType.GetCustomAttribute<MovedFromAttribute>();
                originalNamespaces.Add(movedFromAttribute?.data.nameSpace);
            }

            classNamesArray = classNames.ToArray();
            classNameSpacesArray = nameSpaces.ToArray();
            movedFromNamespacesArray = originalNamespaces.ToArray();

            return !Utility.IsPathsEqual(pathToAssembly, path);
        }

        /// Extract information about all types in the specified assembly, searchDirs might be used to resolve dependencies.
        [RequiredByNativeCode]
        static public AssemblyInfoManaged[] ExtractAssemblyTypeInfo(bool isEditor)
        {
            var extractAssemblyTypeInfo = m_BuildPlayerDataExtractor.ExtractAssemblyTypeInfo(isEditor);
            return extractAssemblyTypeInfo;
        }

        static public AssemblyInfoManaged[] ExtractAssemblyTypeInfoFromFiles(string[] typeDbJsonPaths)
        {
            return m_BuildPlayerDataExtractor.ExtractAssemblyTypeInfoFromFiles(typeDbJsonPaths);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RuntimeInitializeOnLoadMethodsData
        {
            public RuntimeInitializeClassInfo[] classInfos;
            public int methodsCount;
        }

        [RequiredByNativeCode]
        public static void ExtractPlayerRuntimeInitializeOnLoadMethods(string jsonPath)
        {
            try
            {
                m_BuildPlayerDataExtractor.ExtractPlayerRuntimeInitializeOnLoadMethods(jsonPath);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed extracting RuntimeInitializeOnLoadMethods. Player will not be able to execute RuntimeInitializeOnLoadMethods: {exception.Message}{Environment.NewLine}{exception.StackTrace}");
            }
        }

        internal static Type[] GetTypesFromAssembly(Assembly assembly)
        {
            if (assembly == null)
                return new Type[] {};
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException)
            {
                return new Type[] {};
            }
        }

        public static bool IsManagedAssembly(string file)
        {
            bool isManagedDll;
            if (managedToDllType.TryGetValue(file, out isManagedDll))
            {
                return isManagedDll;
            }
            var res = InternalEditorUtility.IsDotNetDll(file);
            managedToDllType[file] = res;
            return res;
        }

        public static bool IsInternalAssembly(string file)
        {
            return ModuleUtils.GetAdditionalReferencesForUserScripts().Any(p => p.Equals(file));
        }

        /// <summary>
        /// Performs a depth-first-search topological sort on the input assemblies,
        /// based on the outgoing assembly references from each assembly. The
        /// returned list is sorted such that assembly A appears before any
        /// assemblies that depend (directly or indirectly) on assembly A.
        /// </summary>
        internal static Assembly[] TopologicalSort(Assembly[] assemblies)
        {
            using var _ = new ProfilerMarker("SortAssembliesTopologically").Auto();

            var assembliesByName = new Dictionary<string, int>(assemblies.Length);
            for (var i = 0; i < assemblies.Length; i++)
            {
                assembliesByName[assemblies[i].GetName().Name] = i;
            }

            var result = new Assembly[assemblies.Length];
            var resultIndex = 0;

            var visited = new TopologicalSortVisitStatus[assemblies.Length];

            void VisitAssembly(int index)
            {
                var visitStatus = visited[index];

                switch (visitStatus)
                {
                    case TopologicalSortVisitStatus.Visiting:
                        // We have a cyclic dependency between assemblies. This should really be an error, but...
                        // We need to allow cyclic dependencies between assemblies, because the rest of Unity allows them.
                        // For example if you make an assembly override for UnityEngine.AccessibilityModule.dll,
                        // it will reference UnityEditor.CoreModule.dll, which in turn references
                        // UnityEngine.AccessibilityModule.dll... and that doesn't trigger an error.
                        // The topological sort won't be correct in this case, but it's better than erroring-out.
                        break;

                    case TopologicalSortVisitStatus.NotVisited:
                        visited[index] = TopologicalSortVisitStatus.Visiting;

                        var assembly = assemblies[index];

                        var assemblyReferences = assembly.GetReferencedAssemblies();
                        foreach (var assemblyReference in assemblyReferences)
                        {
                            // It's okay if we can't resolve the assembly. It just means that the referenced assembly
                            // is not in the input set of assemblies, so we wouldn't be able to sort it anyway.
                            if (assembliesByName.TryGetValue(assemblyReference.Name, out var referencedAssembly))
                            {
                                VisitAssembly(referencedAssembly);
                            }
                        }

                        visited[index] = TopologicalSortVisitStatus.Visited;

                        result[resultIndex++] = assembly;
                        break;
                }
            }

            for (var i = 0; i < assemblies.Length; i++)
            {
                VisitAssembly(i);
            }

            return result;
        }

        private enum TopologicalSortVisitStatus : byte
        {
            NotVisited,
            Visiting,
            Visited
        }
    }
}
