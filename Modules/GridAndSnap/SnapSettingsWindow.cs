// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Overlays;
using Cursor = UnityEngine.UIElements.Cursor;

namespace UnityEditor.Snap
{
    sealed class SnapSettingsWindow : OverlayPopupWindow
    {
        const string k_GridSnapSettingsWindowUxmlPath = "UXML/GridAndSnap/GridSnapSettings.uxml";
        const string k_SnapWindowsCommonStyleSheet = "StyleSheets/SceneViewToolbarElements/SnapWindowsCommon.uss";
        
        readonly SceneViewGrid.GridRenderAxis[] m_Axes =
        {
            SceneViewGrid.GridRenderAxis.X,
            SceneViewGrid.GridRenderAxis.Y,
            SceneViewGrid.GridRenderAxis.Z,
        };

        Toggle m_DisplayGrid;
        Toggle m_ShowClosestToHandle;
        
        ButtonStripField m_GridPlane;
        Slider m_GridOpacity;
        
        LinkedVector3Field m_GridSize;
        LinkedVector3Field m_IncrementSnapSize;
        Vector3Field m_GridPosition;
        Vector3Field m_GridRotation;
        bool m_IgnoreGridChangeCallback;
        
        Button m_CopyFromActiveButton;
        Button m_ApplyLastCustomButton;
        Button m_ResetWorldButton;

        SceneView sceneView => SceneView.lastActiveSceneView;

