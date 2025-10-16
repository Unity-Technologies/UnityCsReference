// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
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
        protected BuildProfileCard m_BuildProfileCard;

        internal BuildProfilePlatformElement()
        {
            var uxml = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var stylesheet = EditorGUIUtility.LoadRequired(Util.k_StyleSheet) as StyleSheet;
            styleSheets.Add(stylesheet);
            uxml.CloneTree(this);

            m_Icon = this.Q<Image>();
            m_Text = this.Q<Label>("platform-element-label");
        }

        internal void Set(string displayName, Texture2D icon, BuildProfileCard card)
        {
            m_Icon.image = icon;
            m_Text.text = displayName;
            m_BuildProfileCard = card;
        }

        internal BuildProfileCard GetBuildProfileCard()
        {
            return m_BuildProfileCard;
        }
    }
}
