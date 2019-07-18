// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    [System.Serializable]
    internal class PreviewResizer
    {
        // The raw preview size while dragging (not snapped to allowed values) (shared)
        static float s_DraggedPreviewSize = 0;
        // The returned preview size while dragging (shared)
        static float s_CachedPreviewSizeWhileDragging = 0;
        static float s_MouseDownLocation, s_MouseDownValue;
        static bool s_MouseDragged;

        // The last saved preview size - only saved when not dragging
        // The saved value is the size when expanded - when collapsed the value is negative,
        // so it can be restored when expanded again.
        [SerializeField]
        private float m_CachedPref;
        [SerializeField]
        private int m_ControlHash;
        [SerializeField]
        private string m_PrefName;

        private int m_Id = 0;

        private int id
        {
            get
            {
                if (m_Id == 0)
                    m_Id = EditorGUIUtility.GetControlID(m_ControlHash, FocusType.Passive, new Rect());
                return m_Id;
            }
        }

        public bool localFrame { get; set; }

        // Instances of this class should be serialized.
        // The Init function will only have effect if the serialized values are not already set.
        public void Init(string prefName)
        {
            if (m_ControlHash != 0 && !string.IsNullOrEmpty(m_PrefName))
                return;

            // The controlHash is set by the calling code
            // There's one for the Inspector, one for LightmapEditor, etc.
            m_ControlHash = prefName.GetHashCode();

            // We'll have one pref name per controlHash
            m_PrefName = "Preview_" + prefName;

            // This is the only place the pref is read. This means we can have e.g. multiple
            // Inspectors that can be controlled individually as long as they're open.
            m_CachedPref = EditorPrefs.GetFloat(m_PrefName, 1);
        }

        public float ResizeHandle(Rect windowPosition, float minSize, float minRemainingSize, float resizerHeight)
        {
            return ResizeHandle(windowPosition, minSize, minRemainingSize, resizerHeight, new Rect());
        }

        public float ResizeHandle(Rect windowPosition, float minSize, float minRemainingSize, float resizerHeight, Rect dragRect)
        {
            // Sanity check the cached value. It can be positive or negative, but never smaller than the minSize
            if (Mathf.Abs(m_CachedPref) < minSize)
                m_CachedPref = minSize * Mathf.Sign(m_CachedPref);

            float maxPreviewSize = windowPosition.height - minRemainingSize;
            bool dragging = (GUIUtility.hotControl == id);

            float previewSize = (dragging ? s_DraggedPreviewSize : Mathf.Max(0, m_CachedPref));
            bool expanded = (m_CachedPref > 0);
            float lastSize = Mathf.Abs(m_CachedPref);

            Rect resizerRect = new Rect(0, windowPosition.height - previewSize - resizerHeight, windowPosition.width, resizerHeight);
            if (dragRect.width != 0)
            {
                resizerRect.x = dragRect.x;
                resizerRect.width = dragRect.width;
            }

            bool expandedBefore = expanded;
            previewSize = -PixelPreciseCollapsibleSlider(id, resizerRect, -previewSize, -maxPreviewSize, -0, ref expanded, localFrame);
            previewSize = Mathf.Min(previewSize, maxPreviewSize);
            dragging = (GUIUtility.hotControl == id);

            if (dragging)
                s_DraggedPreviewSize = previewSize;

            // First snap size between 0 and minimum size
            if (previewSize < minSize)
                previewSize = (previewSize < minSize * 0.5f ? 0 : minSize);

            // If user clicked area, adjust size
            if (expanded != expandedBefore)
            {
                previewSize = (expanded ? lastSize : 0);
                GUI.changed = true;
            }

            // Determine new expanded state
            expanded = (previewSize >= minSize / 2);

            // Keep track of last preview size while not dragging or collapsed
            // Note we don't want to save when dragging preview OR window size,
            // so just don't save while dragging anything at all
            if (GUIUtility.hotControl == 0)
            {
                if (previewSize > 0)
                    lastSize = previewSize;
                float newPref = lastSize * (expanded ? 1 : -1);
                if (newPref != m_CachedPref)
                {
                    // Save the value to prefs
                    m_CachedPref = newPref;
                    EditorPrefs.SetFloat(m_PrefName, m_CachedPref);
                }
            }

            s_CachedPreviewSizeWhileDragging = previewSize;
            return previewSize;
        }

        // This value will change in realtime while dragging
        public bool GetExpanded()
        {
            if (GUIUtility.hotControl == id)
                return (s_CachedPreviewSizeWhileDragging > 0);
            else
                return (m_CachedPref > 0);
        }

        public float GetPreviewSize()
        {
            if (GUIUtility.hotControl == id)
                return Mathf.Max(0, s_CachedPreviewSizeWhileDragging);
            else
                return Mathf.Max(0, m_CachedPref);
        }

        // This value won't change until we have stopped dragging again
        public bool GetExpandedBeforeDragging()
        {
            return (m_CachedPref > 0);
        }

        public void SetExpanded(bool expanded)
        {
            // Set the sign based on whether it's collapsed or not, then save to prefs
            m_CachedPref = Mathf.Abs(m_CachedPref) * (expanded ? 1 : -1);
            EditorPrefs.SetFloat(m_PrefName, m_CachedPref);

            if (m_Container != null)
            {
                m_Container.style.minHeight = expanded ? containerMinimumHeightExpanded : m_ContainerMinimumHeightCollapsed;
            }
        }

        public void ToggleExpanded()
        {
            // Reverse the sign, then save to prefs
            m_CachedPref = -m_CachedPref;
            EditorPrefs.SetFloat(m_PrefName, m_CachedPref);

            if (m_Container != null)
            {
                m_Container.style.minHeight = m_CachedPref > 0 ? containerMinimumHeightExpanded : m_ContainerMinimumHeightCollapsed;
            }
        }

        // This is the slider behavior for resizing the preview area
        public static float PixelPreciseCollapsibleSlider(int id, Rect position, float value, float min, float max,
            ref bool expanded)
        {
            return PixelPreciseCollapsibleSlider(id, position, value, min, max, ref expanded, false);
        }

        public static float PixelPreciseCollapsibleSlider(int id, Rect position, float value, float min, float max, ref bool expanded, bool localFrame)
        {
            Event evt = Event.current;

            if (evt.type == EventType.Layout)
            {
                return value;
            }

            var mousePosition = evt.mousePosition;

            if (localFrame)
            {
                mousePosition = GUIClip.UnclipToWindow(evt.mousePosition);
                mousePosition.y -= Editor.k_HeaderHeight;
            }

            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (GUIUtility.hotControl == 0 && evt.button == 0 && position.Contains(mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        s_MouseDownLocation = mousePosition.y;
                        s_MouseDownValue = value;
                        s_MouseDragged = false;
                        evt.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        value = Mathf.Clamp(mousePosition.y - s_MouseDownLocation + s_MouseDownValue, min, max - 1);
                        GUI.changed = true;
                        s_MouseDragged = true;
                        evt.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        if (!s_MouseDragged)
                            expanded = !expanded;
                        evt.Use();
                    }
                    break;
                case EventType.Repaint:
                    float x = position.x;
                    float y = position.y;

                    if (localFrame)
                    {
                        x = 0f;
                        y = 0f;
                    }

                    if (GUIUtility.hotControl == 0)
                    {
                        EditorGUIUtility.AddCursorRect(new Rect(x, y, position.width, position.height), MouseCursor.SplitResizeUpDown);
                    }

                    if (GUIUtility.hotControl == id)
                    {
                        const int yMove = 100;
                        const int heightMove = yMove * 2;
                        EditorGUIUtility.AddCursorRect(new Rect(x, y - yMove, position.width, position.height + heightMove), MouseCursor.SplitResizeUpDown);
                    }
                    break;
            }
            return value;
        }

        IMGUIContainer m_Container;
        internal float containerMinimumHeightExpanded;
        float m_ContainerMinimumHeightCollapsed;

        internal void SetContainer(IMGUIContainer container, float minimumHeightCollapsed)
        {
            m_Container = container;
            m_ContainerMinimumHeightCollapsed = minimumHeightCollapsed;
        }
    }
}
