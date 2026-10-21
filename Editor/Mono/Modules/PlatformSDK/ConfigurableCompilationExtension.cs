// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Modules;

/// <summary>
/// What an SDK platform adds to its base or selected platform's compilation extension.
/// </summary>
internal class ConfigurableCompilationExtension : DefaultCompilationExtension
{
    readonly string[] m_PlatformDefines;

    public override string[] GetAdditionalDefines() => m_PlatformDefines;
    public override string[] GetAdditionalEditorDefines() => m_PlatformDefines;

    public ConfigurableCompilationExtension(params string[] platformDefines)
    {
        m_PlatformDefines = platformDefines;
    }
}
