// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Audio;

namespace UnityEditor.Audio
{
    public class AudioMixerEffectPlugin : IAudioEffectPlugin
    {
        public override bool SetFloatParameter(string name, float value)
        {
            m_Effect.SetValueForParameter(m_Controller, m_Controller.TargetSnapshot, name, value);
            return true;
        }

        public override bool GetFloatParameter(string name, out float value)
        {
            value = m_Effect.GetValueForParameter(m_Controller, m_Controller.TargetSnapshot, name);
            return true;
        }

        public override bool GetFloatParameterInfo(string name, out float minRange, out float maxRange, out float defaultValue)
        {
            foreach (var p in m_ParamDefs)
            {
                if (p.name == name)
                {
                    minRange = p.minRange;
                    maxRange = p.maxRange;
                    defaultValue = p.defaultValue;
                    return true;
                }
            }
            minRange = 0.0f;
            maxRange = 1.0f;
            defaultValue = 0.5f;
            return false;
        }

        public override bool GetFloatBuffer(string name, out float[] data, int numsamples)
        {
            m_Effect.GetFloatBuffer(m_Controller, name, out data, numsamples);
            return true;
        }

        public override int GetSampleRate()
        {
            return AudioSettings.outputSampleRate;
        }

        public override bool IsPluginEditableAndEnabled()
        {
            return AudioMixerController.EditingTargetSnapshot() && !m_Effect.bypass;
        }

        internal AudioMixerController m_Controller;
        internal AudioMixerEffectController m_Effect;
        internal MixerParameterDefinition[] m_ParamDefs;
    }
}
