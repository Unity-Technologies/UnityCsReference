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
using UnityEditor.Scripting.Compilers;
using UnityEngine;

namespace UnityEditor
{
    internal partial class AssemblyHelper
    {
        static readonly Type[] ExtendableScriptTypes = { typeof(MonoBehaviour), typeof(ScriptableObject), typeof(Experimental.AssetImporters.ScriptedImporter) };

        // Check if assmebly internal name doesn't match file name, and show the warning.
        static public void CheckForAssemblyFileNameMismatch(string assemblyPath)
        {
            string fileName = Path.GetFileNameWithoutExtension(assemblyPath);
            string assemblyName = ExtractInternalAssemblyName(assemblyPath);

            if (string.IsNullOrEmpty(assemblyName))
                return;

            if (fileName != assemblyName)
            {
                Debug.LogWarning("Assembly '" + assemblyName + "' has non matching file name: '" + Path.GetFileName(assemblyPath) + "'. This can cause build issues on some platforms.");
            }
        }

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
                return ""; // Possible on just deleted FacebookSDK
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

        static private bool IgnoreAssembly(string assemblyPath, BuildTarget target)
        {
            if (target == BuildTarget.WSAPlayer ||
                (target == BuildTarget.XboxOne && PlayerSettings.GetApiCompatibilityLevel(BuildTargetGroup.XboxOne) == ApiCompatibilityLevel.NET_4_6))
            {
                if (assemblyPath.IndexOf("mscorlib.dll") != -1 ||
                    assemblyPath.IndexOf("System.") != -1 ||
                    assemblyPath.IndexOf("Windows.dll") != -1 ||
                    assemblyPath.IndexOf("Microsoft.") != -1 ||
                    assemblyPath.IndexOf("Windows.") != -1 ||
                    assemblyPath.IndexOf("WinRTLegacy.dll") != -1 ||
                    assemblyPath.IndexOf("platform.dll") != -1)
                    return true;
            }
            return IsInternalAssembly(assemblyPath);
        }

        static private void AddReferencedAssembliesRecurse(string assemblyPath, List<string> alreadyFoundAssemblies, string[] allAssemblyPaths, string[] foldersToSearch, Dictionary<string, AssemblyDefinition> cache, BuildTarget target)
        {
            if (IgnoreAssembly(assemblyPath, target))
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
                if (IgnoreAssembly(referencedAssembly.Name + ".dll", target)) continue;

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

                AddReferencedAssembliesRecurse(foundPath, alreadyFoundAssemblies, allAssemblyPaths, foldersToSearch, cache, target);
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

        static public string[] FindAssembliesReferencedBy(string[] paths, string[] foldersToSearch, BuildTarget target)
        {
            List<string> unique = new List<string>();
            string[] allAssemblyPaths = paths;

            var cache = new Dictionary<string, AssemblyDefinition>();
            for (int i = 0; i < paths.Length; i++)
                AddReferencedAssembliesRecurse(paths[i], unique, allAssemblyPaths, foldersToSearch, cache, target);

            for (int i = 0; i < paths.Length; i++)
                unique.Remove(paths[i]);

            return unique.ToArray();
        }

        static public string[] FindAssembliesReferencedBy(string path, string[] foldersToSearch, BuildTarget target)
        {
            string[] tmp = new string[1];
            tmp[0] = path;
            return FindAssembliesReferencedBy(tmp, foldersToSearch, target);
        }

        static public bool IsUnityEngineModule(string assemblyName)
        {
            return assemblyName.EndsWith("Module") && assemblyName.StartsWith("UnityEngine.");
        }

        private static bool IsTypeAUserExtendedScript(TypeReference type)
        {
            if (type == null || type.FullName == "System.Object")
                return false;

            foreach (var extendableScriptType in ExtendableScriptTypes)
            {
                if (type.Name == extendableScriptType.Name && type.Namespace == extendableScriptType.Namespace)
                    return true;
            }

            try
            {
                var typeDefinition = type.Resolve();

                if (typeDefinition != null)
                    return IsTypeAUserExtendedScript(typeDefinition.BaseType);
            }
            catch (AssemblyResolutionException)
            {
                // just eat exception if we fail to load assembly here.
                // failure should be handled better in other places.
            }

            return false;
        }

        public static void ExtractAllClassesThatAreUserExtendedScripts(string path, out string[] classNamesArray, out string[] classNameSpacesArray, out string[] originalClassNameSpacesArray)
        {
            List<string> classNames = new List<string>();
            List<string> nameSpaces = new List<string>();
            List<string> originalNamespaces = new List<string>();
            var readerParameters = new ReaderParameters();

            // this will resolve any types in assemblies within the same directory as the type's assembly
            // or any folder which contains a currently available precompiled dll
            var assemblyResolver = new DefaultAssemblyResolver();
            assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(path));

            // Add the path to all available precompiled assemblies
            var group = EditorUserBuildSettings.activeBuildTargetGroup;
            var target = EditorUserBuildSettings.activeBuildTarget;
            var precompiledAssemblies = UnityEditorInternal.InternalEditorUtility.GetPrecompiledAssemblies(true, group, target);
            HashSet<string> searchPaths = new HashSet<string>();
            foreach (var asm in precompiledAssemblies)
                searchPaths.Add(Path.GetDirectoryName(asm.Path));
            foreach (var asmpath in searchPaths)
                assemblyResolver.AddSearchDirectory(asmpath);

            readerParameters.AssemblyResolver = assemblyResolver;

            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(path, readerParameters);
            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (TypeDefinition type in module.Types)
                {
                    TypeReference baseType = type.BaseType;

                    try
                    {
                        if (IsTypeAUserExtendedScript(baseType))
                        {
                            classNames.Add(type.Name);
                            nameSpaces.Add(type.Namespace);

                            var originalNamespace = string.Empty;
                            var attribute = type.CustomAttributes.SingleOrDefault(a => a.AttributeType.FullName == typeof(UnityEngine.Scripting.APIUpdating.MovedFromAttribute).FullName);

                            if (attribute != null)
                            {
                                originalNamespace = (string)attribute.ConstructorArguments[0].Value;
                            }

                            originalNamespaces.Add(originalNamespace);
                        }
                    }
                    catch (Exception)
                    {
                        Debug.LogError("Failed to extract " + type.FullName + " class of base type " + baseType.FullName + " when inspecting " + path);
                    }
                }
            }

