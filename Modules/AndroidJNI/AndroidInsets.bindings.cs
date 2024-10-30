// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Android
{
    [NativeHeader("Modules/AndroidJNI/Public/AndroidInsets.bindings.h")]
    [StaticAccessor("AndroidInsets", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    class AndroidInsets
    {
        [Flags]
        internal enum AndroidInsetsType
        {
            StatusBars = 1 << 0,
            NavigationBars = 1 << 1,
            CaptionBar = 1 << 2,
            IME = 1 << 3,
            SystemGestures = 1 << 4,
            MandatorySystemGestures = 1 << 5,
            TappableElement = 1 << 6,
            DisplayCutout = 1 << 7
        }

        IntPtr m_NativeHandle;

        internal AndroidInsets()
        {
        }

        [RequiredByNativeCode(GenerateProxy = true)]
        private void SetNativeHandle(IntPtr ptr)
        {
            m_NativeHandle = ptr;
        }

        private static extern Rect InternalGetAndroidInsets(IntPtr handle, AndroidInsetsType type);

        internal Rect GetInsets(AndroidInsetsType type)
        {
            if (m_NativeHandle == IntPtr.Zero)
                throw new Exception($"You can only query insets from within {nameof(AndroidApplication)}.${nameof(AndroidApplication.onInsetsChanged)}");
            return InternalGetAndroidInsets(m_NativeHandle, type);
        }
    }
}
