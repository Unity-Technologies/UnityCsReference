// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    sealed class ToolbarCanvasModeDefinition : OverlayCanvasModeDefinition
    {
        public override VisualTreeAsset GetUXML()
        {
            return EditorGUIUtility.Load("UXML/Overlays/main-toolbar-overlay-canvas.uxml") as VisualTreeAsset; ;
        }

        public override OverlayDockArea GetDockArea(OverlayCanvas canvas, VisualElement horizontalParent, VisualElement verticalParent, VisualElement anchoredParent) => null;
    }
}
