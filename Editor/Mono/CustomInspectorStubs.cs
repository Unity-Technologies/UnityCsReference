// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;
using UnityEngine.Bindings;

namespace UnityEditor
{
    // Exposed as internal, editor-only, because we only need it do make a custom inspector
    [NativeClass(null, PersistentTypeId = 11)]
    [ExcludeFromPreset]
    internal sealed class AudioManager : ProjectSettingsBase
    {
        internal AudioManager(global::UnityEngine.EntityId id) : base(id) {}
        private AudioManager() {}
    }

    // Exposed as internal, editor-only, because we only need it to make a custom inspector
    [NativeClass(null, PersistentTypeId = 19)]
    internal sealed class Physics2DSettings : ProjectSettingsBase
    {
        internal Physics2DSettings(global::UnityEngine.EntityId id) : base(id) {}
        private Physics2DSettings() {}
    }

    // Exposed as internal, editor-only, because we only need it to make a custom inspector
    [NativeClass(null, PersistentTypeId = 0x0A86CE7B)]
    internal sealed class PhysicsCoreProjectSettings2D : ProjectSettingsBase
    {
        internal PhysicsCoreProjectSettings2D(global::UnityEngine.EntityId id) : base(id) {}
        private PhysicsCoreProjectSettings2D() { }
    }

    // Exposed as internal, editor-only, because we only need it do make a custom inspector
    [NativeClass(null, PersistentTypeId = 116)]
    [ExcludeFromPreset]
    internal sealed class MonoManager : ProjectSettingsBase
    {
        internal MonoManager(global::UnityEngine.EntityId id) : base(id) {}
        private MonoManager() {}
    }

    // Exposed as internal, editor-only, because we only need it do make a custom inspector
    [NativeClass(null, PersistentTypeId = 0x37DF050A)]
    [ExcludeFromPreset]
    internal sealed class VFXManager : ProjectSettingsBase
    {
        internal VFXManager(global::UnityEngine.EntityId id) : base(id) {}
        private VFXManager()
        {
        }

        [SettingsProvider]
        internal static SettingsProvider CreateProjectSettingsProvider()
        {
            if (!UnityEngine.VFX.VFXManager.activateVFX)
                return null;
            var provider = AssetSettingsProvider.CreateProviderFromAssetPath(
                "Project/VFX", "ProjectSettings/VFXManager.asset",
                SettingsProvider.GetSearchKeywordsFromPath("ProjectSettings/VFXManager.asset"));
            return provider;
        }
    }

    // Exposed as internal, editor-only, because we only need it do make a custom inspector
    [NativeClass(null, PersistentTypeId = 13)]
    internal sealed class InputManager : ProjectSettingsBase
    {
        internal InputManager(global::UnityEngine.EntityId id) : base(id) {}
        private InputManager() {}

        [SettingsProvider]
        internal static SettingsProvider CreateProjectSettingsProvider()
        {
            // The new input system adds objects to InputManager.asset. This means we can't use AssetSettingsProvider.CreateProviderFromAssetPath
            // as it will load *all* objects at that path and try to create an editor for it.
            // NOTE: When the input system package is uninstalled, InputManager.asset will contain serialized MonoBehaviour objects for which
            //       the C# classes are no longer available. They will thus not load correctly and appear as null entries.
            var obj = AssetDatabase.LoadAssetAtPath<InputManager>("ProjectSettings/InputManager.asset");
            if (obj != null && obj.name == "InputManager")
            {
                var provider = AssetSettingsProvider.CreateProviderFromObject("Project/Input Manager", obj,
                    SettingsProvider.GetSearchKeywordsFromPath("ProjectSettings/InputManager.asset"));
                return provider;
            }
            return null;
        }
    }

    [CustomEditor(typeof(InputManager))]
    internal sealed class InputManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (PlayerSettings.GetDisableOldInputManagerSupport())
                EditorGUILayout.HelpBox("This is where you can configure the controls to use with the UnityEngine.Input API. But you have switched input handling to \"Input System Package\" in your Player Settings. The Input Manager will not be used.", MessageType.Error);
            else
                EditorGUILayout.HelpBox("This is where you can configure the controls to use with the UnityEngine.Input API. Consider using the new Input System Package instead.", MessageType.Info);
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Physical Keys enables keyboard language layout independent mapping of key codes to physical keys. For example, 'q' will be the key to the right of the tab key no matter which (if any) key on the keyboard currently generates a 'q' character.", MessageType.Info);
        }
    }

    // Exposed as internal, editor-only, because we only need it do make a custom inspector
    [NativeClass(null, PersistentTypeId = 5)]
    internal sealed class TimeManager : ProjectSettingsBase
    {
        internal TimeManager(global::UnityEngine.EntityId id) : base(id) {}
        private TimeManager() {}
    }

    // Exposed as internal, editor-only, because we only need it do make a custom inspector
    [NativeClass(null, PersistentTypeId = 0x1715D37E)]
    internal sealed class MemorySettings : ProjectSettingsBase
    {
        internal MemorySettings(global::UnityEngine.EntityId id) : base(id) {}
        private MemorySettings() {}
    }


    // Exposed as internal, editor-only, because we only need it do make a custom inspector
    [NativeClass(null, PersistentTypeId = 310)]
    internal sealed class UnityConnectSettings : ProjectSettingsBase
    {
        internal UnityConnectSettings(global::UnityEngine.EntityId id) : base(id) {}
        private UnityConnectSettings() {}
    }

    [NativeConditional("ENABLE_CLUSTERINPUT")]
    internal sealed class ClusterInputSettings
    {
        private ClusterInputSettings() {}
    }
}
