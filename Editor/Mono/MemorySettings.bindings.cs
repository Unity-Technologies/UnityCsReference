// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Object = UnityEngine.Object;
using UnityEngine.Bindings;
using UnityEditor;

namespace UnityEditorInternal
{
    [NativeHeader("Editor/Mono/MemorySettings.bindings.h")]
    [StaticAccessor("MemorySettingsBindings", StaticAccessorType.DoubleColon)]
    internal sealed partial class MemorySettingsUtils
    {
        extern internal static void SetPlatformDefaultValues(int platform);
        extern internal static void WriteEditorMemorySettings();
        extern internal static void InitializeDefaultsForPlatform(int platform);
    }
}
