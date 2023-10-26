// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;

namespace UnityEngine
{
    public partial class BoxCollider
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use BoxCollider.size instead. (UnityUpgradable) -> size")]
        public Vector3 extents { get { return size * 0.5F; } set { size = value * 2.0F; } }
    }
}
