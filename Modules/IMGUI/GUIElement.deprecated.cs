// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    [ExcludeFromPreset]
    [ExcludeFromObjectFactory]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("GUIElement has been removed. Consider using https://docs.unity3d.com/ScriptReference/UIElements.Image.html, https://docs.unity3d.com/ScriptReference/UIElements.TextElement.html or TextMeshPro instead.", true)]
    public sealed class GUIElement
    {
        static void FeatureRemoved() { throw new Exception("GUIElement has been removed from Unity. Consider using https://docs.unity3d.com/ScriptReference/UIElements.Image.html, https://docs.unity3d.com/ScriptReference/UIElements.TextElement.html or TextMeshPro instead."); }

        [Obsolete("GUIElement has been removed. Consider using https://docs.unity3d.com/ScriptReference/UIElements.Image.html, https://docs.unity3d.com/ScriptReference/UIElements.TextElement.html or TextMeshPro instead.", true)]
        public bool HitTest(Vector3 screenPosition)
        {
            FeatureRemoved();
            return false;
        }

        [Obsolete("GUIElement has been removed. Consider using https://docs.unity3d.com/ScriptReference/UIElements.Image.html, https://docs.unity3d.com/ScriptReference/UIElements.TextElement.html or TextMeshPro instead.", true)]
        public bool HitTest(Vector3 screenPosition, [Internal.DefaultValue("null")] Camera camera)
        {
            FeatureRemoved();
            return false;
        }

        [Obsolete("GUIElement has been removed. Consider using https://docs.unity3d.com/ScriptReference/UIElements.Image.html, https://docs.unity3d.com/ScriptReference/UIElements.TextElement.html or TextMeshPro instead.", true)]
        public Rect GetScreenRect([Internal.DefaultValue("null")] Camera camera)
        {
            FeatureRemoved();
            return new Rect(0, 0, 0, 0);
        }

        [Obsolete("GUIElement has been removed. Consider using https://docs.unity3d.com/ScriptReference/UIElements.Image.html, https://docs.unity3d.com/ScriptReference/UIElements.TextElement.html or TextMeshPro instead.", true)]
        public Rect GetScreenRect()
        {
            FeatureRemoved();
            return new Rect(0, 0, 0, 0);
        }
    }
}
