// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor;

internal interface IAudioPlatformProperties : IPlatformProperties
{
    // The AudioImporterInspector.OnSampleSettingGUI method uses this property to determine whether or not to display UI
    // to set sample rates.  This property is true for all build targets except WebGL.
    bool HasSampleRateSettings => true;
}
