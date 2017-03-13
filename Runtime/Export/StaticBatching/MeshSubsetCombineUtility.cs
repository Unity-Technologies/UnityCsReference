// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;

namespace UnityEngine
{
    internal class MeshSubsetCombineUtility
    {
        public struct MeshInstance
        {
            public int       meshInstanceID;
            public int       rendererInstanceID;
            public int       additionalVertexStreamsMeshInstanceID;
            public Matrix4x4 transform;
            public Vector4   lightmapScaleOffset;
            public Vector4   realtimeLightmapScaleOffset;
        }

        public struct SubMeshInstance
        {
            public int meshInstanceID;
            public int vertexOffset;
            public int gameObjectInstanceID;
            public int subMeshIndex;
            public Matrix4x4 transform;
        }

        public struct MeshContainer
        {
            public GameObject gameObject;
            public MeshInstance instance;
            public List<SubMeshInstance> subMeshInstances;
        }
    }
} // namespace UnityEngine
