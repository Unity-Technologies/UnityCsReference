// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine;

namespace UnityEditor.Build.Content
{
    [UsedByNativeCode]
    [NativeHeader("Modules/BuildPipeline/Editor/Public/BuildUsageCache.h")]
    public class BuildUsageCache : IDisposable
    {
        private IntPtr m_Ptr;

        public BuildUsageCache()
        {
            m_Ptr = Internal_Create();
        }

        ~BuildUsageCache()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        [NativeMethod(IsThreadSafe = true)]
        private static extern IntPtr Internal_Create();

        [NativeMethod(IsThreadSafe = true)]
        private static extern void Internal_Destroy(IntPtr ptr);
    }
}