        public void CreateGUI()
        {
             var mainTemplate = EditorGUIUtility.Load(k_GridSnapSettingsWindowUxmlPath) as VisualTreeAsset;
            mainTemplate.CloneTree(rootVisualElement);
            
            SceneViewToolbarStyles.AddStyleSheets(rootVisualElement);

            rootVisualElement.Q<TextElement>("PaneTitle").text = L10n.Tr("Grid and Snap Settings");
            rootVisualElement.Q<Button>("PaneOption").clicked += PaneOptionMenu;
            
            m_DisplayGrid = rootVisualElement.Q<Toggle>("DisplayGrid");
            m_DisplayGrid.label = L10n.Tr("Display Grid");
            m_DisplayGrid.tooltip = L10n.Tr("Toggle visibility of the Scene View's grid.");
            m_DisplayGrid.RegisterValueChangedCallback(evt => sceneView.sceneViewGrids.showGrid = evt.newValue);
            m_DisplayGrid.SetValueWithoutNotify(sceneView.sceneViewGrids.showGrid);
            
            m_ShowClosestToHandle = rootVisualElement.Q<Toggle>("ShowClosestToHandle");
            m_ShowClosestToHandle.label = L10n.Tr("Show closest grid to handle");
            m_ShowClosestToHandle.tooltip = L10n.Tr("Toggle whether to display the Scene View grid on a grid plane that is closest to the active handle position.");
            m_ShowClosestToHandle.RegisterValueChangedCallback(evt => sceneView.sceneViewGrids.nearestPlaneToHandleMode = evt.newValue);
            m_ShowClosestToHandle.SetValueWithoutNotify(sceneView.sceneViewGrids.nearestPlaneToHandleMode);
            
            m_GridPlane = rootVisualElement.Q<ButtonStripField>("GridPlane");
            foreach (var axis in m_Axes)
                m_GridPlane.AddButton(axis.ToString());
            m_GridPlane.label = L10n.Tr("Grid Plane");
            m_GridPlane.tooltip = L10n.Tr("The axis that the Scene View grid is drawn on.");
            m_GridPlane.RegisterValueChangedCallback((evt) =>
            {
                sceneView.sceneViewGrids.gridAxis = m_Axes[evt.newValue];
                SceneView.RepaintAll();
            });
            m_GridPlane.SetValueWithoutNotify((int)sceneView.sceneViewGrids.gridAxis);

            m_GridOpacity = rootVisualElement.Q<Slider>("Opacity");
            m_GridOpacity.label = L10n.Tr("Opacity");
            m_GridOpacity.tooltip = L10n.Tr("The opacity of the Scene View grid.");
            m_GridOpacity.lowValue = 0f;
            m_GridOpacity.highValue = 100f;
            m_GridOpacity.RegisterValueChangedCallback(evt =>
            {
                sceneView.sceneViewGrids.gridOpacity = evt.newValue / 100f;
                m_GridOpacity.SetValueWithoutNotify(Mathf.RoundToInt(sceneView.sceneViewGrids.gridOpacity * 100f));
                SceneView.RepaintAll();
            });
            m_GridOpacity.SetValueWithoutNotify(Mathf.RoundToInt(sceneView.sceneViewGrids.gridOpacity * 100f));
            
            var transformGridSection = rootVisualElement.Q<Label>("TransformGrid");
            transformGridSection.text = L10n.Tr("Grid Transform");

            var gridSettings = GridSettings.instance;
          
            m_GridSize = new LinkedVector3Field(L10n.Tr("Grid Size")) { name = "GridSize" };
            m_GridSize.value = gridSettings.gridSize;
            m_GridSize.style.flexGrow = 1;
            m_GridSize.linked = Mathf.Approximately(m_GridSize.value.x, m_GridSize.value.y) && Mathf.Approximately(m_GridSize.value.x, m_GridSize.value.z);
            m_GridSize.tooltip = L10n.Tr("The size of the Scene View grid in world units.");
            m_GridSize.RegisterValueChangedCallback(evt =>
            {
                var value = evt.newValue;
                if (m_GridSize.linked)
                    value = evt.newValue.x * Vector3.one;
                gridSettings.gridSize = value;
            });
            rootVisualElement.Q<VisualElement>("GridSizeContainer").Add(m_GridSize);
            
            m_IncrementSnapSize = new LinkedVector3Field(L10n.Tr("Increment Snap")) { name = "IncrementSnapSize" };
            m_IncrementSnapSize.value = EditorSnapSettings.move;
            m_IncrementSnapSize.style.flexGrow = 1;
            m_IncrementSnapSize.linked = Mathf.Approximately(m_IncrementSnapSize.value.x, m_IncrementSnapSize.value.y) && 
                                         Mathf.Approximately(m_IncrementSnapSize.value.x, m_IncrementSnapSize.value.z);
            m_IncrementSnapSize.tooltip = L10n.Tr("The increment value for moving objects when incremental snapping is enabled.");
            m_IncrementSnapSize.RegisterValueChangedCallback(evt =>
            {
                var value = evt.newValue;
                if (m_IncrementSnapSize.linked)
                    value = evt.newValue.x * Vector3.one;
                EditorSnapSettings.move = value;
            });
            rootVisualElement.Q<VisualElement>("IncrementSnapSizeContainer").Add(m_IncrementSnapSize);
            
            m_GridPosition = rootVisualElement.Q<Vector3Field>("GridPositionField");
            m_GridPosition.value = gridSettings.position;
            m_GridPosition.tooltip = L10n.Tr("The origin position of the Scene View grid in world space.");
            m_GridPosition.RegisterValueChangedCallback(evt =>
            {
                m_IgnoreGridChangeCallback = true;
                ApplyCustomPosition(evt.newValue);
                m_IgnoreGridChangeCallback = false;
            });
          
            m_GridRotation = rootVisualElement.Q<Vector3Field>("GridRotationField");
            m_GridRotation.value = GetSmartRoundedVec(gridSettings.rotation.eulerAngles);
            m_GridRotation.tooltip = L10n.Tr("The rotation of the Scene View grid in world space.");
            m_GridRotation.RegisterValueChangedCallback(evt =>
            {
                m_IgnoreGridChangeCallback = true;
                ApplyCustomRotation(Quaternion.Euler(evt.newValue));
                m_IgnoreGridChangeCallback = false;
            });
            
            m_CopyFromActiveButton = rootVisualElement.Q<Button>("CopyFromActiveObject");
            m_CopyFromActiveButton.text = L10n.Tr("Copy from Active Object");
            m_CopyFromActiveButton.tooltip = L10n.Tr("Apply the active game object's position and rotation to the Scene View grid.");
            m_CopyFromActiveButton.clicked += () =>
            {
                if (Selection.activeGameObject != null)
                {
                    gridSettings.ActivateMode(GridMode.Custom);

                    var transform = Selection.activeGameObject.transform;
                    EditorSnapSettings.gridPosition = transform.position;
                  
                    EditorSnapSettings.gridRotation = transform.rotation;
                    var eulerAngles = GetSmartRoundedVec(transform.rotation.eulerAngles);
                    m_GridRotation.SetValueWithoutNotify(eulerAngles);
                    
                    SceneView.RepaintAll();
                }
            };

            m_ApplyLastCustomButton = rootVisualElement.Q<Button>("ApplyLastCustom");
            m_ApplyLastCustomButton.text = L10n.Tr("Apply Last Custom");
            m_ApplyLastCustomButton.tooltip = L10n.Tr("Restore Scene View grid's position and rotation to the last custom values.\n\n" +
                                                      "Only available when grid is at world origin and has no rotation.");
            m_ApplyLastCustomButton.clicked += () =>
            {
                gridSettings.ActivateMode(GridMode.Custom);
                SceneView.RepaintAll();
            };

            m_ResetWorldButton = rootVisualElement.Q<Button>("ResetToWorld");
            m_ResetWorldButton.text = L10n.Tr("Reset to World");
            m_ResetWorldButton.tooltip = L10n.Tr("Reset Scene View's grid to world origin with default rotation.");
            m_ResetWorldButton.clicked += () =>
            {
                gridSettings.ActivateMode(GridMode.World);
                SceneView.RepaintAll();
            };
            
            RefreshLastCustomAndWorldButtons();
            OnSelectionChanged();
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            
            Selection.selectionChanged += OnSelectionChanged;
           
            var gridSettings = GridSettings.instance;
            gridSettings.sizeChanged += OnGridSizeChanged;
            gridSettings.modeChanged += OnGridModeChanged;
            gridSettings.modeSettingsChanged += OnGridModeSettingsChanged;
            EditorSnapSettings.moveChanged += OnIncrementalSnapSizeChanged;

            sceneView.sceneViewGrids.gridVisibilityChanged += OnDisplayGridChanged;
            sceneView.sceneViewGrids.nearestPlaneToHandleModeChanged += OnShowPlaneClosestToGridChanged;
        }

