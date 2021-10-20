// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Object = UnityEngine.Object;
using System.Collections.Generic;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using UnityEngine;

namespace UnityEditor.Audio
{
    [RequiredByNativeCode]
    internal struct MixerEffectParameter
    {
        public string parameterName;
        public GUID   GUID;
    }

    [NativeHeader("Editor/Src/Audio/Mixer/AudioMixerEffectController.h")]
    [NativeHeader("Modules/AudioEditor/ScriptBindings/AudioMixerEffectController.bindings.h")]
    internal class AudioMixerEffectController : Object
    {
        int m_LastCachedGroupDisplayNameID;
        string m_DisplayName;

        public AudioMixerEffectController(string name)
        {
            Internal_CreateAudioMixerEffectController(this, name);
        }

        [FreeFunction("AudioMixerEffectControllerBindings::Internal_CreateAudioMixerEffectController")]
        private extern static void Internal_CreateAudioMixerEffectController([Writable] AudioMixerEffectController mono, string name);

        public extern GUID effectID { get; }

        public extern string effectName { get; }

        public bool IsSend() { return effectName == "Send"; }
        public bool IsReceive() { return effectName == "Receive"; }
        public bool IsDuckVolume() { return effectName == "Duck Volume"; }
        public bool IsAttenuation() { return effectName == "Attenuation"; }
        public bool DisallowsBypass() { return IsSend() || IsReceive() || IsDuckVolume() || IsAttenuation(); }

        public void ClearCachedDisplayName() {m_DisplayName = null; }

        public string GetDisplayString(Dictionary<AudioMixerEffectController, AudioMixerGroupController> effectMap)
        {
            AudioMixerGroupController group = effectMap[this];
            if (group.GetInstanceID() != m_LastCachedGroupDisplayNameID || m_DisplayName == null)
            {
                // Cache display name to prevent string allocs every event
                m_DisplayName = group.GetDisplayString() + AudioMixerController.s_GroupEffectDisplaySeperator + effectName;
                m_LastCachedGroupDisplayNameID = group.GetInstanceID();
            }
            return m_DisplayName;
        }

        public string GetSendTargetDisplayString(Dictionary<AudioMixerEffectController, AudioMixerGroupController> effectMap) { return (sendTarget != null) ? sendTarget.GetDisplayString(effectMap) : string.Empty; }

        public extern AudioMixerEffectController sendTarget { get; set; }

        public extern bool enableWetMix { get; set; }
        public extern bool bypass { get; set; }

        public extern void PreallocateGUIDs();

        public extern GUID GetGUIDForMixLevel();

        public extern float GetValueForMixLevel(AudioMixerController controller, AudioMixerSnapshotController snapshot);

        public extern void SetValueForMixLevel(AudioMixerController controller, AudioMixerSnapshotController snapshot, float value);

        public extern GUID GetGUIDForParameter(string parameterName);

        public extern float GetValueForParameter(AudioMixerController controller, AudioMixerSnapshotController snapshot, string parameterName);

        public extern void SetValueForParameter(AudioMixerController controller, AudioMixerSnapshotController snapshot, string parameterName, float value);

        public bool GetFloatBuffer(AudioMixerController controller, string name, out float[] data, int numsamples)
        {
            data = new float[numsamples];
            unsafe
            {
                fixed(float* dataPtr = &data[0])
                return GetFloatBuffer_Internal(controller, name, dataPtr, numsamples);
            }
        }

        [NativeName("GetFloatBuffer")]
        private unsafe extern bool GetFloatBuffer_Internal(AudioMixerController controller, string name, float* buffer, int bufferLength);

        public extern float GetCPUUsage(AudioMixerController controller);

        public extern bool ContainsParameterGUID(GUID guid);
    }
}
