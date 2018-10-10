// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering
{
    public abstract class ScriptableBakedReflectionSystem : IScriptableBakedReflectionSystem
    {
        public int stageCount { get; }
        public Hash128 stateHash { get; protected set; }

        protected ScriptableBakedReflectionSystem(int stageCount)
        {
            this.stageCount = stageCount;
        }

        public virtual void Tick(SceneStateHash sceneStateHash, IScriptableBakedReflectionSystemStageNotifier handle) {}
        public virtual void SynchronizeReflectionProbes() {}
        public virtual void Clear() {}
        public virtual void Cancel() {}
        public virtual bool BakeAllReflectionProbes() { return false; }

        protected virtual void Dispose(bool disposing) {}

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
