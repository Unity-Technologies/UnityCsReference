// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor.Compilation;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine.Scripting;

namespace UnityEditor
{
    /// <summary>
    ///  Registry for Unity native-managed class dependencies. Aimed to make native and managed code stripping possible.
    ///  Note: only UnityEngine.dll content is covered there.
    /// </summary>
    internal class RuntimeClassRegistry
    {
        protected Dictionary<string, HashSet<string>> serializedClassesPerAssembly = new Dictionary<string, HashSet<string>>();
        protected Dictionary<string, HashSet<string>> m_UsedTypesPerUserAssembly = new Dictionary<string, HashSet<string>>();
        protected Dictionary<int, List<string>> classScenes = new Dictionary<int, List<string>>();
        protected UnityType objectUnityType = null;

        public Dictionary<string, HashSet<string>> UsedTypePerUserAssembly
        {
            get { return m_UsedTypesPerUserAssembly; }
        }

        // Store all registered native classes (including managers) here.
        // This is a step in the refactor of native code stripping. RuntimeClassRegistry should only be carrying this information to post process scripts rather than doing so much.
        protected Dictionary<int, string> allNativeClasses = new Dictionary<int, string>();

        public List<string> GetScenesForClass(int ID)
        {
            if (!classScenes.ContainsKey(ID))
                return null;
            return classScenes[ID];
        }

        public void AddNativeClassID(int ID)
        {
            string className = UnityType.FindTypeByPersistentTypeID(ID).name;
            ////System.Console.WriteLine("Looking for ID {0} name {1} --> is manager? {2}", ID, className, functionalityGroups.ContainsValue(className));

            // Native class found
            if (className.Length > 0)
                allNativeClasses[ID] = className;
        }

        public void SetUsedTypesInUserAssembly(string[] typeNames, string assemblyName)
        {
            if (!m_UsedTypesPerUserAssembly.TryGetValue(assemblyName, out HashSet<string> types))
                m_UsedTypesPerUserAssembly[assemblyName] = types = new HashSet<string>();

            foreach (var typeName in typeNames)
                types.Add(typeName);
        }

        [RequiredByNativeCode]
        public void SetSerializedTypesInUserAssembly(string[] typeNames, string assemblyName)
        {
            if (!serializedClassesPerAssembly.TryGetValue(assemblyName, out HashSet<string> types))
                serializedClassesPerAssembly[assemblyName] = types = new HashSet<string>();

            foreach (var typeName in typeNames)
                types.Add(typeName);
        }

        public bool IsDLLUsed(string dll)
        {
            if (m_UsedTypesPerUserAssembly == null)
                return true;

            if (Array.IndexOf(CodeStrippingUtils.UserAssemblies, dll) != -1)
            {
                // Don't treat code in packages as used automatically (case 1003047).
                var asmdefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(dll);
                if (asmdefPath == null || !EditorCompilationInterface.Instance.IsPathInPackageDirectory(asmdefPath))
                    return true;
            }

            return m_UsedTypesPerUserAssembly.ContainsKey(dll);
        }

        protected void AddUsedClass(string assemblyName, string className)
        {
            if (string.IsNullOrEmpty(assemblyName))
                throw new ArgumentException(nameof(assemblyName));

            if (string.IsNullOrEmpty(className))
                throw new ArgumentException(nameof(className));

            if (!m_UsedTypesPerUserAssembly.TryGetValue(assemblyName, out HashSet<string> types))
                m_UsedTypesPerUserAssembly[assemblyName] = types = new HashSet<string>();

            types.Add(className);
        }

        protected void AddSerializedClass(string assemblyName, string className)
        {
            if (string.IsNullOrEmpty(assemblyName))
                throw new ArgumentException(nameof(assemblyName));

            if (string.IsNullOrEmpty(className))
                throw new ArgumentException(nameof(className));

            if (!serializedClassesPerAssembly.TryGetValue(assemblyName, out HashSet<string> types))
                serializedClassesPerAssembly[assemblyName] = types = new HashSet<string>();

            types.Add(className);
        }

