// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.DeviceSimulation
{
    internal class UserInterfaceController
    {
        public event Action OnScreenToggled;

        private DeviceSimulatorMain m_Main;
        private ScreenSimulation m_ScreenSimulation;

        private int m_Rotation;
        private int Rotation
        {
            get => m_Rotation;
            set
            {
                m_Rotation = value % 360;
                m_DeviceView.Rotation = m_Rotation;

                if (m_ScreenSimulation != null)
                    m_ScreenSimulation.DeviceRotation = m_Rotation;

                SetScrollViewTopPadding();

                if (m_FitToScreenEnabled)
                    FitToScreenScale();
            }
        }

        private int m_Scale = 20; // Value from (0, 100].
        private int Scale
        {
            get => m_Scale;
            set
            {
                m_Scale = value;
                m_DeviceView.Scale = m_Scale / 100f;

                m_ScaleValueLabel.text = m_Scale.ToString();
                m_ScaleSlider.SetValueWithoutNotify(m_Scale);

                SetScrollViewTopPadding();
            }
        }

        private bool m_HighlightSafeArea;
        private bool HighlightSafeArea
        {
            get => m_HighlightSafeArea;
            set
            {
                m_HighlightSafeArea = value;
                m_DeviceView.ShowSafeArea = m_HighlightSafeArea;
            }
        }

        private const int kScaleMin = 10;
        private const int kScaleMax = 100;
        private bool m_FitToScreenEnabled = true;

        // Controls for the toolbar
        private string m_DeviceSearchContent;
        private VisualElement m_DeviceListMenu;
        private ToolbarButton m_ScreenToggle;
        private TextElement m_SelectedDeviceName;
        private SliderInt m_ScaleSlider;
        private Label m_ScaleValueLabel;
        private ToolbarToggle m_FitToScreenToggle;
        private ToolbarToggle m_HighlightSafeAreaToggle;
        private ToolbarToggle m_ControlPanelToggle;
        private VisualElement m_OnPlayBehaviorListMenu;
        private TextElement m_SelectedOnPlayBehaviorName;

        // Controls for inactive message.
        private VisualElement m_InactiveMsgContainer;

        // Controls for preview.
        private TwoPaneSplitView m_SplitView;
        private VisualElement m_PreviewPanel;
        private VisualElement m_ScrollView;
        private VisualElement m_DeviceViewContainer;

        private DeviceView m_DeviceView;
        public DeviceView DeviceView => m_DeviceView;

        // Control Panel
        private float m_ControlPanelWidth;
        private readonly Dictionary<string, Foldout> m_PluginFoldouts = new Dictionary<string, Foldout>();
        private VisualElement m_ControlPanel;

        public UserInterfaceController(DeviceSimulatorMain deviceSimulatorMain, VisualElement rootVisualElement, SimulatorState serializedState, PluginController pluginController, TouchEventManipulator touchEventManipulator)
        {
            m_Main = deviceSimulatorMain;

            rootVisualElement.styleSheets.Add(EditorGUIUtility.Load($"DeviceSimulator/StyleSheets/styles_{(EditorGUIUtility.isProSkin ? "dark" : "light")}.uss") as StyleSheet);
            rootVisualElement.styleSheets.Add(EditorGUIUtility.Load("DeviceSimulator/StyleSheets/styles_common.uss") as StyleSheet);
            var visualTree = EditorGUIUtility.Load("DeviceSimulator/UXML/ui_device_simulator.uxml") as VisualTreeAsset;
            visualTree.CloneTree(rootVisualElement);

            // Device selection menu set up
            m_DeviceListMenu = rootVisualElement.Q<VisualElement>("device-list-menu");
            m_DeviceListMenu.AddManipulator(new Clickable(ShowDeviceInfoList));
            m_SelectedDeviceName = m_DeviceListMenu.Q<TextElement>("selected-device-name");

            m_ScreenToggle = rootVisualElement.Q<ToolbarButton>("screen-toggle");
            m_ScreenToggle.clickable.clicked += () => { OnScreenToggled?.Invoke(); };

            // Scale slider set up
            m_ScaleSlider = rootVisualElement.Q<SliderInt>("scale-slider");
            m_ScaleSlider.lowValue = kScaleMin;
            m_ScaleSlider.highValue = kScaleMax;
            m_Scale = serializedState.scale;
            m_ScaleSlider.SetValueWithoutNotify(m_Scale);
            m_ScaleSlider.RegisterCallback<ChangeEvent<int>>(SetScale);
            m_ScaleValueLabel = rootVisualElement.Q<Label>("scale-value-label");
            m_ScaleValueLabel.text = Scale.ToString();

            // Fit to Screen button set up
            m_FitToScreenToggle = rootVisualElement.Q<ToolbarToggle>("fit-to-screen");
            m_FitToScreenToggle.RegisterValueChangedCallback(FitToScreen);
            m_FitToScreenEnabled = serializedState.fitToScreenEnabled;
            m_FitToScreenToggle.SetValueWithoutNotify(m_FitToScreenEnabled);

            // Rotate button set up
            m_Rotation = serializedState.rotationDegree;
            var namePostfix = EditorGUIUtility.isProSkin ? "_dark" : "_light";
            rootVisualElement.Q<Image>("rotate-cw-image").image = EditorGUIUtility.Load($"DeviceSimulator/Icons/rotate_cw{namePostfix}.png") as Texture;
            rootVisualElement.Q<VisualElement>("rotate-cw").AddManipulator(new Clickable(() => { Rotation += 90; }));
            rootVisualElement.Q<Image>("rotate-ccw-image").image = EditorGUIUtility.Load($"DeviceSimulator/Icons/rotate_ccw{namePostfix}.png") as Texture;
            rootVisualElement.Q<VisualElement>("rotate-ccw").AddManipulator(new Clickable(() => { Rotation += 270; }));

            // Safe Area button set up
            m_HighlightSafeAreaToggle = rootVisualElement.Q<ToolbarToggle>("highlight-safe-area");
            m_HighlightSafeAreaToggle.RegisterValueChangedCallback((evt) => {
                HighlightSafeArea = evt.newValue;
            });
            m_HighlightSafeArea = serializedState.highlightSafeAreaEnabled;
            m_HighlightSafeAreaToggle.SetValueWithoutNotify(HighlightSafeArea);

            // Enter Play Mode popup up
            m_OnPlayBehaviorListMenu = rootVisualElement.Q<VisualElement>("enter-play-mode-list-menu");
            m_OnPlayBehaviorListMenu.AddManipulator(new Clickable(ShowOnPlayBehaviorSelector));
            m_SelectedOnPlayBehaviorName = m_OnPlayBehaviorListMenu.Q<TextElement>("selected-play-mode-name");
            m_SelectedOnPlayBehaviorName.text = GameViewOnPlayMenu.GetOnPlayBehaviorName(m_Main.playModeView.playModeBehaviorIdx);

            // Inactive message set up
            m_InactiveMsgContainer = rootVisualElement.Q<VisualElement>("inactive-msg-container");
            var closeInactiveMsg = rootVisualElement.Q<Image>("close-inactive-msg");
            closeInactiveMsg.image = AssetDatabase.LoadAssetAtPath<Texture2D>($"packages/com.unity.device-simulator/SimulatorResources/Icons/close_button.png");
            closeInactiveMsg.AddManipulator(new Clickable(CloseInactiveMsg));
            SetInactiveMsgState(false);

            // Device view set up
            m_PreviewPanel = rootVisualElement.Q<VisualElement>("preview-panel");
            m_PreviewPanel.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            m_ScrollView = rootVisualElement.Q<ScrollView>("preview-scroll-view");
            m_DeviceView = new DeviceView(Quaternion.Euler(0, 0, Rotation), Scale / 100f) {ShowSafeArea = HighlightSafeArea};
            m_DeviceView.AddManipulator(touchEventManipulator);
            m_DeviceView.OnViewToScreenChanged += () => { touchEventManipulator.previewImageRendererSpaceToScreenSpace = m_DeviceView.ViewToScreen; };
            m_DeviceViewContainer = rootVisualElement.Q<VisualElement>("preview-container");
            m_DeviceViewContainer.Add(m_DeviceView);
            m_DeviceView.SafeAreaColor = new Color(0.95f, 1f, 0f);
            m_DeviceView.SafeAreaLineWidth = 5;
            m_DeviceView.OnBoundingBoxShapeChange += () =>
            {
                if (m_FitToScreenEnabled)
                    FitToScreenScale();
                SetScrollViewTopPadding();
            };

            // Control Panel set up
            m_SplitView = rootVisualElement.Q<TwoPaneSplitView>("split-view");
            m_ControlPanelToggle = rootVisualElement.Q<ToolbarToggle>("control-panel-toggle");
            m_ControlPanel = rootVisualElement.Q<VisualElement>("control-panel");
            m_ControlPanelToggle.RegisterValueChangedCallback((evt) => { SetControlPanelVisibility(evt.newValue); });
            m_ControlPanelWidth = serializedState.controlPanelWidth;
            m_ControlPanelToggle.SetValueWithoutNotify(serializedState.controlPanelVisible);

            if (!serializedState.controlPanelVisible)
            {
                m_SplitView.fixedPaneInitialDimension = 0;
                m_SplitView.CollapseChild(0);
            }
            else
            {
                ControlPanelWidthFix();
                m_SplitView.fixedPaneInitialDimension = m_ControlPanelWidth;
            }

            InitPluginUI(pluginController, serializedState);
        }

        private void InitPluginUI(PluginController pluginController, SimulatorState serializedState)
        {
            foreach (var plugin in pluginController.CreateUI())
            {
                var foldout = new Foldout()
                {
                    text = plugin.title,
                    value = false
                };
                foldout.AddToClassList("unity-device-simulator__control-panel_foldout");
                foldout.Add(plugin.ui);

                m_ControlPanel.Add(foldout);
                if (serializedState.controlPanelFoldouts.TryGetValue(plugin.serializationKey, out var state))
                    foldout.value = state;
                m_PluginFoldouts.Add(plugin.serializationKey, foldout);
            }
        }

        public void OnSimulationStart(ScreenSimulation screenSimulation)
        {
            m_ScreenSimulation = screenSimulation;
            m_ScreenSimulation.DeviceRotation = Rotation;

            m_SelectedDeviceName.text = m_Main.currentDevice.deviceInfo.friendlyName;

            m_ScreenSimulation.OnOrientationChanged += () => m_DeviceView.ScreenOrientation = m_ScreenSimulation.orientation;
            m_ScreenSimulation.OnInsetsChanged += insets => m_DeviceView.ScreenInsets = insets;
            m_ScreenSimulation.OnScreenSpaceSafeAreaChanged += safeArea => m_DeviceView.SafeArea = safeArea;

            SetScrollViewTopPadding();

            if (m_FitToScreenEnabled)
                FitToScreenScale();
        }

        public void StoreSerializedStates(ref SimulatorState states)
        {
            states.scale = Scale;
            states.fitToScreenEnabled = m_FitToScreenEnabled;
            states.rotationDegree = Rotation;
            states.highlightSafeAreaEnabled = m_HighlightSafeArea;
            states.controlPanelVisible = m_ControlPanelToggle.value;
            states.controlPanelWidth = m_SplitView.fixedPane.worldBound.width;

            foreach (var foldout in m_PluginFoldouts)
                states.controlPanelFoldouts.Add(foldout.Key, foldout.Value.value);
        }

        public void SetScreenToggleName(string name)
        {
            m_ScreenToggle.text = name;
        }

        public void SetScreenToggleVisibility(bool visible)
        {
            m_ScreenToggle.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetScale(ChangeEvent<int> e)
        {
            Scale = e.newValue;

            m_FitToScreenEnabled = false;
            m_FitToScreenToggle.SetValueWithoutNotify(m_FitToScreenEnabled);
        }

        private void FitToScreen(ChangeEvent<bool> evt)
        {
            m_FitToScreenEnabled = evt.newValue;
            if (m_FitToScreenEnabled)
                FitToScreenScale();
        }

        private void FitToScreenScale()
        {
            Vector2 screenSize = m_PreviewPanel.worldBound.size;
            var x = screenSize.x / m_DeviceView.style.width.value.value;
            var y = screenSize.y / m_DeviceView.style.height.value.value;

            Scale = ClampScale(Mathf.FloorToInt(Scale * Math.Min(x, y)));
        }

        private void SetControlPanelVisibility(bool visible)
        {
            if (visible)
            {
                ControlPanelWidthFix();
                m_SplitView.UnCollapse();
                m_SplitView.fixedPaneInitialDimension = m_ControlPanelWidth;
            }
            else
            {
                m_ControlPanelWidth = m_SplitView.fixedPane.worldBound.width;
                m_SplitView.CollapseChild(0);
            }
        }

        // We should restore the Control Panel size to the same one that it was before hiding, to keep it the way the user prefers.
        // But if the window is resized we could end up with control panel larger than the window itself.
        // The window min width is 200 so defaulting to half of that.
        private void ControlPanelWidthFix()
        {
            if (m_ControlPanelWidth <= 0 || m_ControlPanelWidth >= m_SplitView.worldBound.width)
                m_ControlPanelWidth = 100;
        }

        private void CloseInactiveMsg()
        {
            SetInactiveMsgState(false);
        }

        private void SetInactiveMsgState(bool shown)
        {
            m_InactiveMsgContainer.style.visibility = shown ? Visibility.Visible : Visibility.Hidden;
            m_InactiveMsgContainer.style.position = shown ? Position.Relative : Position.Absolute;
        }

        private int ClampScale(int scale)
        {
            if (scale < kScaleMin)
                return kScaleMin;
            if (scale > kScaleMax)
                return kScaleMax;

            return scale;
        }

        private void ShowDeviceInfoList()
        {
            var rect = new Rect(m_DeviceListMenu.worldBound.position + new Vector2(1, m_DeviceListMenu.worldBound.height), new Vector2());
            var maximumVisibleDeviceCount = 10;

            var deviceListPopup = new DeviceListPopup(m_Main.devices, m_Main.deviceIndex, maximumVisibleDeviceCount, m_DeviceSearchContent);
            deviceListPopup.OnDeviceSelected += OnDeviceSelected;
            deviceListPopup.OnSearchInput += OnSearchInput;

            PopupWindow.Show(rect, deviceListPopup);
        }

        private void ShowOnPlayBehaviorSelector()
        {
            var rect = new Rect(m_OnPlayBehaviorListMenu.worldBound.position + new Vector2(1, m_OnPlayBehaviorListMenu.worldBound.height), new Vector2());
            var menuData = new SimulationOnPlayMenuItemProvider();
            var flexibleMenu = new GameViewOnPlayMenu(menuData, m_Main.playModeView.playModeBehaviorIdx, null, m_Main.playModeView as SimulatorWindow);
            PopupWindow.Show(rect, flexibleMenu);
        }

        private void OnDeviceSelected(int selectedDeviceIndex)
        {
            if (m_Main.deviceIndex == selectedDeviceIndex)
                return;
            m_Main.deviceIndex = selectedDeviceIndex;
        }

        private void OnSearchInput(string searchContent)
        {
            m_DeviceSearchContent = searchContent;
        }

        public void OnSimulationStateChanged(SimulationState simulationState)
        {
            SetInactiveMsgState(simulationState == SimulationState.Disabled);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (m_FitToScreenEnabled)
                FitToScreenScale();

            SetScrollViewTopPadding();
        }

        // This is a workaround to fix https://github.com/Unity-Technologies/com.unity.device-simulator/issues/79.
        private void SetScrollViewTopPadding()
        {
            var scrollViewHeight = m_ScrollView.worldBound.height;
            if (float.IsNaN(scrollViewHeight))
                return;

            m_ScrollView.style.paddingTop = scrollViewHeight > m_DeviceView.style.height.value.value ? (scrollViewHeight - m_DeviceView.style.height.value.value) / 2 : 0;
        }

        public void UpdateEnterPlayModeBehaviorMsg()
        {
            m_SelectedOnPlayBehaviorName.text = GameViewOnPlayMenu.GetOnPlayBehaviorName(m_Main.playModeView.playModeBehaviorIdx);
        }
    }
}
