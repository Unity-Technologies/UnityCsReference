// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RenderQueueRange
    {
        public int min;
        public int max;

        public static RenderQueueRange all
        {
            get { return new RenderQueueRange { min = 0, max = 5000 }; }
        }

        public static RenderQueueRange opaque
        {
            get { return new RenderQueueRange { min = 0, max = (int)UnityEngine.Rendering.RenderQueue.GeometryLast }; }
        }

        public static RenderQueueRange transparent
        {
            get { return new RenderQueueRange { min = (int)UnityEngine.Rendering.RenderQueue.GeometryLast + 1, max = 5000 }; }
        }
    }
}
