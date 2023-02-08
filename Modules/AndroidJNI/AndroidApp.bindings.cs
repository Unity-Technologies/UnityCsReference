// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.Android
{
    [NativeHeader("Modules/AndroidJNI/Public/AndroidApp.bindings.h")]
    [StaticAccessor("AndroidApp", StaticAccessorType.DoubleColon)]
    [NativeConditional("PLATFORM_ANDROID")]
    internal static class AndroidApp
    {
        private static AndroidJavaObject m_Activity;

        public static AndroidJavaObject Activity
        {
            get
            {
                if (m_Activity != null)
                    return m_Activity;

                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    m_Activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                }

                return m_Activity;
            }
        }

        public static extern IntPtr UnityPlayerRaw { [ThreadSafe] get; }
    }
}
