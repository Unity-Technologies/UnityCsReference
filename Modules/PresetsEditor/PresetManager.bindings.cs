// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine.Bindings;

namespace UnityEditor.Presets
{
    [NativeType(Header = "Modules/PresetsEditor/PresetManager.h")]
    internal class PresetManager : ProjectSettingsBase
    {
        internal extern string GetPresetTypeNameAtIndex(int index);
        internal extern bool SetAsDefaultInternal(Preset index);
    }
}
