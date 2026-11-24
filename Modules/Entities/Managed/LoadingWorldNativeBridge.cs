// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;
using Debug = Unity.Scripting.LowLevel.Debug;

namespace Unity.Entities
{
    // Native LoadingWorld's calls into LoadingWorldSystem. Native code carries both Worlds as WorldUnmanaged.
    static unsafe class LoadingWorldNativeBridge
    {
        const string k_NotALoadingWorldError = "The World is not a loading World a load has. The request was ignored.";

        // Writes default to outWorld when target is not created or the acquire fails, and default to outTransaction
        // unless the World is for a loading thread.
        [RequiredByNativeCode(GenerateNativeLookup = false)]
        internal static void Acquire(IntPtr target, bool forLoadingThread, IntPtr outWorld, IntPtr outTransaction)
        {
            *(WorldUnmanaged*)outWorld = default;
            *(ExclusiveEntityTransaction*)outTransaction = default;
            var targetWorld = *(WorldUnmanaged*)target;
            if (!targetWorld.IsCreated)
                return;

            *(WorldUnmanaged*)outWorld = LoadingWorldSystem.Acquire(targetWorld, forLoadingThread, out var transaction);
            *(ExclusiveEntityTransaction*)outTransaction = transaction;
        }

        [RequiredByNativeCode(GenerateNativeLookup = false)]
        internal static void EndLoadingThreadAccess(IntPtr world) => Check(LoadingWorldSystem.EndLoadingThreadAccess(*(WorldUnmanaged*)world));

        [RequiredByNativeCode(GenerateNativeLookup = false)]
        internal static void SyncAndRelease(IntPtr world) => Check(LoadingWorldSystem.SyncAndRelease(*(WorldUnmanaged*)world));

        [RequiredByNativeCode(GenerateNativeLookup = false)]
        internal static void Release(IntPtr world) => Check(LoadingWorldSystem.Release(*(WorldUnmanaged*)world));

        static void Check(bool found)
        {
            if (!found)
                Debug.LogError(k_NotALoadingWorldError);
        }
    }
}
