// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine;
using UnityEngineInternal;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;

namespace UnityEngine.Playables
{
    [UsedByNativeCode]
    public partial struct ScriptPlayableOutput
    {
        internal PlayableOutput m_Output;

        public static ScriptPlayableOutput Null
        {
            get { return new ScriptPlayableOutput() { m_Output = PlayableOutput.Null }; }
        }

        internal Object referenceObject
        {
            get { return PlayableOutput.GetInternalReferenceObject(ref m_Output); }
            set { PlayableOutput.SetInternalReferenceObject(ref m_Output, value); }
        }

        public Object userData
        {
            get { return PlayableOutput.GetInternalUserData(ref m_Output); }
            set { PlayableOutput.SetInternalUserData(ref m_Output, value); }
        }

        public bool IsValid()
        {
            return PlayableOutput.IsValidInternal(ref m_Output);
        }

        public PlayableHandle sourcePlayable
        {
            get { return PlayableOutput.InternalGetSourcePlayable(ref m_Output); }
            set { PlayableOutput.InternalSetSourcePlayable(ref m_Output, ref value); }
        }

        public int sourceInputPort
        {
            get { return PlayableOutput.InternalGetSourceInputPort(ref m_Output); }
            set { PlayableOutput.InternalSetSourceInputPort(ref m_Output, value); }
        }
    }
}
