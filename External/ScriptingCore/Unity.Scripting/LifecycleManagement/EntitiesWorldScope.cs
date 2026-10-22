namespace Unity.Scripting.LifecycleManagement
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class EntitiesWorldInitializeAttribute : LifecycleAttributeBase
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class EntitiesWorldCleanupAttribute : LifecycleAttributeBase
    {
    }

    /// <summary>
    /// One Entities World: entered right after CodeLoaded and exited right before it. In the Editor it is also
    /// exited and re-entered around play mode changes, so the Editor World and the play mode World are separate.
    /// Entered and exited by native code only.
    /// </summary>
    internal sealed class EntitiesWorldScope : LifecycleScope
    {
        public static readonly string ScopeName = "EntitiesWorld";

        public EntitiesWorldScope() : base(ScopeName)
        {
            ExplicitRequiredOuterScopes.Add(CodeLoadedScope.ScopeName);
            ExplicitRequiredOuterScopes.Add(nameof(EntitiesScope));
        }

        protected override void Enter(ScopeTransitionHelper scopeTransitionHelper)
        {
            scopeTransitionHelper.ExecuteMethodsInOrder<EntitiesWorldInitializeAttribute>();
        }

        protected override void Exit(ScopeTransitionHelper scopeTransitionHelper)
        {
            scopeTransitionHelper.ExecuteMethodsInReverseOrder<EntitiesWorldCleanupAttribute>();
        }
    }
}
