// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.DeviceSimulation
{
    [EditorWindowTitle(title = "Simulator", useTypeNameAsIconName = true)]
    internal class SimulatorWindow : PlayModeView, IHasCustomMenu, ISerializationCallbackReceiver, IGameViewOnPlayMenuUser
    {
        private static List<SimulatorWindow> s_SimulatorInstances = new List<SimulatorWindow>();
        private bool m_DeviceListDirty;

        [SerializeField] private SimulatorState m_SimulatorState;
        private SimulationState m_State = SimulationState.Enabled;
        private DeviceSimulatorMain m_Main;

        private bool m_PlayFocused = false;
        private bool m_VsyncEnabled = false;

        private Vector2 simulatorViewPadding
        {
            get
            {
                var rotation = m_Main.userInterface.Rotation;
                Vector2 borderPadding;
                switch (rotation)
                {
                    case 90:
                        borderPadding = new Vector2(m_Main.userInterface.DeviceView.borderSize.y,
                            m_Main.userInterface.DeviceView.borderSize.z);
                        break;
                    case 180:
                        borderPadding = new Vector2(m_Main.userInterface.DeviceView.borderSize.z,
                            m_Main.userInterface.DeviceView.borderSize.w);
                        break;
                    case 270:
                        borderPadding = new Vector2(m_Main.userInterface.DeviceView.borderSize.w,
                            m_Main.userInterface.DeviceView.borderSize.x);
                        break;
                    default:
                        borderPadding = new Vector2(m_Main.userInterface.DeviceView.borderSize.x,
                            m_Main.userInterface.DeviceView.borderSize.y);
                        break;
                }

                borderPadding = borderPadding * m_Main.userInterface.DeviceView.Scale;
                return m_Main.userInterface.DeviceView.worldBound.position + borderPadding;
            }
        }

        private float simulatorViewMouseScale => 1f / m_Main.userInterface.DeviceView.Scale;

        public bool playFocused { get => m_PlayFocused; set => m_PlayFocused = value; }
        public bool vSyncEnabled { get => m_VsyncEnabled; set => m_VsyncEnabled = value; }
        public DeviceSimulatorMain main => m_Main;

        [MenuItem("Window/General/Device Simulator", false, 2000)]
        public static void ShowWindow()
        {
            SimulatorWindow window = GetWindow<SimulatorWindow>();
            window.Show();
        }

        void OnEnable()
        {
            titleContent = GetLocalizedTitleContent();
            minSize = new Vector2(200, 200);
            autoRepaintOnSceneChange = true;
            clearColor = Color.black;
            playModeViewName = "Device Simulator";
            showGizmos = false;
            targetDisplay = 0;
            renderIMGUI = true;

            m_Main = new DeviceSimulatorMain(m_SimulatorState, rootVisualElement, this);
            s_SimulatorInstances.Add(this);
            InitPlayModeViewSwapMenu();

            DevicePackage.OnPackageStatus += OnDevicePackageStatus;
        }

        private void InitPlayModeViewSwapMenu()
        {
            var playModeViewTypeMenu = rootVisualElement.Q<ToolbarMenu>("playmode-view-menu");
            playModeViewTypeMenu.text = GetWindowTitle(GetType());

            var types = GetAvailableWindowTypes();
            foreach (var type in types)
            {
                var status = type.Key == GetType() ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                playModeViewTypeMenu.menu.AppendAction(type.Value, action => SwapMainWindow(type.Key), action => status);
            }
        }

        private void OnDisable()
        {
            s_SimulatorInstances.Remove(this);
            m_Main.Dispose();

            DevicePackage.OnPackageStatus -= OnDevicePackageStatus;

            PlayModeAnalytics.SimulatorDisableEvent();
        }

        void Update()
        {
            if (m_DeviceListDirty)
            {
                m_Main.UpdateDeviceList();
                m_DeviceListDirty = false;
            }

            if (m_State == SimulationState.Disabled && GetMainPlayModeView() == this)
            {
                m_State = SimulationState.Enabled;
                m_Main.Enable();
            }
            else if (m_State == SimulationState.Enabled && GetMainPlayModeView() != this)
            {
                m_State = SimulationState.Disabled;
                m_Main.Disable();
            }
        }

        private void OnGUI()
        {
            if (GetMainPlayModeView() != this)
                return;

            var type = Event.current.type;
            if (type == EventType.Repaint)
            {
                m_Main.ScreenSimulation.ApplyChanges();
                targetSize = m_Main.targetSize;
                m_Main.displayTexture = RenderView(m_Main.mousePositionInUICoordinates, false);

                viewPadding = simulatorViewPadding;
                viewMouseScale = simulatorViewMouseScale;
            }
        }

        public void OnBeforeSerialize()
        {
            m_SimulatorState = m_Main.SerializeSimulatorState();
        }

        public void OnAfterDeserialize()
        {
        }

        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Reload"), false, m_Main.InitSimulation);
            if (RenderDoc.IsInstalled() && !RenderDoc.IsLoaded())
            {
                menu.AddItem(RenderDocUtil.LoadRenderDocMenuItem, false, RenderDoc.LoadRenderDoc);
            }
        }

        private void OnFocus()
        {
            SetFocus(true);
        }

        public static void MarkAllDeviceListsDirty()
        {
            foreach (var simulator in s_SimulatorInstances)
            {
                simulator.m_DeviceListDirty = true;
            }
        }

        protected override void OnEnterPlayModeBehaviorChange()
        {
            m_Main.userInterface.UpdateEnterPlayModeBehaviorMsg();
        }

        public void OnPlayPopupSelection(int indexClicked, object objectSelected)
        {
            playModeBehaviorIdx = indexClicked;
            if (playModeBehaviorIdx == 0)
            {
                if (playFocused)
                    enterPlayModeBehavior = EnterPlayModeBehavior.PlayFocused;
                else
                    enterPlayModeBehavior = EnterPlayModeBehavior.PlayUnfocused;
                fullscreenMonitorIdx = PlayModeView.kFullscreenNone;
            }
            else if (playModeBehaviorIdx == 1)
            {
                enterPlayModeBehavior = EnterPlayModeBehavior.PlayMaximized;
                fullscreenMonitorIdx = PlayModeView.kFullscreenNone;
            }
            OnEnterPlayModeBehaviorChange();
        }

        private void OnDevicePackageStatus(DevicePackageStatus status)
        {
            m_Main.userInterface.DeviceButtonState = new DevicePackageInstallButtonState
            {
                PackageStatus = status,
                OnPressed = DevicePackage.Add
            };
        }
    }
}
