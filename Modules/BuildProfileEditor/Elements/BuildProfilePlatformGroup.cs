// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace UnityEditor.Build.Profile.Elements
{
    internal class BuildProfilePlatformGroup : VisualElement
    {
        protected virtual string k_Uxml => "BuildProfile/UXML/BuildProfilePlatformGroup.uxml";
        protected readonly Label m_Text;
        protected readonly VisualElement m_Container;
        readonly PlatformDiscoveryWindow m_Parent;
        protected VisualElement m_selected;
        protected List<BuildProfilePlatformElement> m_CardElements;

        internal BuildProfilePlatformGroup(PlatformDiscoveryWindow parent)
        {
            m_Parent = parent;
            var uxml = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var stylesheet = EditorGUIUtility.LoadRequired(Util.k_StyleSheet) as StyleSheet;
            styleSheets.Add(stylesheet);
            uxml.CloneTree(this);

            m_Text = this.Q<Label>("platform-group-label");
            m_Container = this.Q<VisualElement>("platform-element-container");
        }

        internal void Set(string groupingName, BuildProfileCard[] cards)
        {
            m_Text.text = groupingName;
            m_CardElements = new List<BuildProfilePlatformElement>();

            int i = 0;
            foreach (var card in cards)
            {
                var platformElement = new BuildProfilePlatformElement();

                platformElement.Set(card.displayName, BuildProfileModuleUtil.GetPlatformIcon(card.platformId), card);
                if (!BuildProfileModuleUtil.IsModuleInstalled(card.platformId))
                    platformElement.style.opacity = 0.5f;
                platformElement.RegisterCallback<ClickEvent>(OnBuildProfileClicked);

                m_Container.Insert(i, platformElement);
                i++;

                m_CardElements.Add(platformElement);
            }
        }

        void OnBuildProfileClicked(ClickEvent evt)
        {
            BuildProfilePlatformElement clickedElement = evt.currentTarget as BuildProfilePlatformElement;
            m_Parent.OnCardSelected(clickedElement.GetBuildProfileCard());
        }

        internal void ClearSelection()
        {
            if (m_selected != null)
                m_selected.RemoveFromClassList("build-profile-label-selected");
        }

        internal void SetCardSelected(BuildProfileCard card)
        {
            ClearSelection();
            foreach (var el in m_CardElements)
            {
                if (el.GetBuildProfileCard().platformId == card.platformId)
                {
                    VisualElement elementIcon = el.Q<VisualElement>("platform-icon");

                    m_selected = elementIcon;
                    m_selected.AddToClassList("build-profile-label-selected");
                }
            }
        }

    }
}
