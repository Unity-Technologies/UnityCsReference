// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Rendering
{
    // match DrawRendererSortSettings on C++ side
    [StructLayout(LayoutKind.Sequential)]
    public struct DrawRendererSortSettings
    {
        public Matrix4x4 worldToCameraMatrix;
        public Vector3 cameraPosition;
        public SortFlags flags;
        private int _sortOrthographic;
        private Matrix4x4 _previousVPMatrix;
        private Matrix4x4 _nonJitteredVPMatrix;

        public bool sortOrthographic
        {
            get { return _sortOrthographic != 0; }
            set { _sortOrthographic = value ? 1 : 0; }
        }
    }
}
