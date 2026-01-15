// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace UnityEditor.Snap
{
    static class Shortcuts
    {
        [Shortcut("Grid and Snap/Toggle Snapping", typeof(SceneView), KeyCode.Backslash)]
        internal static void ToggleGridSnap()
        {
            EditorSnapSettings.snapEnabled = !EditorSnapSettings.snapEnabled;
        }

        [Shortcut("Grid and Snap/Increase Grid Size", typeof(SceneView), KeyCode.RightBracket,
            ShortcutModifiers.Action)]
        internal static void IncreaseGridSize()
        {
            GridSettings.instance.sizeMultiplier++;
            SceneView.RepaintAll();
        }

        [Shortcut("Grid and Snap/Decrease Grid Size", typeof(SceneView), KeyCode.LeftBracket, ShortcutModifiers.Action)]
        internal static void DecreaseGridSize()
        {
            GridSettings.instance.sizeMultiplier--;
            SceneView.RepaintAll();
        }

        [Shortcut("Grid and Snap/Reset Grid", typeof(SceneView))]
        internal static void ResetGrid()
        {
            GridSettings.instance.ResetGridSettings();
            SceneView.RepaintAll();
        }
        
        [Shortcut("Grid and Snap/Nudge Grid Forward", typeof(SceneView))]
        internal static void MenuNudgePerspectiveForward()
        {
            NudgeGrid(true);
        }
        
        [Shortcut("Grid and Snap/Nudge Grid Backward", typeof(SceneView))]
        internal static void MenuNudgePerspectiveBackward()
        {
            NudgeGrid(false);
        }

        internal static void NudgeGrid(bool forward)
        {
            SceneView sv = SceneView.lastActiveSceneView;
            SceneViewGrid.GridRenderAxis axis = sv.sceneViewGrids.gridAxis;
            Vector3 gridSize = GridSettings.instance.gridSize;

            var sign = forward ? 1f : -1f;
            var nudgeAmount = Vector3.zero;
            var gridSettings = GridSettings.instance;

            switch (axis)
            {
                case SceneViewGrid.GridRenderAxis.X:
                    nudgeAmount = (gridSettings.rotation * Vector3.right) * gridSize.x;
                    break;
                case SceneViewGrid.GridRenderAxis.Y:
                    nudgeAmount = (gridSettings.rotation * Vector3.up) * gridSize.y;
                    break;
                case SceneViewGrid.GridRenderAxis.Z:
                    nudgeAmount = (gridSettings.rotation * Vector3.forward) * gridSize.z;
                    break;
            }

            if (gridSettings.activeModeIndex != GridMode.Custom)
            {
                gridSettings.ActivateMode(GridMode.Custom);
                GridSettings.instance.rotation = Quaternion.identity;
            }

            GridSettings.instance.position += sign * nudgeAmount;
            
            sv.Repaint();
        }

        [Shortcut("Grid and Snap/Push To Grid", typeof(SceneView), KeyCode.Backslash, ShortcutModifiers.Action)]
        internal static void PushToGrid()
        {
            GridSnapping.SnapSelectionToGrid();
        }

        [Shortcut("Grid and Snap/Align To Grid", typeof(SceneView), KeyCode.Backslash,
            ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        internal static void AlignToGrid()
        {
            GridSnapping.AlignSelectionToGrid();
        }

        [Shortcut("Grid and Snap/Move Grid To Active Object Position", typeof(SceneView), KeyCode.Quote,
            ShortcutModifiers.Action)]
        internal static void MoveGridToActiveObject()
        {
            var activeGO = Selection.activeGameObject;
            if (activeGO != null)
                GridSettings.instance.ApplyCustomPosition(activeGO.transform.position);
        }

        [Shortcut("Grid and Snap/Align Grid To Active Object Rotation", typeof(SceneView), KeyCode.Quote,
            ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        internal static void AlignGridToActiveObject()
        {
            var activeGO = Selection.activeGameObject;
            if (activeGO != null)
                GridSettings.instance.SampleTransformRotation(activeGO.transform);
        }

        [Shortcut("Grid and Snap/Move Grid To Handle Position", typeof(SceneView), KeyCode.Semicolon,
            ShortcutModifiers.Action)]
        internal static void MoveGridToHandle()
        {
            GridSettings.instance.ApplyCustomPosition(Tools.handlePosition);
        }

        [Shortcut("Grid and Snap/Align Grid To Handle Rotation", typeof(SceneView), KeyCode.Semicolon,
            ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        internal static void AlignGridToHandle()
        {
            GridSettings.instance.ApplyCustomRotation(Tools.handleRotation);
        }

        [Shortcut("Grid and Snap/Apply Last Custom Values", typeof(SceneView), KeyCode.Slash, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        internal static void ApplyLastCustomValues()
        {
            var gridSettings = GridSettings.instance;
            gridSettings.ActivateMode(GridMode.Custom);
            SceneView.RepaintAll();
        }

        [Shortcut("Grid and Snap/Reset to World", typeof(SceneView), KeyCode.Slash, ShortcutModifiers.Action)]
        internal static void ResetToWorld()
        {
            var gridSettings = GridSettings.instance;
            gridSettings.ActivateMode(GridMode.World);
            SceneView.RepaintAll();
        }
    }
}
