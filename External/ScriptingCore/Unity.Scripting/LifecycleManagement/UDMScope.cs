using UnityEngine.Scripting;
using PreserveAttribute = Unity.Private.Scripting.PreserveAttribute;

namespace Unity.Scripting.LifecycleManagement
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class UDMInitializeAttribute : LifecycleAttributeBase
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class UDMCleanupAttribute : LifecycleAttributeBase
    {
    }

    internal sealed class UDMScope : ImplicitLifecycleScope
    {
        public static UDMScope Instance { get; } = new UDMScope();

        private UDMScope() : base(nameof(UDMScope))
        {
        }

        protected override void Enter(ScopeTransitionHelper scopeTransitionHelper)
        {
            scopeTransitionHelper.ExecuteMethodsInOrder<UDMInitializeAttribute>();
        }

        protected override void Exit(ScopeTransitionHelper scopeTransitionHelper)
        {
            scopeTransitionHelper.ExecuteMethodsInReverseOrder<UDMCleanupAttribute>();
        }
    }
}
