// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEditorInternal;
using UnityEngine.Bindings;

namespace UnityEditor.Presets
{
    [NativeType(Header = "Modules/PresetsEditor/Public/PresetManager.h")]
    internal class PresetManager : ProjectSettingsBase
    {
        internal extern void AddPresetType(PresetType presetType);
    }

    [NativeType(Header = "Modules/PresetsEditor/Public/PresetManager.h")]
    [StructLayout(LayoutKind.Sequential)]
    public struct DefaultPreset
    {
        public string m_Filter;
        public Preset m_Preset;

        public DefaultPreset(string filter, Preset preset)
        {
            m_Filter = filter;
            m_Preset = preset;
        }
    }
}
