namespace Unity.Scripting.LifecycleManagement
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class EntitiesSystemsInitializeAttribute : LifecycleAttributeBase
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class EntitiesSystemsCleanupAttribute : LifecycleAttributeBase
    {
    }

    /// <summary>
    /// Active while code is initialized and its assets can be loaded: entered after [OnCodeInitializing] and
    /// [InitializeOnLoad], before any restored object gets Awake/OnEnable. In the Editor, entry is also held back
    /// until the first Asset Database refresh has imported the project. Exited and re-entered with EntitiesWorldScope
    /// around play mode changes. Entered and exited by native code only.
    /// </summary>
    internal sealed class EntitiesSystemsScope : LifecycleScope
    {
        public static readonly string ScopeName = "EntitiesSystems";

        public EntitiesSystemsScope() : base(ScopeName)
        {
            ExplicitRequiredOuterScopes.Add(CodeInitializedScope.ScopeName);
            ExplicitRequiredOuterScopes.Add(EntitiesWorldScope.ScopeName);
        }

        protected override void Enter(ScopeTransitionHelper scopeTransitionHelper)
        {
            scopeTransitionHelper.ExecuteMethodsInOrder<EntitiesSystemsInitializeAttribute>();
        }

        protected override void Exit(ScopeTransitionHelper scopeTransitionHelper)
        {
            scopeTransitionHelper.ExecuteMethodsInReverseOrder<EntitiesSystemsCleanupAttribute>();
        }
    }
}
