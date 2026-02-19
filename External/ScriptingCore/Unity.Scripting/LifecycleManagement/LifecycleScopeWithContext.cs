using System.Collections.Generic;

namespace Unity.Scripting.LifecycleManagement
{
    internal abstract class LifecycleScopeWithContext<T> : LifecycleScopeBase where T : class
    {
        protected LifecycleScopeWithContext(string name, T context)
            : base(name)
        {
            Context = context;
        }

        internal T Context { get; }
    }
}
