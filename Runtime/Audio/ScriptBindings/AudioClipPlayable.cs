// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Playables;

using UnityObject = UnityEngine.Object;

namespace UnityEngine.Audio
{
    public partial struct AudioClipPlayable : IPlayable, IEquatable<AudioClipPlayable>
    {
        PlayableHandle m_Handle;

        public static AudioClipPlayable Create(PlayableGraph graph, AudioClip clip, bool looping)
        {
            var handle = CreateHandle(graph, clip, looping);
            var playable = new AudioClipPlayable(handle);
            if (clip != null)
                playable.SetDuration(clip.length);
            return playable;
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph, AudioClip clip, bool looping)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!InternalCreateAudioClipPlayable(ref graph, clip, looping, ref handle))
                return PlayableHandle.Null;
            return handle;
        }

        internal AudioClipPlayable(PlayableHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<AudioClipPlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an AudioClipPlayable.");
            }

            m_Handle = handle;
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        public static implicit operator Playable(AudioClipPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        public static explicit operator AudioClipPlayable(Playable playable)
        {
            return new AudioClipPlayable(playable.GetHandle());
        }

        public bool Equals(AudioClipPlayable other)
        {
            return GetHandle() == other.GetHandle();
        }
    }
}
