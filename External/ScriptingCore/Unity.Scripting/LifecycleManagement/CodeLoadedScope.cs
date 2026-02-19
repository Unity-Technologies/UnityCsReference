using System.Runtime.CompilerServices;
using UnityEngine.Scripting;

[assembly: InternalsVisibleTo("Unity.ScriptingTests.CodeLoadedGeneration")]
namespace Unity.Scripting.LifecycleManagement
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class OnCodeLoadedAttribute : LifecycleAttributeBase
    {
        public OnCodeLoadedAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class OnCodeUnloadingAttribute : LifecycleAttributeBase
    {
        public OnCodeUnloadingAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class ClearCacheBetweenCodeLoadsAttribute : LifecycleAttributeBase
    {
    }

    internal sealed class CodeLoadedScope : LifecycleScope
    {
        public static readonly string ScopeName = "CodeLoaded";
        private static int _codeLoadedGeneration;
        static AsyncLocal<int> _executionContextGeneration = new AsyncLocal<int>();
        public static int CurrentCodeLoadedGeneration => _codeLoadedGeneration;
        public static int ExecutionContextGeneration => _executionContextGeneration.Value;

        private static CancellationTokenSource? _cancellationTokenSource;

        /// <summary>
        /// Get this scope cancellation token:
        /// - when scope is active, cancellation will happen just after running the OnCodeUnloadingAttribute callbacks
        /// - when scope is inactive, cancellation will happen just before running the OnCodeLoadedAttribute callbacks 
        /// </summary>
        public static CancellationToken CancellationToken => (_cancellationTokenSource ??= new CancellationTokenSource()).Token;

        // Making this internal to allow selected editor tests to validate code loaded generation checks
        internal static int IncrementCodeLoadedGeneration()
        {
            RecycleCancellationTokenSource();
            var generation = ++_codeLoadedGeneration;
            _executionContextGeneration.Value = generation;
            return generation;
        }

        public static void CancelIfNotInCorrectGeneration()
        {

            if (_executionContextGeneration.Value != _codeLoadedGeneration)
            {
                throw new OperationCanceledException(
                    "Running async code belongs to the previous CodeLoaded scope and must be cancelled before Code Reload");

            }
        }

        public override ImplicitLifecycleScope[] ImplicitOuterScopes { get; } = new ImplicitLifecycleScope[] { BurstScope.Instance, UDMScope.Instance };

        public CodeLoadedScope() : base(ScopeName)
        {
            ExplicitRequiredOuterScopes.Add(AssemblyLoadedScopeBase.ScopeName);
        }

        protected override void Enter(ScopeTransitionHelper scopeTransitionHelper)
        {
            // before running the callbacks, we update the generation and current cancellation token, so that the callbacks can leverage them as if they were "part of the scope"
            IncrementCodeLoadedGeneration();
            scopeTransitionHelper.ExecuteMethodsInOrder<OnCodeLoadedAttribute>();
        }

        protected override void Exit(ScopeTransitionHelper scopeTransitionHelper)
        {
            // before running the callbacks, we update the generation and current cancellation token, so that the callbacks can leverage them as if they were "out of the scope"
            IncrementCodeLoadedGeneration();
            scopeTransitionHelper.ExecuteMethodsInReverseOrder<OnCodeUnloadingAttribute>();
        }

        static void RecycleCancellationTokenSource()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = null;
            }
        }
    }
}
