// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;

namespace UnityEngine.iOS
{

    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [NativeHeader("Runtime/Export/iOS/OnDemandResources.h")]
    public sealed partial class OnDemandResourcesRequest : AsyncOperation, IDisposable
    {
        extern public string error { get; }
        extern public float  loadingPriority { get; set; }

        extern public string GetResourcePath(string resourceName);

        [ThreadSafe] extern private static void DestroyFromScript(IntPtr ptr);

        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                DestroyFromScript(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }

        internal OnDemandResourcesRequest() {}
        ~OnDemandResourcesRequest()         { Dispose(); }
    }

    [NativeHeader("Runtime/Export/iOS/OnDemandResources.h")]
    public static partial class OnDemandResources
    {
        extern public static bool enabled {[FreeFunction("OnDemandResourcesScripting::Enabled")] get; }

        [FreeFunction("OnDemandResourcesScripting::PreloadAsync")]
        extern private static OnDemandResourcesRequest PreloadAsyncImpl(string[] tags);

        public static OnDemandResourcesRequest PreloadAsync(string[] tags)
        {
            return PreloadAsyncImpl(tags);
        }
    }

}
