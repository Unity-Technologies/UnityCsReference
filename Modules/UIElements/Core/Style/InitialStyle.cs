// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements.StyleSheets;

partial class InitialStyle
{
    static InitialStyle()
    {
        UnloadingUtility.SubscribeToUnloading(UnloadingSubscriber.InitialStyle, Release);
        Initialize();
    }
}
