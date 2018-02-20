// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditorInternal
{
    //*undocumented*
    public enum RegistryView
    {
        Default = 0,
        _32 = 1,
        _64 = 2,
    }

    //*undocumented*
    [NativeType(Header = "Editor/Mono/RegistryUtil.bindings.h")]
    public class RegistryUtil
    {
        public static extern uint GetRegistryUInt32Value(string subKey, string valueName, uint defaultValue, RegistryView view);

        public static extern string GetRegistryStringValue(string subKey, string valueName, string defaultValue, RegistryView view);
    }
}
