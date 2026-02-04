// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.PlayMode.Editor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Multiplayer.PlayMode.Editor;

partial class OrchestratedScenario : PlayModeScenario, ISerializationCallbackReceiver
{
    // Version 0
    [SerializeField] private bool m_EnableEditors = true;
    [SerializeField] private MainEditorInstanceDescription m_MainEditorInstance = null;
    [SerializeField] private List<VirtualEditorInstanceDescription> m_EditorInstances = null;
    [SerializeField] private List<LocalInstanceDescription> m_LocalInstances = null;

    // The following section is for upgrading from 1.0.0-pre.2 to 1.0.0-pre.3.
    // Because m_MainEditorInstance was serialized as reference we need to manually copy the old values to the new instance.
    [SerializeReference, FormerlySerializedAs("m_MainEditorInstance")] private MainEditorInstanceDescription m_MainEditorInstanceObsolete;

    public void UpgradeSerialization()
    {
        if (m_MainEditorInstanceObsolete != null)
        {
            var serialized = JsonUtility.ToJson(m_MainEditorInstanceObsolete);
            JsonUtility.FromJsonOverwrite(serialized, m_MainEditorInstance);
            m_MainEditorInstanceObsolete = null;
        }

        UpgradeMainEditor();
        UpgradeCloneEditors();
        UpgradeLocalInstances();
    }

    void UpgradeMainEditor()
    {
        if (m_MainEditorInstance == null)
            return;

        if (m_Settings.InstanceCount == 0)
        {
            m_Settings.AddInstance<MainEditorController, MainEditorController.InstanceSettings>(
                k_MainEditorName,
                new MainEditorController.InstanceSettings
                {
                    PlayerTag = m_MainEditorInstance.PlayerTag,
                    RoleMask = m_MainEditorInstance.m_Role,
                    InitialScene = m_MainEditorInstance.InitialScene
                });
        }

        m_MainEditorInstance = null;
    }

    void UpgradeCloneEditors()
    {
        if (m_EditorInstances == null)
            return;

        foreach (var editorInstance in m_EditorInstances)
        {
            m_Settings.AddInstance<CloneEditorController, CloneEditorController.InstanceSettings>(
                editorInstance.Name,
                new CloneEditorController.InstanceSettings
                {
                    PlayerTag = editorInstance.PlayerTag,
                    RoleMask = editorInstance.m_Role,
                    PlayerInstanceIndex = editorInstance.PlayerInstanceIndex,
                    StreamLogsToMainEditor = editorInstance.AdvancedConfiguration.StreamLogsToMainEditor,
                    LogsColor = editorInstance.AdvancedConfiguration.LogsColor
                });
        }

        m_EditorInstances = null;
    }

    void UpgradeLocalInstances()
    {
        if (m_LocalInstances == null)
            return;

        foreach (var localInstance in m_LocalInstances)
        {
            m_Settings.AddInstance<LocalPlayerController, LocalPlayerController.InstanceSettings>(
                localInstance.Name,
                new LocalPlayerController.InstanceSettings
                {
                    BuildProfile = localInstance.BuildProfile,
                    StreamLogsToMainEditor = localInstance.AdvancedConfiguration.StreamLogsToMainEditor,
                    LogsColor = localInstance.AdvancedConfiguration.LogsColor,
                    Arguments = localInstance.AdvancedConfiguration.Arguments,
                    DeviceID = localInstance.AdvancedConfiguration.DeviceID,
                    DeviceName = localInstance.AdvancedConfiguration.DeviceName
                });
        }

        m_LocalInstances = null;
    }
}
