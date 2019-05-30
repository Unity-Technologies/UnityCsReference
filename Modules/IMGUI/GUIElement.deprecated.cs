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
    [Obsolete("GUIElement has been removed.", true)]
    public sealed class GUIElement
    {
        static void FeatureRemoved() { throw new Exception("GUIElement has been removed from Unity."); }

        [Obsolete("GUIElement has been removed.", true)]
        public bool HitTest(Vector3 screenPosition)
        {
            FeatureRemoved();
            return false;
        }

        [Obsolete("GUIElement has been removed.", true)]
        public bool HitTest(Vector3 screenPosition, [UnityEngine.Internal.DefaultValue("null")] Camera camera)
        {
            FeatureRemoved();
            return false;
        }

        [Obsolete("GUIElement has been removed.", true)]
        public Rect GetScreenRect([UnityEngine.Internal.DefaultValue("null")] Camera camera)
        {
            FeatureRemoved();
            return new Rect(0, 0, 0, 0);
        }

        [Obsolete("GUIElement has been removed.", true)]
        public Rect GetScreenRect()
        {
            FeatureRemoved();
            return new Rect(0, 0, 0, 0);
        }
    }
}
