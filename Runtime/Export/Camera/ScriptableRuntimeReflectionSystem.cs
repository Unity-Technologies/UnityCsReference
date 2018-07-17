// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.Rendering
{
    public abstract class ScriptableRuntimeReflectionSystem : IScriptableRuntimeReflectionSystem
    {
        public virtual bool TickRealtimeProbes()
        {
            return false;
        }

        protected virtual void Dispose(bool disposing) {}

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
