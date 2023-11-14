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
    internal class BuildProfileListLabel : VisualElement
    {
        const string k_Uxml = "BuildProfile/UXML/BuildProfileLabelElement.uxml";
        readonly Image m_Icon;
        readonly Label m_Text;
        readonly Label m_ActiveIndicator;

        internal BuildProfileListLabel()
        {
            var uxml = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var stylesheet = EditorGUIUtility.LoadRequired(Util.k_StyleSheet) as StyleSheet;
            styleSheets.Add(stylesheet);
            uxml.CloneTree(this);

            m_Icon = this.Q<Image>();
            m_Text = this.Q<Label>("profile-list-label-name");
            m_ActiveIndicator = this.Q<Label>("profile-list-label-active");
            m_ActiveIndicator.text = TrText.active;
            SetActiveIndicator(false);
        }

        internal void Set(string displayName, Texture2D icon)
        {
            m_Icon.image = icon;
            m_Text.text = displayName;
        }

        internal void SetActiveIndicator(bool active)
        {
            if (active)
                m_ActiveIndicator.Show();
            else
                m_ActiveIndicator.Hide();
        }
    }
}
