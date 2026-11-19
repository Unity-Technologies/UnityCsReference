// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Animations;

namespace Unity.Animations
{
    // Bakes the RigDefinition stored in an Avatar's blob. Avatar::Bake calls it through MotionProxy.BakeAvatar
    // when the blob is read before it was baked.
    static partial class AvatarBaking
    {
        [OnAssemblyLoaded]
        static void RegisterBakeCallback()
        {
            MotionProxy.BakeAvatar = Bake;
        }

        [OnAssemblyUnloading]
        static void UnregisterBakeCallback()
        {
            MotionProxy.BakeAvatar = null;
        }

        internal static void Bake(Avatar avatar)
        {
            var rigBuilder = RigBuilder.Create(avatar, Allocator.Temp);

            var rigDefinition = rigBuilder.ToRigDefinition(Allocator.Temp);
            avatar.SetBlobAsset(rigDefinition, RigDefinition.BlobVersion);
            rigDefinition.Dispose();

            rigBuilder.Dispose();

            RegisterEntityReferences(avatar);
        }

        // The default EntityIds name objects of this session, so they are rewritten when the Avatar is read
        static unsafe void RegisterEntityReferences(Avatar avatar)
        {
            // The Avatar holds its own copy of the blob, which is the one that is saved
            var blobData = (byte*)avatar.GetBlobData(out _, out _);
            if (blobData == null)
                return;

            ref var rig = ref UnsafeUtility.AsRef<RigDefinition>(blobData + BlobObject.k_BlobAssetHeaderSize);
            var entityIdCount = rig.Bindings.EntityIdBindings.Length;
            if (entityIdCount == 0)
                return;

            var defaultEntityIds = (EntityId*)((float*)rig.DefaultValues.GetUnsafePtr() + rig.Bindings.EntityIdSamplesOffset);
            avatar.RegisterEntityReferences(new Span<EntityId>(defaultEntityIds, entityIdCount));
        }
    }
}
