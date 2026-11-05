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
            int numReadFencesOffset, int typeIndexOffset, int maxReadFences, int nullDependencyHandleIndex);

        // Returns the byte that marks native transform access as possibly having to wait for Entities jobs, or null.
        [FreeFunction("EntitiesTransformDependencyBindings::RegisterWorld")]
        internal static extern byte* RegisterWorld(ulong owner, void* typeArrayIndices, void* dependencyHandles, void* dependencyHandlesCount,
            void* readJobFences, void* isInTransaction, void* exclusiveTransactionDependency, void* runningTransformSystemJobHandle);

        [FreeFunction("EntitiesTransformDependencyBindings::UnregisterWorld")]
        internal static extern void UnregisterWorld(ulong owner);

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
