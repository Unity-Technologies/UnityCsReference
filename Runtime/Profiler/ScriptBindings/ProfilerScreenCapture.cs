// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Profiling
{
    public struct DebugScreenCapture
    {
        public NativeArray<byte> RawImageDataReference { get; set; }
        public TextureFormat ImageFormat { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
