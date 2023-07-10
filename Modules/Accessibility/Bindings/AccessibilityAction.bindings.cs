// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Accessibility
{
    /// <summary>
    /// An action to perform on an accessibility node.
    /// </summary>
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/Accessibility/Native/AccessibilityAction.h")]
    internal sealed class AccessibilityAction : IDisposable
    {
        IntPtr m_Ptr;

        public AccessibilityAction()
        {
            m_Ptr = Internal_Create(this);
        }

        public AccessibilityAction(IntPtr ptr)
        {
            m_Ptr = ptr;
        }

        ~AccessibilityAction()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        static extern IntPtr Internal_Create([Unmarshalled] AccessibilityAction self);
        static extern void Internal_Destroy(IntPtr ptr);

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(AccessibilityAction obj) => obj.m_Ptr;
            public static AccessibilityAction ConvertToManaged(IntPtr ptr) => new AccessibilityAction(ptr);
        }

        /// <summary>
        /// Identifies the accessibility action.
        /// </summary>
        public extern int id { get; set; }

        /// <summary>
        /// Succinctly describes the accessibility action.
        /// </summary>
        public extern string label { get; set; }

        /// <summary>
        /// Called when the accessibility action is activated.
        /// </summary>
        public Func<bool> activated { get; set; }

        [RequiredByNativeCode]
        bool Internal_InvokeActivated()
        {
            return activated != null && activated.Invoke();
        }
    }
}
