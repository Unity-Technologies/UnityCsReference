// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Scripting.LifecycleManagement;
// EntityId lives in namespace UnityEngine (UnityEngineObject.bindings.cs:141). The
// test-resources compile context (UNITY_NATIVE_TEST_RESOURCES) provides a stub in the
// same namespace via Runtime/Testing/ScriptWithManagedRefTestFixture.Resources_cs, so
// the bare `EntityId` field type below resolves identically in both compile contexts.
using UnityEngine;

namespace UnityEngine.Serialization;

internal static unsafe partial class SerializationBackendManagedCommands
{
    // Wrapper construction path for SimpleNativeType reads in ExecuteReadCommands.
    // For SimpleNativeType wrappers the parameterless ctor is what allocates the
    // native peer, so running it is required to get a usable m_Ptr afterwards.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe object CreateWrapperInstance(IntPtr runtimeTypeHandle, IntPtr ctorFunctionPtr)
    {
        // ctorFunctionPtr is baked at build time on every backend now that the
        // native ResolveParameterlessCtorFunctionPointer passes kConstructor to
        // the method lookup (CoreCLR included), so no per-backend fallback is
        // needed here. Zero still means the type has no parameterless ctor.
        Type type = UnmarshalSystemType(runtimeTypeHandle);
        object obj = RuntimeHelpers.GetUninitializedObject(type);
        if (ctorFunctionPtr != IntPtr.Zero)
        {
            try
            {
                ((delegate*<object, void>)ctorFunctionPtr)(obj);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        return obj;
    }

}
