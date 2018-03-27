// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.Experimental.U2D
{
    [NativeHeader("Runtime/2D/Common/PixelSnapping.h")]
    public static class PixelPerfectRendering
    {
        extern static public float pixelSnapSpacing
        {
            [FreeFunction("GetPixelSnapSpacing")]
            get;

            [FreeFunction("SetPixelSnapSpacing")]
            set;
        }
    }
}
