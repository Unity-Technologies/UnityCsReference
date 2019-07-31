// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor.Audio
{
    internal struct MixerParameterDefinition
    {
        public string name;
        public string description;
        public string units;
        public float  displayScale;
        public float  displayExponent;
        public float  minRange;
        public float  maxRange;
        public float  defaultValue;
    }

    [StaticAccessor("AudioMixerDescriptionBindings", StaticAccessorType.DoubleColon)]
    [NativeHeader("Modules/AudioEditor/ScriptBindings/AudioMixerDescription.bindings.h")]
    internal partial class MixerEffectDefinitions
    {
        private extern static void ClearDefinitionsRuntime();

        extern private static void AddDefinitionRuntime(string name, MixerParameterDefinition[] parameters);

        extern public static string[] GetAudioEffectNames();

        extern public static MixerParameterDefinition[] GetAudioEffectParameterDesc(string effectName);

        //Change to use effect
        public extern static bool EffectCanBeSidechainTarget(AudioMixerEffectController effect);
    }
}
