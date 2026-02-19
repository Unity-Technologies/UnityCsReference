using System.Reflection;

namespace Unity.Scripting.LifecycleManagement;

internal sealed class AssemblyLoadedScopeIl2Cpp : AssemblyLoadedScopeBase
{
    public AssemblyLoadedScopeIl2Cpp(IReadOnlyList<Assembly> assemblies)
        : base(assemblies)
    {
    }
}
