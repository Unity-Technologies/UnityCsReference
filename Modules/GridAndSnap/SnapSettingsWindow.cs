// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEngine;
using UnityEngine.UIElements;
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

        protected override void OnEnable()
        {
            base.OnEnable();

            rootVisualElement.styleSheets.Add((StyleSheet)EditorGUIUtility.Load("StyleSheets/SceneViewToolbarElements/SnapWindowsCommon.uss"));
            rootVisualElement.Add(new SnapSettingsHeader(L10n.Tr("Snapping"), ResetValues));

            // Grid size == Increment snap
            m_GridSize = new LinkedVector3Field(L10n.Tr("Grid Size")) { name = "GridSize" };
            m_GridSize.value = GridSettings.size;
            m_GridSize.linked = Mathf.Approximately(m_GridSize.value.x, m_GridSize.value.y) && Mathf.Approximately(m_GridSize.value.x, m_GridSize.value.z);
            GridSettings.sizeChanged += (value) => m_GridSize.SetValueWithoutNotify(value);
            m_GridSize.RegisterValueChangedCallback(OnGridSizeChanged);
            rootVisualElement.Add(m_GridSize);

            var gridSnap = new Toggle("Snap to Grid") { tooltip = "Enable snapping objects to the absolute position on " +
                                                                  "the grid. This option is only available when handle " +
                                                                  "rotation is set to Global." };
            gridSnap.SetValueWithoutNotify(EditorSnapSettings.gridSnapEnabled);
            EditorSnapSettings.gridSnapEnabledChanged += () => gridSnap.SetValueWithoutNotify(EditorSnapSettings.gridSnapEnabled);
            gridSnap.RegisterValueChangedCallback(evt => EditorSnapSettings.gridSnapEnabled = evt.newValue);
            rootVisualElement.Add(gridSnap);

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

            // Align Selected
            var alignSelected = new VisualElement() { name = "AlignSelected" };
            alignSelected.Add(new Label(L10n.Tr("Align Selected")) { name = "AlignSelectedLabel" });

            var allAxis = new Button() { name = "AllAxes", text = L10n.Tr("All Axes") };
            allAxis.clicked += () => GridSnapping.SnapSelectionToGrid();

            var x = new Button() { name = "X", text = L10n.Tr("X") };
            x.clicked += () => GridSnapping.SnapSelectionToGrid(SnapAxis.X);

            var y = new Button() { name = "Y", text = L10n.Tr("Y") };
            y.clicked += () => GridSnapping.SnapSelectionToGrid(SnapAxis.Y);

            var z = new Button() { name = "Z", text = L10n.Tr("Z") };
            z.clicked += () => GridSnapping.SnapSelectionToGrid(SnapAxis.Z);

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
            EditorSnapSettings.gridSnapEnabled = SnapSettings.defaultGridSnapEnabled;
            EditorSnapSettings.rotate = SnapSettings.defaultRotation;
            EditorSnapSettings.scale = SnapSettings.defaultScale;
        }

        void OnGridSizeChanged(ChangeEvent<Vector3> evt)
        {
            var value = evt.newValue;
            if (m_GridSize.linked)
                value = evt.newValue.x * Vector3.one;
            GridSettings.size = value;
        }
    }
}
