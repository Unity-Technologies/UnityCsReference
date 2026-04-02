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
using UnityEditor.AssetImporters;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine.Scripting.APIUpdating;
using UnityEditor.Scripting;
using UnityEngine.Bindings;
using Unity.Collections;

namespace UnityEditor
{
    [VisibleToOtherModules("UnityEditor.BurstModule")]
    internal class AssemblyHelper
    {
        static Dictionary<string, bool> managedToDllType = new Dictionary<string, bool>();
        static BuildPlayerDataExtractor m_BuildPlayerDataExtractor = new BuildPlayerDataExtractor();

        public static bool IsUnityEngineModule(AssemblyDefinition assembly)
        {
            return assembly.CustomAttributes.Exists(a => a.AttributeType.FullName == typeof(UnityEngineModuleAssembly).FullName);
        }

        public static bool IsUnityEngineModule(Assembly assembly)
        {
            return assembly.IsDefined(typeof(UnityEngineModuleAssembly), false);
        }

        public static string[] GetDefaultAssemblySearchPaths()
        {
            // Add the path to all available precompiled assemblies
            var target = EditorUserBuildSettings.activeBuildTarget;
            var precompiledAssemblyPaths = InternalEditorUtility.GetPrecompiledAssemblyPaths();

            HashSet<string> searchPaths = new HashSet<string>(precompiledAssemblyPaths);
            // bcl extensions search path. Note: when we have CoreCLR editor, we'll want to make this search path dependant on current runtime
            searchPaths.Add(BCLExtensions.NetstandardRuntimeDirectory());

            var precompiledAssemblies = InternalEditorUtility.GetUnityAssemblies(true, target);
            foreach (var asm in precompiledAssemblies)
                searchPaths.Add(Path.GetDirectoryName(asm.Path));

            // Add Unity compiled assembly output directory.
            // Required for MonoBehaviour derived types like UIBehaviour that
            // were previous in a precompiled UnityEngine.UI.dll, but are now
            // compiled in a package.
            searchPaths.Add(InternalEditorUtility.GetEditorScriptAssembliesPath());

#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return searchPaths.ToArray();
#pragma warning restore UA2001
        }

        [RequiredByNativeCode]
        public static bool ExtractAllClassesThatAreUserExtendedScripts(string path, out string[] classNamesArray, out string[] classNameSpacesArray, out string[] movedFromNamespacesArray)
        {
            var typesDerivedFromMonoBehaviour = TypeCache.GetTypesDerivedFrom<MonoBehaviour>();
            var typesDerivedFromScriptableObject = TypeCache.GetTypesDerivedFrom<ScriptableObject>();
            var typesDerivedFromScriptedImporter = TypeCache.GetTypesDerivedFrom<ScriptedImporter>();

            var fileName = Path.GetFileName(path);
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            IEnumerable<Type> userTypes = typesDerivedFromMonoBehaviour.Where(x => Path.GetFileName(x.Assembly.GetLoadedAssemblyPath()) == fileName);
            userTypes = userTypes
                .Concat(typesDerivedFromScriptableObject.Where(x => Path.GetFileName(x.Assembly.GetLoadedAssemblyPath()) == fileName))
                .Concat(typesDerivedFromScriptedImporter.Where(x => Path.GetFileName(x.Assembly.GetLoadedAssemblyPath()) == fileName)).ToList();
#pragma warning restore UA2001

#pragma warning disable UA2005 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var userTypesCount = userTypes.Count();
#pragma warning restore UA2005
            List<string> classNames = new List<string>(userTypesCount);
            List<string> nameSpaces = new List<string>(userTypesCount);
            List<string> originalNamespaces = new List<string>(userTypesCount);

            string pathToAssembly = null;
            foreach (var userType in userTypes)
            {
                if (string.IsNullOrEmpty(pathToAssembly))
                    pathToAssembly = Path.GetFullPath(userType.Assembly.GetLoadedAssemblyPath());

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
                return Array.Empty<Type>();
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException)
            {
                return Array.Empty<Type>();
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
            return Array.Exists(ModuleUtils.GetAdditionalReferencesForUserScripts(), p => p.Equals(file));
        }
    }
}
