// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;

using UnityObject = UnityEngine.Object;

namespace UnityEngine.Audio
{
    [NativeHeader("Modules/Audio/Public/ScriptBindings/AudioMixerPlayable.bindings.h")]
    [NativeHeader("Modules/Audio/Public/Director/AudioMixerPlayable.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [StaticAccessor("AudioMixerPlayableBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public partial struct AudioMixerPlayable : IPlayable, IEquatable<AudioMixerPlayable>
    {
        PlayableHandle m_Handle;

        public static AudioMixerPlayable Create(PlayableGraph graph, int inputCount = 0, bool normalizeInputVolumes = false)
        {
            var handle = CreateHandle(graph, inputCount, normalizeInputVolumes);
            return new AudioMixerPlayable(handle);
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph, int inputCount, bool normalizeInputVolumes)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!CreateAudioMixerPlayableInternal(ref graph, inputCount, normalizeInputVolumes, ref handle))
                return PlayableHandle.Null;

            return handle;
        }

        internal AudioMixerPlayable(PlayableHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<AudioMixerPlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an AudioMixerPlayable.");
            }

            m_Handle = handle;
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        public static implicit operator Playable(AudioMixerPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        public static explicit operator AudioMixerPlayable(Playable playable)
        {
            return new AudioMixerPlayable(playable.GetHandle());
        }

        public bool Equals(AudioMixerPlayable other)
        {
            return GetHandle() == other.GetHandle();
        }


        //public bool GetAutoNormalizeVolumes()
        //{
        //    return GetAutoNormalizeInternal(ref m_Handle);
        //}

        //public void SetAutoNormalizeVolumes(bool value)
        //{
        //    SetAutoNormalizeInternal(ref m_Handle, value);
        //}

        //extern private static bool GetAutoNormalizeInternal(ref PlayableHandle hdl);
        //extern private static void SetAutoNormalizeInternal(ref PlayableHandle hdl, bool normalise);
        extern private static bool CreateAudioMixerPlayableInternal(ref PlayableGraph graph, int inputCount, bool normalizeInputVolumes, ref PlayableHandle handle);

    }
}
