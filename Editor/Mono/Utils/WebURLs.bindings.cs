// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditorInternal
{
    [NativeHeader("Editor/Src/Utility/WebURLs.h")]
    internal static class WebURLs
    {
        [NativeProperty("kURLUnity", true, TargetType.Field)]
        public static extern string unity { get; }
        [NativeProperty("kURLUnityConnect", true, TargetType.Field)]
        public static extern string unityConnect { get; }
        [NativeProperty("kURLUnityForum", true, TargetType.Field)]
        public static extern string unityForum { get; }
        [NativeProperty("kURLUnityAnswers", true, TargetType.Field)]
        public static extern string unityAnswers { get; }
        [NativeProperty("kURLUnityFeedback", true, TargetType.Field)]
        public static extern string unityFeedback { get; }
        [NativeProperty("kURLWhatsNewPage", true, TargetType.Field)]
        public static extern string whatsNewPage { get; }
        [NativeProperty("kURLBetaLandingPage", true, TargetType.Field)]
        public static extern string betaLandingPage { get; }
        [NativeProperty("kURLCloudBuildPage", true, TargetType.Field)]
        public static extern string cloudBuildPage { get; }
    }
}
