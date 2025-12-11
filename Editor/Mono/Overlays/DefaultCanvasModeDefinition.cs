// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    sealed class DefaultCanvasModeDefinition : OverlayCanvasModeDefinition
    {
        static VisualTreeAsset m_UXML;

        public override VisualTreeAsset GetUXML()
        {
            if (m_UXML == null)
                m_UXML = EditorGUIUtility.Load("UXML/Overlays/overlay-canvas.uxml") as VisualTreeAsset;

            return m_UXML;
        }

        public override OverlayDockArea GetDockArea(OverlayCanvas canvas, VisualElement parent) => new DefaultModeDockArea(canvas, parent);
    }
}
