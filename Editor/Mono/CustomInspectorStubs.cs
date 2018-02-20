// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor
{
    // Exposed as internal, editor-only, because we only need it do make a custom inspector
    [NativeClass(null)]
    internal sealed class PhysicsManager : ProjectSettingsBase
    {
        private PhysicsManager() {}
    }

    // Exposed as internal, editor-only, because we only need it do make a custom inspector
    [NativeClass(null)]
    internal sealed class AudioManager : ProjectSettingsBase
    {
        private AudioManager() {}
    }

    // Exposed as internal, editor-only, because we only need it do make a custom inspector
    [NativeClass(null)]
    internal sealed class Physics2DSettings : ProjectSettingsBase
    {
        private Physics2DSettings() {}
    }

    // Exposed as internal, editor-only, because we only need it do make a custom inspector
    [NativeClass(null)]
    [ExcludeFromPreset]
    internal sealed class MonoManager : ProjectSettingsBase
    {
        private MonoManager() {}
    }

    // Exposed as internal, editor-only, because we only need it do make a custom inspector
    [NativeClass(null)]
    internal sealed class TagManager : ProjectSettingsBase
    {
        private TagManager() {}
    }

    // Exposed as internal, editor-only, because we only need it do make a custom inspector
    [NativeClass(null)]
    internal sealed class InputManager : ProjectSettingsBase
    {
        private InputManager() {}
    }

    // Exposed as internal, editor-only, because we only need it do make a custom inspector
    [NativeClass(null)]
    internal sealed class TimeManager : ProjectSettingsBase
    {
        private TimeManager() {}
    }

    // Exposed as internal, editor-only, because we only need it do make a custom inspector
    [NativeClass(null)]
    internal sealed class UnityConnectSettings : ProjectSettingsBase
    {
        private UnityConnectSettings() {}
    }
}
