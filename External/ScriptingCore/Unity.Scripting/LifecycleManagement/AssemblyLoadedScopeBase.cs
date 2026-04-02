using System.Reflection;

namespace Unity.Scripting.LifecycleManagement;

internal abstract class AssemblyLoadedScopeBase : LifecycleScopeWithContext<ReadOnlyAssemblyList>
{
    public const string ScopeName = "AssemblyLoaded";

    public ReadOnlyAssemblyList OrderedAssemblies { get; }

    protected AssemblyLoadedScopeBase(IReadOnlyList<Assembly> assemblies)
        : base(ScopeName, new ReadOnlyAssemblyList(assemblies))
    {
        OrderedAssemblies = (ReadOnlyAssemblyList)Context;
    }

    protected override void Enter(ScopeTransitionHelper scopeTransitionHelper)
    {
        // Inform the lifecycle controller that assemblies have been loaded. This could have
        // been done as a native callback for more separation, but doing it here avoid
        // a managed->native->managed transition
        LifecycleController.Instance.OnAssembliesLoaded(OrderedAssemblies);

        EnterManaged(scopeTransitionHelper);
    }

    private void EnterManaged(ScopeTransitionHelper scopeTransitionHelper)
    {
        scopeTransitionHelper.ExecuteMethodsInOrder<OnAssemblyLoadedAttribute>(OrderedAssemblies);
    }

    protected override void Exit(ScopeTransitionHelper scopeTransitionHelper)
    {
        // Inform the lifecycle controller that we're exiting the assembly loaded scope.
        // This could have been done as a native callback for more separation, but doing
        // it here avoid a managed->native->managed transition.
        LifecycleController.Instance.OnAssemblyLoadedScopeExiting(OrderedAssemblies);

        ExitManaged(scopeTransitionHelper);

        LifecycleController.Instance.OnAssemblyLoadedScopeExited(OrderedAssemblies);
    }

    private void ExitManaged(ScopeTransitionHelper scopeTransitionHelper)
    {
        scopeTransitionHelper.ExecuteMethodsInReverseOrder<OnAssemblyUnloadingAttribute>(OrderedAssemblies);
    }
}
