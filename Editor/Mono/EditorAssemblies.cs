// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor
{
    /// <summary>
    /// Marks a class as requiring eager initialization.
    ///
    /// Classes marked with this attribute will have their static constructors
    /// executed whenever assemblies are loaded or reloaded.
    ///
    /// Very useful for event (re)wiring.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class InitializeOnLoadAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class InitializeOnLoadMethodAttribute : Attribute
    {
    }

    /// <summary>
    /// Holds information about the current set of editor assemblies.
    /// </summary>
    static class EditorAssemblies
    {
        /// <summary>
        /// The currently loaded editor assemblies
        /// (This is kept up to date from <see cref="SetLoadedEditorAssemblies"/>)
        /// </summary>
        static internal Assembly[] loadedAssemblies
        {
            get; private set;
        }

        static internal IEnumerable<Type> loadedTypes
        {
            get { return loadedAssemblies.SelectMany(assembly => AssemblyHelper.GetTypesFromAssembly(assembly)); }
        }

        static internal IEnumerable<Type> SubclassesOf(Type parent)
        {
            return loadedTypes.Where(klass => klass.IsSubclassOf(parent));
        }

        static internal List<RuntimeInitializeClassInfo> m_RuntimeInitializeClassInfoList;
        static internal int m_TotalNumRuntimeInitializeMethods;

        [RequiredByNativeCode]
        private static void SetLoadedEditorAssemblies(Assembly[] assemblies)
        {
            loadedAssemblies = assemblies;
        }

        //this method finds all public classes that implement any of the passed in interface types
        static internal void FindClassesThatImplementAnyInterface(List<Type> results, params Type[] interfaces)
        {
            results.AddRange(loadedTypes.Where(x => interfaces.Any(i => i.IsAssignableFrom(x) && i != x)));
        }

        [RequiredByNativeCode]
        private static RuntimeInitializeClassInfo[] GetRuntimeInitializeClassInfos()
        {
            if (m_RuntimeInitializeClassInfoList == null)
                return null;
            return m_RuntimeInitializeClassInfoList.ToArray();
        }

        [RequiredByNativeCode]
        private static int GetTotalNumRuntimeInitializeMethods()
        {
            return m_TotalNumRuntimeInitializeMethods;
        }

        private static void StoreRuntimeInitializeClassInfo(Type type, List<string> methodNames, List<RuntimeInitializeLoadType> loadTypes)
        {
            RuntimeInitializeClassInfo classInfo = new RuntimeInitializeClassInfo();
            classInfo.assemblyName = type.Assembly.GetName().Name.ToString();
            classInfo.className = (string)type.ToString();
            classInfo.methodNames = methodNames.ToArray();
            classInfo.loadTypes = loadTypes.ToArray();
            m_RuntimeInitializeClassInfoList.Add(classInfo);
            m_TotalNumRuntimeInitializeMethods += methodNames.Count;
        }

        private static void ProcessStaticMethodAttributes(Type type)
        {
            List<string> runtimeInitializeMethodNames = null;
            List<RuntimeInitializeLoadType> runtimeInitializeLoadTypes = null;
            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            for (int i =  0; i < methods.GetLength(0); i++)
            {
                MethodInfo mi = methods[i];
                if (Attribute.IsDefined(mi, typeof(RuntimeInitializeOnLoadMethodAttribute)))
                {
                    RuntimeInitializeLoadType loadType = RuntimeInitializeLoadType.AfterSceneLoad;
                    object[] attrs = mi.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attrs != null && attrs.Length > 0)
                        loadType = ((RuntimeInitializeOnLoadMethodAttribute)attrs[0]).loadType;

                    if (runtimeInitializeMethodNames == null)
                    {
                        runtimeInitializeMethodNames = new List<string>();
                        runtimeInitializeLoadTypes = new List<RuntimeInitializeLoadType>();
                    }
                    runtimeInitializeMethodNames.Add(mi.Name);
                    runtimeInitializeLoadTypes.Add(loadType);
                }
                if (Attribute.IsDefined(mi, typeof(InitializeOnLoadMethodAttribute)))
                {
                    try
                    {
                        mi.Invoke(null, null);
                    }
                    catch (TargetInvocationException x)
                    {
                        Debug.LogError(x.InnerException);
                    }
                }
            }
            if (runtimeInitializeMethodNames != null)
                StoreRuntimeInitializeClassInfo(type, runtimeInitializeMethodNames, runtimeInitializeLoadTypes);
        }

        private static void ProcessEditorInitializeOnLoad(Type type)
        {
            try
            {
                RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            }
            catch (TypeInitializationException x)
            {
                Debug.LogError(x.InnerException);
            }
        }

        [RequiredByNativeCode]
        private static int[] ProcessInitializeOnLoadAttributes()
        {
            List<int> failedAssemblies = null;
            Assembly[] assemblies = loadedAssemblies;
            m_TotalNumRuntimeInitializeMethods = 0;
            m_RuntimeInitializeClassInfoList = new List<RuntimeInitializeClassInfo>();
            for (int ass = 0; ass < assemblies.Length; ++ass)
            {
                int oldTotalNumRuntimeInitializeMethods = m_TotalNumRuntimeInitializeMethods;
                int oldRuntimeInitializeClassInfoListCount = m_RuntimeInitializeClassInfoList.Count;
                try
                {
                    Type[] types = AssemblyHelper.GetTypesFromAssembly(assemblies[ass]);
                    foreach (var type in types)
                    {
                        if (type.IsDefined(typeof(InitializeOnLoadAttribute), false))
                        {
                            ProcessEditorInitializeOnLoad(type);
                        }
                        ProcessStaticMethodAttributes(type);
                    }
                }
                catch (Exception x)
                {
                    Debug.LogException(x);
                    if (failedAssemblies == null)
                        failedAssemblies = new List<int>();
                    if (oldTotalNumRuntimeInitializeMethods != m_TotalNumRuntimeInitializeMethods)
                        m_TotalNumRuntimeInitializeMethods = oldTotalNumRuntimeInitializeMethods;
                    if (oldRuntimeInitializeClassInfoListCount != m_RuntimeInitializeClassInfoList.Count)
                        m_RuntimeInitializeClassInfoList.RemoveRange(oldRuntimeInitializeClassInfoListCount, m_RuntimeInitializeClassInfoList.Count - oldRuntimeInitializeClassInfoListCount);
                    failedAssemblies.Add(ass);
                }
            }
            if (failedAssemblies == null)
                return null;
            return failedAssemblies.ToArray();
        }
    }
}
