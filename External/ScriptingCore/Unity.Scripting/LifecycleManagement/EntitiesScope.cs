namespace Unity.Scripting.LifecycleManagement
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class EntitiesInitializeAttribute : LifecycleAttributeBase
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class EntitiesCleanupAttribute : LifecycleAttributeBase
    {
    }

    /// <summary>
    /// Entities state that lives for one code load and is shared by every World, e.g. the type registry.
    /// Entered after BurstScope and before [OnCodeLoaded], as an implicit outer scope of CodeLoadedScope.
    /// </summary>
    internal sealed class EntitiesScope : ImplicitLifecycleScope
    {
        // Scope singleton: part of the scope hierarchy itself, referenced from other scopes' outer-scope
        // lists, and must outlive every scope transition.
        [NoAutoStaticsCleanup]
        public static EntitiesScope Instance { get; } = new();

        private EntitiesScope() : base(nameof(EntitiesScope))
        {
            ExplicitRequiredOuterScopes.Add(nameof(BurstScope));
        }

        protected override void Enter(ScopeTransitionHelper scopeTransitionHelper)
        {
            scopeTransitionHelper.ExecuteMethodsInOrder<EntitiesInitializeAttribute>();
        }

        protected override void Exit(ScopeTransitionHelper scopeTransitionHelper)
        {
            scopeTransitionHelper.ExecuteMethodsInReverseOrder<EntitiesCleanupAttribute>();
        }
    }
}
