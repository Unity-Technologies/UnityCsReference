// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Profiling;
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
    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Class)]
    public class InitializeOnLoadAttribute : Attribute
    {
    }

    [RequiredByNativeCode]
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

        [RequiredByNativeCode]
        private static void SetLoadedEditorAssemblies(Assembly[] assemblies)
        {
            loadedAssemblies = assemblies;

            // clear cached subtype -> types when assemblies change
            m_subClasses.Clear();
        }

        [NonSerialized]
        static ProfilerMarker s_ProcessInitializeOnLoadAttribute = new ProfilerMarker("ProcessInitializeOnLoadAttribute");

        [NonSerialized]
        static ProfilerMarker s_ProcessInitializeOnLoadMethodAttribute = new ProfilerMarker("ProcessInitializeOnLoadMethodAttribute");

        [RequiredByNativeCode]
        private static void ProcessInitializeOnLoadAttributes(Type[] types, MethodInfo[] methods)
        {
            foreach (Type type in types)
            {
                using (s_ProcessInitializeOnLoadAttribute.Auto())
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
            }

            foreach (MethodInfo method in methods)
            {
                using (s_ProcessInitializeOnLoadMethodAttribute.Auto())
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
            }
        }
    }
}
