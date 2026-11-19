// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Profile;

public class BuildAutomationSettings : ScriptableObject
{
    [SerializeField]
    string m_OperatingSystemValue;
    public string operatingSystemValue
    {
        get => m_OperatingSystemValue;
        set => m_OperatingSystemValue = value;
    }

    [SerializeField]
    string m_OperatingSystemFamily;
    public string operatingSystemFamily
    {
        get => m_OperatingSystemFamily;
        set => m_OperatingSystemFamily = value;
    }

    [SerializeField]
    string m_XcodeVersion;
    public string xcodeVersion
    {
        get => m_XcodeVersion;
        set => m_XcodeVersion = value;
    }

    [SerializeField]
    string m_AndroidSdkVersion;
    public string androidSdkVersion
    {
        get => m_AndroidSdkVersion;
        set => m_AndroidSdkVersion = value;
    }

    [SerializeField]
    string m_UnityArchitecture;
    public string unityArchitecture
    {
        get => m_UnityArchitecture;
        set => m_UnityArchitecture = value;
    }

    [SerializeField]
    string m_MachineTypeId;
    public string machineTypeId
    {
        get => m_MachineTypeId;
        set => m_MachineTypeId = value;
    }

    [SerializeField]
    string m_CredentialsId;
    public string credentialsId
    {
        get => m_CredentialsId;
        set => m_CredentialsId = value;
    }

    [SerializeField]
    BuildTarget m_BuildTarget;
    public BuildTarget buildTarget
    {
        get => m_BuildTarget;
        [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
        internal set => m_BuildTarget = value;
    }
}
