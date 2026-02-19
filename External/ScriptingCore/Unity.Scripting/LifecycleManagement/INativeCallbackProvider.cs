using System.Runtime.InteropServices;

namespace Unity.Scripting.LifecycleManagement;

internal interface INativeCallbackProvider
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void LifecycleNativeDelegate(IntPtr invocationContext, IntPtr context);

    IReadOnlyList<DelegateWithContext<LifecycleNativeDelegate>> GetInitLifecycleNativeEventHandlers(string lifecycleScopeName);
    IReadOnlyList<DelegateWithContext<LifecycleNativeDelegate>> GetCleanupLifecycleNativeEventHandlers(string lifecycleScopeName);
}
