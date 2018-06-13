// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;


namespace UnityEngine.Playables
{
    [NativeHeader("Runtime/Director/Core/HPlayableOutput.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [NativeHeader("Runtime/Export/Director/PlayableOutputHandle.bindings.h")]
    [UsedByNativeCode]
    public struct PlayableOutputHandle : IEquatable<PlayableOutputHandle>
    {
        internal IntPtr m_Handle;
        internal UInt32 m_Version;

        static readonly PlayableOutputHandle m_Null = new PlayableOutputHandle();
        public static PlayableOutputHandle Null
        {
            get { return m_Null; }
        }

        [VisibleToOtherModules]
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
            return p is PlayableOutputHandle && Equals((PlayableOutputHandle)p);
        }

        public bool Equals(PlayableOutputHandle other)
        {
            return CompareVersion(this, other);
        }

        static internal bool CompareVersion(PlayableOutputHandle lhs, PlayableOutputHandle rhs)
        {
            return (lhs.m_Handle == rhs.m_Handle) && (lhs.m_Version == rhs.m_Version);
        }

        [VisibleToOtherModules]
        extern internal bool IsNull();

        [VisibleToOtherModules]
        extern internal bool IsValid();

        [FreeFunction("PlayableOutputHandleBindings::GetPlayableOutputType", HasExplicitThis = true, ThrowsException = true)]
        extern internal Type GetPlayableOutputType();

        [FreeFunction("PlayableOutputHandleBindings::GetReferenceObject", HasExplicitThis = true, ThrowsException = true)]
        extern internal Object GetReferenceObject();

        [FreeFunction("PlayableOutputHandleBindings::SetReferenceObject", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetReferenceObject(Object target);

        [FreeFunction("PlayableOutputHandleBindings::GetUserData", HasExplicitThis = true, ThrowsException = true)]
        extern internal Object GetUserData();

        [FreeFunction("PlayableOutputHandleBindings::SetUserData", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetUserData([Writable] Object target);

        [FreeFunction("PlayableOutputHandleBindings::GetSourcePlayable", HasExplicitThis = true, ThrowsException = true)]
        extern internal PlayableHandle GetSourcePlayable();

        [FreeFunction("PlayableOutputHandleBindings::SetSourcePlayable", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetSourcePlayable(PlayableHandle target);

        [FreeFunction("PlayableOutputHandleBindings::GetSourceOutputPort", HasExplicitThis = true, ThrowsException = true)]
        extern internal int GetSourceOutputPort();

        [FreeFunction("PlayableOutputHandleBindings::SetSourceOutputPort", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetSourceOutputPort(int port);

        [FreeFunction("PlayableOutputHandleBindings::GetWeight", HasExplicitThis = true, ThrowsException = true)]
        extern internal float GetWeight();

        [FreeFunction("PlayableOutputHandleBindings::SetWeight", HasExplicitThis = true, ThrowsException = true)]
        extern internal void SetWeight(float weight);

        [FreeFunction("PlayableOutputHandleBindings::PushNotification", HasExplicitThis = true, ThrowsException = true)]
        extern internal void PushNotification(PlayableHandle origin, INotification notification, object context);

        [FreeFunction("PlayableOutputHandleBindings::GetNotificationReceivers", HasExplicitThis = true, ThrowsException = true)]
        extern internal INotificationReceiver[] GetNotificationReceivers();

        [FreeFunction("PlayableOutputHandleBindings::AddNotificationReceiver", HasExplicitThis = true, ThrowsException = true)]
        extern internal void AddNotificationReceiver(INotificationReceiver receiver);

        [FreeFunction("PlayableOutputHandleBindings::RemoveNotificationReceiver", HasExplicitThis = true, ThrowsException = true)]
        extern internal void RemoveNotificationReceiver(INotificationReceiver receiver);
    }
}
