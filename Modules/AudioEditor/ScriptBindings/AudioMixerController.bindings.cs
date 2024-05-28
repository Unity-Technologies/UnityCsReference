// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Audio;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine;

namespace UnityEditor.Audio
{
    [RequiredByNativeCode]
    internal struct ExposedAudioParameter
    {
        public GUID guid;
        public string name;
    }

    [RequiredByNativeCode]
    internal struct MixerGroupView
    {
        public GUID[] guids;
        public string name;
    }

    ///*undocumented*
    [NativeHeader("Editor/Src/Audio/Mixer/AudioMixerController.h")]
    [NativeHeader("Modules/AudioEditor/ScriptBindings/AudioMixerController.bindings.h")]
    internal partial class AudioMixerController : AudioMixer
    {
        public AudioMixerController()
        {
            Internal_CreateAudioMixerController(this);
        }

        [FreeFunction("AudioMixerControllerBindings::Internal_CreateAudioMixerController")]
        private static extern void Internal_CreateAudioMixerController([Writable] AudioMixerController mono);

        public extern int numExposedParameters { [NativeMethod("AudioMixerControllerBindings::GetNumExposedParameters", HasExplicitThis = true, IsFreeFunction = true)] get; }

        public extern ExposedAudioParameter[] exposedParameters { get; set; }

        public extern AudioMixerGroupController masterGroup
        {
            [NativeName("GetMasterGroupController")]
            get;
            [NativeName("SetMasterGroupController")]
            set;
        }

        public extern AudioMixerSnapshot startSnapshot { get; set; }

        public AudioMixerSnapshotController TargetSnapshot { get { return (AudioMixerSnapshotController)currentSnapshot; } set { currentSnapshot = value; } }

        private extern AudioMixerSnapshot currentSnapshot { get; set; }

        public AudioMixerSnapshotController[] snapshots
        {
            get
            {
                return System.Array.ConvertAll(snapshots_Internal, ams => (AudioMixerSnapshotController)ams);
            }
            set
            {
                snapshots_Internal = System.Array.ConvertAll(value, amsc => (AudioMixerSnapshot)amsc);
                ValidateSnapshots();
            }
        }

        [NativeName("Snapshots")]
        private extern AudioMixerSnapshot[] snapshots_Internal { get; set; }

        private extern void ValidateSnapshots();

        [NativeMethod("AudioMixerControllerBindings::GetGroupVUInfo", HasExplicitThis = true, IsFreeFunction = true)]
        public extern int GetGroupVUInfo(GUID group, bool fader, [Unmarshalled] float[] vuLevel, [Unmarshalled] float[] vuPeak);

        public extern void UpdateMuteSolo();

        public extern void UpdateBypass();

        [System.NonSerialized] public int m_HighlightEffectIndex = -1;

        [System.NonSerialized] private List<AudioMixerGroupController> m_CachedSelection = null;
        public List<AudioMixerGroupController> CachedSelection
        {
            get
            {
                if (m_CachedSelection == null)
                    m_CachedSelection = new List<AudioMixerGroupController>();
                return m_CachedSelection;
            }
        }

        public extern int currentViewIndex { get; set; }

        public extern bool CurrentViewContainsGroup(GUID group);

        [NativeName("AudioMixerGroupViews")]
        public extern MixerGroupView[] views { get; set; }

        [FreeFunction("AudioMixer::CheckForCyclicReferences")]
        static internal extern bool CheckForCyclicReferences(AudioMixer mixer, AudioMixerGroup group);

        [FreeFunction("AudioMixerControllerBindings::GetMaxVolume")]
        static internal extern float GetMaxVolume();

        [FreeFunction("AudioMixerControllerBindings::GetVolumeSplitPoint")]
        static internal extern float GetVolumeSplitPoint();

        public extern bool isSuspended { [NativeMethod("IsSuspended")] get; }

        [FreeFunction("AudioMixerController::EditingTargetSnapshot")]
        public extern static bool EditingTargetSnapshot();
        [NativeMethod]
        internal extern bool HasValidSnapshots();
    }
}
