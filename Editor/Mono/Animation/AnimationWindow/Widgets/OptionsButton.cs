// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

using UnityEditor.Animations.AnimationWindow.TimelineFoundation;
using UnityEditorInternal;

namespace UnityEditor.Animations.AnimationWindow.Widgets
{
    class OptionsButton : VisualElement, IDisposable
    {
        [global::System.Runtime.CompilerServices.CompilerGenerated]
        [global::System.Serializable]
        internal new class UxmlSerializedData : global::UnityEngine.UIElements.VisualElement.UxmlSerializedData
        {

            public override object CreateInstance() => new OptionsButton();
        }

        const string k_TimeFormatGroupName = "timeFormat";
        const string k_ToolOptionsGroupName = "toolOptions";
        const string k_FrameRateGroupName = "frameRate";
        const string k_GeneralOptionsGroupName = "generalOptions";

        static readonly (string, float)[] k_AvailableFrameRates = new (string, float)[]
        {
            new ("Set Sample Rate/24", 24f),
            new ("Set Sample Rate/25", 25f),
            new ("Set Sample Rate/30", 30f),
            new ("Set Sample Rate/50", 50f),
            new ("Set Sample Rate/60", 60f)
        };

        AnimEditor m_AnimEditor;
        AnimationWindowState state => m_AnimEditor.state;

        class OptionsMenuAction
        {
            public OptionsMenuAction()
            {
                action = menuAction => {};
                actionStatusCallback = menuAction => DropdownMenuAction.Status.Disabled;
            }

            public string Name;
            public string Group;
            public Func<string> lateNameUpdateAction;
            public Action<DropdownMenuAction> action;
            public Func<DropdownMenuAction, DropdownMenuAction.Status> actionStatusCallback;

            public void UpdateName()
            {
                if (lateNameUpdateAction != null)
                {
                    Name = lateNameUpdateAction.Invoke();
                }
            }
        }

        class OptionsMenuActionCollection
        {
            readonly Dictionary<string, List<OptionsMenuAction>> m_ActionsDictionary;
            const string k_DefaultActionsGroup = "default";

            public OptionsMenuActionCollection()
            {
                m_ActionsDictionary = new Dictionary<string, List<OptionsMenuAction>>
                {
                    [k_DefaultActionsGroup] = new()
                };
            }

            public void Add(OptionsMenuAction action)
            {
                var group = action.Group;
                if (group == string.Empty)
                {
                    group = k_DefaultActionsGroup;
                }

                if (!m_ActionsDictionary.TryGetValue(group, out var groupActions))
                {
                    groupActions = new List<OptionsMenuAction>();
                    m_ActionsDictionary[group] = groupActions;
                }

                groupActions.Add(action);
            }

            public void FillMenu(DropdownMenu dropdownMenu)
            {
                if (m_ActionsDictionary.Count == 0) return;

                var currentGroup = String.Empty;
                foreach (var kvp in m_ActionsDictionary)
                {
                    var group = kvp.Key;
                    if (currentGroup != group)
                    {
                        dropdownMenu.AppendSeparator();
                        currentGroup = group;
                    }

                    foreach (var option in kvp.Value)
                    {
                        option.UpdateName();
                        dropdownMenu.AppendAction(option.Name, option.action, option.actionStatusCallback);
                    }
                }
            }
        }

        Func<TimeFormat> m_GetTimeFormatFunc;
        Action<TimeFormat> m_SetTimeFormatFunc;
        Func<bool> m_IsValidFunc;

        readonly OptionsMenuActionCollection m_ActionCollection;

        public OptionsButton()
        {
            this.AddToTimelineClassList("optionsButton");

            ContextualMenuManipulator optionsMenuManipulator = new(CreateOptionsMenu);
            optionsMenuManipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });

            this.AddManipulator(optionsMenuManipulator);

