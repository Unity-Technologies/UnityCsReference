// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering
{
    public interface IScriptableBakedReflectionSystem : IDisposable
    {
        int stageCount { get; }
        Hash128 stateHash { get; }

        void Tick(SceneStateHash sceneStateHash, IScriptableBakedReflectionSystemStageNotifier handle);
        void SynchronizeReflectionProbes();
        void Clear();
        void Cancel();
        bool BakeAllReflectionProbes();
    }
}
