// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace UnityEngine.Analytics
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityAnalytics/Events/UserCustomEvent.h")]
    internal partial class CustomEventData : IDisposable
    {
        [System.NonSerialized]
        internal IntPtr m_Ptr;

        private CustomEventData() {}

        public CustomEventData(string name)
        {
            m_Ptr = Internal_Create(this, name);
        }

        ~CustomEventData()
        {
            Destroy();
        }

        void Destroy()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        internal static extern IntPtr Internal_Create(CustomEventData ced, string name);
        [ThreadSafe]
        internal static extern void Internal_Destroy(IntPtr ptr);

        public extern bool AddString(string key, string value);
        public extern bool AddInt32(string key, Int32 value);
        public extern bool AddUInt32(string key, UInt32 value);
        public extern bool AddInt64(string key, Int64 value);
        public extern bool AddUInt64(string key, UInt64 value);
        public extern bool AddBool(string key, bool value);
        public extern bool AddDouble(string key, double value);
    }
}
