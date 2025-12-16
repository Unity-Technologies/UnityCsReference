// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Android
{
    [NativeHeader("Modules/AndroidJNI/Public/AndroidWindowInsets.bindings.h")]
    [StaticAccessor("AndroidWindowInsets", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    class AndroidWindowInsets
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
            var values = (Type[])Enum.GetValues(typeof(Type));
            var result = new int[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                result[i] = (int)values[i];
            }
            return result;
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
    }
}
