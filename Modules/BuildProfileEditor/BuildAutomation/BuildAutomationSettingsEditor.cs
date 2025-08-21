// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Editor class responsible for visualizing build automation settings.
    /// </summary>
    internal class BuildAutomationSettingsEditor : VisualElement
    {
        const string k_Uxml = "BuildProfile/UXML/BuildAutomationSettings.uxml";
        const string k_StyleSheet = "BuildProfile/StyleSheets/BuildAutomation.uss";

        const string k_BuildAutomationRoot = "editor-build-automation-settings";
        const string k_BuildAutomationErrorBox = "custom-build-automation-info-errorbox";
        const string k_BuildAutomationPackageName = "com.unity.services.cloud-build";

        internal static readonly string buildAutomationLabelText = L10n.Tr("Build Automation Settings");
        private static readonly string BuildAutomationError = L10n.Tr("Build Automation Settings failed to load");
        private static readonly string s_PackageRequiredError = L10n.Tr("Build Automation Settings require the Build Automation package to be installed.");

        VisualElement m_BuildAutomationRoot;

        HelpBox m_BuildAutomationErrorBox;

        readonly BuildAutomationSettings m_BuildAutomationSettings;
        Editor m_BuildAutomationSettingsEditor;
        VisualElement m_BuildAutomationInspector;

        public BuildAutomationSettingsEditor(BuildProfile buildProfile)
        {
            var visualTree = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var windowUss = EditorGUIUtility.LoadRequired(k_StyleSheet) as StyleSheet;
            visualTree.CloneTree(this);
            styleSheets.Add(windowUss);

            InitializeVisualElements();

            if (!IsBuildAutomationPackagePresent())
            {
                m_BuildAutomationErrorBox.text = s_PackageRequiredError;
                m_BuildAutomationErrorBox.Show();
                return;
            }

            m_BuildAutomationSettings = buildProfile.GetComponent<BuildAutomationSettings>();

            if (m_BuildAutomationSettings == null)
            {
                throw new InvalidOperationException("Build Automation Settings do not exist in the build profile.");
            }

            RegisterCallback<AttachToPanelEvent>(ShowBuildAutomationEditor);
            RegisterCallback<DetachFromPanelEvent>(RemoveBuildAutomationEditor);
        }

        internal static bool IsBuildAutomationPackagePresent() => PackageManager.PackageInfo.IsPackageRegistered(k_BuildAutomationPackageName);

        private void InitializeVisualElements()
        {
            m_BuildAutomationRoot = this.Q<VisualElement>(k_BuildAutomationRoot);
            m_BuildAutomationErrorBox = m_BuildAutomationRoot.Q<HelpBox>(k_BuildAutomationErrorBox);
            m_BuildAutomationRoot.Show();
        }

        /// <summary>
        /// Creates a BuildAutomationSettings sub asset for the specified BuildProfile.
        /// </summary>
        internal static void AddBuildAutomationSettings(BuildProfile buildProfile)
        {
            if (buildProfile.GetComponent<BuildAutomationSettings>() is not null)
                return;

            var subAsset = ScriptableObject.CreateInstance<BuildAutomationSettings>();
            subAsset.name = "BuildAutomationSettings";
            var (buildTarget, _) = BuildProfileModuleUtil.GetBuildTargetAndSubtarget(buildProfile.platformGuid);
            subAsset.buildTarget = buildTarget;
            subAsset.hideFlags |= HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(subAsset, buildProfile);
        }

        private void ShowBuildAutomationEditor(AttachToPanelEvent evt)
        {
            try
            {
                if (m_BuildAutomationSettingsEditor == null)
                {
                    m_BuildAutomationSettingsEditor = Editor.CreateEditor(m_BuildAutomationSettings);
                }

                if (m_BuildAutomationInspector == null)
                {
                    m_BuildAutomationInspector = new InspectorElement(m_BuildAutomationSettingsEditor);
                    m_BuildAutomationRoot.Add(m_BuildAutomationInspector);
                }

                m_BuildAutomationErrorBox.Hide();
            }
            catch (Exception e)
            {
                m_BuildAutomationErrorBox.Show();
                m_BuildAutomationErrorBox.text = $"{BuildAutomationError}: {e.Message}";
                Debug.LogException(e);
            }
        }

        private void RemoveBuildAutomationEditor(DetachFromPanelEvent evt)
        {
            if (m_BuildAutomationSettingsEditor != null)
            {
                UnityEngine.Object.DestroyImmediate(m_BuildAutomationSettingsEditor);
                m_BuildAutomationSettingsEditor = null;
            }

            m_BuildAutomationRoot.Clear();
            m_BuildAutomationInspector = null;
        }
    }
}
