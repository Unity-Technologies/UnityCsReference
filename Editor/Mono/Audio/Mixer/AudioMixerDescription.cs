// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

//FIXME: change this to nested namespaces when we merge in trunk
namespace UnityEditor.Audio
{
    internal class MixerEffectDefinition
    {
        public MixerEffectDefinition(string name, MixerParameterDefinition[] parameters)
        {
            this.m_EffectName = name;
            this.m_Parameters = new MixerParameterDefinition[parameters.Length];
            Array.Copy(parameters, this.m_Parameters, parameters.Length);
        }

        public string name
        {
            get { return this.m_EffectName; }
        }

        public MixerParameterDefinition[] parameters
        {
            get { return this.m_Parameters; }
        }

        private readonly string m_EffectName;
        //TODO: GUI callback
        private readonly MixerParameterDefinition[] m_Parameters;
    }


    [InitializeOnLoad]
    static class MixerEffectDefinitionReloader
    {
        // We use this class with InitializeOnLoad attribute for ensuring MixerEffectDefinitions are refreshed
        // when needed: 1) At startup, 2) after script recompile and 3) when project changes (new effects can have been added)
        static MixerEffectDefinitionReloader()
        {
            MixerEffectDefinitions.Refresh();

            EditorApplication.projectWindowChanged += OnProjectChanged;
        }

        static void OnProjectChanged()
        {
            MixerEffectDefinitions.Refresh();
        }
    }


    internal sealed partial class MixerEffectDefinitions
    {
        public static void Refresh()
        {
            ClearDefinitions();

            RegisterAudioMixerEffect("Attenuation", new MixerParameterDefinition[0]);
            RegisterAudioMixerEffect("Send", new MixerParameterDefinition[0]);
            RegisterAudioMixerEffect("Receive", new MixerParameterDefinition[0]);

            var duckVolDef = new MixerParameterDefinition[7];
            duckVolDef[0] = new MixerParameterDefinition { name = "Threshold", units = "dB", displayScale = 1.0f, displayExponent = 1.0f, minRange = -80.0f, maxRange = 0.0f, defaultValue = -10.0f, description = "Threshold of side-chain level detector" };
            duckVolDef[1] = new MixerParameterDefinition { name = "Ratio", units = "%", displayScale = 100.0f, displayExponent = 1.0f, minRange = 0.2f, maxRange = 10.0f, defaultValue = 2.0f, description = "Ratio of compression applied when side-chain signal exceeds threshold" };
            duckVolDef[2] = new MixerParameterDefinition { name = "Attack Time", units = "ms", displayScale = 1000.0f, displayExponent = 3.0f, minRange = 0.0f, maxRange = 10.0f, defaultValue = 0.1f, description = "Level detector attack time" };
            duckVolDef[3] = new MixerParameterDefinition { name = "Release Time", units = "ms", displayScale = 1000.0f, displayExponent = 3.0f, minRange = 0.0f, maxRange = 10.0f, defaultValue = 0.1f, description = "Level detector release time" };
            duckVolDef[4] = new MixerParameterDefinition { name = "Make-up Gain", units = "dB", displayScale = 1.0f, displayExponent = 1.0f, minRange = -80.0f, maxRange = 40.0f, defaultValue = 0.0f, description = "Make-up gain" };
            duckVolDef[5] = new MixerParameterDefinition { name = "Knee", units = "dB", displayScale = 1.0f, displayExponent = 1.0f, minRange = 0.0f, maxRange = 50.0f, defaultValue = 10.0f, description = "Sharpness of compression curve knee" };
            duckVolDef[6] = new MixerParameterDefinition { name = "Sidechain Mix", units = "%", displayScale = 100.0f, displayExponent = 1.0f, minRange = 0.0f, maxRange = 1.0f, defaultValue = 1.0f, description = "Sidechain/source mix. If set to 100% the compressor detects level entirely from sidechain signal." };
            RegisterAudioMixerEffect("Duck Volume", duckVolDef);
            AddDefinitionRuntime("Duck Volume", duckVolDef);

            string[] effectNames = GetAudioEffectNames();
            foreach (var effectName in effectNames)
            {
                MixerParameterDefinition[] paramDesc = GetAudioEffectParameterDesc(effectName);
                RegisterAudioMixerEffect(effectName, paramDesc);
            }
        }

        public static bool EffectExists(string name)
        {
            foreach (MixerEffectDefinition definition in s_MixerEffectDefinitions)
            {
                if (definition.name == name)
                {
                    return true;
                }
            }

            return false;
        }

        public static string[] GetEffectList()
        {
            string[] effectNames = new string[s_MixerEffectDefinitions.Count];
            for (int i = 0; i < s_MixerEffectDefinitions.Count; i++)
            {
                effectNames[i] = s_MixerEffectDefinitions[i].name;
            }

            return effectNames;
        }

        public static void ClearDefinitions()
        {
            s_MixerEffectDefinitions.Clear();
            ClearDefinitionsRuntime();
        }

        public static MixerParameterDefinition[] GetEffectParameters(string effect)
        {
            foreach (MixerEffectDefinition definition in s_MixerEffectDefinitions)
            {
                if (definition.name == effect)
                {
                    return definition.parameters;
                }
            }

            return new MixerParameterDefinition[0];
        }

        public static bool RegisterAudioMixerEffect(string name, MixerParameterDefinition[] definitions)
        {
            foreach (MixerEffectDefinition definition in s_MixerEffectDefinitions)
            {
                if (definition.name == name)
                {
                    //Cannot add this type, already exists in the system.
                    return false;
                }
            }

            MixerEffectDefinition newDefinition = new MixerEffectDefinition(name, definitions);
            s_MixerEffectDefinitions.Add(newDefinition);

            //Wasteful - Clears the runtime representation each time a new effect is added and rebuilds all runtime
            //representations.
            ClearDefinitionsRuntime();
            foreach (MixerEffectDefinition definition in s_MixerEffectDefinitions)
            {
                AddDefinitionRuntime(definition.name,  definition.parameters);
            }

            return true;
        }

        private static readonly List<MixerEffectDefinition> s_MixerEffectDefinitions = new List<MixerEffectDefinition>();
    }
}
