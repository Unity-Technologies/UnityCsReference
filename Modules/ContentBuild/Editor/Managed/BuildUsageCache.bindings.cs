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
    ///<summary>Caching object for the Scriptable Build Pipeline.</summary>
    ///<remarks>This class helps improve performance when calling the <see cref="ContentBuildInterface.CalculateBuildUsageTags" /> api multiple times.
    ///
    ///Note: this class and its members exist to provide low-level support for the **Scriptable Build Pipeline** package. This is intended for internal use only; use the &lt;a href="https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@latest/index.html"&gt;Scriptable Build Pipeline package&lt;/a&gt; to implement a fully featured build pipeline. You can install this via the [Package Manager window](/upm-ui.md).</remarks>
    [UsedByNativeCode]
    [NativeHeader("Modules/ContentBuild/Editor/BuildUsage/BuildUsageCache.h")]
    public class BuildUsageCache : IDisposable
    {
        private IntPtr m_Ptr;

        ///<summary>Default contructor.</summary>
        ///<remarks>Internal use only. See <see cref="BuildUsageCache" />.</remarks>
        public BuildUsageCache()
        {
            m_Ptr = Internal_Create();
        }

        ~BuildUsageCache()
        {
            Dispose(false);
        }

        ///<summary>Dispose the BuildUsageCache destroying all internal state.</summary>
        ///<remarks>Internal use only. See <see cref="BuildUsageCache" />.</remarks>
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

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(BuildUsageCache buildUsageCache) => buildUsageCache.m_Ptr;
        }
    }
}
