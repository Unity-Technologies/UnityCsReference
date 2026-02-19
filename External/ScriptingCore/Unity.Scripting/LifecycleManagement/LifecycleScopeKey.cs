namespace Unity.Scripting.LifecycleManagement;

internal readonly struct LifecycleScopeKey : IEquatable<LifecycleScopeKey>
{
    private readonly Type _scopeType;
    private readonly object? _context;

    public Type Type => _scopeType;
    public object? Context => _context;

    public LifecycleScopeKey(Type scopeType)
        : this(scopeType, null!)
    {
    }

    public LifecycleScopeKey(Type scopeType, object context)
    {
        if (scopeType.IsAbstract || scopeType.IsInterface || scopeType.IsGenericType)
        {
            throw new InvalidOperationException($"{nameof(LifecycleScopeKey)} cannot be an abstract, interface or generic class");
        }

        _scopeType = scopeType;
        _context = context;
    }

    public static LifecycleScopeKey CreateFromScope(LifecycleScope scope)
    {
        return new LifecycleScopeKey(scope.GetType(), null!);
    }

    public static LifecycleScopeKey CreateFromScope<T>(LifecycleScopeWithContext<T> scope)
        where T : class
    {
        return new LifecycleScopeKey(scope.GetType(), scope.Context);
    }

    public override bool Equals(object? obj)
    {
        return obj switch
        {
            LifecycleScopeKey scopeKey => Equals(scopeKey),
            _ => false
        };
    }

    public bool Equals(LifecycleScopeKey other)
    {
        return _scopeType == other._scopeType
               && _context == other._context;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_scopeType, _context);
    }
}
