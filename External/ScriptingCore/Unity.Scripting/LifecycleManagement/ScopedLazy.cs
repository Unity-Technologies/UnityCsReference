namespace Unity.Scripting.LifecycleManagement
{
    /// <summary>
    /// A Lazy initialized value that is scoped to a lifecycle scope.
    /// </summary>
    /// <typeparam name="TValue">Type of the value to construct</typeparam>
    /// <typeparam name="TScope">Type of the scope</typeparam>
    internal sealed class ScopedLazy<TValue, TScope> : CodeGen.ClassAutoCleanup
        where TValue : class
        where TScope : LifecycleScopeBase
    {
        private TValue? _data;
        private readonly Func<TValue> _factory;
        private readonly bool _checkScopeActive;

        /// <summary>
        /// Construct a ScopedLazy with a factory function.
        /// </summary>
        public ScopedLazy(Func<TValue> factory, bool checkScopeActive = true) : base(typeof(TScope), ScopeTransitionType.Exiting)
        {
            _factory = factory;
            _checkScopeActive = checkScopeActive;
        }

        /// <summary>
        /// Construct a ScopedLazy with a default constructor.
        /// </summary>
        public ScopedLazy(bool checkScopeActive = true) : this(() => Activator.CreateInstance<TValue>(), checkScopeActive) { }

        /// <summary>
        /// Do not call explicitly, this is called by the lifecycle controller.
        /// </summary>
        public override void Cleanup()
        {
            _data = null;
        }

        /// <summary>
        /// Get the lazily initialized value.
        /// </summary>
        public TValue Value
        {
            get
            {
                if (_data == null)
                {
                    // Will get enabled in SCP-1514
                    //if (_checkScopeActive)
                    //{
                    //    if (!LifecycleController.Instance.IsScopePresent<TScope>())
                    //    {
                    //        throw new InvalidOperationException($"Cannot access ScopedLazy<{typeof(TValue).Name}> outside of scope {typeof(TScope).Name}");
                    //    }
                    //}
                    _data = _factory();
                }
                return _data;
            }
        }
    }
}
