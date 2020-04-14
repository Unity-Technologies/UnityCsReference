// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    internal class LightingWindowBakeSettings
    {
        bool m_LightingSettingsReadOnlyMode;
        SerializedObject m_LightingSettings;
        SharedLightingSettingsEditor m_LightingSettingsEditor;

        SerializedObject lightingSettings
        {
            get
            {
                // if we set a new scene as the active scene, we need to make sure to respond to those changes
                if (m_LightingSettings == null || m_LightingSettings.targetObject == null || m_LightingSettings.targetObject != Lightmapping.lightingSettingsInternal)
                {
                    var targetObject = Lightmapping.lightingSettingsInternal;
                    m_LightingSettingsReadOnlyMode = false;

                    if (targetObject == null)
                    {
                        targetObject = Lightmapping.lightingSettingsDefaults;
                        m_LightingSettingsReadOnlyMode = true;
                    }

                    SerializedObject lso = m_LightingSettings = new SerializedObject(targetObject);

                    if (lso != null)
                    {
                        m_LightingSettingsEditor.UpdateSettings(lso);
                    }
                }

                return m_LightingSettings;
            }
        }

        public void OnEnable()
        {
            if (m_LightingSettingsEditor == null)
                m_LightingSettingsEditor = new SharedLightingSettingsEditor();

            m_LightingSettingsEditor.OnEnable();
        }

        public void OnDisable()
        {
            if (m_LightingSettings != null)
                m_LightingSettings.Dispose();
        }

        public void OnGUI()
        {
            lightingSettings.Update();

            using (new EditorGUI.DisabledScope(m_LightingSettingsReadOnlyMode))
            {
                m_LightingSettingsEditor.OnGUI(false, false);
            }

            lightingSettings.ApplyModifiedProperties();
        }
    }
}
