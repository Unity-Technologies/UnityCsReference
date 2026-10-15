// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    // Drag handle that sets the maximum height of the pinned modules area. Only shown while the area scrolls.
    class PinnedModulesAreaResizer : PointerManipulator
    {
        const string k_UssClass_Visible = "profiler-view__pinned-modules-resizer--visible";
        internal const string k_HeightPreferenceKey = "ProfilerWindow.PinnedModulesAreaHeight";
        internal const float k_MinimumHeight = 50f;
        const float k_MaximumParentFraction = 0.75f;
        // Matches the height in ProfilerWindow.uss; only used before the band's first layout.
        const float k_FallbackHitBandHeight = 11f;

        readonly ScrollView m_PinnedModulesScroll;

        bool m_Active;
        int m_ActivePointerId;
        float m_DragStartY;
        float m_DragStartHeight;
        // NaN until the user drags, which leaves the USS max-height in charge.
        float m_CapHeight = float.NaN;

        public PinnedModulesAreaResizer(VisualElement resizer, ScrollView pinnedModulesScroll)
        {
            m_PinnedModulesScroll = pinnedModulesScroll;

            if (EditorPrefs.HasKey(k_HeightPreferenceKey))
                m_CapHeight = Mathf.Max(EditorPrefs.GetFloat(k_HeightPreferenceKey), k_MinimumHeight);

            m_PinnedModulesScroll.contentViewport.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            m_PinnedModulesScroll.contentContainer.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            resizer.AddManipulator(this);
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        }

        bool IsScrollable =>
            m_PinnedModulesScroll.contentContainer.layout.height >
            m_PinnedModulesScroll.contentViewport.layout.height + 0.5f;

        void OnGeometryChanged(GeometryChangedEvent evt) => UpdateResizer();

        void UpdateResizer()
        {
            // Re-clamp on resize without overwriting the saved preference.
            if (!m_Active && !float.IsNaN(m_CapHeight))
                m_PinnedModulesScroll.style.maxHeight = ClampCap(m_CapHeight, ParentHeight);

            var bandHeight = target.resolvedStyle.height;
            if (float.IsNaN(bandHeight) || bandHeight <= 0f)
                bandHeight = k_FallbackHitBandHeight;
            target.style.top = m_PinnedModulesScroll.layout.yMax - bandHeight * 0.5f;

            if (!m_Active)
                target.EnableInClassList(k_UssClass_Visible, IsScrollable);
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (m_Active || evt.button != 0)
                return;

            m_Active = true;
            m_ActivePointerId = evt.pointerId;
            target.CapturePointer(evt.pointerId);
            m_DragStartY = evt.position.y;
            m_DragStartHeight = m_PinnedModulesScroll.layout.height;
            evt.StopPropagation();
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (!m_Active || !target.HasPointerCapture(m_ActivePointerId))
                return;

            m_CapHeight = ClampCap(m_DragStartHeight + (evt.position.y - m_DragStartY), ParentHeight);
            m_PinnedModulesScroll.style.maxHeight = m_CapHeight;
            evt.StopPropagation();
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (!m_Active || evt.pointerId != m_ActivePointerId)
                return;

            m_Active = false;
            target.ReleasePointer(evt.pointerId);
            if (!float.IsNaN(m_CapHeight))
                EditorPrefs.SetFloat(k_HeightPreferenceKey, m_CapHeight);
            UpdateResizer();
            evt.StopPropagation();
        }

        float ParentHeight => m_PinnedModulesScroll.parent?.layout.height ?? float.NaN;

        internal static float ClampCap(float cap, float parentHeight)
        {
            var maximumHeight = float.IsNaN(parentHeight)
                ? float.MaxValue
                : Mathf.Max(k_MinimumHeight, parentHeight * k_MaximumParentFraction);
            return Mathf.Clamp(cap, k_MinimumHeight, maximumHeight);
        }
    }
}
