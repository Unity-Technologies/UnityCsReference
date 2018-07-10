// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.Bindings;

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
    [ExcludeFromPreset]
    internal sealed class VFXManager : ProjectSettingsBase
    {
        private VFXManager()
        {
        }

        [SettingsProvider]
        internal static SettingsProvider CreateProjectSettingsProvider()
        {
            var provider = new AssetSettingsProvider("Project/VFX", "ProjectSettings/VFXManager.asset");
            SettingsProvider.GetSearchKeywordsFromSerializedObject(provider.CreateEditor().serializedObject, provider.keywords);
            return provider;
        }
    }

    // Exposed as internal, editor-only, because we only need it do make a custom inspector
    [NativeClass(null)]
    internal sealed class InputManager : ProjectSettingsBase
    {
        private InputManager() {}

        [SettingsProvider]
        internal static SettingsProvider CreateProjectSettingsProvider()
        {
            var provider = new AssetSettingsProvider("Project/Input", "ProjectSettings/InputManager.asset")
            {
                icon = EditorGUIUtility.FindTexture("UnityEngine/EventSystems/StandaloneInputModule Icon")
            };

            SettingsProvider.GetSearchKeywordsFromSerializedObject(provider.CreateEditor().serializedObject, provider.keywords);

            return provider;
        }
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

    [NativeConditional("ENABLE_CLUSTERINPUT")]
    internal sealed class ClusterInputSettings
    {
        private ClusterInputSettings() {}

        [SettingsProvider]
        internal static SettingsProvider CreateProjectSettingsProvider()
        {
            var provider = new AssetSettingsProvider("Project/Cluster Input", "ProjectSettings/ClusterInputManager.asset");
            SettingsProvider.GetSearchKeywordsFromSerializedObject(provider.CreateEditor().serializedObject, provider.keywords);
            return provider;
        }
    }
}
