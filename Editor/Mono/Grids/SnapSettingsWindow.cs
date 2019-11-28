// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    sealed class SnapSettingsWindow : EditorWindow
    {
        internal const string k_WindowTitle = "Grid and Snap";
        const float k_MinFieldWidth = 150;
        const float k_MinWindowWidth = 220;
        const float k_MinWindowHeight = 32;

        [SerializeField]
        bool m_GridVisualFoldout = true;

        [SerializeField]
        bool m_IncrementSnapFoldout = true;

        [SerializeField]
        bool m_AlignToGridFoldout = true;

        [SerializeField]
        Vector2 m_Scroll = Vector2.zero;

        [MenuItem("Edit/" + k_WindowTitle + " Settings...")]
        static void Init()
        {
            GetWindow<SnapSettingsWindow>(false, k_WindowTitle, true);
        }

        static class Contents
        {
            public static readonly GUIContent rotateValue = EditorGUIUtility.TrTextContent("Rotate", "Snap value for the Rotate tool");
            public static readonly GUIContent scaleValue = EditorGUIUtility.TrTextContent("Scale", "Snap value for the Scale tool");
            public static readonly GUIContent reset = EditorGUIUtility.TrTextContent("Reset");
            public static readonly GUIContent gridVisuals = EditorGUIUtility.TrTextContent("World Grid", "Settings for the Scene View grid that is aligned in world space.");
            public static readonly GUIContent incrementSnap = EditorGUIUtility.TrTextContent("Increment Snap", "Snap values relative to the origin of movement.");
            public static readonly GUIContent alignSelectionToGridHeader = EditorGUIUtility.TrTextContent("Align Selection to Grid", "Snap selected objects to the nearest grid position.");
            public static readonly GUIContent movePivot = EditorGUIUtility.TrTextContent("Set Position", "Sets the grid center point match the handle position, or reset it to world origin.");
            public static readonly GUIContent gridSize = EditorGUIUtility.TrTextContent("Size", "The amount of space between X, Y, and Z grid lines.");
            public static readonly GUIContent moveContent = EditorGUIUtility.TrTextContent("Move", "Snap value for the Move tool per-axis.");

            public static GUIContent[] pushToGrid = new GUIContent[]
            {
                EditorGUIUtility.TrTextContent("All Axes", "Snaps selected objects to the nearest grid point."),
                EditorGUIUtility.TrTextContent("X", "Snaps selected object to the grid on the X axis."),
                EditorGUIUtility.TrTextContent("Y", "Snaps selected object to the grid on the Y axis."),
                EditorGUIUtility.TrTextContent("Z", "Snaps selected object to the grid on the Z axis.")
            };
        }

        static bool gridValueLinked
        {
            get { return EditorPrefs.GetBool("SnapSettingsWindow.gridValueLinked", true); }
            set { EditorPrefs.SetBool("SnapSettingsWindow.gridValueLinked", value); }
        }

        static bool snapValueLinked
        {
            get { return EditorPrefs.GetBool("SnapSettingsWindow.snapValueLinked", true); }
            set { EditorPrefs.SetBool("SnapSettingsWindow.snapValueLinked", value); }
        }

        void OnEnable()
        {
            minSize = new Vector2(k_MinWindowWidth, k_MinWindowHeight);
        }

        public void OnGUI()
        {
            bool wideMode = EditorGUIUtility.wideMode;
            bool hierarchyMode = EditorGUIUtility.hierarchyMode;
            EditorGUIUtility.wideMode = true;
            EditorGUIUtility.hierarchyMode = true;

            // if the width is less than prefix width + field width, subtract from the prefix label until we start clipping
            // labels.
            float desired = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = position.width < (desired + k_MinFieldWidth)
                ? Mathf.Min(desired, Mathf.Max(72, position.width - k_MinFieldWidth))
                : desired;

            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);

            m_GridVisualFoldout = DoTitlebar(m_GridVisualFoldout, Contents.gridVisuals, GridSettings.ResetGridSettings);

            if (m_GridVisualFoldout)
                DoGridVisualSettings();

            m_IncrementSnapFoldout = DoTitlebar(m_IncrementSnapFoldout, Contents.incrementSnap, EditorSnapSettings.ResetSnapSettings);

            if (m_IncrementSnapFoldout)
                DoIncrementSnapSettings();

            m_AlignToGridFoldout = DoTitlebar(m_AlignToGridFoldout, Contents.alignSelectionToGridHeader, null);

            if (m_AlignToGridFoldout)
                DoSnapActions();

            EditorGUILayout.EndScrollView();
            EditorGUIUtility.labelWidth = 0;
            EditorGUIUtility.wideMode = wideMode;
            EditorGUIUtility.hierarchyMode = hierarchyMode;
        }

        bool DoTitlebar(bool isOpen, GUIContent title, GenericMenu.MenuFunction reset)
        {
            // title bars don't need to clip the prefix label since there's nothing to the right
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = position.width;
            EditorGUILayout.BeginHorizontal();
            isOpen = EditorGUILayout.FoldoutTitlebar(isOpen, title, true);
            GUILayout.FlexibleSpace();
            if (reset != null && GUILayout.Button(EditorGUI.GUIContents.titleSettingsIcon, EditorStyles.iconButton))
            {
                var menu = new GenericMenu();
                menu.AddItem(Contents.reset, false, () =>
                {
                    reset();
                    SceneView.RepaintAll();
                });
                menu.ShowAsContext();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = labelWidth;
            return isOpen;
        }

        void DoGridVisualSettings()
        {
            Vector3 grid = GridSettings.size;
            bool linked = gridValueLinked;

            EditorGUI.BeginChangeCheck();
            grid = EditorGUILayout.LinkedVector3Field(Contents.gridSize, grid, ref linked);
            if (EditorGUI.EndChangeCheck())
            {
                gridValueLinked = linked;
                GridSettings.size = grid;
                SceneView.RepaintAll();
            }

            EditorGUI.BeginChangeCheck();

            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(Contents.movePivot);
            if (GUILayout.Button("Handle"))
                foreach (var view in SceneView.sceneViews)
                    ((SceneView)view).sceneViewGrids.SetAllGridsPivot(Snapping.Snap(Tools.handlePosition, GridSettings.size));
            if (GUILayout.Button("Reset"))
                foreach (var view in SceneView.sceneViews)
                    ((SceneView)view).sceneViewGrids.ResetPivot(SceneViewGrid.GridRenderAxis.All);
            GUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();
        }

        void DoIncrementSnapSettings()
        {
            EditorGUI.BeginChangeCheck();
            var linked = snapValueLinked;
            EditorSnapSettings.move = EditorGUILayout.LinkedVector3Field(Contents.moveContent, EditorSnapSettings.move, ref linked);
            snapValueLinked = linked;
            EditorSnapSettings.rotate = EditorGUILayout.FloatField(Contents.rotateValue, EditorSnapSettings.rotate);
            EditorSnapSettings.scale = EditorGUILayout.FloatField(Contents.scaleValue, EditorSnapSettings.scale);

            if (EditorGUI.EndChangeCheck())
                EditorSnapSettings.Save();
        }

        static void DoSnapActions()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Contents.pushToGrid[0]))
                SnapSelectionToGrid();
            if (GUILayout.Button(Contents.pushToGrid[1]))
                SnapSelectionToGrid(SnapAxis.X);
            if (GUILayout.Button(Contents.pushToGrid[2]))
                SnapSelectionToGrid(SnapAxis.Y);
            if (GUILayout.Button(Contents.pushToGrid[3]))
                SnapSelectionToGrid(SnapAxis.Z);
            GUILayout.EndHorizontal();
        }

        public static bool IsVector3FieldMixed(Vector3 v)
        {
            return v.x != v.y || v.y != v.z;
        }

        internal static void SnapSelectionToGrid(SnapAxis axis = SnapAxis.All)
        {
            var selections = Selection.transforms;
            if (selections != null && selections.Length > 0)
            {
                Undo.RecordObjects(selections, L10n.Tr("Snap to Grid"));
                Handles.SnapToGrid(selections, axis);
            }
        }

        internal static void RepaintAll()
        {
            EditorWindow[] openWindows = Resources.FindObjectsOfTypeAll<SnapSettingsWindow>();
            if (openWindows != null)
            {
                foreach (var item in openWindows)
                    item.Repaint();
            }
        }
    }
}
