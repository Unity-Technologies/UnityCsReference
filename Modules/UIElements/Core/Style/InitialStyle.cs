// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements.StyleSheets;

partial class InitialStyle
{
    // The generated s_InitialStyle is [NoAutoStaticsCleanup]: Release, registered here, owns the
    // teardown by dropping the ComputedStyle native refcount.
    static InitialStyle()
    {
        UnloadingUtility.SubscribeToUnloading(UnloadingSubscriber.InitialStyle, Release);
        Initialize();
    }
}
