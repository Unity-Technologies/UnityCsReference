// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace UnityEditor.Snap
{
    internal static class Shortcuts
    {
        [Shortcut("Snap/Toggle Snap", typeof(SceneView), KeyCode.Backslash)]
        internal static void ToggleSnap()
        {
            EditorSnapSettings.enabled = !EditorSnapSettings.enabled;
        }

        [Shortcut("Grid/Increase Grid Size", typeof(SceneView), KeyCode.RightBracket, ShortcutModifiers.Action)]
        internal static void IncreaseGridSize()
        {
            if (!EditorSnapSettings.enabled)
                return;
            var val = EditorSnapSettings.snapMultiplier;
            if (val.x < int.MaxValue / 2)
                val.x *= 2;
            if (val.y < int.MaxValue / 2)
                val.y *= 2;
            if (val.z < int.MaxValue / 2)
                val.z *= 2;
            EditorSnapSettings.snapMultiplier = val;
        }

        [Shortcut("Grid/Decrease Grid Size", typeof(SceneView), KeyCode.LeftBracket, ShortcutModifiers.Action)]
        internal static void DecreaseGridSize()
        {
            if (!EditorSnapSettings.enabled)
                return;

            var val = EditorSnapSettings.snapMultiplier;
            if (val.x > 1)
                val.x /= 2;
            if (val.y > 1)
                val.y /= 2;
            if (val.z > 1)
                val.z /= 2;
            EditorSnapSettings.snapMultiplier = val;
        }

        [Shortcut("Grid/Reset Grid", typeof(SceneView))]
        internal static void ResetGrid()
        {
            MenuNudgePerspectiveReset();
            ResetGridSize();
        }

        internal static void ResetGridSize()
        {
            if (!EditorSnapSettings.enabled)
                return;

            EditorSnapSettings.ResetMultiplier();
        }

        [Shortcut("Grid/Nudge Grid Backward", typeof(SceneView), KeyCode.LeftBracket, ShortcutModifiers.Shift)]
        internal static void MenuNudgePerspectiveBackward()
        {
            SceneView sv = SceneView.lastActiveSceneView;
            SceneViewGrid.Grid grid = sv.sceneViewGrids.activeGrid;
            SceneViewGrid.GridRenderAxis axis = sv.sceneViewGrids.gridAxis;
            Vector3 v = sv.sceneViewGrids.GetPivot(axis);
            switch (axis)
            {
                case SceneViewGrid.GridRenderAxis.X:
                    v -= Vector3.right * EditorSnapSettings.move.x;
                    break;
                case SceneViewGrid.GridRenderAxis.Y:
                    v -= Vector3.up * EditorSnapSettings.move.y;
                    break;
                case SceneViewGrid.GridRenderAxis.Z:
                    v -= Vector3.forward * EditorSnapSettings.move.z;
                    break;
            }

            sv.sceneViewGrids.SetPivot(axis, v);
            sv.Repaint();
        }

        [Shortcut("Grid/Nudge Grid Forward", typeof(SceneView), KeyCode.RightBracket, ShortcutModifiers.Shift)]
        internal static void MenuNudgePerspectiveForward()
        {
            SceneView sv = SceneView.lastActiveSceneView;
            SceneViewGrid.Grid grid = sv.sceneViewGrids.activeGrid;
            SceneViewGrid.GridRenderAxis axis = sv.sceneViewGrids.gridAxis;
            Vector3 v = sv.sceneViewGrids.GetPivot(axis);
            switch (axis)
            {
                case SceneViewGrid.GridRenderAxis.X:
                    v += Vector3.right * EditorSnapSettings.move.x;
                    break;
                case SceneViewGrid.GridRenderAxis.Y:
                    v += Vector3.up * EditorSnapSettings.move.y;
                    break;
                case SceneViewGrid.GridRenderAxis.Z:
                    v += Vector3.forward * EditorSnapSettings.move.z;
                    break;
            }

            sv.sceneViewGrids.SetPivot(axis, v);
            sv.Repaint();
        }

        internal static void MenuNudgePerspectiveReset()
        {
            SceneView sv = SceneView.lastActiveSceneView;
            sv.ResetGrid();
            sv.Repaint();
        }
    }
}
