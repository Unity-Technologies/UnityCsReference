// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor.Profiling;
using UnityEngine;
using System.Text;
using Unity.Profiling;
using UnityEngine.Scripting;
using Debug = UnityEngine.Debug;
using UnityEngine.Bindings;
using Unity.Scripting.LifecycleManagement;

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

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Method)]
    public class InitializeOnEnterPlayModeAttribute : Attribute
    {
    }

    /// <summary>
    /// Holds information about the current set of editor assemblies.
    /// </summary>
    static partial class EditorAssemblies
    {
        static Dictionary<Type, Type[]> m_subClasses = new Dictionary<Type, Type[]>();

        /// <summary>
        /// The same set of assemblies as <see cref="loadedAssemblies"/>, but
        /// sorted topologically according to each assembly's assembly references.
        /// </summary>
        private static Assembly[] m_topologicallySortedAssemblies;

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
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            get { return loadedAssemblies.SelectMany(assembly => AssemblyHelper.GetTypesFromAssembly(assembly)); }
#pragma warning restore UA2001
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

            m_topologicallySortedAssemblies = AssemblyHelper.TopologicalSort(loadedAssemblies);

            ValidateSourceGenerators(assemblies);
        }

        /// <summary>
        /// Ensure that, where applicable, source generators that are required to be referenced
        /// have in fact been referenced.
        /// </summary>
        private static void ValidateSourceGenerators(Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                var lifecycleMethods = TypeCache.GetMethodsWithAttribute<LifecycleAttributeBase>(assembly.GetName().Name);

                if (lifecycleMethods.Count > 0 && !LifecycleController.Instance.HasInitializationMethod(assembly))
                {
                    var firstLifecycleMethod = lifecycleMethods[0];
                    var firstLifecycleMethodName = $"{firstLifecycleMethod.DeclaringType.ToString()}.{firstLifecycleMethod.Name}";

                    Debug.LogError($"Assembly '{assembly.GetName().Name}' contains lifecycle methods (e.g. '{firstLifecycleMethodName}') but does not reference 'Unity.Analyzers.Common.dll'. To fix this, add a reference to 'Unity.Analyzers.Common.dll' when compiling this assembly.");
                }
            }
        }

        static ProfilerMarkerWithStringData _profilerMarkerProcessInitializeOnLoadAttributes = ProfilerMarkerWithStringData.Create("ProcessInitializeOnLoadAttribute", "Type");
        static ProfilerMarkerWithStringData _profilerMarkerProcessInitializeOnLoadMethodAttributes = ProfilerMarkerWithStringData.Create("ProcessInitializeOnLoadMethodAttribute", "MethodInfo");
        private static readonly ProfilerMarker _profilerMarkerSortTypes = new ProfilerMarker("SortTypesTopologically");

        [RequiredByNativeCode]
        private static void ProcessInitializeOnLoadAttributes(ReadOnlySpan<IntPtr> typeHandles)
        {
            if (typeHandles.Length == 0)
                return;

            var types = SystemReflectionMarshalling.UnmarshalSystemTypes(typeHandles);

            bool reportTimes = (bool)Debug.GetDiagnosticSwitch("EnableDomainReloadTimings").value;

            IEnumerable<Type> sortedTypes;
            using (_profilerMarkerSortTypes.Auto())
            {
                // Sort types according to topologically-sorted assemblies, such that we guarantee that
                // [InitializeOnLoad] classes in assemblies referenced by a given assembly will have been
                // initialized prior to that assembly's own [InitializeOnLoad] classes.
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                sortedTypes = types.OrderBy(x => Array.IndexOf(m_topologicallySortedAssemblies, x.Assembly));
#pragma warning restore UA2001
            }

            using var scope = new ProgressScope("Running managed callbacks", "Initializing InitializeOnLoad Types", forceUpdate: true);

            foreach (Type type in sortedTypes)
            {
                using (_profilerMarkerProcessInitializeOnLoadAttributes.Auto(reportTimes,
                           () => type.AssemblyQualifiedName))
                {
                    var typeFullName = type?.FullName;
                    try
                    {
                        RuntimeHelpers.RunClassConstructor(type.TypeHandle);
                    }
                    catch (TypeLoadException x)
                    {
                        Debug.LogError(x.InnerException);
                    }
                    catch (TypeInitializationException x)
                    {
                        Debug.LogError(x.InnerException);
                    }
                }
            }
        }

        [RequiredByNativeCode]
        private static void ProcessInitializeOnLoadMethodAttributes()
        {
            bool reportTimes = (bool)Debug.GetDiagnosticSwitch("EnableDomainReloadTimings").value;
            var methods = TypeCache.GetMethodsWithAttribute<InitializeOnLoadMethodAttribute>();
            if (methods.Count == 0) return;

            using var scope = new ProgressScope("Running managed callbacks", "Processing InitializeOnLoadMethod Attributes", forceUpdate: true);
            foreach (var method in methods)
            {
                using (_profilerMarkerProcessInitializeOnLoadMethodAttributes.Auto(reportTimes,
                           () => $"{method.DeclaringType?.FullName}::{method.Name}"))
                {
                    try
                    {
                        method.Invoke(null, null);
                    }
                    catch (Exception x)
                    {
                        Debug.LogError($"Exception while executing InitializeOnLoad for {method.DeclaringType?.Name}.{method.Name}");
                        Debug.LogException(x);
                    }
                }
            }
        }
    }
}
