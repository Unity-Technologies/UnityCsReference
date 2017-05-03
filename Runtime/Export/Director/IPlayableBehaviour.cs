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
    public interface IPlayableBehaviour
    {
        void OnGraphStart(Playable playable);
        void OnGraphStop(Playable playable);

        void OnPlayableCreate(Playable playable);
        void OnPlayableDestroy(Playable playable);

        void OnBehaviourPlay(Playable playable, FrameData info);
        void OnBehaviourPause(Playable playable, FrameData info);

        void PrepareFrame(Playable playable, FrameData info);
        void ProcessFrame(Playable playable, FrameData info, object playerData);
    }
}
