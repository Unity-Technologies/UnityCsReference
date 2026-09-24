using System.Runtime.CompilerServices;
using PreserveAttribute = Unity.Private.Scripting.PreserveAttribute;

[assembly: InternalsVisibleTo("Unity.ScriptingTests.CodeLoadedGeneration")]
[assembly: InternalsVisibleTo("DomainReload-editor")]
namespace Unity.Scripting.LifecycleManagement
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class OnCodeLoadedAttribute : LifecycleAttributeBase
    {
        public OnCodeLoadedAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class OnCodeUnloadingAttribute : LifecycleAttributeBase
    {
        public OnCodeUnloadingAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ClearCacheBetweenCodeLoadsAttribute : LifecycleAttributeBase
    {
    }

    internal sealed class CodeLoadedScope : LifecycleScope
    {
        public static readonly string ScopeName = "CodeLoaded";

        // Callers compare their own captured generation against this to detect stale async work.
        private static int _codeLoadedGeneration;
        public static int CurrentCodeLoadedGeneration => _codeLoadedGeneration;

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
            return ++_codeLoadedGeneration;
        }

        // Generation should be captured by the caller when the async work started.
        public static void CancelIfNotInGeneration(int generation)
        {
            if (generation != _codeLoadedGeneration)
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
