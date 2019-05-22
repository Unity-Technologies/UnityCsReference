// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;

namespace UnityEngine.Profiling.Experimental
{
    public struct DebugScreenCapture
    {
        public NativeArray<byte> rawImageDataReference { get; set; }
        public TextureFormat imageFormat { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }
}
