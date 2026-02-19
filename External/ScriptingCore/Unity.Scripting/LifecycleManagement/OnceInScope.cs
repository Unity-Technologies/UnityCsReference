namespace Unity.Scripting.LifecycleManagement
{
    /// <summary>
    /// Provides a way to run an action only once within a specified scope.
    /// </summary>
    internal static class OnceInScope
    {
        /// <summary>
        /// Return an action wrapping the provided lambda so that it will only execute only once
        /// within the specified scope even when called multiple times.
        /// </summary>
        public static Action OnceIn(Action todo, Type scopeType)
        {
            return new OnceInScopeCleanup(todo, scopeType).Run;
        }

        /// <summary>
        /// Return an action wrapping the provided lambda so that it will only execute only once
        /// within the specified scope even when called multiple times.
        /// </summary>
        public static Action OnceIn<ScopeType>(Action todo) => OnceIn(todo, typeof(ScopeType));

        /// <summary>
        /// Return an action wrapping the provided lambda so that it will only execute only once
        /// within a code loaded scope even when called multiple times.
        /// </summary>
        public static Action OnceInCodeLoaded(Action todo) => OnceIn(todo, typeof(CodeLoadedScope));

        class OnceInScopeCleanup : CodeGen.ClassAutoCleanup
        {
            private readonly Action _toRun;
            private object _lock = new object();
            private bool _hasRun = false;
            override public void Cleanup()
            {
                lock (_lock)
                {
                    _hasRun = false;
                }
            }
            public void Run()
            {
                lock (_lock)
                {
                    if (_hasRun)
                    {
                        return;
                    }
                    _hasRun = true;
                }
                _toRun();
            }


            public OnceInScopeCleanup(Action toRun, Type scopeType) : base(scopeType, ScopeTransitionType.Entering)
            {
                _toRun = toRun;
            }
        }
    }
}
