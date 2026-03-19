using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DomainReload-editor")]
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
