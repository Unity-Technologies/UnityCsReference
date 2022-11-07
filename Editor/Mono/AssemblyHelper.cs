// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using Mono.Cecil;
using UnityEditor.Build;
using UnityEditor.Modules;
using UnityEditorInternal;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEditor.VisualStudioIntegration;
using UnityEngine.Scripting;
using Debug = UnityEngine.Debug;
using Unity.Profiling;
using UnityEditor.AssetImporters;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor
{
    internal partial class AssemblyHelper
    {
        static Dictionary<string, bool> managedToDllType = new Dictionary<string, bool>();
        static BuildPlayerDataExtractor m_BuildPlayerDataExtractor = new BuildPlayerDataExtractor();

        static public string[] GetNamesOfAssembliesLoadedInCurrentDomain()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var locations = new List<string>();
            foreach (var a in assemblies)
            {
                try
                {
                    locations.Add(a.Location);
                }
                catch (NotSupportedException)
                {
                    //we have some "dynamic" assmeblies that do not have a filename
                }
            }
            return locations.ToArray();
        }

        static public string ExtractInternalAssemblyName(string path)
        {
            try
            {
                AssemblyDefinition definition = AssemblyDefinition.ReadAssembly(path);
                return definition.Name.Name;
            }
            catch
            {
                return "";
            }
        }

        static AssemblyDefinition GetAssemblyDefinitionCached(string path, Dictionary<string, AssemblyDefinition> cache)
        {
            if (cache.ContainsKey(path))
                return cache[path];

            AssemblyDefinition definition = AssemblyDefinition.ReadAssembly(path);
            cache[path] = definition;
            return definition;
        }

        static private bool CouldBelongToDotNetOrWindowsRuntime(string assemblyPath)
        {
            return assemblyPath.IndexOf("mscorlib.dll") != -1 ||
                assemblyPath.IndexOf("System.") != -1 ||
                assemblyPath.IndexOf("Microsoft.") != -1 ||
                assemblyPath.IndexOf("Windows.") != -1 ||
                assemblyPath.IndexOf("WinRTLegacy.dll") != -1 ||
                assemblyPath.IndexOf("platform.dll") != -1;
        }

        static private bool IgnoreAssembly(string assemblyPath, BuildTarget target, ScriptingImplementation scriptingImplementation)
        {
#pragma warning disable 618
            if (target == BuildTarget.WSAPlayer || scriptingImplementation == ScriptingImplementation.CoreCLR)
            {
                if (CouldBelongToDotNetOrWindowsRuntime(assemblyPath))
                    return true;
            }
            else if (target == BuildTarget.XboxOne)
            {
                var profile = PlayerSettings.GetApiCompatibilityLevel(NamedBuildTarget.XboxOne);
                if (profile == ApiCompatibilityLevel.NET_4_6 || profile == ApiCompatibilityLevel.NET_Standard_2_0)
                {
                    if (CouldBelongToDotNetOrWindowsRuntime(assemblyPath))
                        return true;
                }
            }

            return IsInternalAssembly(assemblyPath);
        }

        static private void AddReferencedAssembliesRecurse(string assemblyPath, List<string> alreadyFoundAssemblies, string[] allAssemblyPaths, string[] foldersToSearch, Dictionary<string, AssemblyDefinition> cache, BuildTarget target, ScriptingImplementation scriptingImplementation)
        {
            if (IgnoreAssembly(assemblyPath, target, scriptingImplementation))
                return;

            if (!File.Exists(assemblyPath))
                return;

            AssemblyDefinition assembly = GetAssemblyDefinitionCached(assemblyPath, cache);
            if (assembly == null)
                throw new System.ArgumentException("Referenced Assembly " + Path.GetFileName(assemblyPath) + " could not be found!");

            // Ignore it if we already added the assembly
            if (alreadyFoundAssemblies.IndexOf(assemblyPath) != -1)
                return;

            alreadyFoundAssemblies.Add(assemblyPath);

            var architectureSpecificPlugins = PluginImporter.GetImporters(target).Where(i =>
            {
                var cpu = i.GetPlatformData(target, "CPU");
                return !string.IsNullOrEmpty(cpu) && !string.Equals(cpu, "AnyCPU", StringComparison.InvariantCultureIgnoreCase);
            }).Select(i => Path.GetFileName(i.assetPath)).Distinct();

            // Go through all referenced assemblies
            foreach (AssemblyNameReference referencedAssembly in assembly.MainModule.AssemblyReferences)
            {
                // Special cases for Metro
                if (referencedAssembly.Name == "BridgeInterface") continue;
                if (referencedAssembly.Name == "WinRTBridge") continue;
                if (referencedAssembly.Name == "UnityEngineProxy") continue;
                if (IgnoreAssembly(referencedAssembly.Name + ".dll", target, scriptingImplementation)) continue;

                string foundPath = FindAssemblyName(referencedAssembly.FullName, referencedAssembly.Name, allAssemblyPaths, foldersToSearch, cache);

                if (foundPath == "")
                {
                    // Ignore architecture specific plugin references
                    var found = false;
                    foreach (var extension in new[] { ".dll", ".winmd" })
                    {
                        if (architectureSpecificPlugins.Any(p => string.Equals(p, referencedAssembly.Name + extension, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found)
                        continue;
                    throw new System.ArgumentException(string.Format("The Assembly {0} is referenced by {1} ('{2}'). But the dll is not allowed to be included or could not be found.",
                        referencedAssembly.Name,
                        assembly.MainModule.Assembly.Name.Name,
                        assemblyPath));
                }

                AddReferencedAssembliesRecurse(foundPath, alreadyFoundAssemblies, allAssemblyPaths, foldersToSearch, cache, target, scriptingImplementation);
            }
        }

        static string FindAssemblyName(string fullName, string name, string[] allAssemblyPaths, string[] foldersToSearch, Dictionary<string, AssemblyDefinition> cache)
        {
            // Search in provided assemblies
            for (int i = 0; i < allAssemblyPaths.Length; i++)
            {
                if (!File.Exists(allAssemblyPaths[i]))
                    continue;

                AssemblyDefinition definition = GetAssemblyDefinitionCached(allAssemblyPaths[i], cache);
                if (definition.MainModule.Assembly.Name.Name == name)
                    return allAssemblyPaths[i];
            }

            // Search in GAC
            foreach (string folder in foldersToSearch)
            {
                string pathInGacFolder = Path.Combine(folder, name + ".dll");
                if (File.Exists(pathInGacFolder))
                    return pathInGacFolder;
            }
            return "";
        }

        [RequiredByNativeCode]
        static public string[] FindAssembliesReferencedBy(string[] paths, string[] foldersToSearch, BuildTarget target, ScriptingImplementation scriptingImplementation)
        {
            List<string> unique = new List<string>();
            string[] allAssemblyPaths = paths;

            var cache = new Dictionary<string, AssemblyDefinition>();
            for (int i = 0; i < paths.Length; i++)
                AddReferencedAssembliesRecurse(paths[i], unique, allAssemblyPaths, foldersToSearch, cache, target, scriptingImplementation);

            for (int i = 0; i < paths.Length; i++)
                unique.Remove(paths[i]);

            return unique.ToArray();
        }

        static public bool IsUnityEngineModule(AssemblyDefinition assembly)
        {
            return assembly.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(UnityEngineModuleAssembly).FullName);
        }

        static public bool IsUnityEngineModule(Assembly assembly)
        {
            return assembly.GetCustomAttributes(typeof(UnityEngineModuleAssembly), false).Length > 0;
        }

        private static bool IsTypeAUserExtendedScript(TypeReference type)
        {
            if (type == null || type.FullName == "System.Object")
                return false;

            try
            {
                var typeDefinition = type.Resolve();
                var attributes = typeDefinition.CustomAttributes;
                for (var i = 0; i < attributes.Count; i++)
                {
                    if (attributes[i].Constructor.DeclaringType.FullName == "UnityEngine.ExtensionOfNativeClassAttribute")
                        return true;
                }

                if (typeDefinition.BaseType != null)
                    return IsTypeAUserExtendedScript(typeDefinition.BaseType);
            }
            catch (AssemblyResolutionException)
            {
                // just eat exception if we fail to load assembly here.
                // failure should be handled better in other places.
            }

            return false;
        }

        public static string[] GetDefaultAssemblySearchPaths()
        {
            // Add the path to all available precompiled assemblies
            var group = EditorUserBuildSettings.activeBuildTargetGroup;
            var target = EditorUserBuildSettings.activeBuildTarget;
            var precompiledAssemblyPaths = InternalEditorUtility.GetPrecompiledAssemblyPaths();

            HashSet<string> searchPaths = new HashSet<string>(precompiledAssemblyPaths);

            var precompiledAssemblies = InternalEditorUtility.GetUnityAssemblies(true, group, target);
            foreach (var asm in precompiledAssemblies)
                searchPaths.Add(Path.GetDirectoryName(asm.Path));

            // Add Unity compiled assembly output directory.
            // Required for MonoBehaviour derived types like UIBehaviour that
            // were previous in a precompiled UnityEngine.UI.dll, but are now
            // compiled in a package.
            searchPaths.Add("Library/ScriptAssemblies");

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

        const int kDefaultDepth = 10;
        internal static ICollection<string> FindAssemblies(string basePath)
        {
            return FindAssemblies(basePath, kDefaultDepth);
        }

        internal static ICollection<string> FindAssemblies(string basePath, int maxDepth)
        {
            var assemblies = new List<string>();

            if (0 == maxDepth)
                return assemblies;

            try
            {
                DirectoryInfo directory = new DirectoryInfo(basePath);
                assemblies.AddRange(directory.GetFiles()
                    .Where(file => IsManagedAssembly(file.FullName))
                    .Select(file => file.FullName));
                foreach (DirectoryInfo subdirectory in directory.GetDirectories())
                    assemblies.AddRange(FindAssemblies(subdirectory.FullName, maxDepth - 1));
            }
            catch (Exception)
            {
                // Return what we have now
            }

            return assemblies;
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
