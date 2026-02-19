using System;
using System.Collections.Generic;
using System.Text;

namespace Unity.Scripting.LifecycleManagement
{
    internal abstract class ImplicitLifecycleScope : LifecycleScope
    {
        protected ImplicitLifecycleScope(string name) : base(name) { }
    }
}
