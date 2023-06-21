// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("Editor Utility/Play Mode")]
    sealed class PlayModeButtons : VisualElement
    {
        const float k_ImguiOverrideWidth = 240f;

        readonly EditorToolbarToggle m_PlayButton;
        readonly EditorToolbarToggle m_PauseButton;
        readonly EditorToolbarButton m_StepButton;
        readonly VisualElement m_UIElementsRoot;
        readonly IMGUIContainer m_ImguiOverride;

        static readonly GUIContent s_PlayButtonContextMenuItem = EditorGUIUtility.TrTextContent("Create Game View On Play");

        public PlayModeButtons()
        {
            name = "PlayMode";

            Add(m_UIElementsRoot = new VisualElement());
            m_UIElementsRoot.style.flexDirection = FlexDirection.Row;

            m_UIElementsRoot.Add(m_PlayButton = new EditorToolbarToggle
            {
                name = "Play",
                tooltip = "Play",
                onIcon = EditorGUIUtility.LoadIcon("StopButton"),
                offIcon = EditorGUIUtility.LoadIcon("PlayButton"),
            });
            m_PlayButton.RegisterCallback<MouseDownEvent>(evt => OnPlayButtonRMBClick(evt));
            m_PlayButton.RegisterValueChangedCallback(OnPlayButtonValueChanged);

            m_UIElementsRoot.Add(m_PauseButton = new EditorToolbarToggle
            {
                name = "Pause",
                tooltip = "Pause",
                onIcon = EditorGUIUtility.LoadIcon("PauseButton On"),
                offIcon = EditorGUIUtility.LoadIcon("PauseButton"),
            });
            m_PauseButton.RegisterValueChangedCallback(OnPauseButtonValueChanged);

            m_UIElementsRoot.Add(m_StepButton = new EditorToolbarButton
            {
                name = "Step",
                tooltip = "Step"
            });
            m_StepButton.clickable.activators.Add(new ManipulatorActivationFilter {button = MouseButton.RightMouse});
            m_StepButton.clicked += OnStepButtonClicked;
            m_StepButton.icon = EditorGUIUtility.LoadIcon("StepButton");

            EditorToolbarUtility.SetupChildrenAsButtonStrip(m_UIElementsRoot);

            Add(m_ImguiOverride = new IMGUIContainer());
            m_ImguiOverride.style.display = DisplayStyle.None;
            m_ImguiOverride.style.width = k_ImguiOverrideWidth;

            UpdatePlayState();
            UpdatePauseState();
            UpdateStepState();

            //Immediately after a domain reload, Modes might be initialized after the toolbar so we wait a frame to check it
            EditorApplication.delayCall += () =>
            {
                CheckAvailability();
                CheckImguiOverride();
            };

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.pauseStateChanged += OnPauseStateChanged;
            ModeService.modeChanged += OnModeChanged;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.pauseStateChanged -= OnPauseStateChanged;
            ModeService.modeChanged -= OnModeChanged;
        }

        void OnModeChanged(ModeService.ModeChangedArgs args)
        {
            CheckAvailability();
            CheckImguiOverride();
        }

        void CheckAvailability()
        {
            style.display = ModeService.HasCapability(ModeCapability.Playbar, true) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void CheckImguiOverride()
        {
            var hasOverride = ModeService.HasExecuteHandler("gui_playbar");
            m_ImguiOverride.style.display = hasOverride ? DisplayStyle.Flex : DisplayStyle.None;
            m_ImguiOverride.onGUIHandler = hasOverride ? (Action)OverrideGUIHandler : null;
            m_UIElementsRoot.style.display = hasOverride ? DisplayStyle.None : DisplayStyle.Flex;
        }

        void OnPlayButtonValueChanged(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                EditorApplication.EnterPlaymode();
            }
            else
            {
                EditorApplication.ExitPlaymode();
            }
        }

        void OnPlayButtonRMBClick(MouseDownEvent evt)
        {
            if (evt.button == 1)
            {
                GenericMenu menu = new GenericMenu();
                bool enabled = GameView.openWindowOnEnteringPlayMode;
                menu.AddItem(s_PlayButtonContextMenuItem, enabled, ChangeOpenGameViewOnPlayModeBehavior);
                menu.ShowAsContext();
            }
        }

        void ChangeOpenGameViewOnPlayModeBehavior()
        {
            PlayModeView.openWindowOnEnteringPlayMode = !PlayModeView.openWindowOnEnteringPlayMode;
        }

        void OnPauseButtonValueChanged(ChangeEvent<bool> evt)
        {
            EditorApplication.isPaused = evt.newValue;
        }

        void OnStepButtonClicked()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.Step();
            }
        }

        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            UpdatePlayState();
            UpdateStepState();
        }

        void OnPauseStateChanged(PauseState state)
        {
            UpdatePauseState();
        }

        void UpdatePlayState()
        {
            m_PlayButton.SetValueWithoutNotify(EditorApplication.isPlayingOrWillChangePlaymode);
        }

        void UpdatePauseState()
        {
            m_PauseButton.SetValueWithoutNotify(EditorApplication.isPaused);
        }

        void UpdateStepState()
        {
            m_StepButton.SetEnabled(EditorApplication.isPlaying);
        }

        static void OverrideGUIHandler()
        {
            ModeService.Execute("gui_playbar", EditorApplication.isPlayingOrWillChangePlaymode);
        }
    }
}
