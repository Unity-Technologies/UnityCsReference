// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting;
using System.Collections.Generic;

namespace UnityEngine.Playables
{
    [RequiredByNativeCode]
    public struct PlayableOutput : IPlayableOutput, IEquatable<PlayableOutput>
    {
        PlayableOutputHandle m_Handle;

        static readonly PlayableOutput m_NullPlayableOutput = new PlayableOutput(PlayableOutputHandle.Null);
        public static PlayableOutput Null { get { return m_NullPlayableOutput; } }

        internal PlayableOutput(PlayableOutputHandle handle)
        {
            m_Handle = handle;
        }

        public PlayableOutputHandle GetHandle()
        {
            return m_Handle;
        }

        public bool IsPlayableOutputOfType<T>()
            where T : struct, IPlayableOutput
        {
            return GetHandle().IsPlayableOutputOfType<T>();
        }

        public Type GetPlayableOutputType()
        {
            return GetHandle().GetPlayableOutputType();
        }

        public bool Equals(PlayableOutput other)
        {
            return GetHandle() == other.GetHandle();
        }
    }
}
