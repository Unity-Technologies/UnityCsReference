// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.WSA
{
    public delegate void AppCallbackItem();

    public delegate void WindowSizeChanged(int width, int height);

    public enum WindowActivationState
    {
        CodeActivated = 0,
        Deactivated = 1,
        PointerActivated = 2
    }

    public delegate void WindowActivated(WindowActivationState state);

    [NativeHeader("PlatformDependent/MetroPlayer/Bindings/WSAApplication.bindings.h")]
    [StaticAccessor("WSAApplicationBindings", StaticAccessorType.DoubleColon)]
    public sealed class Application
    {
        public static event WindowSizeChanged windowSizeChanged;
        public static event WindowActivated windowActivated;

        public static string arguments
        {
            get
            {
                return GetAppArguments();
            }
        }

        public static string advertisingIdentifier
        {
            get
            {
                string advertisingId = GetAdvertisingIdentifier();
                UnityEngine.Application.InvokeOnAdvertisingIdentifierCallback(advertisingId, true);
                return advertisingId;
            }
        }

        [NativeConditional("PLATFORM_WINRT")]
        private static extern string GetAdvertisingIdentifier();

        [NativeConditional("PLATFORM_WINRT")]
        private static extern string GetAppArguments();

        [RequiredByNativeCode]
        internal static void InvokeWindowSizeChangedEvent(int width, int height)
        {
            if (windowSizeChanged != null)
                windowSizeChanged.Invoke(width, height);
        }

        [RequiredByNativeCode]
        internal static void InvokeWindowActivatedEvent(WindowActivationState state)
        {
            if (windowActivated != null) windowActivated.Invoke(state);
        }

        public static void InvokeOnAppThread(AppCallbackItem item, bool waitUntilDone)
        {
            item();
        }

        public static void InvokeOnUIThread(AppCallbackItem item, bool waitUntilDone)
        {
            item();
        }

        [ThreadAndSerializationSafe]
        [NativeConditional("PLATFORM_WINRT")]
        internal static extern void InternalInvokeOnAppThread(object item, bool waitUntilDone);

        [ThreadAndSerializationSafe]
        [NativeConditional("PLATFORM_WINRT")]
        internal static extern void InternalInvokeOnUIThread(object item, bool waitUntilDone);

        [ThreadAndSerializationSafe]
        [NativeConditional("PLATFORM_WINRT", StubReturnStatement = "true")]
        public static extern bool RunningOnAppThread();

        [ThreadAndSerializationSafe]
        [NativeConditional("PLATFORM_WINRT", StubReturnStatement = "true")]
        public static extern bool RunningOnUIThread();
    }
}
