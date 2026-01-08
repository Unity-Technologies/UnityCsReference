// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Elements
{
    /// <summary>
    /// List item showing a build profile name and icon in the <see cref="BuildProfileWindow"/>
    /// classic platform or build profile columns.
    /// </summary>
    internal class BuildProfilePlatformElement : VisualElement
    {
        readonly Image m_Icon;
        protected virtual string k_Uxml => "BuildProfile/UXML/BuildProfilePlatformElement.uxml";
        protected readonly Label m_Text;
        private readonly Action<BuildProfileCard> m_OnClicked;
        protected BuildProfileCard m_BuildProfileCard;

        internal BuildProfilePlatformElement(BuildProfileCard card, Action<BuildProfileCard> onSelected)
        {
            var uxml = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var stylesheet = EditorGUIUtility.LoadRequired(Util.k_StyleSheet) as StyleSheet;
            styleSheets.Add(stylesheet);
            uxml.CloneTree(this);

            m_Icon = this.Q<Image>();
            m_Text = this.Q<Label>("platform-element-label");

            if (!BuildProfileModuleUtil.IsModuleInstalled(card.platformId))
                this.style.opacity = 0.5f;

            Set(card);
            m_OnClicked = onSelected;
            this.focusable = true;

            RegisterCallback<ClickEvent>(OnClickEvent);
            RegisterCallback<FocusEvent>(OnFocusEvent);
        }

        internal BuildProfileCard GetBuildProfileCard()
        {
            return m_BuildProfileCard;
        }

        void OnClickEvent(ClickEvent evt)
        {
            if (evt.currentTarget is BuildProfilePlatformElement element)
            {
                m_OnClicked?.Invoke(m_BuildProfileCard);
            }
        }

        void OnFocusEvent(FocusEvent evt)
        {
            if (evt.currentTarget is BuildProfilePlatformElement element)
            {
                m_OnClicked?.Invoke(m_BuildProfileCard);
            }
        }

        void Set(BuildProfileCard card)
        {
            m_BuildProfileCard = card;
            m_Icon.image = BuildProfileModuleUtil.GetPlatformIcon(card.platformId);
            m_Text.text = card.displayName;
        }
    }
}
