// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Android
{
    [NativeHeader("Modules/AndroidJNI/Public/AndroidApplication.bindings.h")]
    [StaticAccessor("AndroidApplication", StaticAccessorType.DoubleColon)]
    public static class AndroidApplication
    {
        private static SynchronizationContext m_MainThreadSynchronizationContext;
        private static AndroidJavaObjectUnityOwned m_Context = null;
        private static AndroidJavaObjectUnityOwned m_Activity = null;
        private static AndroidJavaObjectUnityOwned m_UnityPlayer = null;
        internal static extern IntPtr UnityPlayerRaw { [ThreadSafe] get; }
        private static extern IntPtr CurrentContextRaw { [ThreadSafe] get; }
        private static extern IntPtr CurrentActivityRaw { [ThreadSafe] get; }

        [RequiredByNativeCode(GenerateProxy = true)]
        private static void AcquireMainThreadSynchronizationContext()
        {
            m_MainThreadSynchronizationContext = UnitySynchronizationContext.Current;
            if (m_MainThreadSynchronizationContext == null)
                throw new Exception("Failed to acquire main thread synchronization context");
        }

        public static AndroidJavaObject currentContext
        {
            get
            {
                return m_Context;
            }
        }

        public static AndroidJavaObject currentActivity
        {
            get
            {
                return m_Activity;
            }
        }


        public static AndroidJavaObject unityPlayer
        {
            get
            {
                return m_UnityPlayer;
            }
        }

        private static AndroidConfiguration m_CurrentConfiguration;
        private static AndroidInsets m_CurrentAndroidInsets;

        [RequiredByNativeCode(GenerateProxy = true)]
        private static void SetCurrentConfiguration(AndroidConfiguration config)
        {
            m_CurrentConfiguration = config;
        }

        [RequiredByNativeCode(GenerateProxy = true)]
        private static AndroidConfiguration GetCurrentConfiguration()
        {
            return m_CurrentConfiguration;
        }

        [RequiredByNativeCode(GenerateProxy = true)]
        private static void DispatchConfigurationChanged(bool notifySubscribers)
        {
            if (notifySubscribers)
                onConfigurationChanged?.Invoke(m_CurrentConfiguration);
        }

        [RequiredByNativeCode(GenerateProxy = true)]
        private static void SetCurrentInsets(AndroidInsets insets)
        {
            m_CurrentAndroidInsets = insets;
        }

        [RequiredByNativeCode(GenerateProxy = true)]
        private static AndroidInsets GetCurrentInsets()
        {
            return m_CurrentAndroidInsets;
        }

        [RequiredByNativeCode(GenerateProxy = true)]
        private static void DispatchInsetsChanged()
        {
            onInsetsChanged?.Invoke(m_CurrentAndroidInsets);
        }

        public static AndroidConfiguration currentConfiguration => m_CurrentConfiguration;

        public static event Action<AndroidConfiguration> onConfigurationChanged;
		
		internal static event Action<AndroidInsets> onInsetsChanged;

        public static void InvokeOnUIThread(Action action)
        {
        }

        public static void InvokeOnUnityMainThread(Action action)
        {
        }
    }
}
