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
    [Serializable]
    [RequiredByNativeCode]
    public abstract class PlayableBehaviour : IPlayableBehaviour, ICloneable
    {
        public PlayableBehaviour() {}

        public virtual void OnGraphStart(Playable playable) {}
        public virtual void OnGraphStop(Playable playable)  {}

        public virtual void OnPlayableCreate(Playable playable) {}
        public virtual void OnPlayableDestroy(Playable playable) {}

        public virtual void OnBehaviourDelay(Playable playable, FrameData info) {}
        public virtual void OnBehaviourPlay(Playable playable, FrameData info) {}
        public virtual void OnBehaviourPause(Playable playable, FrameData info) {}

        public virtual void PrepareData(Playable playable, FrameData info) {}
        public virtual void PrepareFrame(Playable playable, FrameData info) {}
        public virtual void ProcessFrame(Playable playable, FrameData info, object playerData) {}

        public virtual object Clone()
        {
            return MemberwiseClone();
        }
    }
}
