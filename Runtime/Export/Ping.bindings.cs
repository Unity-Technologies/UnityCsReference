// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Export/Ping.bindings.h")]
    public sealed partial class Ping
    {
        internal IntPtr m_Ptr;

        public Ping(string address)
        {
            m_Ptr = Internal_Create(address);
        }

        ~Ping()
        {
            DestroyPing();
        }

        [ThreadAndSerializationSafe]
        public void DestroyPing()
        {
            Internal_Destroy(m_Ptr);
            m_Ptr = IntPtr.Zero;
        }

        [NativeMethod(Name = "DestroyPing", IsFreeFunction = true, IsThreadSafe = true)]
        private static extern void Internal_Destroy(IntPtr ptr);
        [FreeFunction("CreatePing")]
        private static extern IntPtr Internal_Create(string address);

        public bool isDone
        {
            get
            {
                if (m_Ptr == IntPtr.Zero)
                    return false;

                return Internal_IsDone();
            }
        }

        [NativeName("GetIsDone")]
        private extern bool Internal_IsDone();

        public extern int time { get; }

        public extern string ip
        {
            [NativeName("GetIP")]
            get;
        }
    }

}
