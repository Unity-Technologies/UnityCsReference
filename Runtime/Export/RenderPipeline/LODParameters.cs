// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LODParameters
    {
        public bool isOrthographic;
        public Vector3 cameraPosition;
        public float fieldOfView;
        public float orthoSize;
        public int cameraPixelHeight;
    }
}
