// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    [Serializable]
    struct SRPGraphicsSettingsContainer : ISerializationCallbackReceiver
    {
        [SerializeReference] List<ISRPGraphicsSetting> m_SRPGraphicsSettingsList;

        private Dictionary<Type, ISRPGraphicsSetting> m_SRPGraphicsSettings;

        public SRPGraphicsSettingsContainer()
        {
            m_SRPGraphicsSettingsList = new();
            m_SRPGraphicsSettings = new();
        }

        internal bool Contains(Type type)
        {
            return m_SRPGraphicsSettings.ContainsKey(type);
        }

        internal bool Add(ISRPGraphicsSetting element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            var type = element.GetType();
            if (Contains(type))
            {
                Debug.LogWarning($"Element of type {type} is already added");
                return false;
            }

            m_SRPGraphicsSettings.Add(type, element);

            return true;
        }

        internal bool Remove(ISRPGraphicsSetting element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            var type = element.GetType();
            return Contains(type) && m_SRPGraphicsSettings.Remove(type);
        }

        internal bool TryGet(Type type, out ISRPGraphicsSetting element)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (!m_SRPGraphicsSettings.TryGetValue(type, out element))
                return false;
            return element != null;
        }

        public void OnAfterDeserialize()
        {
            m_SRPGraphicsSettings.Clear();

            foreach (var setting in m_SRPGraphicsSettingsList)
                m_SRPGraphicsSettings.Add(setting.GetType(), setting);
        }

        public void OnBeforeSerialize()
        {
            m_SRPGraphicsSettingsList.Clear();

            foreach (var kvp in m_SRPGraphicsSettings)
            {
                m_SRPGraphicsSettingsList.Add(kvp.Value);
            }
        }
    }
}
