// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine.Playables
{
    [Obsolete("DataStreamType is no longer required and will be removed in a future release.", false)]
    public enum DataStreamType
    {
        Animation = 0,
        Audio = 1,
        Texture = 2,
        None = 3
    }

    // Represents the description of a playable output
    public struct PlayableBinding
    {
        [VisibleToOtherModules]
        internal delegate PlayableOutput CreateOutputMethod(PlayableGraph graph, string name);

        private string m_StreamName;
        private UnityEngine.Object m_SourceObject;
        private System.Type m_SourceBindingType;
        private CreateOutputMethod m_CreateOutputMethod;

        public static readonly PlayableBinding[] None = new PlayableBinding[0];
        public static readonly double DefaultDuration = double.PositiveInfinity;

        public string streamName { get { return m_StreamName; }  set { m_StreamName = value; } }
        public UnityEngine.Object sourceObject { get { return m_SourceObject; } set { m_SourceObject = value; } }
        public System.Type outputTargetType { get { return m_SourceBindingType; } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("sourceBindingType is no longer supported on PlayableBinding. Use outputBindingType instead to get the required output target type, and the appropriate binding create method (e.g. AnimationPlayableBinding.Create(name, key)) to create PlayableBindings", true)]
        public System.Type sourceBindingType { get { return m_SourceBindingType; } set {} }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("streamType is no longer supported on PlayableBinding. Use the appropriate binding create method (e.g. AnimationPlayableBinding.Create(name, key)) instead.", true)]
        public DataStreamType streamType { get { return DataStreamType.None; } set {} }


        internal PlayableOutput CreateOutput(PlayableGraph graph)
        {
            if (m_CreateOutputMethod != null)
                return m_CreateOutputMethod(graph, m_StreamName);
            return PlayableOutput.Null;
        }

        [VisibleToOtherModules]
        internal static PlayableBinding CreateInternal(string name, UnityEngine.Object sourceObject, System.Type sourceType, CreateOutputMethod createFunction)
        {
            PlayableBinding pb = new PlayableBinding();
            pb.m_StreamName = name;
            pb.m_SourceObject = sourceObject;
            pb.m_SourceBindingType = sourceType;
            pb.m_CreateOutputMethod = createFunction;
            return pb;
        }
    };
}
