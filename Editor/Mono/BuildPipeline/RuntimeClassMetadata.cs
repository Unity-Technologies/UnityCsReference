// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using UnityEditorInternal;
using System;


namespace UnityEditor
{
    /// <summary>
    ///  Registry for Unity native-managed class dependencies. Aimed to make native and managed code stripping possible.
    ///  Note: only UnityEngine.dll content is covered there.
    /// </summary>
    internal class RuntimeClassRegistry
    {
        protected BuildTarget buildTarget;
        protected HashSet<string> monoBaseClasses = new HashSet<string>();
        protected Dictionary<string, string[]> m_UsedTypesPerUserAssembly = new Dictionary<string, string[]>();
        protected Dictionary<int, List<string>> classScenes = new Dictionary<int, List<string>>();
        protected UnityType objectUnityType = null;

        public Dictionary<string, string[]> UsedTypePerUserAssembly
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
            m_UsedTypesPerUserAssembly[assemblyName] = typeNames;
        }

        public bool IsDLLUsed(string dll)
        {
            if (m_UsedTypesPerUserAssembly == null)
                return true;
            if (Array.IndexOf(CodeStrippingUtils.UserAssemblies, dll) != -1)
                return true;
            return m_UsedTypesPerUserAssembly.ContainsKey(dll);
        }

        protected void AddManagedBaseClass(string className)
        {
            monoBaseClasses.Add(className);
        }

        protected void AddNativeClassFromName(string className)
        {
            if (objectUnityType == null)
                objectUnityType = UnityType.FindTypeByName("Object");

            var t = UnityType.FindTypeByName(className);

            ////System.Console.WriteLine("Looking for name {1}  ID {0}", classID, className);
            if (t != null && t.persistentTypeID != objectUnityType.persistentTypeID)
                allNativeClasses[t.persistentTypeID] = className;
        }

        public List<string> GetAllNativeClassesIncludingManagersAsString()
        {
            return new List<string>(allNativeClasses.Values);
        }

        public List<string> GetAllManagedBaseClassesAsString()
        {
            return new List<string>(monoBaseClasses);
        }

        public static RuntimeClassRegistry Create()
        {
            return new RuntimeClassRegistry();
        }

        public void Initialize(int[] nativeClassIDs, BuildTarget buildTarget)
        {
            this.buildTarget = buildTarget;
            InitRuntimeClassRegistry();
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

        internal class MethodDescription
        {
            public string assembly;
            public string fullTypeName;
            public string methodName;
        }

        internal List<MethodDescription> m_MethodsToPreserve = new List<MethodDescription>();

        //invoked by native code
        internal void AddMethodToPreserve(string assembly, string @namespace, string klassName, string methodName)
        {
            m_MethodsToPreserve.Add(new MethodDescription()
            {
                assembly = assembly,
                fullTypeName = @namespace + (@namespace.Length > 0 ? "." : "") + klassName,
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

        protected void InitRuntimeClassRegistry()
        {
            BuildTargetGroup group = UnityEditor.BuildPipeline.GetBuildTargetGroup(buildTarget);

            // Don't strip any classes which extend the following base classes
            AddManagedBaseClass("UnityEngine.MonoBehaviour");
            AddManagedBaseClass("UnityEngine.ScriptableObject");

            if (group == BuildTargetGroup.Android)
            {
                AddManagedBaseClass("UnityEngine.AndroidJavaProxy");
            }

            string[] runtimeLoadDontStripClassNames = RuntimeInitializeOnLoadManager.dontStripClassNames;
            foreach (string kclass in runtimeLoadDontStripClassNames)
                AddManagedBaseClass(kclass);
        }
    }
}
