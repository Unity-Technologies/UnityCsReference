// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace Unity.Entities
{
    [NativeHeader("Modules/Entities/Public/EntitiesTransformDependency.bindings.h")]
    internal static unsafe class EntitiesTransformDependencyBindings
    {
        [FreeFunction("EntitiesTransformDependencyBindings::CheckManagedLayout")]
        internal static extern void CheckManagedLayout(int jobHandleSize, int dependencyHandleSize, int writeFenceOffset,
            int numReadFencesOffset, int typeIndexOffset, int maxReadFences, int nullDependencyHandleIndex, int atomicSafetyHandleSize,
            int componentSafetyHandleSize, int safetyHandleOffset, int bufferHandleOffset, int safetyTypeIndexOffset);

        // Returns the byte that marks native transform access as possibly having to wait for Entities jobs, or null. The
        // safety arrays are null without collection checks.
        internal static byte* RegisterWorld(WorldUnmanaged owner, void* typeArrayIndices, void* dependencyHandles, void* dependencyHandlesCount,
            void* readJobFences, void* isInTransaction, void* exclusiveTransactionDependency, void* runningTransformSystemJobHandle,
            void* safetyTypeArrayIndices, void* componentSafetyHandles) =>
            RegisterWorld(&owner, typeArrayIndices, dependencyHandles, dependencyHandlesCount, readJobFences, isInTransaction,
                exclusiveTransactionDependency, runningTransformSystemJobHandle, safetyTypeArrayIndices, componentSafetyHandles);

        internal static void UnregisterWorld(WorldUnmanaged owner) => UnregisterWorld(&owner);

        // The bindings generator can't marshal WorldUnmanaged
        [FreeFunction("EntitiesTransformDependencyBindings::RegisterWorld")]
        static extern byte* RegisterWorld(void* owner, void* typeArrayIndices, void* dependencyHandles, void* dependencyHandlesCount,
            void* readJobFences, void* isInTransaction, void* exclusiveTransactionDependency, void* runningTransformSystemJobHandle,
            void* safetyTypeArrayIndices, void* componentSafetyHandles);

        [FreeFunction("EntitiesTransformDependencyBindings::UnregisterWorld")]
        static extern void UnregisterWorld(void* owner);

        [FreeFunction("EntitiesTransformDependencyBindings::SetTransformTypeIndices")]
        static extern void SetTransformTypeIndices(int transformRef, int transformRefIndex,
            int entityJobsOnGameObjectTransforms, int entityJobsOnGameObjectTransformsIndex,
            int nativeJobsOnGameObjectTransforms, int nativeJobsOnGameObjectTransformsIndex);

        internal static void SetTransformTypeIndices(TypeIndex transformRef, TypeIndex entityJobsOnGameObjectTransforms,
            TypeIndex nativeJobsOnGameObjectTransforms)
        {
            SetTransformTypeIndices(transformRef.Value, transformRef.Index,
                entityJobsOnGameObjectTransforms.Value, entityJobsOnGameObjectTransforms.Index,
                nativeJobsOnGameObjectTransforms.Value, nativeJobsOnGameObjectTransforms.Index);
        }
    }
}
