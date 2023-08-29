// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;


namespace UnityEditor.Analytics
{
    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    public class MetalPatchShaderComputeBufferAnalytic : UnityEngine.Analytics.AnalyticsEventBase
    {
        public MetalPatchShaderComputeBufferAnalytic() : base("MetalPatchShaderComputeBuffersUsage", 1) { }

        [UsedByNativeCode]
        internal static MetalPatchShaderComputeBufferAnalytic CreateMetalPatchShaderComputeBufferAnalytic() { return new MetalPatchShaderComputeBufferAnalytic(); }
    }
}
