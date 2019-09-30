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
        const float k_LabelWidth = 80f;

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
            public static readonly GUIContent gridVisuals = EditorGUIUtility.TrTextContent("Grid Visuals", "The step size between lines on each axis of the Scene view grid.");
            public static readonly GUIContent incrementSnap = EditorGUIUtility.TrTextContent("Increment Snap", "Snap values relative to the origin of movement.");
            public static readonly GUIContent alignSelectionToGridHeader = EditorGUIUtility.TrTextContent("Align Selection to Grid", "Snap selected objects to the nearest grid position.");

            public static GUIContent[] moveContent = new GUIContent[]
            {
                EditorGUIUtility.TrTextContent("Move", "Snap value for the Move tool."),
                EditorGUIUtility.TrTextContent("Axis", "Snap value for the Move tool per-axis.")
            };

            public static GUIContent[] gridSize = new GUIContent[]
            {
                EditorGUIUtility.TrTextContent("Grid Size", "The amount of space between X, Y, and Z grid lines."),
                EditorGUIUtility.TrTextContent("Axis", "The amount of space between grid lines.")
            };

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

        public void OnGUI()
        {
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
        }

        static bool DoTitlebar(bool isOpen, GUIContent title, GenericMenu.MenuFunction reset)
        {
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
            return isOpen;
        }

        void DoGridVisualSettings()
        {
            EditorGUIUtility.labelWidth = k_LabelWidth;

            Vector3 grid = GridSettings.size;
            bool linked = gridValueLinked;

            EditorGUI.BeginChangeCheck();
            grid = DoLinkedVector3Field(Contents.gridSize, grid, ref linked);
            if (EditorGUI.EndChangeCheck())
            {
                gridValueLinked = linked;
                GridSettings.size = grid;
                SceneView.RepaintAll();
            }
            EditorGUIUtility.labelWidth = 0;
        }

        void DoIncrementSnapSettings()
        {
            EditorGUIUtility.labelWidth = k_LabelWidth;

            EditorGUI.BeginChangeCheck();
            var linked = snapValueLinked;
            EditorSnapSettings.move = DoLinkedVector3Field(Contents.moveContent, EditorSnapSettings.move, ref linked);
            snapValueLinked = linked;
            EditorSnapSettings.rotate = EditorGUILayout.FloatField(Contents.rotateValue, EditorSnapSettings.rotate);
            EditorSnapSettings.scale = EditorGUILayout.FloatField(Contents.scaleValue, EditorSnapSettings.scale);

            if (EditorGUI.EndChangeCheck())
                EditorSnapSettings.Save();

            EditorGUIUtility.labelWidth = 0f;
        }

        static void DoSnapActions()
        {
            int selected = -1;
            selected = GUILayout.Toolbar(selected, Contents.pushToGrid);
            if (selected > -1 && Selection.count > 0)
            {
                switch (selected)
                {
                    case 0:
                        SnapSelectionToGrid();
                        break;
                    case 1:
                        SnapSelectionToGrid(SnapAxis.X);
                        break;
                    case 2:
                        SnapSelectionToGrid(SnapAxis.Y);
                        break;
                    case 3:
                        SnapSelectionToGrid(SnapAxis.Z);
                        break;
                }
            }
        }

        public static bool IsVector3FieldMixed(Vector3 v)
        {
            return v.x != v.y || v.y != v.z;
        }

        static Vector3 DoLinkedVector3Field(GUIContent[] content, Vector3 value, ref bool linked)
        {
            EditorGUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(!linked))
            {
                EditorGUI.showMixedValue = !linked && IsVector3FieldMixed(value);
                EditorGUI.BeginChangeCheck();
                var result = EditorGUILayout.FloatField(content[0], value.x);
                if (EditorGUI.EndChangeCheck())
                    value = new Vector3(result, result, result);
                EditorGUI.showMixedValue = false;
            }

            linked = EditorGUILayout.Toggle(linked, GUILayout.Width(15));
            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(linked))
            {
                ++EditorGUI.indentLevel;
                var wide = EditorGUIUtility.wideMode;
                EditorGUIUtility.wideMode = true;
                value = EditorGUILayout.Vector3Field(content[1], value);
                EditorGUIUtility.wideMode = wide;
                --EditorGUI.indentLevel;
            }

            return value;
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
