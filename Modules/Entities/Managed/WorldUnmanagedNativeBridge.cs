// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace Unity.Entities
{
    // The native WorldUnmanaged (Modules/Entities/Public/WorldUnmanaged.h) calls these
    static unsafe class WorldUnmanagedNativeBridge
    {
        [RequiredByNativeCode]
        internal static uint GetGlobalSystemVersion(IntPtr world) => ((WorldUnmanaged*)world)->EntityManager.GlobalSystemVersion;

        [RequiredByNativeCode]
        internal static void IncrementGlobalSystemVersion(IntPtr world) =>
            ((WorldUnmanaged*)world)->EntityManager.GetCheckedEntityDataAccess()->EntityComponentStore->IncrementGlobalSystemVersion();
    }
}
