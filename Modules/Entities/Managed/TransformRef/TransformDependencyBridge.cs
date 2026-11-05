// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Entities
{
    // Dependency key for Entities jobs that can reach the transform hierarchies of GameObjects. No entity has this
    // component. Queries that don't exclude IsGameObject add it next to TransformRef, so native code that only touches
    // GameObject hierarchies does not have to wait for jobs that iterate pure entities.
    internal struct EntityJobsOnGameObjectTransforms : IComponentData
    {
        internal byte m_Unused;
    }

    // Dependency key for native jobs on GameObject transform hierarchies. Entities jobs that can reach GameObjects wait
    // for it; native jobs never do, since per-hierarchy fences already order them and they must stay parallel.
    internal struct NativeJobsOnGameObjectTransforms : IComponentData
    {
        internal byte m_Unused;
    }

    // Gives native transform code direct access to the TransformRef job dependencies of each World.
    // The native side is Modules/Entities/Public/EntitiesTransformDependency.h; it identifies a World by its owner value.
    internal static unsafe class TransformDependencyBridge
    {
        internal static void Initialize()
        {
            ComponentDependencyManager.CheckNativeTransformDependencyLayout();
            EntitiesTransformDependencyBindings.SetTransformTypeIndices(TypeManager.GetTypeIndex<TransformRef>(),
                TypeManager.GetTypeIndex<EntityJobsOnGameObjectTransforms>(), TypeManager.GetTypeIndex<NativeJobsOnGameObjectTransforms>());
        }

        // 0 is reserved by native code for "no owner", and sequence numbers start at 0.
        internal static ulong GetOwner(EntityComponentStore* store) => store->WorldSequenceNumber + 1;

        internal static void RegisterWorld(EntityDataAccess* access)
        {
            ComponentDependencyManager.RegisterWithNativeTransformDependencies(access->DependencyManager, GetOwner(access->EntityComponentStore));
        }

        internal static void UnregisterWorld(EntityComponentStore* store)
        {
            EntitiesTransformDependencyBindings.UnregisterWorld(GetOwner(store));
        }
    }
}
