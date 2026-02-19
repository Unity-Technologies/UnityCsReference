namespace Unity.Scripting.LifecycleManagement;

using LifecycleDelegate = DelegateWithContext<INativeCallbackProvider.LifecycleNativeDelegate>;

internal class DependencyOrderedNativeCallbackProvider : INativeCallbackProvider
{
    private readonly Dictionary<string, SortedSubsystemGroup<LifecycleDelegate, LifecycleDelegate>> _nativeLifecycleEventHandlers = new();

    public void RegisterNativeLifecycleEventHandlers(string identifierName, string lifecycleScopeName,
        IntPtr invocationContext,
        LifecycleDelegate? initScopeHandler, LifecycleDelegate? cleanupScopeHandler,
        params string[] dependencyIdentifierNames)
    {
        if (initScopeHandler == null && cleanupScopeHandler == null)
        {
            throw new ArgumentNullException(nameof(initScopeHandler), $"At least one of {nameof(initScopeHandler)} or {nameof(cleanupScopeHandler)} must be provided");
        }

        if (!_nativeLifecycleEventHandlers.TryGetValue(lifecycleScopeName, out var handler))
        {
            handler = new SortedSubsystemGroup<LifecycleDelegate, LifecycleDelegate>();
            _nativeLifecycleEventHandlers[lifecycleScopeName] = handler;
        }

        handler.RegisterSubsystem(identifierName, initScopeHandler, cleanupScopeHandler, dependencyIdentifierNames);
    }

    public IReadOnlyList<LifecycleDelegate> GetInitLifecycleNativeEventHandlers(string lifecycleScopeName)
    {
        if (_nativeLifecycleEventHandlers.TryGetValue(lifecycleScopeName, out var list))
        {
            return list.SortedInitCallbacks;
        }

        return Array.Empty<LifecycleDelegate>();
    }

    public IReadOnlyList<LifecycleDelegate> GetCleanupLifecycleNativeEventHandlers(string lifecycleScopeName)
    {
        if (_nativeLifecycleEventHandlers.TryGetValue(lifecycleScopeName, out var list))
        {
            return list.SortedCleanupCallbacks;
        }

        return Array.Empty<LifecycleDelegate>();
    }
}
