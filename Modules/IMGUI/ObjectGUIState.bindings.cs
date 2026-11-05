// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Modules/IMGUI/GUIState.h")]
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal class ObjectGUIState : IDisposable
    {
        internal IntPtr m_Ptr;

        public ObjectGUIState()
        {
            m_Ptr = Internal_Create();
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

#pragma warning disable UA5000 // Releases the native state; Internal_Destroy is thread-safe, so the finalizer is the safety net when Dispose is missed.
        ~ObjectGUIState()
        {
            Destroy();
        }
#pragma warning restore UA5000

        void Destroy()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        private static extern IntPtr Internal_Create();

        [NativeMethod(IsThreadSafe = true)]
        private static extern void Internal_Destroy(IntPtr ptr);

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(ObjectGUIState objectGUIState) => objectGUIState.m_Ptr;
        }
    }
}
