// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(GridBrushBase))]
    public class GridBrushEditorBase : Editor
    {
        private static class Styles
        {
            public static readonly Color activeColor = new Color(1f, .5f, 0f);
            public static readonly Color executingColor = new Color(1f, .75f, 0.25f);
        }

        public virtual void OnPaintSceneGUI(GridLayout gridLayout, GameObject brushTarget, BoundsInt position, GridBrushBase.Tool tool, bool executing)
        {
            OnPaintSceneGUIInternal(gridLayout, brushTarget, position, tool, executing);
        }

        public virtual void OnPaintInspectorGUI()
        {
            OnInspectorGUI();
        }

        public virtual void OnSelectionInspectorGUI() {}

        public virtual void OnMouseLeave() {}
        public virtual void OnMouseEnter() {}
        public virtual void OnToolActivated(GridBrushBase.Tool tool) {}
        public virtual void OnToolDeactivated(GridBrushBase.Tool tool) {}
        public virtual void RegisterUndo(GameObject brushTarget, GridBrushBase.Tool tool) {}
        public virtual GameObject[] validTargets { get { return null; } }

        internal static void OnPaintSceneGUIInternal(GridLayout gridLayout, GameObject brushTarget, BoundsInt position, GridBrushBase.Tool tool, bool executing)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Color color = Color.white;
            if (tool == GridBrushBase.Tool.Pick && executing)
                color = Color.cyan;
            if (tool == GridBrushBase.Tool.Paint && executing)
                color = Color.yellow;

            if (tool == GridBrushBase.Tool.Select ||
                tool == GridBrushBase.Tool.Move)
            {
                if (executing)
                    color = Styles.executingColor;
                else if (GridSelection.active)
                    color = Styles.activeColor;
            }

            GridEditorUtility.DrawGridMarquee(gridLayout, position, color);
        }
    }
}
