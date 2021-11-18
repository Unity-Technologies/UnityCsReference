// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEditor.Toolbars;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    public class OverlayToolbar : VisualElement
    {
        public OverlayToolbar()
        {
            EditorToolbarUtility.LoadStyleSheets("EditorToolbar", this);
            AddToClassList("unity-toolbar-overlay");
        }

        public void SetupChildrenAsButtonStrip()
        {
            EditorToolbarUtility.SetupChildrenAsButtonStrip(this);
        }
    }
}