        public void AddNativeClassFromName(string className)
        {
            if (objectUnityType == null)
                objectUnityType = UnityType.FindTypeByName("Object");

            var t = UnityType.FindTypeByName(className);

            ////System.Console.WriteLine("Looking for name {1}  ID {0}", classID, className);
            if (t != null && t.persistentTypeID != objectUnityType.persistentTypeID)
                allNativeClasses[t.persistentTypeID] = className;
        }

        public Dictionary<string, string[]> GetAllManagedTypesInScenes()
        {
            var items = new Dictionary<string, string[]>();

            // Use a hashset to remove duplicate types.
            // Duplicates of UnityEngine.Object will happen because native types without a managed type will come back as
            // UnityEngine.Object
            var engineModuleTypes = new HashSet<string>();
            foreach (var nativeClassID in allNativeClasses.Keys)
            {
                var managedName = RuntimeClassMetadataUtils.ScriptingWrapperTypeNameForNativeID(nativeClassID);

                if (string.IsNullOrEmpty(managedName))
                    continue;

                engineModuleTypes.Add(managedName);
            }

            items.Add("UnityEngine.dll", engineModuleTypes.ToArray());

            foreach (var userAssembly in m_UsedTypesPerUserAssembly)
                items.Add(userAssembly.Key, userAssembly.Value.ToArray());

            return items;
        }

        public List<string> GetAllNativeClassesIncludingManagersAsString()
        {
            return new List<string>(allNativeClasses.Values);
        }

        public IEnumerable<KeyValuePair<string, string[]>> GetAllSerializedClassesAsString()
        {
            foreach (var pair in serializedClassesPerAssembly)
            {
                yield return new KeyValuePair<string, string[]>(pair.Key, pair.Value.ToArray());
            }
        }

        [RequiredByNativeCode]
        public MethodDescription[] GetAllMethodsToPreserve()
        {
            return m_MethodsToPreserve.ToArray();
        }

        [RequiredByNativeCode]
        public string[] GetAllSerializedClassesAssemblies()
        {
            return serializedClassesPerAssembly.Keys.ToArray();
        }

        [RequiredByNativeCode]
        public string[] GetAllSerializedClassesForAssembly(string assembly)
        {
            return serializedClassesPerAssembly[assembly].ToArray();
        }

        public static RuntimeClassRegistry Create()
        {
            return new RuntimeClassRegistry();
        }

        public void Initialize(int[] nativeClassIDs)
        {
            foreach (int ID in nativeClassIDs)
                AddNativeClassID(ID);
        }

        public void SetSceneClasses(int[] nativeClassIDs, string scene)
        {
            foreach (int ID in nativeClassIDs)
            {
                AddNativeClassID(ID);
                if (!classScenes.ContainsKey(ID))
                    classScenes[ID] = new List<string>();
                classScenes[ID].Add(scene);
            }
        }

        // Needs to stay in sync with MethodDescription in Editor/Src/BuildPipeline/BuildSerialization.h
        [StructLayout(LayoutKind.Sequential)]
        internal class MethodDescription
        {
            public string assembly;
            public string fullTypeName;
            public string methodName;
        }

        internal List<MethodDescription> m_MethodsToPreserve = new List<MethodDescription>();

        //invoked by native code
        [RequiredByNativeCode]
        public void AddMethodToPreserve(string assembly, string ns, string klassName, string methodName)
        {
            m_MethodsToPreserve.Add(new MethodDescription()
            {
                assembly = assembly,
                fullTypeName = ns + (ns.Length > 0 ? "." : "") + klassName,
                methodName = methodName
            });
        }

        [RequiredByNativeCode]
        public void AddMethodToPreserveWithFullTypeName(string assembly, string fullTypeName, string methodName)
        {
            m_MethodsToPreserve.Add(new MethodDescription()
            {
                assembly = assembly,
                fullTypeName = fullTypeName,
                methodName = methodName
            });
        }

        internal List<MethodDescription> GetMethodsToPreserve()
        {
            return m_MethodsToPreserve;
        }

        internal List<string> m_UserAssemblies = new List<string>();

        //invoked by native code
        internal void AddUserAssembly(string assembly)
        {
            if (!m_UserAssemblies.Contains(assembly))
                m_UserAssemblies.Add(assembly);
        }

        internal string[] GetUserAssemblies()
        {
            return m_UserAssemblies.ToArray();
        }
    }
}
