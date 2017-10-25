// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [NativeHeader("Runtime/Export/BootConfig.bindings.h")]
    internal class BootConfigData
    {
        #pragma warning disable 0414
        private IntPtr m_Ptr;
        #pragma warning restore 0414

        public void AddKey(string key)
        {
            Append(key, null);
        }

        public string Get(string key)
        {
            return GetValue(key, 0);
        }

        public string Get(string key, int index)
        {
            return GetValue(key, index);
        }

        extern public void Append(string key, string value);
        extern public void Set(string key, string value);
        extern private string GetValue(string key, int index);

        [RequiredByNativeCode]
        static BootConfigData WrapBootConfigData(IntPtr nativeHandle)
        {
            return new BootConfigData(nativeHandle);
        }

        private BootConfigData(IntPtr nativeHandle)
        {
            if (nativeHandle == IntPtr.Zero)
                throw new ArgumentException("native handle can not be null");
            m_Ptr = nativeHandle;
        }
    }
}
