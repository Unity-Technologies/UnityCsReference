// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Snap
{
    sealed class GridSettingsWindow : OverlayPopupWindow
    {
        readonly SceneViewGrid.GridRenderAxis[] m_Axes =
        {
            SceneViewGrid.GridRenderAxis.X,
            SceneViewGrid.GridRenderAxis.Y,
            SceneViewGrid.GridRenderAxis.Z,
        };

        const string k_GridSettingsWindowUxmlPath = "UXML/GridAndSnap/GridSettings.uxml";
        SceneView m_SceneView;
        ButtonStripField m_GridPlane;
        Slider m_GridOpacity;

        void OnEnable()
        {
            var mainTemplate = EditorGUIUtility.Load(k_GridSettingsWindowUxmlPath) as VisualTreeAsset;
            mainTemplate.CloneTree(rootVisualElement);

            rootVisualElement.Q<TextElement>("PaneTitle").text = L10n.Tr("Grid Visual");
            rootVisualElement.Q<Button>("PaneOption").clicked += PaneOptionMenu;

            m_GridPlane = rootVisualElement.Q<ButtonStripField>("GridPlane");

            foreach (var axis in m_Axes)
                m_GridPlane.AddButton(axis.ToString());

            m_GridPlane.label = L10n.Tr("Grid Plane");
            m_GridPlane.RegisterValueChangedCallback((evt) =>
            {
                m_SceneView.sceneViewGrids.gridAxis = m_Axes[evt.newValue];
                SceneView.RepaintAll();
            });

            m_GridOpacity = rootVisualElement.Q<Slider>("Opacity");
            m_GridOpacity.label = L10n.Tr("Opacity");

            m_GridOpacity.RegisterValueChangedCallback(evt =>
            {
                m_SceneView.sceneViewGrids.gridOpacity = evt.newValue;
                SceneView.RepaintAll();
            });

            var toHandle = rootVisualElement.Q<Button>("ToHandle");
            toHandle.text = L10n.Tr("To Handle");
            toHandle.clicked += () =>
            {
                foreach (var view in SceneView.sceneViews)
                    ((SceneView)view).sceneViewGrids.SetAllGridsPivot(Snapping.Snap(Tools.handlePosition, GridSettings.size));
                SceneView.RepaintAll();
            };

            var toOrigin = rootVisualElement.Q<Button>("ToOrigin");
            toOrigin.text = L10n.Tr("To Origin");
            toOrigin.clicked += () =>
            {
                foreach (var view in SceneView.sceneViews)
                    ((SceneView)view).sceneViewGrids.ResetPivot(SceneViewGrid.GridRenderAxis.All);
                SceneView.RepaintAll();
            };
        }

        void PaneOptionMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(EditorGUIUtility.TrTextContent("Reset"), false, ResetValues);
            menu.ShowAsContext();
        }

        void ResetValues()
        {
            m_SceneView.sceneViewGrids.gridAxis = SceneViewGrid.defaultRenderAxis;
            m_SceneView.sceneViewGrids.gridOpacity = SceneViewGrid.defaultGridOpacity;
            Init(m_SceneView);
        }

        void Init(SceneView sceneView)
        {
            m_SceneView = sceneView;
            m_GridOpacity.SetValueWithoutNotify(m_SceneView.sceneViewGrids.gridOpacity);

            SceneViewGrid grid = m_SceneView.sceneViewGrids;
            grid.gridRenderAxisChanged += axis => { m_GridPlane.SetValueWithoutNotify((int)axis); };
            m_GridPlane.SetValueWithoutNotify((int)grid.gridAxis);
        }

        public static void ShowDropDownAtTrigger(VisualElement trigger, SceneView sceneView)
        {
            var w = ShowOverlayPopup<GridSettingsWindow>(trigger, new Vector2(300, 88));
            w.Init(sceneView);
        }
    }
}
