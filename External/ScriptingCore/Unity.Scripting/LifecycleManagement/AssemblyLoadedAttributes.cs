using System;

namespace Unity.Scripting.LifecycleManagement
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class OnAssemblyLoadedAttribute : LifecycleAttributeBase
    {
        public OnAssemblyLoadedAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class OnAssemblyUnloadingAttribute : LifecycleAttributeBase
    {
        public OnAssemblyUnloadingAttribute() { }
    }
}
