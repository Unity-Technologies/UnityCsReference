// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.DeviceSimulation
{
    internal class UserInterfaceController
    {
        private DeviceSimulatorMain m_Main;
        private ScreenSimulation m_ScreenSimulation;

        public RenderTexture PreviewTexture
        {
            set => m_DeviceView.PreviewTexture = value;
        }

        public Texture OverlayTexture
        {
            set => m_DeviceView.OverlayTexture = value;
        }

        private int m_RotationDegree;
        private int Rotation
        {
            get => m_RotationDegree;
            set
            {
                m_RotationDegree = value % 360;
                m_DeviceView.Rotation = m_RotationDegree;

                if (m_ScreenSimulation != null)
                    m_ScreenSimulation.DeviceRotation = m_RotationDegree;

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
        private TextElement m_SelectedDeviceName;
        private SliderInt m_ScaleSlider;
        private Label m_ScaleValueLabel;
        private ToolbarToggle m_FitToScreenToggle;
        private ToolbarToggle m_HighlightSafeAreaToggle;

        // Controls for inactive message.
        private VisualElement m_InactiveMsgContainer;

        // Controls for preview.
        private VisualElement m_ScrollViewContainer;
        private VisualElement m_ScrollView;
        private VisualElement m_DeviceViewContainer;
        private DeviceView m_DeviceView;

        public UserInterfaceController(DeviceSimulatorMain deviceSimulatorMain, VisualElement rootVisualElement)
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

            // Scale slider set up
            m_ScaleSlider = rootVisualElement.Q<SliderInt>("scale-slider");
            m_ScaleSlider.lowValue = kScaleMin;
            m_ScaleSlider.highValue = kScaleMax;
            m_ScaleSlider.SetValueWithoutNotify(Scale);
            m_ScaleSlider.RegisterCallback<ChangeEvent<int>>(SetScale);
            m_ScaleValueLabel = rootVisualElement.Q<Label>("scale-value-label");
            m_ScaleValueLabel.text = Scale.ToString();

            // Fit to Screen button set up
            m_FitToScreenToggle = rootVisualElement.Q<ToolbarToggle>("fit-to-screen");
            m_FitToScreenToggle.RegisterValueChangedCallback(FitToScreen);
            m_FitToScreenToggle.SetValueWithoutNotify(m_FitToScreenEnabled);

            // Rotate button set up
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
            m_HighlightSafeAreaToggle.SetValueWithoutNotify(HighlightSafeArea);

            // Inactive message set up
            m_InactiveMsgContainer = rootVisualElement.Q<VisualElement>("inactive-msg-container");
            var closeInactiveMsg = rootVisualElement.Q<Image>("close-inactive-msg");
            closeInactiveMsg.image = AssetDatabase.LoadAssetAtPath<Texture2D>($"packages/com.unity.device-simulator/SimulatorResources/Icons/close_button.png");
            closeInactiveMsg.AddManipulator(new Clickable(CloseInactiveMsg));
            SetInactiveMsgState(false);

            // Device view set up
            m_ScrollViewContainer = rootVisualElement.Q<VisualElement>("scrollview-container");
            m_ScrollViewContainer.RegisterCallback<WheelEvent>(OnScrollWheel, TrickleDown.TrickleDown);
            m_ScrollViewContainer.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            m_ScrollView = rootVisualElement.Q<ScrollView>("preview-scroll-view");
            m_DeviceView = new DeviceView(Quaternion.Euler(0, 0, 360 - Rotation), Scale / 100f) {ShowSafeArea = HighlightSafeArea};
            m_DeviceViewContainer = rootVisualElement.Q<VisualElement>("preview-container");
            m_DeviceViewContainer.Add(m_DeviceView);
            m_DeviceView.SafeAreaColor = new Color(0.95f, 1f, 0f);
            m_DeviceView.SafeAreaLineWidth = 5;
        }

        public void OnSimulationStart(ScreenSimulation screenSimulation)
        {
            m_ScreenSimulation = screenSimulation;
            m_ScreenSimulation.DeviceRotation = Rotation;

            m_SelectedDeviceName.text = m_Main.Devices[m_Main.DeviceIndex].friendlyName;

            var screen = m_Main.Devices[m_Main.DeviceIndex].screens[0];
            m_DeviceView.SetDevice(screen.width, screen.height, screen.presentation.borderSize);
            m_DeviceView.ScreenOrientation = m_ScreenSimulation.orientation;
            m_DeviceView.ScreenInsets = m_ScreenSimulation.Insets;
            m_DeviceView.SafeArea = m_ScreenSimulation.ScreenSpaceSafeArea;

            m_ScreenSimulation.OnOrientationChanged += autoRotate => m_DeviceView.ScreenOrientation = m_ScreenSimulation.orientation;
            m_ScreenSimulation.OnInsetsChanged += insets => m_DeviceView.ScreenInsets = insets;
            m_ScreenSimulation.OnScreenSpaceSafeAreaChanged += safeArea => m_DeviceView.SafeArea = safeArea;

            SetScrollViewTopPadding();

            if (m_FitToScreenEnabled)
                FitToScreenScale();
        }

        public void StoreSerializedStates(ref SimulatorSerializationStates states)
        {
            states.scale = Scale;
            states.fitToScreenEnabled = m_FitToScreenEnabled;
            states.rotationDegree = Rotation;
            states.highlightSafeAreaEnabled = m_HighlightSafeArea;
        }

        public void ApplySerializedStates(SimulatorSerializationStates states)
        {
            if (states != null)
            {
                Scale = states.scale;
                m_FitToScreenEnabled = states.fitToScreenEnabled;
                Rotation = states.rotationDegree;
                HighlightSafeArea = states.highlightSafeAreaEnabled;
                m_ScaleSlider.SetValueWithoutNotify(Scale);
                m_ScaleValueLabel.text = Scale.ToString();
                m_FitToScreenToggle.SetValueWithoutNotify(m_FitToScreenEnabled);
                m_HighlightSafeAreaToggle.SetValueWithoutNotify(HighlightSafeArea);
            }
        }

        private void SetScale(ChangeEvent<int> e)
        {
            UpdateScale(e.newValue);

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
            Vector2 screenSize = m_ScrollViewContainer.worldBound.size;
            var x = screenSize.x / m_DeviceView.style.width.value.value;
            var y = screenSize.y / m_DeviceView.style.height.value.value;

            UpdateScale(ClampScale(Mathf.FloorToInt(Scale * Math.Min(x, y))));
        }

        private void UpdateScale(int newScale)
        {
            Scale = newScale;

            m_ScaleValueLabel.text = newScale.ToString();
            m_ScaleSlider.SetValueWithoutNotify(newScale);

            SetScrollViewTopPadding();
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

        private void OnScrollWheel(WheelEvent evt)
        {
            var newScale = (int)(Scale - evt.delta.y);
            UpdateScale(ClampScale(newScale));
            evt.StopPropagation();

            m_FitToScreenEnabled = false;
            m_FitToScreenToggle.SetValueWithoutNotify(m_FitToScreenEnabled);
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

            var deviceListPopup = new DeviceListPopup(m_Main.Devices, m_Main.DeviceIndex, maximumVisibleDeviceCount, m_DeviceSearchContent);
            deviceListPopup.OnDeviceSelected += OnDeviceSelected;
            deviceListPopup.OnSearchInput += OnSearchInput;

            PopupWindow.Show(rect, deviceListPopup);
        }

        private void OnDeviceSelected(int selectedDeviceIndex)
        {
            if (m_Main.DeviceIndex == selectedDeviceIndex)
                return;
            m_Main.DeviceIndex = selectedDeviceIndex;
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
    }
}
