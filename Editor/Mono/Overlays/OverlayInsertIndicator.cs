// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    class OverlayInsertIndicator : VisualElement
    {
        const string k_ClassName = "unity-overlay-insert-indicator";
        const string k_VerticalState = k_ClassName + "--vertical";
        const string k_Horizontal = k_ClassName + "--horizontal";
        const string k_VisualClass = k_ClassName + "__visual";
        const string k_MarkerClass = k_ClassName + "__marker";
        const string k_FirstVisibleClass = k_ClassName + "--first-visible";
        const string k_InToolbarClass = k_ClassName + "--in-toolbar";

        readonly VisualElement m_Visual;
        readonly VisualElement m_Marker;
        readonly VisualElement m_RenderOnTopParent;

        public OverlayInsertIndicator(VisualElement renderOnTopParent)
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList(k_ClassName);

            m_Marker = new VisualElement();
            m_Marker.AddToClassList(k_MarkerClass);
            Add(m_Marker);

            m_RenderOnTopParent = renderOnTopParent;
            m_Visual = new VisualElement();
            m_Visual.AddToClassList(k_VisualClass);
            m_Marker.RegisterCallback<GeometryChangedEvent>(MarkerGeometryChanged);
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            m_RenderOnTopParent.Add(m_Visual);
            m_Visual.style.display = DisplayStyle.None; // We don't want the visual to show before the size has been set
        }

        void OnDetach(DetachFromPanelEvent evt)
        {
            m_Visual.RemoveFromHierarchy();
        }

        void MarkerGeometryChanged(GeometryChangedEvent evt)
        {
            var targetRect = m_RenderOnTopParent.WorldToLocal(m_Marker.worldBound);
            m_Visual.transform.position = targetRect.position;
            m_Visual.style.width = targetRect.width;
            m_Visual.style.height = targetRect.height;
            m_Visual.style.display = DisplayStyle.Flex;
        }

        public void Setup(bool vertical, bool inToolbar, bool firstVisible)
        {
            style.width = StyleKeyword.Null;
            style.height = StyleKeyword.Null;
            EnableInClassList(k_VerticalState, vertical);
            EnableInClassList(k_Horizontal, !vertical);
            EnableInClassList(k_FirstVisibleClass, firstVisible);
            m_Visual.EnableInClassList(k_InToolbarClass, inToolbar);
        }
    }
}
