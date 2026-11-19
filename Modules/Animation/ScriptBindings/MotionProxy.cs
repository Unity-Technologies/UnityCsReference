// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [VisibleToOtherModules("UnityEngine.AnimationCoreModule")]
    internal static class MotionProxy
    {
        public delegate void BakeAvatarCallback(Avatar avatar);

        // The AnimationCore module registers the callback when it loads
        [NoAutoStaticsCleanup]
        public static BakeAvatarCallback BakeAvatar;

        public delegate void BakeAvatarMaskCallback(AvatarMask mask);

        // The AnimationCore module registers the callback when it loads
        [NoAutoStaticsCleanup]
        public static BakeAvatarMaskCallback BakeAvatarMask;
    }
}
