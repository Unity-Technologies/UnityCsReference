// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using UnityEngine.Scripting;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.Serialization;

// Managed serialization V2: instance creation (doc §6).
//
// The EnterObject arm materializes null class slots through per-type
// registered constructor data ({cached Type, baked ctor fnptr}). The compose
// side registers one entry per class (V2RegisterConstructor) and stamps its
// table index into the command, so the executor's null-slot path is a table
// load, GetUninitializedObject, and a ctor calli.
//
// Exception contract: the try/catch sits in the executor's EnterObject case
// and wraps only the ctor calli. Allocation precedes the try, so when a ctor
// throws the uninitialized instance already exists and the transfer continues
// with it (v1's contract). The factory arm allocates inside its calli, so its
// catch re-materializes the instance from the interned Type instead
// (V2FactoryThrowFallback) — same observable contract. The narrow try pins only the construction temps;
// the dispatch loop's locals never enter an EH region. Recovery runs
// out-of-line through s_V2CommandExceptionHandler, and the reverse-P/Invoke
// shim's catch-all in Extensions.cs remains the boundary backstop.
//
// Whether the registered form or the fallback runs is decided at compose time
// via the opcodes; the executor never branches on it.
internal static unsafe partial class SerializationBackendManagedCommands
{
    // Per-domain dedup: a class registers once per domain. The reload clear
    // resets this alongside SerializationCommandObjectTable's per-domain
    // generation, so indexes cannot dangle and Type entries cannot pin a
    // user-script ALC across reloads. Players never code-reload.
    [AutoStaticsCleanupOnCodeReload(CleanupStrategy = CleanupStrategy.Clear)]
    private static Dictionary<Type, int> s_V2ConstructorIndexByType;
    // A plain lock object with no user references; safe to persist.
    [NoAutoStaticsCleanup]
    private static readonly object s_V2ConstructorLock = new object();

    // Pure data; construction runs in the executor cases and the fallback.
    private sealed class V2Constructor
    {
        public readonly Type Type;
        public readonly IntPtr CtorFunctionPtr;

        public V2Constructor(Type type, IntPtr ctorFunctionPtr)
        {
            Type = type;
            CtorFunctionPtr = ctorFunctionPtr;
        }
    }

    // Compose-side registration, one entry per type per domain. The entry
    // carries the ctor to run after GetUninitializedObject; the factory arm
    // (kEnterObjectFactory, which owns allocation and construction through
    // the TypeManager registry) interns with a zero ctor and reads only the
    // Type, for its catch arm. typeHandleRaw uses the same encoding as the
    // commands' runtimeTypeHandle (ResolveRuntimeTypeHandleForVrt). Returns -1
    // when the type cannot be resolved, and the composer then emits
    // kEnterObjectFallback.
    [RequiredByNativeCode]
    internal static int V2RegisterConstructor(IntPtr typeHandleRaw, IntPtr ctorFunctionPtr)
    {
        Type type = UnmarshalSystemType(typeHandleRaw);
        if (type == null)
            return -1;

        lock (s_V2ConstructorLock)
        {
            var map = s_V2ConstructorIndexByType ??= new Dictionary<Type, int>(64);
            if (map.TryGetValue(type, out int existing))
            {
                // The factory arm interns with a zero ctor (Type-only catch
                // data), and kEnterObject callis its entry unconditionally,
                // so a classic registration must never reuse a zero-ctor
                // entry: re-intern with the ctor. The old index stays valid
                // for its Type-only consumers.
                if (ctorFunctionPtr == IntPtr.Zero)
                    return existing;
                var entry = (V2Constructor)SerializationCommandObjectTable.Get(existing);
                if (entry.CtorFunctionPtr != IntPtr.Zero)
                    return existing;
                int upgraded = SerializationCommandObjectTable.Intern(new V2Constructor(type, ctorFunctionPtr));
                map[type] = upgraded;
                return upgraded;
            }
            int index = SerializationCommandObjectTable.Intern(new V2Constructor(type, ctorFunctionPtr));
            map.Add(type, index);
            return index;
        }
    }

    // Command-exception policy as a settable fnptr; the case-level catches
    // call through it. Production logs and lets the transfer continue (v1's
    // contract). Native-test builds default to rethrow so tests observe the
    // failure, with ExceptionDispatchInfo preserving the original trace. The
    // build-flavor #if lives only here, at the default's initializer.
    // A function pointer to a CoreModule static; no user references, and a
    // reload clear would erase the once-run initializer, so it must persist.
    [NoAutoStaticsCleanup]
    private static delegate*<Exception, void> s_V2CommandExceptionHandler =
        &V2LogCommandException;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void V2LogCommandException(Exception e)
    {
        Debug.LogException(e);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void V2RethrowCommandException(Exception e)
    {
        ExceptionDispatchInfo.Capture(e).Throw();
    }

    // Catch arm of the executors' kEnterObjectFactory case: the factory
    // threw, so report through the shared policy and materialize the
    // uninitialized instance the classic arm would have continued with
    // (v1's throwing-ctor contract). Out of line so the dispatch loop's
    // EH region stays a single calli.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object V2FactoryThrowFallback(int constructorIndex, Exception e)
    {
        s_V2CommandExceptionHandler(e);
        var ctor = (V2Constructor)SerializationCommandObjectTable.Get(constructorIndex);
        return RuntimeHelpers.GetUninitializedObject(ctor.Type);
    }

    // Construction for kEnterObjectFallback, emitted when registration was
    // unavailable at compose time. Same contract as the executor cases, with
    // the Type resolved per call.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object V2CreateInstanceFallback(IntPtr runtimeTypeHandle, IntPtr ctorFunctionPtr)
    {
        Type type = UnmarshalSystemType(runtimeTypeHandle);
        if (type == null)
            return null;
        return V2CreateInstanceFallback(type, ctorFunctionPtr);
    }

    // For callers that already resolved the Type, so the handle is unmarshalled once.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object V2CreateInstanceFallback(Type type, IntPtr ctorFunctionPtr)
    {
        object obj = RuntimeHelpers.GetUninitializedObject(type);
        if (ctorFunctionPtr != IntPtr.Zero)
        {
            try
            {
                ((delegate*<object, void>)ctorFunctionPtr)(obj);
            }
            catch (Exception e)
            {
                s_V2CommandExceptionHandler(e);
            }
        }
        return obj;
    }
}
