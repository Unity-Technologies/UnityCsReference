// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEngine.Playables
{
    [Serializable]
    [RequiredByNativeCode]
    public abstract partial class ScriptPlayable : IPlayable, IScriptPlayable, ICloneable
    {
        public PlayableHandle handle;
        PlayableHandle IPlayable.playableHandle { get { return handle; } set { handle = value; } }

        public static implicit operator PlayableHandle(ScriptPlayable b) { return b.handle; }
        public bool IsValid() { return handle.IsValid(); }

        public virtual void OnGraphStart() {}
        public virtual void OnGraphStop()  {}

        public virtual void OnDestroy() {}
        public virtual void PrepareFrame(FrameData info) {}
        public virtual void ProcessFrame(FrameData info, object playerData) {}
        public virtual void OnPlayStateChanged(FrameData info, PlayState newState) {}

        public virtual object Clone()
        {
            ScriptPlayable clone = (ScriptPlayable)MemberwiseClone();
            clone.handle = PlayableHandle.Null;
            return clone;
        }
    }
}
