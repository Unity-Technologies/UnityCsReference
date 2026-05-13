// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Animations.AnimationWindow.Widgets
{
    [UxmlElement]
    partial class ClipDropdownField : PopupField<IAnimationWindowClip>, IDisposable
    {
        static string s_CreateNewClip = L10n.Tr("Create New Clip...");

        private AnimationWindowState m_State;

        // UXML serialization requires a string-typed attribute named "value" because no converter
        // exists for IAnimationWindowClip. Hidden so it does not appear in the inspector.
        [UxmlAttribute("value"), HideInInspector]
        internal string valueOverride { get; set; }

        public void Initialize(AnimationWindowState state)
        {
            m_State = state;

            SetValueWithoutNotify(m_State.activeClip);
            m_State.onRefresh += OnRefresh;

            OnRefresh();
        }

        public void Dispose()
        {
            m_State.onRefresh -= OnRefresh;
        }

        private List<IAnimationWindowClip> GetOrderedClipList()
        {
            var clips = new List<IAnimationWindowClip>(m_State.selection.GetClips());
            clips.Sort((clip1, clip2) => EditorUtility.NaturalCompare(clip1.name, clip2.name));
            return clips;
        }

        internal override void AddMenuItems(AbstractGenericMenu menu)
        {
            if (menu == null)
            {
                throw new ArgumentNullException(nameof(menu));
            }

            choices = GetOrderedClipList();
            foreach (var menuItem in choices)
            {
                var isSelected = menuItem.Equals(value) && !showMixedValue;
                menu.AddItem(
                    GetListItemToDisplay(menuItem),
                    isSelected,
                    () => ChangeValueFromMenu(menuItem));
            }

            if (m_State.selection.canChangeClip)
            {
                menu.AddSeparator(String.Empty);
                menu.AddItem(s_CreateNewClip, false, CreateNewClipFromMenu);
            }
        }

        string GetClipName(IAnimationWindowClip clip)
        {
            if (!clip?.isValid ?? true)
                return "[No Clip]";

            string name = clip.name;

            if (clip.isReadOnly)
                name += " (Read-Only)";

            return name;
        }

        new string GetListItemToDisplay(IAnimationWindowClip clip) =>
            GetClipName(clip);

        internal override string GetValueToDisplay() => rawValue != null ? GetClipName(rawValue) : String.Empty;

        private void ChangeValueFromMenu(IAnimationWindowClip menuItem)
        {
            if (m_State.animEditor.DisplayUnsavedChangesDialogIfNecessary())
            {
                value = menuItem;
                m_State.activeClip = value;
            }
        }

        private void CreateNewClipFromMenu()
        {
            var newClip = m_State.selection.CreateNewClip();

            if (newClip != null)
            {
                if (m_State.animEditor.DisplayUnsavedChangesDialogIfNecessary())
                {
                    value = newClip;
                    m_State.activeClip = newClip;
                }

            }
        }

        private void OnRefresh()
        {
            SetEnabled(!m_State.disabled && m_State.selection.canChangeClip);
            SetValueWithoutNotify(m_State.activeClip);
        }
    }
}
