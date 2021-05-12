// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.ShortcutManagement;
using UnityEditor.Overlays;

namespace UnityEditor.Snap
{
    sealed class SnapSettingsHeader : VisualElement
    {
        readonly GenericMenu.MenuFunction m_Reset;

        public SnapSettingsHeader(string title, GenericMenu.MenuFunction reset)
        {
            name = "Header";
            Add(new Label {name = "PaneTitle", text = title});
            var button = new Button {name = "PaneOption"};
            button.clicked += OpenMenu;
            m_Reset = reset;
            Add(button);
        }

        void OpenMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(EditorGUIUtility.TrTextContent("Reset"), false, m_Reset);
            menu.ShowAsContext();
        }
    }

    sealed class SnapSettingsWindow : OverlayPopupWindow
    {
        LinkedVector3Field m_GridSize;

        [Shortcut("Grid/Push To Grid", typeof(SceneView), KeyCode.Backslash, ShortcutModifiers.Action)]
        internal static void PushToGrid()
        {
            SnapSelectionToGrid();
        }

        void OnEnable()
        {
            rootVisualElement.styleSheets.Add((StyleSheet)EditorGUIUtility.Load("StyleSheets/SceneViewToolbarElements/SnapWindowsCommon.uss"));

            rootVisualElement.Add(new SnapSettingsHeader(L10n.Tr("Grid Snapping"), ResetValues));

            m_GridSize = new LinkedVector3Field(L10n.Tr("Grid Size")) { name = "GridSize" };
            m_GridSize.value = GridSettings.size;
            m_GridSize.linked = Mathf.Approximately(m_GridSize.value.x, m_GridSize.value.y) && Mathf.Approximately(m_GridSize.value.x, m_GridSize.value.z);
            GridSettings.sizeChanged += (value) => m_GridSize.SetValueWithoutNotify(value);
            m_GridSize.RegisterValueChangedCallback((evt) =>
            {
                GridSettings.size = evt.newValue;
            });
            rootVisualElement.Add(m_GridSize);

            // Align Selected
            var alignSelected = new VisualElement() { name = "AlignSelected" };
            alignSelected.style.flexDirection = FlexDirection.Row;

            alignSelected.Add(new Label(L10n.Tr("Align Selected")));

            var allAxis = new Button() { name = "AllAxes", text = L10n.Tr("All Axes") };
            allAxis.clicked += () =>
            {
                SnapSelectionToGrid();
            };

            var x = new Button() { name = "X", text = L10n.Tr("X") };
            x.clicked += () =>
            {
                SnapSelectionToGrid(SnapAxis.X);
            };

            var y = new Button() { name = "Y", text = L10n.Tr("Y") };
            y.clicked += () =>
            {
                SnapSelectionToGrid(SnapAxis.Y);
            };

            var z = new Button() { name = "Z", text = L10n.Tr("Z") };
            z.clicked += () =>
            {
                SnapSelectionToGrid(SnapAxis.Z);
            };

            alignSelected.Add(allAxis);
            alignSelected.Add(x);
            alignSelected.Add(y);
            alignSelected.Add(z);
            rootVisualElement.Add(alignSelected);
        }

        void ResetValues()
        {
            GridSettings.size = Vector3.one * GridSettings.defaultGridSize;
            m_GridSize.linked = true;
        }

        static void SnapSelectionToGrid(SnapAxis axis = SnapAxis.All)
        {
            var selections = Selection.transforms;
            if (selections != null && selections.Length > 0)
            {
                Undo.RecordObjects(selections, L10n.Tr("Snap to Grid"));
                Handles.SnapToGrid(selections, axis);
            }
        }
    }

    sealed class SnapIncrementSettingsWindow : OverlayPopupWindow
    {
        LinkedVector3Field m_MoveLinkedField;

        void OnEnable()
        {
            rootVisualElement.styleSheets.Add((StyleSheet)EditorGUIUtility.Load("StyleSheets/SceneViewToolbarElements/SnapWindowsCommon.uss"));

            rootVisualElement.Add(new SnapSettingsHeader(L10n.Tr("Increment Snapping"), ResetValues));

            // Move
            m_MoveLinkedField = new LinkedVector3Field(L10n.Tr("Move")) { name = "Move" };
            m_MoveLinkedField.value = EditorSnapSettings.move;
            m_MoveLinkedField.linked = Mathf.Approximately(m_MoveLinkedField.value.x, m_MoveLinkedField.value.y)
                && Mathf.Approximately(m_MoveLinkedField.value.x, m_MoveLinkedField.value.z);
            rootVisualElement.Add(m_MoveLinkedField);

            EditorSnapSettings.moveChanged += (value) => m_MoveLinkedField.SetValueWithoutNotify(value);
            m_MoveLinkedField.RegisterValueChangedCallback(evt =>
            {
                EditorSnapSettings.move = evt.newValue;
                EditorSnapSettings.Save();
            });

            // Rotate
            var rotate = new FloatField(L10n.Tr("Rotate")) { name = "Rotate" };
            rotate.value = EditorSnapSettings.rotate;
            rootVisualElement.Add(rotate);

            EditorSnapSettings.rotateChanged += (value) => rotate.SetValueWithoutNotify(value);
            rotate.RegisterValueChangedCallback(evt =>
            {
                EditorSnapSettings.rotate = evt.newValue;
                EditorSnapSettings.Save();
            });

            // Scale
            var scale = new FloatField(L10n.Tr("Scale")) { name = "Scale" };
            scale.value = EditorSnapSettings.scale;
            rootVisualElement.Add(scale);

            EditorSnapSettings.scaleChanged += (value) => scale.SetValueWithoutNotify(value);
            scale.RegisterValueChangedCallback(evt =>
            {
                EditorSnapSettings.scale = evt.newValue;
                EditorSnapSettings.Save();
            });
        }

        void ResetValues()
        {
            EditorSnapSettings.move = SnapSettings.defaultMove;
            m_MoveLinkedField.linked = true;
            EditorSnapSettings.rotate = SnapSettings.defaultRotation;
            EditorSnapSettings.scale = SnapSettings.defaultScale;
        }
    }
}
