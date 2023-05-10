// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    [Serializable]
    struct RenderPipelineGraphicsSettingsContainer : ISerializationCallbackReceiver
    {
        [SerializeReference] List<IRenderPipelineGraphicsSettings> m_SettingsList;

        private Dictionary<Type, int> m_RandomQuickAccess;

        public RenderPipelineGraphicsSettingsContainer()
        {
            m_SettingsList = new();
            m_RandomQuickAccess = new();
        }

        internal bool Contains(Type type)
        {
            return m_RandomQuickAccess.ContainsKey(type);
        }

        internal bool Add(IRenderPipelineGraphicsSettings element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            var type = element.GetType();
            if (Contains(type))
            {
                Debug.LogWarning($"Element of type {type} is already added");
                return false;
            }

            m_RandomQuickAccess.Add(type, m_SettingsList.Count);
            m_SettingsList.Add(element);
            return true;
        }

        internal bool Remove(IRenderPipelineGraphicsSettings element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            var type = element.GetType();
            if (!Contains(type))
                return false;

            m_SettingsList.RemoveAt(m_RandomQuickAccess[type]);
            RecreateQuickAccess();
            return true;
        }

        internal bool TryGet(Type type, out IRenderPipelineGraphicsSettings element)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            element = null;
            if (!m_RandomQuickAccess.TryGetValue(type, out var index))
                return false;

            element = m_SettingsList[index];
            return element != null;
        }

        void RecreateQuickAccess()
        {
            if (m_SettingsList == null)
                throw new Exception("Settings list not initialized");

            if (m_RandomQuickAccess == null)
                m_RandomQuickAccess = new();
            else
                m_RandomQuickAccess.Clear();

            var length = m_SettingsList.Count;
            for (int i = 0; i < length; ++i)
            {
                var element = m_SettingsList[i];

                if (element == null)
                    continue; //missing script can cause this, preserve data, just not access it

                m_RandomQuickAccess.Add(element.GetType(), i);
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
            => RecreateQuickAccess();

        void ISerializationCallbackReceiver.OnBeforeSerialize() {}
    }
}
