using System.Collections.Generic;

namespace Unity.Scripting.LifecycleManagement
{
    internal abstract class LifecycleScope : LifecycleScopeBase
    {
        protected LifecycleScope(string name) : base(name) { }
    }
}
