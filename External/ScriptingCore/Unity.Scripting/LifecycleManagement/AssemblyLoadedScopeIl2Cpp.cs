// Only Mono and IL2CPP enter this scope. Its sole consumer,
// Runtime/Scripting/LifecycleManagement/DomainReloadLifecycleController.cs, is itself
// #if !ENABLE_CORECLR, so this uses the same gate rather than a narrower ENABLE_IL2CPP one.
using System.Reflection;

namespace Unity.Scripting.LifecycleManagement;

internal sealed class AssemblyLoadedScopeIl2Cpp : AssemblyLoadedScopeBase
{
    public AssemblyLoadedScopeIl2Cpp(IReadOnlyList<Assembly> assemblies)
        : base(assemblies)
    {
    }
}
