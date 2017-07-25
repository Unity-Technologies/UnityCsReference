// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine;
using UnityEngineInternal;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEngine.Playables
{
    [RequiredByNativeCode]
    public partial struct ScriptPlayableOutput : IPlayableOutput
    {
        private PlayableOutputHandle m_Handle;

        public static ScriptPlayableOutput Create(PlayableGraph graph, string name)
        {
            PlayableOutputHandle handle;
            if (!graph.CreateScriptOutputInternal(name, out handle))
                return ScriptPlayableOutput.Null;
            return new ScriptPlayableOutput(handle);
        }

        internal ScriptPlayableOutput(PlayableOutputHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOutputOfType<ScriptPlayableOutput>())
                    throw new InvalidCastException("Can't set handle: the playable is not a ScriptPlayableOutput.");
            }

            m_Handle = handle;
        }

        public static ScriptPlayableOutput Null
        {
            get { return new ScriptPlayableOutput(PlayableOutputHandle.Null); }
        }

        public PlayableOutputHandle GetHandle()
        {
            return m_Handle;
        }

        public static implicit operator PlayableOutput(ScriptPlayableOutput output)
        {
            return new PlayableOutput(output.GetHandle());
        }

        public static explicit operator ScriptPlayableOutput(PlayableOutput output)
        {
            return new ScriptPlayableOutput(output.GetHandle());
        }
    }
}
