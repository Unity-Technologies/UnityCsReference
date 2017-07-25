// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

using Object = UnityEngine.Object;

namespace UnityEngine.Playables
{
    [NativeHeader("Runtime/Director/Core/HPlayableOutput.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [UsedByNativeCode]
    public partial struct PlayableOutputHandle
    {
        internal IntPtr m_Handle;
        internal int m_Version;

        public static PlayableOutputHandle Null
        {
            get { return new PlayableOutputHandle() { m_Version = Int32.MaxValue }; }
        }

        internal bool IsPlayableOutputOfType<T>()
        {
            return GetPlayableOutputType() == typeof(T);
        }

        public override int GetHashCode()
        {
            return m_Handle.GetHashCode() ^ m_Version.GetHashCode();
        }

        public static bool operator==(PlayableOutputHandle lhs, PlayableOutputHandle rhs)
        {
            return CompareVersion(lhs, rhs);
        }

        public static bool operator!=(PlayableOutputHandle lhs, PlayableOutputHandle rhs)
        {
            return !CompareVersion(lhs, rhs);
        }

        public override bool Equals(object p)
        {
            return p is PlayableOutputHandle && CompareVersion(this, (PlayableOutputHandle)p);
        }

        static internal bool CompareVersion(PlayableOutputHandle lhs, PlayableOutputHandle rhs)
        {
            return (lhs.m_Handle == rhs.m_Handle) && (lhs.m_Version == rhs.m_Version);
        }

        // Bindings methods.
        extern internal bool IsValid();
        extern internal Type GetPlayableOutputType();
        extern internal Object GetReferenceObject();
        extern internal void SetReferenceObject(Object target);
        //extern internal Object GetUserData();
        //extern internal void SetUserData([Writable] Object target);
        extern internal PlayableHandle GetSourcePlayable();
        extern internal void SetSourcePlayable(PlayableHandle target);
        extern internal int GetSourceInputPort();
        extern internal void SetSourceInputPort(int port);
        extern internal float GetWeight();
        extern internal void SetWeight(float weight);
    }
}
