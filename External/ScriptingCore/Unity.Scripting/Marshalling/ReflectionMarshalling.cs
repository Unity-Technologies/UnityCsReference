using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Scripting.LifecycleManagement;

namespace Unity.Scripting.Marshalling
{
    /// <summary>
    /// The native->managed reflection-handle conversions, with an allocation-free cache on CoreCLR.
    /// Native passes raw CLR pointers (MethodDesc*/FieldDesc*/MethodTable*) as
    /// IntPtrs, and turning one back into a managed object needs RuntimeXHandle.FromIntPtr followed
    /// by GetXFromHandle. Upstream declined to make FromIntPtr alloc-free 
    /// https://github.com/dotnet/runtime/issues/103601
    /// and recommends client caching instead.
    ///
    /// Only methods and fields are cached: their FromIntPtr creates a stub (96 and 72 bytes) to carry
    /// the LoaderAllocator, while the type one reads the already materialized RuntimeType out of
    /// the MethodTable and allocates nothing. A hit is a lock-free dictionary lookup; a miss runs
    /// the normal resolve once. The caches are replaced on code reload via a call to Clear() so 
    /// collectible ALCs can unload and recycled addresses never return stale entries.
    ///
    /// The Mono/IL2CPP arms of the callers keep their Unsafe.As reinterpret. This
    /// assembly compiles against netstandard2.1, which does not expose FromIntPtr, so it is bound
    /// by reflection or via explicitly via delegates passed in via Initialize.
    /// </summary>
    internal static class ReflectionMarshalling
    {
        // GetXFromHandle needs the declaring type as well as the member handle for members of
        // generic classes, and native already passes both.
        internal readonly struct MemberKey : IEquatable<MemberKey>
        {
            public readonly IntPtr Member;        // MethodDesc*/FieldDesc*
            public readonly IntPtr DeclaringType; // MethodTable*

            public MemberKey(IntPtr member, IntPtr declaringType)
            {
                Member = member;
                DeclaringType = declaringType;
            }

            public bool Equals(MemberKey other) => Member == other.Member && DeclaringType == other.DeclaringType;
            public override bool Equals(object? obj) => obj is MemberKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Member, DeclaringType);
        }

        // [NoAutoStaticsCleanup] throughout: the caches hold user members that root collectible
        // ALCs, and they are released by Clear() instead.
        [NoAutoStaticsCleanup]
        private static ConcurrentDictionary<MemberKey, MethodBase> s_methods = new();
        [NoAutoStaticsCleanup]
        private static ConcurrentDictionary<MemberKey, FieldInfo> s_fields = new();

        [NoAutoStaticsCleanup]
        private static Func<IntPtr, RuntimeTypeHandle>? s_typeFromIntPtr;
        [NoAutoStaticsCleanup]
        private static Func<IntPtr, RuntimeMethodHandle>? s_methodFromIntPtr;
        [NoAutoStaticsCleanup]
        private static Func<IntPtr, RuntimeFieldHandle>? s_fieldFromIntPtr;

        public static void Initialize(Func<IntPtr, RuntimeTypeHandle> typeFromIntPtr, Func<IntPtr, RuntimeMethodHandle> methodFromIntPtr, Func<IntPtr, RuntimeFieldHandle> fieldFromIntPtr)
        {
            s_typeFromIntPtr = typeFromIntPtr;
            s_methodFromIntPtr = methodFromIntPtr;
            s_fieldFromIntPtr = fieldFromIntPtr;
        }

        /// <summary>
        /// Resolves a native RuntimeTypeHandle IntPtr to a Type. Not cached, unlike the two below:
        /// Type.GetTypeFromHandle doesn't allocate like Method?Field cases.
        /// </summary>
        internal static Type? ResolveType(IntPtr typeHandlePtr)
        {
            return Type.GetTypeFromHandle(GetRuntimeTypeHandle(typeHandlePtr));
        }

        /// <summary>Resolves a native (method handle, declaring type handle) pair to a MethodBase.</summary>
        internal static MethodBase? ResolveMethod(IntPtr methodHandlePtr, IntPtr declaringTypeHandlePtr)
        {
            MemberKey key = new MemberKey(methodHandlePtr, declaringTypeHandlePtr);
            ConcurrentDictionary<MemberKey, MethodBase> methods = Volatile.Read(ref s_methods);
            if (methods.TryGetValue(key, out MethodBase cached))
                return cached;

            MethodBase? method = MethodBase.GetMethodFromHandle(
                GetRuntimeMethodHandle(methodHandlePtr),
                GetRuntimeTypeHandle(declaringTypeHandlePtr));
            if (method != null)
                methods[key] = method;
            return method;
        }

