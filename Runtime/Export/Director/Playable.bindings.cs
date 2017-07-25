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
    public struct Playable : IPlayable, IEquatable<Playable>
    {
        PlayableHandle m_Handle;

        static readonly Playable m_NullPlayable = new Playable(PlayableHandle.Null);
        public static Playable Null { get { return m_NullPlayable; } }

        public static Playable Create(PlayableGraph graph, int inputCount = 0)
        {
            var playable = new Playable(graph.CreatePlayableHandle());
            playable.SetInputCount(inputCount);
            return playable;
        }

        internal Playable(PlayableHandle handle)
        {
            m_Handle = handle;
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        public bool IsPlayableOfType<T>()
            where T : struct, IPlayable
        {
            return GetHandle().IsPlayableOfType<T>();
        }

        public Type GetPlayableType()
        {
            return GetHandle().GetPlayableType();
        }

        public bool Equals(Playable other)
        {
            return GetHandle() == other.GetHandle();
        }
    }
}
