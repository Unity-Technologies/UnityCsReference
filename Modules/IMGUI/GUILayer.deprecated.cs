// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [ExcludeFromPreset]
    [ExcludeFromObjectFactory]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("GUILayer has been removed.", true)]
    public sealed class GUILayer
    {
        [Obsolete("GUILayer has been removed.", true)]
        public GUIElement HitTest(Vector3 screenPosition)
        {
            throw new Exception("GUILayer has been removed from Unity.");
        }
    }
}
