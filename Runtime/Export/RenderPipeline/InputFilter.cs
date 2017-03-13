// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Rendering
{
    // match layout of InputFilter on C++ side
    [StructLayout(LayoutKind.Sequential)]
    public struct InputFilter
    {
        public int renderQueueMin;
        public int renderQueueMax;
        public int layerMask;

        public static InputFilter Default()
        {
            var res = new InputFilter();
            res.renderQueueMin = 0;
            res.renderQueueMax = 5000;
            res.layerMask = ~0;
            return res;
        }

        public void SetQueuesOpaque()
        {
            renderQueueMin = 0;
            renderQueueMax = (int)UnityEngine.Rendering.RenderQueue.GeometryLast;
        }

        public void SetQueuesTransparent()
        {
            renderQueueMin = (int)UnityEngine.Rendering.RenderQueue.GeometryLast + 1;
            renderQueueMax = 5000;
        }
    }
}
