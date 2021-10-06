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
    internal class SimulatorWindow : PlayModeView, IHasCustomMenu, ISerializationCallbackReceiver
    {
        private static List<SimulatorWindow> s_SimulatorInstances = new List<SimulatorWindow>();
        private bool m_DeviceListDirty;

        [SerializeField] private SimulatorState m_SimulatorState;
        private SimulationState m_State = SimulationState.Enabled;
        private DeviceSimulatorMain m_Main;

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
                targetSize = m_Main.targetSize;
                m_Main.displayTexture = RenderView(m_Main.mousePositionInUICoordinates, false);
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
                menu.AddItem(EditorGUIUtility.TrTextContent(RenderDocUtil.loadRenderDocLabel), false, LoadRenderDoc);
            }
        }

        private void LoadRenderDoc()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                RenderDoc.Load();
                ShaderUtil.RecreateGfxDevice();
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
    }
}
