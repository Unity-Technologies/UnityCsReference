// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/Utility/ChangeTracker.h")]
    [StructLayout(LayoutKind.Sequential)]
    internal sealed partial class SerializedObjectChangeTracker
    {
#pragma warning disable 414
        internal IntPtr m_NativeObjectPtr;
        private SerializedObject m_Object;
        public SerializedObjectChangeTracker(SerializedObject obj)
        {
            m_NativeObjectPtr = Internal_Create(obj);
            m_Object = obj;
        }

        ~SerializedObjectChangeTracker()
        {
            Dispose();
        }

        [ThreadAndSerializationSafe()]
        public void Dispose()
        {
            if (m_NativeObjectPtr != IntPtr.Zero)
            {
                Internal_Destroy(m_NativeObjectPtr);
                m_NativeObjectPtr = IntPtr.Zero;
                m_Object = null;
            }
        }

        internal extern UInt64 CurrentRevision
        {
            [NativeMethod("GetCurrentRevision")]
            get;
        }

        internal bool UpdateTrackedVersion() {return UpdateTrackedVersion(m_Object); }

        [NativeName("UpdateTrackedVersion")]
        extern public bool UpdateTrackedVersion(SerializedObject obj);

        [NativeMethod(Name = "SerializedObjectChangeTracker::Internal_Create", IsFreeFunction = true, ThrowsException = false)]
        private extern static IntPtr Internal_Create(SerializedObject obj);

        [NativeMethod(Name = "SerializedObjectChangeTracker::Internal_Destroy", IsThreadSafe = true, ThrowsException = false)]
        private extern static void Internal_Destroy(IntPtr native);
    }
}
