// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Bindings;

namespace UnityEditor.Audio
{
    ///*undocumented*
    [NativeHeader("Editor/Src/Audio/Mixer/AudioMixerGroupController.h")]
    [NativeHeader("Modules/AudioEditor/ScriptBindings/AudioMixerGroupController.bindings.h")]
    internal partial class AudioMixerGroupController : AudioMixerGroup
    {
        public AudioMixerGroupController(AudioMixer owner)
        {
            Internal_CreateAudioMixerGroupController(this, owner);
        }

        [FreeFunction("AudioMixerGroupControllerBindings::Internal_CreateAudioMixerGroupController")]
        private extern static void Internal_CreateAudioMixerGroupController([Writable] AudioMixerGroupController mono, AudioMixer owner);

        public extern GUID groupID { get; }

        public extern int userColorIndex { get; set; }

        public extern AudioMixerController controller { get; }

        public extern void PreallocateGUIDs();

        public extern GUID GetGUIDForVolume();

        public extern float GetValueForVolume(AudioMixerController controller, AudioMixerSnapshotController snapshot);

        public extern void SetValueForVolume(AudioMixerController controller, AudioMixerSnapshotController snapshot, float value);

        public extern GUID GetGUIDForPitch();

        public extern float GetValueForPitch(AudioMixerController controller, AudioMixerSnapshotController snapshot);

        public extern void SetValueForPitch(AudioMixerController controller, AudioMixerSnapshotController snapshot, float value);

        public extern GUID GetGUIDForSend();

        public extern float GetValueForSend(AudioMixerController controller, AudioMixerSnapshotController snapshot);

        public extern void SetValueForSend(AudioMixerController controller, AudioMixerSnapshotController snapshot, float value);

        public extern bool HasDependentMixers();

        public AudioMixerGroupController[] children
        {
            get
            {
                return System.Array.ConvertAll(children_Internal, amg => (AudioMixerGroupController)amg);
            }
            set
            {
                children_Internal = System.Array.ConvertAll(value, amgc => (AudioMixerGroup)amgc);
            }
        }

        [NativeName("Children")]
        private extern AudioMixerGroup[] children_Internal { get; set; }

        public extern AudioMixerEffectController[] effects { get; set; }

        public extern bool mute { get; set; }
        public extern bool solo { get; set; }
        public extern bool bypassEffects { get; set; }
    }
}
