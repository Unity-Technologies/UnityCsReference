namespace Unity.Scripting;

internal sealed class DelegateWithContext<T>
{
    public IntPtr NativeInvocationContext { get; }
    public T NativeDelegate { get; }
    public Profiling.ProfilerMarker ProfilerMarker { get; }

    public DelegateWithContext(IntPtr nativeInvocationContext, T nativeDelegate, string subsystemIdentifier)
    {
        NativeInvocationContext = nativeInvocationContext;
        NativeDelegate = nativeDelegate;
        ProfilerMarker = new Profiling.ProfilerMarker(subsystemIdentifier);
    }

    public override bool Equals(object? obj)
    {
        return obj is DelegateWithContext<T> other && Equals(other);
    }

    private bool Equals(DelegateWithContext<T> other)
    {
        return NativeInvocationContext.Equals(other.NativeInvocationContext)
            && EqualityComparer<T>.Default.Equals(NativeDelegate, other.NativeDelegate);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(NativeInvocationContext, NativeDelegate);
    }
}
