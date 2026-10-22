// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using Unity.Burst;

namespace Unity.Entities
{
    internal class EntitiesProfilerInitializedState
    {
        internal static readonly SharedStatic<EntitiesProfilerInitializedStateData> storage = SharedStatic<EntitiesProfilerInitializedStateData>.GetOrCreate<EntitiesProfilerInitializedStateData>();
        internal struct EntitiesProfilerInitializedStateData
        {
            internal bool initialized;
        }
    }

    internal unsafe static class EntitiesProfilerPackageCalls
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void ArchetypeAddedDelegate(Archetype* archetype);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void OnSystemCreatedDelegate(ref SystemTypeIndex systemType, in SystemHandle systemHandle);

        internal struct ProfilerPackageCallsStorage
        {
            internal FunctionPointer<ArchetypeAddedDelegate> archetypeAddedFunctionPointer;
            internal FunctionPointer<OnSystemCreatedDelegate> onSystemCreatedFunctionPointer;
        }

        internal static readonly SharedStatic<ProfilerPackageCallsStorage> shimStorage = SharedStatic<ProfilerPackageCallsStorage>.GetOrCreate<ProfilerPackageCallsStorage>();

        internal static void ArchetypeAdded(Archetype* archetype)
        {
            ref var functionPtr = ref shimStorage.Data.archetypeAddedFunctionPointer;
            if (EntitiesProfilerInitializedState.storage.Data.initialized)
                functionPtr.Invoke(archetype);
        }

        internal static void OnSystemCreated(SystemTypeIndex systemType, in SystemHandle systemHandle)
        {
            ref var functionPtr = ref shimStorage.Data.onSystemCreatedFunctionPointer;
            if (EntitiesProfilerInitializedState.storage.Data.initialized)
                functionPtr.Invoke(ref systemType, in systemHandle);
        }
    }
}
