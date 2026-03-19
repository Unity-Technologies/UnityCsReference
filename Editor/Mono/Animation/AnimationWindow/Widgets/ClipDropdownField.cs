// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine.UIElements;

namespace UnityEditor.Animations.AnimationWindow.Widgets
{
    class ClipDropdownField : PopupField<IAnimationWindowClip>, IDisposable
    {
        [global::System.Runtime.CompilerServices.CompilerGenerated]
        [global::System.Serializable]
        internal new class UxmlSerializedData : global::UnityEngine.UIElements.VisualElement.UxmlSerializedData
        {

            public override object CreateInstance() => new ClipDropdownField();
        }

        static string s_CreateNewClip = L10n.Tr("Create New Clip...");

        private AnimationWindowState m_State;

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
                var isSelected = (menuItem == value) && !showMixedValue;
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

        new string GetListItemToDisplay(IAnimationWindowClip clip) =>
            CurveUtility.GetClipName(clip);

        internal override string GetValueToDisplay() => rawValue != null ? CurveUtility.GetClipName(rawValue) : String.Empty;

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
            SetEnabled(m_State.selection.canChangeClip);
            SetValueWithoutNotify(m_State.activeClip);
        }
    }
}
