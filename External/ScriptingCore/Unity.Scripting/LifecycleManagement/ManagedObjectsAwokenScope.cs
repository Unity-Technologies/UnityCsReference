using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DomainReload-editor")]
[assembly: InternalsVisibleTo("Assembly-CSharp-testable")]

namespace Unity.Scripting.LifecycleManagement
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class AfterManagedObjectsAwokenAttribute : LifecycleAttributeBase
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class BeforeManagedObjectsDisabledAttribute : LifecycleAttributeBase
    {
    }
    internal sealed class ManagedObjectsAwokenScope : LifecycleScope
    {
        public static readonly string ScopeName = "ManagedObjectsAwoken";
        public ManagedObjectsAwokenScope() : base(ScopeName)
        {
            ExplicitRequiredOuterScopes.Add(CodeInitializedScope.ScopeName);
        }
        protected override void Enter(ScopeTransitionHelper scopeTransitionHelper)
        {
            scopeTransitionHelper.ExecuteMethodsInOrder<AfterManagedObjectsAwokenAttribute>();
        }

        protected override void Exit(ScopeTransitionHelper scopeTransitionHelper)
        {
            scopeTransitionHelper.ExecuteMethodsInReverseOrder<BeforeManagedObjectsDisabledAttribute>();
        }
    }
}
