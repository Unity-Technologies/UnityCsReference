// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;

namespace UnityEngine
{
    partial struct RaycastHit
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use textureCoord2 instead. (UnityUpgradable) -> textureCoord2")]
        public Vector2 textureCoord1 { get { return textureCoord2; } }
    }
}
