// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Android
{
    /// <summary>
    /// Mirrors https://developer.android.com/reference/android/view/WindowInsetsController instance.
    /// </summary>
    [NativeHeader("Modules/AndroidJNI/Public/AndroidWindowInsets.bindings.h")]
    [StaticAccessor("AndroidWindowInsets", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public class AndroidWindowInsets
    {
        [Flags]
        public enum Type
        {
            StatusBars = 1 << 0,
            NavigationBars = 1 << 1,
            /*
            CaptionBar = 1 << 2,
            IME = 1 << 3,
            SystemGestures = 1 << 4,
            MandatorySystemGestures = 1 << 5,
            TappableElement = 1 << 6,
            DisplayCutout = 1 << 7
            */
        }

        public enum SystemBarsBehavior : int
        {
            Undefined = -1,
            Default = 1,
            ShowTransientBarsBySwipe = 2
        }

        IntPtr m_NativeHandle;

        internal AndroidWindowInsets()
        {
        }

        [RequiredByNativeCode(GenerateProxy = true)]
        private void SetNativeHandle(IntPtr ptr)
        {
            m_NativeHandle = ptr;
        }

        [RequiredByNativeCode(GenerateProxy = true)]
        private static int[] GetSupportedInsets()
        {
            // For internal purposes, remove if is exposed in Type
            const Type CaptionBar = (Type)(1 << 2);
            const Type Ime = (Type)(1 << 3);
            return new[]
            {
                (int)Type.StatusBars,
                (int)Type.NavigationBars,
                (int)CaptionBar,
                (int)Ime
            };
        }

        private static extern void InternalShow(Type type);

        public void Show(Type type)
        {
            InternalShow(type);
        }

        private static extern void InternalHide(Type type);

        public void Hide(Type type)
        {
            InternalHide(type);
        }

        private static extern RectInt InternalGetInsets(IntPtr handle, Type type);

        internal RectInt GetInsets(Type type)
        {
            return InternalGetInsets(m_NativeHandle, type);
        }

        public bool IsVisible(Type type)
        {
            var insets = GetInsets(type);
            // Note: Android has different coordinate system, thus height can be negative here
            return insets.width != 0 || insets.height != 0;
        }

        private static extern void InternalSetSystemBarsBehavior(int behavior);

        public void SetSystemBarsBehavior(SystemBarsBehavior behavior)
        {
            InternalSetSystemBarsBehavior((int)behavior);
        }

        private static extern int InternalGetSystemBarsBehavior();

        public SystemBarsBehavior GetSystemBarsBehavior()
        {
            return (SystemBarsBehavior)InternalGetSystemBarsBehavior();
        }
    }
}
