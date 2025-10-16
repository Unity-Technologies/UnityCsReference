// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Elements
{
    internal class BuildProfileWelcomeElement : VisualElement
    {
        protected virtual string k_Uxml => "BuildProfile/UXML/BuildProfileWelcomeView.uxml";
        protected readonly Label m_WelcomeTitle;
        protected readonly Label m_WelcomeBody;


        internal BuildProfileWelcomeElement()
        {
            var uxml = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var stylesheet = EditorGUIUtility.LoadRequired(Util.k_StyleSheet) as StyleSheet;
            styleSheets.Add(stylesheet);
            uxml.CloneTree(this);

            m_WelcomeTitle = this.Q<Label>("welcome-title-label");
            m_WelcomeBody = this.Q<Label>("welcome-body-label");
            var addProfileButton = this.Q<Button>("add-profile-button");
            addProfileButton.clicked += PlatformDiscoveryWindow.ShowWindow;
            addProfileButton.text = TrText.addBuildProfile;

            m_WelcomeTitle.text = TrText.welcomeToBuildProfiles;
            m_WelcomeBody.text = TrText.welcomeToBuildProfilesMessage;
        }
    }
}
