// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;

namespace UnityEngine
{
    public partial class MeshCollider
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Configuring smooth sphere collisions is no longer needed.", true)]
        public bool smoothSphereCollisions { get { return true; } set { } }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("MeshCollider.skinWidth is no longer used.")]
        public float skinWidth { get { return 0f; } set { } }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("MeshCollider.inflateMesh is no longer supported. The new cooking algorithm doesn't need inflation to be used.")]
        public bool inflateMesh
        {
            get { return false; }
            set { }
        }
    }
}
