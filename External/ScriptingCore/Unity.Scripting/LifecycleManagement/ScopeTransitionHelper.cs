using System.Reflection;

namespace Unity.Scripting.LifecycleManagement
{
    /// <summary>
    /// Utility class that helps with executing scope transition hooks in the correct order.
    /// </summary>
    internal sealed class ScopeTransitionHelper
    {
        private static readonly string k_ProfilerMarkerPrefix = "LifeCycle.Process";
        private static readonly string k_DetailedInvokeMarkerPrefix = "LifeCycle.Invoke";

        /// <summary>
        /// If the EnableDomainReloadTimings diagnostic switch is enabled, then detailed profiling for every invoked method is enabled.
        /// </summary>
        private static bool EnableDetailedProfiling => Debug.IsDiagnosticSwitchEnabled("EnableDomainReloadTimings");

        private readonly StackOrderedAssemblyList _assemblyList = new();
        private readonly LifecycleMethodRegistry _lifecycleMethodRegistry;

        internal INativeCallbackProvider? NativeCallbackProvider { get; set; }

        /// <summary>
        /// Returns the list of all loaded assemblies in order of assembly reference, so every dependency of an assembly comes before that assembly. All assemblies within an ALC parent ALC comes before any assembly in a child ALC. Unrelated sets of assemblies is in an ALC is returned in an undefined order.
        /// </summary>
        public IReadOnlyList<Assembly> AllAssemblies => _assemblyList;

        public ScopeTransitionHelper(LifecycleMethodRegistry lifecycleMethodRegistry)
        {
            _lifecycleMethodRegistry = lifecycleMethodRegistry;
        }

        private List<LifecycleMethodData> FindStaticMethodsWithAttribute(Type attributeType, IReadOnlyList<Assembly> assemblies)
        {
            return _lifecycleMethodRegistry.Get(attributeType, assemblies);
        }

        /// <summary>
        /// Executes all static methods with the given attribute type in the given assemblies in the assembly order.
        /// </summary>
        /// <typeparam name="T">Attribute type that marks up the methods that should be executed</typeparam>
        /// <param name="assemblies">Ordered assembly list or null. If the value is null, then <see cref="AllAssemblies" /> list is used</param>
        /// <remarks>
        /// The exection of methods is visible in profiler. Each scope has a profiler marker with "LifeCycle.Process" prefix (e.g. "LifeCycle.ProcessOnAssemblyLoadedAttribute").
        /// When detailed profiling is enabled (diagnostic switch "EnableDomainReloadTimings"),
        /// each method invocation is wrapped in a separate profiler marker with "LifeCycle.Invoke" prefix (e.g. "LifeCycle.InvokeOnAssemblyLoadedAttribute") and string metadata with a full method name.
        /// </remarks>
        public void ExecuteMethodsInOrder<T>(ReadOnlyAssemblyList? assemblies = null)
        {
            ExecuteMethodsInOrder(typeof(T), assemblies ?? AllAssemblies);
        }

        private void ExecuteMethodsInOrder(Type attributeType, IReadOnlyList<Assembly> assemblies)
        {
            using var executeMethodsProfilerScope = new Profiling.ProfilerMarker(k_ProfilerMarkerPrefix + attributeType.Name).Auto();

            var methods = FindStaticMethodsWithAttribute(attributeType, assemblies);
            if (methods.Count == 0)
            {
                return;
            }

            DebugLifecycle.Log($"Lifecycle : *inside scope transition* executing {methods.Count} hooks for type {attributeType}");

            // Check if detailed profiling is enabled and create a marker which would wrap each method invocation
            Profiling.ProfilerMarker? detailedInvokeMarker = EnableDetailedProfiling ? new Profiling.ProfilerMarker(k_DetailedInvokeMarkerPrefix + attributeType.Name) : null;

            for (int i = 0; i < methods.Count; i++)
            {
                var method = methods[i];

                detailedInvokeMarker?.Begin(method.FullName);

                try
                {
                    method.Callback();
                }
                catch (Exception ex)
                {
                    DebugLifecycle.ReportError($"Lifecycle ERROR : Exception while executing scope transition handler {method.FullName}: {ex}", true);
                }

                detailedInvokeMarker?.End();
            }
        }

        /// <summary>
        /// Executes all static methods with the given attribute type in the given assemblies in the reverse assembly order.
        /// </summary>
        /// <typeparam name="T">Attribute type that marks up the methods that should be executed</typeparam>
        /// <param name="assemblies">Ordered assembly list or null. If the value is null, then <see cref="AllAssemblies" /> list is used</param>
        /// <remarks>
        /// The exection of methods is visible in profiler. Each scope has a profiler marker with "LifeCycle.Process" prefix (e.g. "LifeCycle.ProcessOnAssemblyLoadedAttribute").
        /// When detailed profiling is enabled (diagnostic switch "EnableDomainReloadTimings"),
        /// each method invocation is wrapped in a separate profiler marker with "LifeCycle.Invoke" prefix (e.g. "LifeCycle.InvokeOnAssemblyLoadedAttribute") and string metadata with a full method name.
        /// </remarks>
        public void ExecuteMethodsInReverseOrder<T>(ReadOnlyAssemblyList? assemblies = null)
        {
            ExecuteMethodsInReverseOrder(typeof(T), assemblies ?? AllAssemblies);
        }

        private void ExecuteMethodsInReverseOrder(Type attributeType, IReadOnlyList<Assembly> assemblies)
        {
            using var executeMethodsProfilerScope = new Profiling.ProfilerMarker(k_ProfilerMarkerPrefix + attributeType.Name).Auto();

            var methods = FindStaticMethodsWithAttribute(attributeType, assemblies);
            if (methods.Count == 0)
            {
                return;
            }

            DebugLifecycle.Log($"Lifecycle : *inside scope transition* executing {methods.Count} hooks for type {attributeType} in reverse");

            // Check if detailed profiling is enabled and create a marker which would wrap each method invocation
            Profiling.ProfilerMarker? detailedInvokeMarker = EnableDetailedProfiling ? new Profiling.ProfilerMarker(k_DetailedInvokeMarkerPrefix + attributeType.Name) : null;

            for (int i = methods.Count - 1; i >= 0; i--)
            {
                var method = methods[i];

                detailedInvokeMarker?.Begin(method.FullName);

                try
                {
                    method.Callback();
                }
                catch (Exception ex)
                {
                    DebugLifecycle.ReportError($"Lifecycle ERROR : Exception while executing scope transition handler {method.FullName}: {ex}", true);
                }

                detailedInvokeMarker?.End();
            }
        }

        internal void PushStack(ReadOnlyAssemblyList assemblies)
        {
            _assemblyList.PushStack(assemblies);
        }

        internal void PopStack()
        {
            _assemblyList.PopStack();
        }
    }
}
