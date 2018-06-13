// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

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

    [NativeHeader("Runtime/Export/WSA/WSAApplication.bindings.h")]
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

        private static extern string GetAdvertisingIdentifier();

        private static extern string GetAppArguments();

        internal static void InvokeWindowSizeChangedEvent(int width, int height)
        {
            if (windowSizeChanged != null)
                windowSizeChanged.Invoke(width, height);
        }

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

        [Obsolete("TryInvokeOnAppThread is deprecated, use InvokeOnAppThread")]
        public static bool TryInvokeOnAppThread(AppCallbackItem item, bool waitUntilDone)
        {
            item();
            return true;
        }

        [Obsolete("TryInvokeOnUIThread is deprecated, use InvokeOnUIThread")]
        public static bool TryInvokeOnUIThread(AppCallbackItem item, bool waitUntilDone)
        {
            item();
            return true;
        }



        [ThreadAndSerializationSafe]
        internal static extern bool InternalTryInvokeOnAppThread(AppCallbackItem item, bool waitUntilDone);

        [ThreadAndSerializationSafe]
        internal static extern bool InternalTryInvokeOnUIThread(AppCallbackItem item, bool waitUntilDone);

        [ThreadAndSerializationSafe]
        public static extern bool RunningOnAppThread();

        [ThreadAndSerializationSafe]
        public static extern bool RunningOnUIThread();
    }
}
