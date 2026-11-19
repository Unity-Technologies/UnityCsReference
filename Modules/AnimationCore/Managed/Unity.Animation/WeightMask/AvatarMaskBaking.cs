// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace Unity.Animations
{
    // Bakes the WeightMask stored in an AvatarMask's blob. AvatarMask::BakeWeightMask calls it through
    // MotionProxy.BakeAvatarMask when the blob is read before it was baked.
    static partial class AvatarMaskBaking
    {
        [OnAssemblyLoaded]
        static void RegisterBakeCallback()
        {
            MotionProxy.BakeAvatarMask = Bake;
        }

        [OnAssemblyUnloading]
        static void UnregisterBakeCallback()
        {
            MotionProxy.BakeAvatarMask = null;
        }

        internal static void Bake(AvatarMask mask)
        {
            using var builder = WeightMaskBuilder.Create(mask, Allocator.Temp);
            using var weightMask = builder.ToWeightMask(Allocator.Temp);
            mask.SetBlobAsset(weightMask, WeightMask.BlobVersion);
        }
    }
}
