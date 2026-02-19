using System;
using System.Collections.Generic;
using System.Text;

namespace Unity.Scripting.LifecycleManagement.CodeGen
{
    /// <summary>
    /// This class is not supposed to be used directly, but is supporting the codegen for the AutoStaticsCleanupAttribute.
    /// It is public to allow the code generator to be able to create subclassed of it in user code.
    /// </summary>
    internal abstract class ClassAutoCleanup
    {
        public abstract void Cleanup();

        protected ClassAutoCleanup(Type scopeType, ScopeTransitionType cleanOn)
        {
            LifecycleController.Instance.RegisterAutoCleanup(this, scopeType, cleanOn);
        }
    }
}
