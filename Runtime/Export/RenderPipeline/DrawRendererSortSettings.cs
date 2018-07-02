// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Rendering
{
    public enum DrawRendererSortMode
    {
        Perspective,
        Orthographic,
        CustomAxis
    }

    // match DrawRendererSortSettings on C++ side
    [StructLayout(LayoutKind.Sequential)]
    public struct DrawRendererSortSettings
    {
        public Matrix4x4 worldToCameraMatrix;
        public Vector3 cameraPosition;
        public Vector3 cameraCustomSortAxis;
        public SortFlags flags;
        public DrawRendererSortMode sortMode;
        private Matrix4x4 _previousVPMatrix;
        private Matrix4x4 _nonJitteredVPMatrix;

        [System.Obsolete("Use sortMode instead")]
        public bool sortOrthographic
        {
            get { return sortMode == DrawRendererSortMode.Orthographic; }
            set { sortMode = value ? DrawRendererSortMode.Orthographic : DrawRendererSortMode.Perspective; }
        }
    }
}
