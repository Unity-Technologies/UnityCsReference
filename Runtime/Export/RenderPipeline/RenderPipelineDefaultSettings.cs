// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    public abstract class RenderPipelineGlobalSettings : ScriptableObject
    {
        [SerializeField] SRPGraphicsSettingsContainer m_Settings = new();

        public virtual void Initialize(RenderPipelineGlobalSettings source = null)
        {
        }

        protected internal bool Add(ISRPGraphicsSetting setting)
        {
            if (!m_Settings.Add(setting))
                return false;

            MarkDirty();
            return true;

        }

        protected internal bool Remove(ISRPGraphicsSetting setting)
        {
            if (!m_Settings.Remove(setting))
                return false;

            MarkDirty();
            return true;
        }

        protected internal bool TryGet(Type type, out ISRPGraphicsSetting setting) => m_Settings.TryGet(type, out setting);
        protected internal bool Contains(Type type) => m_Settings.Contains(type);
    }
}
