// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.EngineDiagnostics;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Serialization;

namespace UnityEditor.EngineDiagnostics;

[Serializable]
[VisibleToOtherModules]
internal struct BuildProfileInsightsSettingsContainer
{
    [SerializeField] BuildProfileEngineDiagnosticsState m_BuildProfileEngineDiagnosticsState =
        BuildProfileEngineDiagnosticsState.ProjectSettings;

    internal BuildProfileEngineDiagnosticsState buildProfileEngineDiagnosticsState
    {
        get => m_BuildProfileEngineDiagnosticsState;
        set => m_BuildProfileEngineDiagnosticsState = value;
    }

    public BuildProfileInsightsSettingsContainer()
    {
        m_BuildProfileEngineDiagnosticsState = BuildProfileEngineDiagnosticsState.ProjectSettings;
    }
}
