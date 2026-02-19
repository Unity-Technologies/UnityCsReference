using System;
using System.Runtime.CompilerServices;
using Unity.Scripting;

[assembly: InternalsVisibleTo("DomainReload-editor")]
[assembly: InternalsVisibleTo("LifecycleTestAssembly1")]
[assembly: InternalsVisibleTo("LifecycleTestAssembly2")]
[assembly: InternalsVisibleTo("LifecycleTestAssembly3")]
[assembly: InternalsVisibleTo("LifecycleTestAssembly4")]
[assembly: InternalsVisibleTo("Assembly-CSharp-testable")]

namespace Unity.Scripting.LifecycleManagement
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class OnCodeInitializingAttribute : LifecycleAttributeBase
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class OnCodeDeinitializingAttribute : LifecycleAttributeBase
    {
    }

    internal sealed class CodeInitializedScope : LifecycleScope
    {
        public static readonly string ScopeName = "CodeInitialized";

        public CodeInitializedScope() : base(ScopeName)
        {
            ExplicitRequiredOuterScopes.Add(CodeLoadedScope.ScopeName);
        }

        protected override void Enter(ScopeTransitionHelper scopeTransitionHelper)
        {
            scopeTransitionHelper.ExecuteMethodsInOrder<OnCodeInitializingAttribute>();
        }

        protected override void Exit(ScopeTransitionHelper scopeTransitionHelper)
        {
            scopeTransitionHelper.ExecuteMethodsInReverseOrder<OnCodeDeinitializingAttribute>();
        }
    }
}
