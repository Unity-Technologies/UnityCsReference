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
    static partial class EditorAssemblies
    {
        static Dictionary<Type, Type[]> m_subClasses = new Dictionary<Type, Type[]>();

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
            return parent.IsInterface ?
                GetAllTypesWithInterface(parent) :
                SubclassesOfClass(parent);
        }

        static internal IEnumerable<Type> SubclassesOfClass(Type parent)
        {
            Type[] types;
            if (!m_subClasses.TryGetValue(parent, out types))
            {
                types = loadedTypes.Where(klass => klass.IsSubclassOf(parent)).ToArray();
                m_subClasses[parent] = types;
            }
            return types;
        }

        static internal IEnumerable<Type> SubclassesOfGenericType(Type genericType)
        {
            return loadedTypes.Where(klass => IsSubclassOfGenericType(klass, genericType));
        }

        private static bool IsSubclassOfGenericType(Type klass, Type genericType)
        {
            if (klass.IsGenericType && klass.GetGenericTypeDefinition() == genericType)
                return false;

            for (klass = klass.BaseType; klass != null; klass = klass.BaseType)
            {
                if (klass.IsGenericType && klass.GetGenericTypeDefinition() == genericType)
                    return true;
            }

            return false;
        }

        static internal List<RuntimeInitializeClassInfo> m_RuntimeInitializeClassInfoList;
        static internal int m_TotalNumRuntimeInitializeMethods;

        [RequiredByNativeCode]
        private static void SetLoadedEditorAssemblies(Assembly[] assemblies)
        {
            loadedAssemblies = assemblies;

            // clear cached subtype -> types when assemblies change
            m_subClasses.Clear();
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

        private static void ProcessRuntimeInitializeOnLoad(MethodInfo method)
        {
            RuntimeInitializeLoadType loadType = RuntimeInitializeLoadType.AfterSceneLoad;

            object[] attrs = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
            if (attrs != null && attrs.Length > 0)
                loadType = ((RuntimeInitializeOnLoadMethodAttribute)attrs[0]).loadType;

            StoreRuntimeInitializeClassInfo(method.DeclaringType, new List<string>() { method.Name }, new List<RuntimeInitializeLoadType>() { loadType });
        }

        private static void ProcessInitializeOnLoadMethod(MethodInfo method)
        {
            try
            {
                method.Invoke(null, null);
            }
            catch (TargetInvocationException x)
            {
                Debug.LogError(x.InnerException);
            }
        }

        [RequiredByNativeCode]
        private static int[] ProcessInitializeOnLoadAttributes()
        {
            const BindingFlags kStaticMethodFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

            m_TotalNumRuntimeInitializeMethods = 0;
            m_RuntimeInitializeClassInfoList = new List<RuntimeInitializeClassInfo>();

            foreach (Type type in GetAllTypesWithAttribute<InitializeOnLoadAttribute>())
                ProcessEditorInitializeOnLoad(type);
            foreach (MethodInfo method in GetAllMethodsWithAttribute<RuntimeInitializeOnLoadMethodAttribute>(kStaticMethodFlags))
                ProcessRuntimeInitializeOnLoad(method);
            foreach (MethodInfo method in GetAllMethodsWithAttribute<InitializeOnLoadMethodAttribute>(kStaticMethodFlags))
                ProcessInitializeOnLoadMethod(method);

            return null;
        }
    }
}
