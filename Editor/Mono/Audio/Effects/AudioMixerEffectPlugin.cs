// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Audio
{
    public class AudioMixerEffectPlugin : IAudioEffectPlugin
    {
        internal AudioMixerController m_Controller;
        internal AudioMixerEffectController m_Effect;
        internal MixerParameterDefinition[] m_ParamDefs;

        static bool s_ParameterChangeUndoIsRecorded;
        static bool s_ParameterChangeUndoGroupNameIsSet;

        static readonly Dictionary<string, float> k_UpdatedParameterMap = new Dictionary<string, float>();

        readonly HashSet<string> m_VerifiedParameters = new HashSet<string>();

        public override bool SetFloatParameter(string name, float value)
        {
            if (!HasParameter(name))
            {
                return false;
            }

            GetFloatParameter(name, out var previousValue);

            if (Mathf.Approximately(value, previousValue))
            {
                return true;
            }

            if (k_UpdatedParameterMap.Count == 0 && !s_ParameterChangeUndoIsRecorded)
            {
                Undo.RecordObject(m_Controller.TargetSnapshot, $"Change {name}");
            }

            k_UpdatedParameterMap[name] = value;

            if (k_UpdatedParameterMap.Count > 1 && !s_ParameterChangeUndoGroupNameIsSet)
            {
                Undo.SetCurrentGroupName("Change Effect Parameters");
                s_ParameterChangeUndoGroupNameIsSet = true;
            }

            m_Effect.SetValueForParameter(m_Controller, m_Controller.TargetSnapshot, name, value);

            return true;
        }

        public override bool GetFloatParameter(string name, out float value)
        {
            if (!HasParameter(name))
            {
                value = default;

                return false;
            }

            if (k_UpdatedParameterMap.TryGetValue(name, out value))
            {
                return true;
            }

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

        // Call this after each interaction that changes effect parameters.
        // NOTE: It is assumed that parameter changes can only happen on a single effect at a time.
        internal static void OnParameterChangesDone()
        {
            k_UpdatedParameterMap.Clear();
            s_ParameterChangeUndoIsRecorded = false;
            s_ParameterChangeUndoGroupNameIsSet = false;
        }

        bool HasParameter(string name)
        {
            if (m_VerifiedParameters.Contains(name))
            {
                return true;
            }

            for (var i = 0; i < m_ParamDefs.Length; ++i)
            {
                if (m_ParamDefs[i].name.Equals(name))
                {
                    m_VerifiedParameters.Add(name);
                    return true;
                }
            }

            return false;
        }
    }
}
