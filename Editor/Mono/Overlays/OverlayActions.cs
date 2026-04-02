// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using State = UnityEditor.Overlays.DynamicPanelOverlayContainer.State;

namespace UnityEditor.Overlays
{
    class OverlayActions : VisualElement
    {
        public const string className = "unity-overlay-actions";
        public const string toolbarToggleName = "ToolbarModeToggle";
        public const string minimizedToggleName = "MinimizedModeToggle";
        public readonly string togglePanelTooltip = L10n.Tr("Expand as dynamic panel");
        public readonly string toggleToolbarTooltip = L10n.Tr("Show as toolbar");
        public readonly string toggleMinimizeTooltip = L10n.Tr("Minimize");

        public event Action<State> stateChanged;

        public State state
        {
            get => m_State;
            set
            {
                if (m_State == value)
                    return;

                m_State = value;
                UpdateElements();
            }
        }

        public bool showMinimized
        {
            get => m_MinimizedToggle.style.display.value != DisplayStyle.None;
            set => m_MinimizedToggle.style.display = value ? StyleKeyword.Null : DisplayStyle.None;
        }

        public bool showToolbar
        {
            get => m_ToolbarToggle.style.display.value != DisplayStyle.None;
            set => m_ToolbarToggle.style.display = value ? StyleKeyword.Null : DisplayStyle.None;
        }

        State m_State;
        ToolbarToggle m_ToolbarToggle;
        ToolbarToggle m_MinimizedToggle;

        public OverlayActions()
        {
            AddToClassList(className);

            Add(m_ToolbarToggle = new ToolbarToggle { name = toolbarToggleName });
            Add(m_MinimizedToggle = new ToolbarToggle { name = minimizedToggleName });

            m_ToolbarToggle.RegisterCallback<ChangeEvent<bool>>((args) =>
            {
                if (m_State != State.Minimized)
                {
                    SetState(args.newValue ? State.Toolbar : State.Panel);
                }
                else
                {
                    SetState(args.newValue ? State.Panel : State.Toolbar);
                }
            });
            m_MinimizedToggle.RegisterCallback<ChangeEvent<bool>>((args) =>
            {
                SetState(args.newValue ? State.Minimized : State.Panel);
            });
            RegisterCallback<GeometryChangedEvent>(DelayPickingModeSet);

            //Init the values and tooltips
            UpdateElements();
        }

        // The overlay canvas turns off all picking when initialized (before overlay are added)
        void DelayPickingModeSet(GeometryChangedEvent evt)
        {
            UnregisterCallback<GeometryChangedEvent>(DelayPickingModeSet);

            m_ToolbarToggle.pickingMode = PickingMode.Position;
            m_MinimizedToggle.pickingMode = PickingMode.Position;
        }

        void SetState(State state)
        {
            if (state == m_State)
                return;

            m_State = state;
            stateChanged?.Invoke(m_State);
            UpdateElements();
        }

        void UpdateElements()
        {
            m_ToolbarToggle.SetValueWithoutNotify(m_State == State.Toolbar || m_State == State.Minimized);
            m_MinimizedToggle.SetValueWithoutNotify(m_State == State.Minimized);

            if (m_State == State.Panel)
            {
                m_ToolbarToggle.tooltip = toggleToolbarTooltip;
                m_MinimizedToggle.tooltip = toggleMinimizeTooltip;
            }
            else if (m_State == State.Toolbar)
            {
                m_ToolbarToggle.tooltip = togglePanelTooltip;
                m_MinimizedToggle.tooltip = toggleMinimizeTooltip;
            }
            else // minimize
            {
                m_ToolbarToggle.tooltip = toggleToolbarTooltip;
                m_MinimizedToggle.tooltip = togglePanelTooltip;
            }
        }
    }
}
