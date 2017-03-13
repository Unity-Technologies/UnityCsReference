// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [System.Serializable]
    internal class CurveEditorSettings
    {
        // Label settings
        private TickStyle m_HTickStyle = new TickStyle();
        private TickStyle m_VTickStyle = new TickStyle();

        internal TickStyle hTickStyle { get { return m_HTickStyle; } set { m_HTickStyle = value; } }
        internal TickStyle vTickStyle { get { return m_VTickStyle; } set { m_VTickStyle = value; } }

        // Range lock settings
        private bool m_HRangeLocked;
        private bool m_VRangeLocked;
        internal bool hRangeLocked { get { return m_HRangeLocked; } set { m_HRangeLocked = value; } }
        internal bool vRangeLocked { get { return m_VRangeLocked; } set { m_VRangeLocked = value; } }

        // Range settings
        private float m_HRangeMin = Mathf.NegativeInfinity;
        private float m_HRangeMax = Mathf.Infinity;
        private float m_VRangeMin = Mathf.NegativeInfinity;
        private float m_VRangeMax = Mathf.Infinity;
        public float hRangeMin { get { return m_HRangeMin; } set { m_HRangeMin = value; } }
        public float hRangeMax { get { return m_HRangeMax; } set { m_HRangeMax = value; } }
        public float vRangeMin { get { return m_VRangeMin; } set { m_VRangeMin = value; } }
        public float vRangeMax { get { return m_VRangeMax; } set { m_VRangeMax = value; } }
        public bool hasUnboundedRanges
        {
            get
            {
                return
                    m_HRangeMin == Mathf.NegativeInfinity ||
                    m_HRangeMax == Mathf.Infinity ||
                    m_VRangeMin == Mathf.NegativeInfinity ||
                    m_VRangeMax == Mathf.Infinity;
            }
        }

        // Offset to move the labels along the horizontal axis to make room for the overlaid scrollbar in the
        // curve editor popup.
        public float hTickLabelOffset = 0;
        public EditorGUIUtility.SkinnedColor wrapColor = new EditorGUIUtility.SkinnedColor(new Color(1.0f, 1.0f, 1.0f, 0.5f), new Color(.65f, .65f, .65f, 0.5f));
        public bool useFocusColors = false;
        public bool showAxisLabels = true;
        public bool showWrapperPopups = false;
        public bool allowDraggingCurvesAndRegions = true;
        public bool allowDeleteLastKeyInCurve = false;
        public bool undoRedoSelection = false;

        // Display options
        internal enum RectangleToolFlags
        {
            NoRectangleTool,
            MiniRectangleTool,
            FullRectangleTool
        }

        internal RectangleToolFlags rectangleToolFlags = RectangleToolFlags.NoRectangleTool;

        // Window resize settings
        private bool m_ScaleWithWindow = true;
        internal bool scaleWithWindow { get { return m_ScaleWithWindow; } set { m_ScaleWithWindow = value; } }

        // Slider settings
        private bool m_HSlider = true;
        private bool m_VSlider = true;
        public bool hSlider { get { return m_HSlider; } set { m_HSlider = value; } }
        public bool vSlider { get { return m_VSlider; } set { m_VSlider = value; } }
    }
}