            classNamesArray = classNames.ToArray();
            classNameSpacesArray = nameSpaces.ToArray();
            originalClassNameSpacesArray = originalNamespaces.ToArray();
        }

        /// Extract information about all types in the specified assembly, searchDirs might be used to resolve dependencies.
        static public AssemblyTypeInfoGenerator.ClassInfo[] ExtractAssemblyTypeInfo(BuildTarget targetPlatform, bool isEditor, string assemblyPathName, string[] searchDirs)
        {
            try
            {
                var target = ModuleManager.GetTargetStringFromBuildTarget(targetPlatform);
                var extension = ModuleManager.GetCompilationExtension(target);
                var extraPaths = extension.GetCompilerExtraAssemblyPaths(isEditor, assemblyPathName);
                if (extraPaths != null && extraPaths.Length > 0)
                {
                    var dirs = new List<string>(searchDirs);
                    dirs.AddRange(extraPaths);
                    searchDirs = dirs.ToArray();
                }
                var assemblyResolver = extension.GetAssemblyResolver(isEditor, assemblyPathName, searchDirs);

                AssemblyTypeInfoGenerator gen;
                if (assemblyResolver == null)
                {
                    gen = new AssemblyTypeInfoGenerator(assemblyPathName, searchDirs);
                }
                else
                {
                    gen = new AssemblyTypeInfoGenerator(assemblyPathName, assemblyResolver);
                }

                return gen.GatherClassInfo();
            }
            catch (System.Exception ex)
            {
                throw new Exception("ExtractAssemblyTypeInfo: Failed to process " + assemblyPathName + ", " + ex);
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

        internal static IEnumerable<T> FindImplementors<T>(Assembly assembly) where T : class
        {
            Type interfaze = typeof(T);
            foreach (Type type in AssemblyHelper.GetTypesFromAssembly(assembly))
            {
                if (/*type.IsNotPublic - future! ||*/ type.IsInterface || type.IsAbstract || !interfaze.IsAssignableFrom(type))
                    continue;
                T module = null;

                if (typeof(ScriptableObject).IsAssignableFrom(type))
                    module = ScriptableObject.CreateInstance(type) as T;
                else
                    module = Activator.CreateInstance(type) as T;
                if (module != null)
                    yield return module;
            }
        }

        public static bool IsManagedAssembly(string file)
        {
            UnityEditorInternal.DllType type = UnityEditorInternal.InternalEditorUtility.DetectDotNetDll(file);
            return type != UnityEditorInternal.DllType.Unknown && type != UnityEditorInternal.DllType.Native;
        }

        public static bool IsInternalAssembly(string file)
        {
            return UnityEditor.Modules.ModuleManager.IsRegisteredModule(file) ||
                ModuleUtils.GetAdditionalReferencesForUserScripts().Any(p => p.Equals(file));
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
    }
}