            m_ActionCollection = new OptionsMenuActionCollection();
        }

        // Probably better to use Actions. Using direct references for now since it's more straightforward
        public void Initialize(AnimEditor animEditor, Func<TimeFormat> getTimeFormatFunc, Action<TimeFormat> setTimeFormatFunc, Func<bool> isValidFunc)
        {
            m_AnimEditor = animEditor;
            m_GetTimeFormatFunc = getTimeFormatFunc;
            m_SetTimeFormatFunc = setTimeFormatFunc;
            m_IsValidFunc = isValidFunc;
            CreateDefaultActions();

            m_AnimEditor.state.onRefresh += OnRefresh;

            OnRefresh();
        }

        public void Dispose()
        {
            m_AnimEditor.state.onRefresh -= OnRefresh;
        }

        void CreateOptionsMenu(ContextualMenuPopulateEvent evt)
        {
            m_ActionCollection.FillMenu(evt.menu);
        }

        void AddAction(
            string name,
            string group,
            Action<DropdownMenuAction> action,
            Func<DropdownMenuAction, DropdownMenuAction.Status> actionStatusCallback,
            Func<string> updateNameAction = null)
        {
            m_ActionCollection.Add(new OptionsMenuAction()
            {
                Name = name,
                Group = group,
                lateNameUpdateAction = updateNameAction,
                action = action,
                actionStatusCallback = actionStatusCallback
            });
        }

        void CreateDefaultActions()
        {
            CreateTimeFormatSubMenuActions();
            CreateToolOptionsSubMenuActions();
            CreateFrameRateSubMenuActions();
            CreateGeneralOptionsSubMenuActions();
        }

        void CreateTimeFormatSubMenuActions()
        {
            AddAction(
                L10n.Tr("Frames"),
                k_TimeFormatGroupName,
                _ => m_SetTimeFormatFunc?.Invoke(TimeFormat.Frames),
                _ =>
                {
                    if (!Valid) return DropdownMenuAction.Status.Disabled;
                    return m_GetTimeFormatFunc?.Invoke() == TimeFormat.Frames ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                });

            AddAction(
                L10n.Tr("Time Code"),
                k_TimeFormatGroupName,
                _ => m_SetTimeFormatFunc?.Invoke(TimeFormat.Timecode),
                _ =>
                {
                    if (!Valid) return DropdownMenuAction.Status.Disabled;
                    return m_GetTimeFormatFunc?.Invoke() == TimeFormat.Timecode ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                }
            );

            AddAction(
                L10n.Tr("Seconds"),
                k_TimeFormatGroupName,
                _ => m_SetTimeFormatFunc?.Invoke(TimeFormat.Seconds),
                _ =>
                {
                    if (!Valid) return DropdownMenuAction.Status.Disabled;
                    return m_GetTimeFormatFunc?.Invoke() == TimeFormat.Seconds ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                }
            );
        }

        void CreateToolOptionsSubMenuActions()
        {
            AddAction(
                L10n.Tr("Ripple"),
                k_ToolOptionsGroupName,
                _ => state.rippleTime = !state.rippleTime,
                _ =>
                {
                    if (!Valid) return DropdownMenuAction.Status.Disabled;
                    return state.rippleTime ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                });
        }

        void CreateFrameRateSubMenuActions()
        {
            AddAction(
                L10n.Tr("Show Sample Rate"),
                k_FrameRateGroupName,
                _ => state.showFrameRate = !state.showFrameRate,
                _ =>
                {
                    if (!Valid) return DropdownMenuAction.Status.Disabled;
                    return state.showFrameRate ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                });

            for (int i = 0; i < k_AvailableFrameRates.Length; ++i)
            {
                var (name, value) = k_AvailableFrameRates[i];

                AddAction(
                    L10n.Tr(name),
                    k_FrameRateGroupName,
                    _ => m_AnimEditor.SetFrameRate(value),
                    _ =>
                    {
                        if (!Valid || (state.selection?.isReadOnly ?? true)) return DropdownMenuAction.Status.Disabled;
                        return state.frameRate.Equals(value) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                    });
            }
        }

        void CreateGeneralOptionsSubMenuActions()
        {
            AddAction(
                L10n.Tr("Show Read-only Properties"),
                k_GeneralOptionsGroupName,
                _ =>  state.showReadOnly = ! state.showReadOnly,
                _ =>
                {
                    if (!Valid) return DropdownMenuAction.Status.Disabled;
                    return state.showReadOnly ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                });
        }

        void OnRefresh()
        {
            SetEnabled(!m_AnimEditor.state.disabled);
        }

        bool Valid => m_IsValidFunc?.Invoke() ?? false;
    }
}
