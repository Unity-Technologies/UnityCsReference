// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeType(Header = "Modules/PresetsEditor/PresetManager.h")]
    internal class PresetManager : ProjectSettingsBase
    {
        internal static extern string GetPresetTypeNameAtIndex(int index);
    }
}
