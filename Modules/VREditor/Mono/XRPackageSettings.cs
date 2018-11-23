// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace UnityEditorInternal.VR
{
    internal class XRProjectSettings : ScriptableObject
    {
        internal static class KnownSettings
        {
            internal static readonly string k_VRDeviceDidAlertUser = "VR Device User Alert";
            internal static readonly string k_VRDeviceDisabled = "VR Device Disabled";
            internal static readonly string k_VRDeviceTransitionGroups = "VR Device Transitioned Groups";
        }

        static readonly string k_ProjectSettingsPath = "ProjectSettings";
        static readonly string k_ProjectSettingsFileName = "XRSettings.asset";

        private static XRProjectSettings s_PackageSettings = null;
        private static object s_Lock = new object();

        [SerializeField]
        private List<string> m_SettingKeys = new List<string>();
        [SerializeField]
        private List<string> m_SettingValues = new List<string>();
        private object m_SettingsLock = new object();
        private bool m_IsDirty = false;
        private string m_StorageFilePath = "";

        private XRProjectSettings() {}

        private static XRProjectSettings Instance
        {
            get
            {
                if (s_PackageSettings == null)
                {
                    lock (s_Lock)
                    {
                        if (s_PackageSettings == null)
                        {
                            s_PackageSettings = ScriptableObject.CreateInstance<XRProjectSettings>();
                        }
                    }
                }
                return s_PackageSettings;
            }
        }

        void OnEnable()
        {
            Internal_LoadSettings();
        }

        void OnDisable()
        {
            Internal_SaveSettings();
        }

        void OnDestroy()
        {
            Internal_SaveSettings();
        }

        private string GetStorageFilePath()
        {
            if (String.IsNullOrEmpty(m_StorageFilePath))
                m_StorageFilePath = Path.Combine(k_ProjectSettingsPath, k_ProjectSettingsFileName);

            return m_StorageFilePath;
        }

        private void Internal_LoadSettings()
        {
            if (m_IsDirty)
                Internal_SaveSettings();

            string packageInitPath = GetStorageFilePath();

            if (File.Exists(packageInitPath))
            {
                using (StreamReader sr = new StreamReader(packageInitPath))
                {
                    string settings = sr.ReadToEnd();
                    JsonUtility.FromJsonOverwrite(settings, this);
                }
            }
            m_IsDirty = false;
        }

        private void Internal_SaveSettings()
        {
            if (!m_IsDirty)
                return;

            string packageInitPath = GetStorageFilePath();
            using (StreamWriter sw = new StreamWriter(packageInitPath))
            {
                string settings = JsonUtility.ToJson(this, true);
                sw.Write(settings);
            }
            m_IsDirty = false;
        }

        public bool Internal_HasSetting(string key)
        {
            return m_SettingKeys.Contains(key);
        }

        private void Internal_AddSetting(string key, string value)
        {
            if (!Internal_HasSetting(key))
            {
                lock (m_SettingsLock)
                {
                    m_SettingKeys.Add(key);
                    m_SettingValues.Add(value);
                    m_IsDirty = true;
                }
            }
        }

        private void Internal_SetSetting(string key, string value)
        {
            if (!Internal_HasSetting(key))
            {
                Internal_AddSetting(key, value);
            }
            else
            {
                lock (m_SettingsLock)
                {
                    int index = m_SettingKeys.IndexOf(key);
                    m_SettingKeys[index] = key;
                    m_SettingValues[index] = value;
                    m_IsDirty = true;
                }
            }
        }

        private void Internal_RemoveSetting(string key)
        {
            if (Internal_HasSetting(key))
            {
                lock (m_SettingsLock)
                {
                    int index = m_SettingKeys.IndexOf(key);
                    m_SettingKeys.RemoveAt(index);
                    m_SettingValues.RemoveAt(index);
                    m_IsDirty = true;
                }
            }
        }

        private string Internal_GetSetting(string key)
        {
            string ret = String.Empty;
            if (Internal_HasSetting(key))
            {
                lock (m_SettingsLock)
                {
                    int index = m_SettingKeys.IndexOf(key);
                    ret = m_SettingValues[index];
                }
            }
            return ret;
        }

        private bool Internal_GetBool(string key, bool defval = false)
        {
            bool ret = defval;
            string val = Internal_GetSetting(key);
            if (!String.IsNullOrEmpty(val))
            {
                if (!Boolean.TryParse(val, out ret))
                    ret = defval;
            }
            return ret;
        }

        private void Internal_SetBool(string key, bool value)
        {
            string val = value.ToString();
            Internal_SetSetting(key, val);
        }

        private string Internal_GetString(string key, string defval = "")
        {
            string ret = defval;
            string val = Internal_GetSetting(key);
            if (!String.IsNullOrEmpty(val))
                ret = val;
            return ret;
        }

        private void Internal_SetString(string key, string value)
        {
            Internal_SetSetting(key, value);
        }

        internal static void SaveSettings()
        {
            Instance.Internal_SaveSettings();
        }

        internal static bool HasSetting(string key)
        {
            return Instance.m_SettingKeys.Contains(key);
        }

        internal static void AddSetting(string key, string value)
        {
            Instance.Internal_AddSetting(key, value);
        }

        internal static void SetSetting(string key, string value)
        {
            Instance.Internal_SetSetting(key, value);
        }

        internal static void RemoveSetting(string key)
        {
            Instance.Internal_RemoveSetting(key);
        }

        internal static string GetSetting(string key)
        {
            return Instance.Internal_GetSetting(key);
        }

        internal static bool GetBool(string key, bool defval = false)
        {
            return Instance.Internal_GetBool(key, defval);
        }

        internal static void SetBool(string key, bool value)
        {
            Instance.Internal_SetBool(key, value);
        }

        internal static string GetString(string key, string defval = "")
        {
            return Instance.Internal_GetString(key, defval);
        }

        internal static void SetString(string key, string value)
        {
            Instance.Internal_SetString(key, value);
        }
    }
}
