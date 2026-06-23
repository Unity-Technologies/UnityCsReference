// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Multiplayer.Internal;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class ReuseBuildElement
    {
        internal static event Action<BuildProfile> RebuildStateChanged;
        internal static event Action<BuildProfile, bool> UseExistingBuildChanged;
        internal static BuildProfile RebuildingBuildProfile { get; private set; }

        private const string k_UseExistingBuildLabel = "Use Existing Build";
        private const string k_SharedBuildProfileRunningWarning = "Build options are disabled because another instance with the same build profile is currently running.";
        private const string k_SharedBuildProfileRebuildingWarning = "Build options are disabled because another instance with the same build profile is currently rebuilding.";

        private TextField m_LastBuildTimeField;
        private TextField m_RecompileCountField;
        private Button m_RebuildButton;
        private Button m_ClearBuildsButton;
        private HelpBox m_SharedBuildProfileHelpBox;
        private VisualElement m_BuildOptionsContainer;
        private PropertyField m_UseExistingBuildField;

        private BuildProfile m_BuildProfile;
        private Instance m_Instance;
        private SerializedProperty m_UseExistingBuildProperty;

        private DateTime? m_CachedLastBuildTime;
        private DateTime m_CachedReportModifiedTime;
        private bool m_UpdatingFromSharedEvent;

        public ReuseBuildElement(Instance instance, BuildProfile buildProfile, SerializedProperty userSettingsProperty)
        {
            m_Instance = instance;
            m_BuildProfile = buildProfile;
            m_UseExistingBuildProperty = userSettingsProperty.FindPropertyRelative(nameof(LocalPlayerController.UserSettings.UseExistingBuild));
        }

        internal void BindElements(VisualElement container)
        {
            // Add Use Existing Build checkbox
            m_UseExistingBuildField = new PropertyField(m_UseExistingBuildProperty) { label = k_UseExistingBuildLabel };
            m_UseExistingBuildField.BindProperty(m_UseExistingBuildProperty);
            m_UseExistingBuildField.AddToClassList("unity-base-field__aligned");
            container.Add(m_UseExistingBuildField);

            // Add last build time field
            m_LastBuildTimeField = new TextField("Last Build At") { isReadOnly = true };
            m_LastBuildTimeField.SetValueWithoutNotify("N/A");
            m_LastBuildTimeField.AddToClassList("unity-base-field__aligned");
            container.Add(m_LastBuildTimeField);

            // Add recompile count field
            m_RecompileCountField = new TextField("Number of Recompiles") { isReadOnly = true };
            m_RecompileCountField.SetValueWithoutNotify("0");
            m_RecompileCountField.AddToClassList("unity-base-field__aligned");
            container.Add(m_RecompileCountField);

            UpdateBuildStats();

            // Add build options section - label and buttons in one row
            m_BuildOptionsContainer = new VisualElement();
            m_BuildOptionsContainer.style.flexDirection = FlexDirection.Row;
            m_BuildOptionsContainer.style.alignItems = Align.Center;
            m_BuildOptionsContainer.style.marginTop = 10;

            var buildOptionsLabel = new Label("Build Options:");
            buildOptionsLabel.style.marginRight = 10;
            m_BuildOptionsContainer.Add(buildOptionsLabel);

            m_RebuildButton = new Button(() => RebuildNow()) { text = "Rebuild Now" };
            m_RebuildButton.style.marginRight = 5;
            m_BuildOptionsContainer.Add(m_RebuildButton);

            m_ClearBuildsButton = new Button(() => ClearAllBuilds()) { text = "Clear All Builds" };
            m_BuildOptionsContainer.Add(m_ClearBuildsButton);

            container.Add(m_BuildOptionsContainer);

            // Add help box for shared build profile warning
            m_SharedBuildProfileHelpBox = new HelpBox(k_SharedBuildProfileRunningWarning, HelpBoxMessageType.Info);
            m_SharedBuildProfileHelpBox.style.display = DisplayStyle.None;
            container.Add(m_SharedBuildProfileHelpBox);

            // Set initial visibility
            UpdateVisibility();

            // Track changes to the checkbox and notify other instances with the same build profile
            m_UseExistingBuildField.TrackPropertyValue(m_UseExistingBuildProperty, prop =>
            {
                UpdateVisibility();
                if (!m_UpdatingFromSharedEvent)
                    UseExistingBuildChanged?.Invoke(m_BuildProfile, prop.boolValue);
            });

            // Listen for changes from other instances with the same build profile
            UseExistingBuildChanged += OnUseExistingBuildChanged;

            // Schedule periodic updates
            container.schedule.Execute(() => UpdateBuildStats()).Every(1000);
            container.schedule.Execute(() => UpdateButtonStates()).Every(500);

            // Subscribe to events
            RebuildStateChanged += OnRebuildStateChanged;
            m_Instance.StatusRefreshed += OnInstanceStatusRefreshed;
        }

        internal void OnAttachToPanel()
        {
            var scenario = ScenarioRunner.instance?.ActiveScenario;
            if (scenario != null)
                scenario.StatusRefreshed += OnScenarioStatusRefreshed;
        }

        internal void OnDetachFromPanel()
        {
            RebuildStateChanged -= OnRebuildStateChanged;
            UseExistingBuildChanged -= OnUseExistingBuildChanged;
            m_Instance.StatusRefreshed -= OnInstanceStatusRefreshed;

            var scenario = ScenarioRunner.instance?.ActiveScenario;
            if (scenario != null)
                scenario.StatusRefreshed -= OnScenarioStatusRefreshed;
        }

        private void OnUseExistingBuildChanged(BuildProfile changedProfile, bool newValue)
        {
            if (changedProfile != m_BuildProfile)
                return;

            if (m_UseExistingBuildProperty.boolValue != newValue)
            {
                m_UpdatingFromSharedEvent = true;
                try
                {
                    m_UseExistingBuildProperty.boolValue = newValue;
                    m_UseExistingBuildProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    UpdateVisibility();
                }
                finally
                {
                    m_UpdatingFromSharedEvent = false;
                }
            }
        }

        private void UpdateVisibility()
        {
            var isChecked = m_UseExistingBuildProperty.boolValue;
            m_LastBuildTimeField.style.display = isChecked ? DisplayStyle.Flex : DisplayStyle.None;
            m_RecompileCountField.style.display = isChecked ? DisplayStyle.Flex : DisplayStyle.None;
            m_BuildOptionsContainer.style.display = isChecked ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnScenarioStatusRefreshed(ScenarioStatusData status)
        {
            UpdateButtonStates();
        }

        private void OnRebuildStateChanged(BuildProfile rebuildingProfile)
        {
            UpdateButtonStates();
        }

        private void OnInstanceStatusRefreshed(Instance instance, InstanceStatusData status)
        {
            UpdateButtonStates();
        }

        private void UpdateBuildStats()
        {
            if (m_BuildProfile == null)
            {
                if (m_LastBuildTimeField != null)
                    m_LastBuildTimeField.SetValueWithoutNotify("N/A");
                if (m_RecompileCountField != null)
                    m_RecompileCountField.SetValueWithoutNotify("0");
                return;
            }

            var buildPath = ScenarioFactory.GenerateBuildPath(m_BuildProfile);
            var buildDirectory = Path.GetDirectoryName(buildPath);
            var reportPath = Path.Combine(buildDirectory, ".buildreport");

            if (File.Exists(reportPath))
            {
                var modifiedTime = File.GetLastWriteTimeUtc(reportPath);
                if (modifiedTime != m_CachedReportModifiedTime)
                {
                    m_CachedReportModifiedTime = modifiedTime;
                    m_CachedLastBuildTime = null;

                    try
                    {
                        var json = File.ReadAllText(reportPath);
                        var reportData = JsonUtility.FromJson<BuildReportData>(json);
                        if (reportData != null && reportData.buildEndedAtTicks > 0)
                            m_CachedLastBuildTime = new DateTime(reportData.buildEndedAtTicks, DateTimeKind.Local);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Failed to load build report from '{reportPath}': {e.Message}");
                    }
                }
            }
            else
            {
                m_CachedLastBuildTime = null;
                m_CachedReportModifiedTime = default;
            }

            if (m_LastBuildTimeField != null)
                m_LastBuildTimeField.SetValueWithoutNotify(m_CachedLastBuildTime.HasValue
                    ? GetFormattedBuildTime(m_CachedLastBuildTime.Value)
                    : "No build yet");

            if (m_RecompileCountField != null)
                m_RecompileCountField.SetValueWithoutNotify(m_CachedLastBuildTime.HasValue
                    ? RecompileTracker.GetRecompileCountSince(m_CachedLastBuildTime.Value).ToString()
                    : "0");
        }

        private static string GetFormattedBuildTime(DateTime pastTime)
        {
            var localTime = pastTime.Kind == DateTimeKind.Utc ? pastTime.ToLocalTime() : pastTime;
            return localTime.ToString("MMM d, yyyy h:mm:ss tt");
        }

        private void RebuildNow()
        {
            if (m_BuildProfile == null)
            {
                Debug.LogWarning("Cannot rebuild: No build profile set");
                return;
            }

            m_RebuildButton.SetEnabled(false);
            m_RebuildButton.text = "Rebuilding...";

            RebuildingBuildProfile = m_BuildProfile;
            RebuildStateChanged?.Invoke(m_BuildProfile);

            EditorApplication.delayCall += () =>
            {
                try
                {
                    BuildPlayerNode.BuildNow(m_BuildProfile);
                    UpdateBuildStats();
                }
                finally
                {
                    RebuildingBuildProfile = null;
                    RebuildStateChanged?.Invoke(null);
                    m_RebuildButton.SetEnabled(true);
                    m_RebuildButton.text = "Rebuild Now";
                }
            };
        }

        private void ClearAllBuilds()
        {
            if (m_BuildProfile == null)
            {
                Debug.LogWarning("Cannot clear builds: No build profile set");
                return;
            }

            var buildPath = ScenarioFactory.GenerateBuildPath(m_BuildProfile);
            var buildDirectory = Path.GetDirectoryName(buildPath);

            if (Directory.Exists(buildDirectory))
            {
                try
                {
                    Directory.Delete(buildDirectory, recursive: true);
                    UpdateBuildStats();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to clear builds: {ex.Message}");
                }
            }
        }

        internal void UpdateButtonStates()
        {
            if (m_RebuildButton == null || m_ClearBuildsButton == null)
                return;

            var isInstanceRunning = m_Instance != null && (m_Instance.IsActive() || m_Instance.HasStartedAsFreeRunning());
            var isInPlayMode = EditorApplication.isPlayingOrWillChangePlaymode;
            var isSharedBuildProfileRunning = IsAnotherInstanceWithSameBuildProfileRunning();
            var isSharedBuildProfileRebuilding = m_BuildProfile != null && RebuildingBuildProfile == m_BuildProfile;
            var shouldDisable = isInstanceRunning || isInPlayMode || isSharedBuildProfileRunning || isSharedBuildProfileRebuilding;

            m_RebuildButton.SetEnabled(!shouldDisable);
            m_ClearBuildsButton.SetEnabled(!shouldDisable);

            if (m_SharedBuildProfileHelpBox != null)
            {
                if (isSharedBuildProfileRebuilding)
                {
                    m_SharedBuildProfileHelpBox.text = k_SharedBuildProfileRebuildingWarning;
                    m_SharedBuildProfileHelpBox.style.display = DisplayStyle.Flex;
                }
                else if (isSharedBuildProfileRunning)
                {
                    m_SharedBuildProfileHelpBox.text = k_SharedBuildProfileRunningWarning;
                    m_SharedBuildProfileHelpBox.style.display = DisplayStyle.Flex;
                }
                else
                {
                    m_SharedBuildProfileHelpBox.style.display = DisplayStyle.None;
                }
            }
        }

        private bool IsAnotherInstanceWithSameBuildProfileRunning()
        {
            if (m_BuildProfile == null)
                return false;

            var runtimeScenario = ScenarioRunner.instance?.ActiveScenario;
            if (runtimeScenario == null)
                return false;

            foreach (var instance in runtimeScenario.GetAllInstances())
            {
                if (instance == m_Instance)
                    continue;

                if (instance.Controller is LocalPlayerController localController)
                {
                    if (localController.Settings.BuildProfile == m_BuildProfile)
                    {
                        if (instance.IsActive() || instance.HasStartedAsFreeRunning())
                            return true;
                    }
                }
            }

            return false;
        }
    }
}