        void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            
            var gridSettings = GridSettings.instance;
            gridSettings.sizeChanged -= OnGridSizeChanged;
            gridSettings.modeChanged -= OnGridModeChanged;
            gridSettings.modeSettingsChanged -= OnGridModeSettingsChanged;
            EditorSnapSettings.moveChanged -= OnIncrementalSnapSizeChanged;
            
            sceneView.sceneViewGrids.gridVisibilityChanged -= OnDisplayGridChanged;
            sceneView.sceneViewGrids.nearestPlaneToHandleModeChanged -= OnShowPlaneClosestToGridChanged;
        }
        
        void PaneOptionMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(EditorGUIUtility.TrTextContent("Reset"), false, ResetValues);
            menu.ShowAsContext();
        }

        void ApplyCustomPosition(Vector3 newPosition)
        {
            // Switch to custom grid mode if user manually enters origin values
            var gridSettings = GridSettings.instance;
            if (gridSettings.activeModeIndex != GridMode.Custom)
            {
                var retainedRotation = gridSettings.rotation;
                gridSettings.ActivateMode(GridMode.Custom);
                gridSettings.rotation = retainedRotation;
            }
            
            EditorSnapSettings.gridPosition = newPosition;
            SceneView.RepaintAll();
        }
        
        void ApplyCustomRotation(Quaternion newRotation)
        {
            // Switch to custom grid mode if user manually enters origin values
            var gridSettings = GridSettings.instance;
            if (gridSettings.activeModeIndex != GridMode.Custom)
            {
                var retainedPosition = gridSettings.position;
                gridSettings.ActivateMode(GridMode.Custom);
                EditorSnapSettings.gridPosition = retainedPosition;
            }

            EditorSnapSettings.gridRotation = newRotation;
            SceneView.RepaintAll();
        }

        void ResetValues()
        {
            var gridSettings = GridSettings.instance;
            gridSettings.ResetGridSettings();
            
            m_GridSize.linked = true;
            m_IncrementSnapSize.linked = true;

            var sceneViewGrids = sceneView.sceneViewGrids;
            sceneViewGrids.gridAxis = SceneViewGrid.defaultRenderAxis;
            sceneViewGrids.gridOpacity = SceneViewGrid.defaultGridOpacity;
            sceneViewGrids.showGrid = true;
            sceneViewGrids.nearestPlaneToHandleMode = false;
            
            var snapVal = SnapSettings.defaultIncrementalSnapSize;
            EditorSnapSettings.move = new Vector3(snapVal, snapVal, snapVal);
            ResetVisualSettings(sceneView);
            
            SceneView.RepaintAll();
        }
        
        public void ResetVisualSettings(SceneView targetSceneView)
        {
            m_GridOpacity.SetValueWithoutNotify(Mathf.RoundToInt(sceneView.sceneViewGrids.gridOpacity * 100f));

            SceneViewGrid grid = targetSceneView.sceneViewGrids;
            grid.gridRenderAxisChanged += axis => { m_GridPlane.SetValueWithoutNotify((int)axis); };
            m_GridPlane.SetValueWithoutNotify((int)grid.gridAxis);
        }

        void OnGridSizeChanged(Vector3 size)
        {
            m_GridSize.SetValueWithoutNotify(size);
        }

        void OnGridModeChanged(IGridMode mode)
        {
            var gridSettings = GridSettings.instance;
            m_GridSize.SetValueWithoutNotify(gridSettings.gridSize);
            m_GridPosition.SetValueWithoutNotify(gridSettings.position);
            if (!m_IgnoreGridChangeCallback)
                m_GridRotation.SetValueWithoutNotify(gridSettings.rotation.eulerAngles);

            RefreshLastCustomAndWorldButtons();
        }

        void OnGridModeSettingsChanged(IGridMode mode)
        {
            var gridSettings = GridSettings.instance;
            m_GridPosition.SetValueWithoutNotify(gridSettings.position);
            if (!m_IgnoreGridChangeCallback)
                m_GridRotation.SetValueWithoutNotify(gridSettings.rotation.eulerAngles);

            RefreshLastCustomAndWorldButtons();
        }
        
        void OnIncrementalSnapSizeChanged(Vector3 newValue)
        {
            m_IncrementSnapSize.SetValueWithoutNotify(newValue);
        }

        void OnDisplayGridChanged(bool newValue)
        {
            m_DisplayGrid.SetValueWithoutNotify(sceneView.sceneViewGrids.showGrid);
        }

        void OnShowPlaneClosestToGridChanged(bool newValue)
        {
            m_ShowClosestToHandle.SetValueWithoutNotify(sceneView.sceneViewGrids.nearestPlaneToHandleMode);
        }

        void RefreshLastCustomAndWorldButtons()
        {
            m_ApplyLastCustomButton.SetEnabled(false);
            m_ResetWorldButton.SetEnabled(false);

            var gridSettings = GridSettings.instance;
            if (gridSettings.currentGridIsWorld)
            {
                // Enable apply last custom grid button if the custom mode's data is not identity
                if (!gridSettings.customGridIsWorld)
                    m_ApplyLastCustomButton.SetEnabled(true);
            }
            else
            {
                m_ResetWorldButton.SetEnabled(true); 
            }
        }
        
        void OnSelectionChanged()
        {
            if (Selection.count == 0)
                m_CopyFromActiveButton.SetEnabled(false);
            else
                m_CopyFromActiveButton.SetEnabled(Selection.activeGameObject != null);
        }
        
        Vector3 GetSmartRoundedVec(Vector3 vector)
        {
            vector.x = Mathf.RoundBasedOnMinimumDifference(vector.x, 0f);
            vector.y = Mathf.RoundBasedOnMinimumDifference(vector.y, 0f);
            vector.z = Mathf.RoundBasedOnMinimumDifference(vector.z, 0f);
            return vector;
        }
    }
}