        /// <summary>Resolves a native (field handle, declaring type handle) pair to a FieldInfo.</summary>
        internal static FieldInfo? ResolveField(IntPtr fieldHandlePtr, IntPtr declaringTypeHandlePtr)
        {
            MemberKey key = new MemberKey(fieldHandlePtr, declaringTypeHandlePtr);
            ConcurrentDictionary<MemberKey, FieldInfo> fields = Volatile.Read(ref s_fields);
            if (fields.TryGetValue(key, out FieldInfo cached))
                return cached;

            FieldInfo? field = FieldInfo.GetFieldFromHandle(
                GetRuntimeFieldHandle(fieldHandlePtr),
                GetRuntimeTypeHandle(declaringTypeHandlePtr));
            if (field != null)
                fields[key] = field;
            return field;
        }

        /// <summary>Converts a native IntPtr to a RuntimeTypeHandle. Not cached, see the note above the caches.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static RuntimeTypeHandle GetRuntimeTypeHandle(IntPtr typeHandlePtr)
        {
            return GetHandle(typeHandlePtr, ref s_typeFromIntPtr);
        }

        /// <summary>Converts a native IntPtr to a RuntimeMethodHandle. Not cached, see the note above the caches.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static RuntimeMethodHandle GetRuntimeMethodHandle(IntPtr methodHandlePtr)
        {
            return GetHandle(methodHandlePtr, ref s_methodFromIntPtr);
        }

        /// <summary>Converts a native IntPtr to a RuntimeFieldHandle. Not cached, see the note above the caches.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static RuntimeFieldHandle GetRuntimeFieldHandle(IntPtr fieldHandlePtr)
        {
            return GetHandle(fieldHandlePtr, ref s_fieldFromIntPtr);
        }

        /// <summary>
        /// Replaces every cache with an empty one. Called from
        /// CodeReloadLifecycleController.ClearInteropCaches at the end of AssemblyLoadedScope.Exit,
        /// once per ALC as its scope tears down, so the members held here do not root collectible
        /// ALCs and recycled addresses cannot return stale entries. The FromIntPtr delegates target
        /// the BCL and are left intact.
        ///
        /// Swapping the instance rather than emptying it in place closes a race with a resolver
        /// running on another thread: a resolve that missed before this ran would otherwise publish
        /// its entry after the clear, keeping a member of the unloading ALC reachable and leaving a
        /// stale entry under an address the next code load can recycle. Emptying under a lock does
        /// not help, since the late insert is merely ordered after the clear rather than prevented.
        /// The resolvers hold the instance they read, so a late insert lands in the orphaned
        /// dictionary and is dropped with it.
        /// </summary>
        internal static void Clear()
        {
            Volatile.Write(ref s_methods, new ConcurrentDictionary<MemberKey, MethodBase>());
            Volatile.Write(ref s_fields, new ConcurrentDictionary<MemberKey, FieldInfo>());
        }

        // The reflection bind runs at most once per handle kind.
        private static THandle GetHandle<THandle>(IntPtr handlePtr, ref Func<IntPtr, THandle>? fromIntPtr)
        {
            fromIntPtr ??= CreateFromIntPtrDelegate<THandle>();
            return fromIntPtr(handlePtr);
        }

        // FromIntPtr exists on CoreCLR (.NET 5+) but not in the netstandard2.1 reference assembly
        // this compiles against. NoInlining keeps the cold bind out of the GetHandle path.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Func<IntPtr, THandle> CreateFromIntPtrDelegate<THandle>()
        {
            MethodInfo? mi = typeof(THandle).GetMethod("FromIntPtr", BindingFlags.Static | BindingFlags.Public);
            if (mi == null)
                throw new MissingMethodException(typeof(THandle).Name, "FromIntPtr");
            return (Func<IntPtr, THandle>)Delegate.CreateDelegate(typeof(Func<IntPtr, THandle>), mi);
        }
    }
}
