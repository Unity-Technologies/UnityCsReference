// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Cloud Build button modal prompts the user to install the Build Automation package,
    /// create a Build Profile, or add credentials to the selected Build Profile.
    /// </summary>
    internal class BuildAutomationModalWindow : EditorWindow
    {
        const string k_PackageId = "com.unity.services.cloud-build";
        const string k_Uxml = "BuildProfile/UXML/BuildAutomationModalWindow.uxml";

        static readonly string s_Label_Install = L10n.Tr("Cloud Builds require the Build Automation package. Clicking install will add the package your project. It will also create a Build Profile so you can sign your builds with credentials from the Build Automation service.");
        static readonly string s_Label_Credentials = L10n.Tr("Cloud Builds require Cloud Build credential settings to be added to the selected Build Profile.");
        static readonly string s_Button_Install = L10n.Tr("Install");
        static readonly string s_Button_Credentials = L10n.Tr("Add Settings");

        static readonly string s_Helpbox = L10n.Tr("Cloud Build is a pay-as-you-go service provided by Unity. You can start using it for free without a credit card on file, and we will notify you when you reach the limits of the free tier.");

        BuildProfileWindow m_ParentWindow;
        BuildProfile m_TargetProfile;
        Button m_SubmitButton;
        Label m_InfoLabel;

        /// <summary>
        /// On Cloud Build button callback.
        /// Tries to invoke UBA package callback OR displays modal
        /// for cloud build integration.
        /// </summary>
        /// <param name="profile">Target build profile.</param>
        public static void OnCloudBuildClicked(BuildProfile profile, BuildProfileWindow parentWindow)
        {
            if (BuildProfileContext.IsClassicPlatformProfile(profile))
            {
                // Classic profiles should not be invoked by Cloud Build.
                return;
            }

            bool isInstalled = PackageManager.PackageInfo.IsPackageRegistered(k_PackageId);
            bool hasCloudSettings = BuildAutomationSettingsEditor.GetSubAssetFromBuildProfile(profile) != null;
            if (isInstalled && hasCloudSettings)
            {
                BuildAutomation.OnCloudBuildClicked(profile);
                return;
            }

            // Show modal automating cloud build workflow integration,
            // handle package installation and initial configuration object.
            var window = GetWindow<BuildAutomationModalWindow>();
            window.minSize = new Vector2(600, 180);
            window.maxSize = new Vector2(600, 180);
            window.m_TargetProfile = profile;
            window.m_ParentWindow = parentWindow;

            if (!isInstalled)
            {
                window.SetInstallInfo();
            }
            else if (!hasCloudSettings)
            {
                window.SetAddCredentialsInfo();
            }

            window.ShowModal();
        }

        public void CreateGUI()
        {
            var windowUxml = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var windowUss = EditorGUIUtility.LoadRequired(Util.k_StyleSheet) as StyleSheet;
            rootVisualElement.styleSheets.Add(windowUss);
            windowUxml.CloneTree(rootVisualElement);

            m_InfoLabel = rootVisualElement.Q<Label>("uba-label");
            m_SubmitButton = rootVisualElement.Q<Button>("submit-button");
            var helpbox = rootVisualElement.Q<HelpBox>("uba-helpbox");
            var cancel = rootVisualElement.Q<Button>("cancel-button");

            helpbox.text = s_Helpbox;
            cancel.text = TrText.cancelButtonText;

            cancel.clicked += Close;
            rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnGeometryCalculated);
        }

        void OnGeometryCalculated(GeometryChangedEvent evt)
        {
            var target = (VisualElement)evt.target;

            var dimensions = new Vector2(
                target.resolvedStyle.width,
                target.resolvedStyle.height
            );

            minSize = dimensions;
            maxSize = dimensions;
        }

        void SetInstallInfo()
        {
            m_InfoLabel.text = s_Label_Install;
            m_SubmitButton.text = s_Button_Install;
            m_SubmitButton.clicked += OnInstall;
        }

        void SetAddCredentialsInfo()
        {
            m_InfoLabel.text = s_Label_Credentials;
            m_SubmitButton.text = s_Button_Credentials;
            m_SubmitButton.clicked += OnAddCredentialsObject;
        }

        void OnInstall()
        {
            var request = PackageManager.Client.Add(k_PackageId);
            if (BuildAutomationSettingsEditor.GetSubAssetFromBuildProfile(m_TargetProfile) == null)
            {
                BuildAutomationSettingsEditor.AddBuildAutomationSettings(m_TargetProfile);
                m_ParentWindow.RepaintBuildProfileInspector();
            }

            Close();
        }

        void OnAddCredentialsObject()
        {
            if (BuildAutomationSettingsEditor.GetSubAssetFromBuildProfile(m_TargetProfile) == null)
            {
                BuildAutomationSettingsEditor.AddBuildAutomationSettings(m_TargetProfile);
                m_ParentWindow.RepaintBuildProfileInspector();
            }

            Close();
        }
    }
}
