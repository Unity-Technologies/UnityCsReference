// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEditorInternal;
using UnityEngine;
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
    [Serializable]
    public struct DefaultPreset
    {
        [SerializeField]
        [Obsolete("Use the new getter/setter instead. (UnityUpgradable) -> filter")]
        public string m_Filter;
        [SerializeField]
        [Obsolete("Use the new getter/setter instead. (UnityUpgradable) -> preset")]
        public Preset m_Preset;
        [SerializeField]
        private bool m_Disabled;

        public string filter
        {
#pragma warning disable 618
            get { return m_Filter; }
            set { m_Filter = value; }
#pragma warning restore 618
        }

        public Preset preset
        {
#pragma warning disable 618
            get { return m_Preset; }
            set { m_Preset = value; }
#pragma warning restore 618
        }

        public bool enabled
        {
            get { return !m_Disabled; }
            set { m_Disabled = !value; }
        }

        public DefaultPreset(string filter, Preset preset) :
            this(filter, preset, true) {}

        public DefaultPreset(string filter, Preset preset, bool enabled)
        {
#pragma warning disable 618
            m_Filter = filter;
            m_Preset = preset;
#pragma warning restore 618
            m_Disabled = !enabled;
        }
    }
}
