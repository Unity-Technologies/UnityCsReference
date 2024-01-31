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
        private static AndroidJavaObject m_Context;
        private static AndroidJavaObject m_Activity;

        public static AndroidJavaObject Context
        {
            get
            {
                AcquireContextAndActivity();
                return m_Context;
            }
        }

        public static AndroidJavaObject Activity
        {
            get
            {
                AcquireContextAndActivity();
                return m_Activity; // can be null if context is not an activity
            }
        }

        private static void AcquireContextAndActivity()
        {
            if (m_Context != null)
                return;

            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                m_Context = unityPlayer.GetStatic<AndroidJavaObject>("currentContext");
                m_Activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            }
        }

        public static extern IntPtr UnityPlayerRaw { [ThreadSafe] get; }

        private static AndroidJavaObject m_UnityPlayer;

        public static AndroidJavaObject UnityPlayer
        {
            get
            {
                if (m_UnityPlayer != null)
                    return m_UnityPlayer;

                m_UnityPlayer = new AndroidJavaObject(UnityPlayerRaw);
                return m_UnityPlayer;
            }
        }
    }
}
