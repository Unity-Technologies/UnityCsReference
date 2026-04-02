namespace Unity.Scripting.LifecycleManagement
{
    [System.Diagnostics.DebuggerDisplay("Scope Name = {Name}")]
    internal abstract class LifecycleScopeBase
    {
        public string Name { get; private set; } // TODO: Make this obsolete once we are Net8 based
        protected List<string> ExplicitRequiredOuterScopes { get; private set; } // TODO: Change this to be type based instead of string based once we are Net8 based
        public IEnumerable<string> RequiredOuterScopes
        {
            get
            {
                foreach (var scope in ExplicitRequiredOuterScopes)
                {
                    yield return scope;
                }
                foreach (var scope in ImplicitOuterScopes)
                {
                    yield return scope.Name;
                }
            }
        }
        public bool AllowNestedTransitions { get; set; }

        /// <summary>
        /// Outer scopes that will be implicitly entered when this scope is entered, and exited when this scope is exited.
        /// </summary>
        public virtual ImplicitLifecycleScope[] ImplicitOuterScopes { get; } = Array.Empty<ImplicitLifecycleScope>();

        public LifecycleScopeBase(string name)
        {
            Name = name;
            ExplicitRequiredOuterScopes = new List<string>();
        }

        public bool MustBeNestedInsideScope(string scopeName)
        {
            return ExplicitRequiredOuterScopes.Contains(scopeName) || (Array.IndexOf(ImplicitOuterScopes, scopeName) != -1);
        }

        protected abstract void Enter(ScopeTransitionHelper scopeTransitionHelper);

        protected abstract void Exit(ScopeTransitionHelper scopeTransitionHelper);

        internal void OnEnter(ScopeTransitionHelper scopeTransitionHelper)
        {
            DebugLifecycle.Log($"Lifecycle : Entering scope '{Name}'");
            Enter(scopeTransitionHelper);
            DebugLifecycle.Log($"Lifecycle : Entered scope '{Name}'");
        }

        internal void OnExit(ScopeTransitionHelper scopeTransitionHelper)
        {
            DebugLifecycle.Log($"Lifecycle : Exiting scope '{Name}'");
            Exit(scopeTransitionHelper);
            DebugLifecycle.Log($"Lifecycle : Exited scope '{Name}'");
        }
    }
}
