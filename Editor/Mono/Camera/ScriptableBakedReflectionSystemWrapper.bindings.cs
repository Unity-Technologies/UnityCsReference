// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Experimental.Rendering
{
    [RequiredByNativeCode]
    [NativeHeader("Editor/Src/Camera/ScriptableBakedReflectionSystem.h")]
    [StructLayout(LayoutKind.Sequential)]
    class ScriptableBakedReflectionSystemWrapper : IDisposable, IScriptableBakedReflectionSystemStageNotifier
    {
        IntPtr m_Ptr = IntPtr.Zero;
        bool Disposed { get { return m_Ptr == IntPtr.Zero; } }

        internal IScriptableBakedReflectionSystem implementation { get; set; }

        internal ScriptableBakedReflectionSystemWrapper(IntPtr ptr)
        {
            m_Ptr = ptr;
        }

        ~ScriptableBakedReflectionSystemWrapper()
        {
            Dispose(false);
        }

        Hash128 Internal_ScriptableBakedReflectionSystemWrapper_stateHash
        {
            [RequiredByNativeCode]
            get { return implementation != null ? implementation.stateHash : new Hash128(); }
        }

        int Internal_ScriptableBakedReflectionSystemWrapper_stageCount
        {
            [RequiredByNativeCode]
            get { return implementation != null ? implementation.stageCount : 0; }
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void IScriptableBakedReflectionSystemStageNotifier.EnterStage(int stage, string progressMessage, float progress)
        {
            if (Disposed)
                throw new ObjectDisposedException("ScriptableBakedReflectionSystemWrapper");

            ScriptingEnterStage(m_Ptr, stage, progressMessage, progress);
        }

        void IScriptableBakedReflectionSystemStageNotifier.ExitStage(int stage)
        {
            if (Disposed)
                throw new ObjectDisposedException("ScriptableBakedReflectionSystemWrapper");

            ScriptingExitStage(m_Ptr, stage);
        }

        void IScriptableBakedReflectionSystemStageNotifier.SetIsDone(bool isDone)
        {
            if (Disposed)
                throw new ObjectDisposedException("ScriptableBakedReflectionSystemWrapper");

            ScriptingSetIsDone(m_Ptr, isDone);
        }

        void Dispose(bool disposing)
        {
            m_Ptr = IntPtr.Zero;
            implementation = null;
        }

        [RequiredByNativeCode]
        void Internal_ScriptableBakedReflectionSystemWrapper_Tick(SceneStateHash deps)
        {
            if (Disposed)
                throw new ObjectDisposedException("ScriptableBakedReflectionSystemWrapper");

            if (implementation != null)
                implementation.Tick(deps, this);
        }

        [RequiredByNativeCode]
        void Internal_ScriptableBakedReflectionSystemWrapper_SynchronizeReflectionProbes()
        {
            if (Disposed)
                throw new ObjectDisposedException("ScriptableBakedReflectionSystemWrapper");

            if (implementation != null)
                implementation.SynchronizeReflectionProbes();
        }

        [RequiredByNativeCode]
        void Internal_ScriptableBakedReflectionSystemWrapper_Clear()
        {
            if (Disposed)
                throw new ObjectDisposedException("ScriptableBakedReflectionSystemWrapper");

            if (implementation != null)
                implementation.Clear();
        }

        [RequiredByNativeCode]
        void Internal_ScriptableBakedReflectionSystemWrapper_Cancel()
        {
            if (Disposed)
                throw new ObjectDisposedException("ScriptableBakedReflectionSystemWrapper");

            if (implementation != null)
                implementation.Cancel();
        }

        [RequiredByNativeCode]
        bool Internal_ScriptableBakedReflectionSystemWrapper_BakeAllReflectionProbes()
        {
            if (Disposed)
                throw new ObjectDisposedException("ScriptableBakedReflectionSystemWrapper");

            if (implementation != null)
                return implementation.BakeAllReflectionProbes();

            return false;
        }

        [StaticAccessor("ScriptableBakedReflectionSystem", StaticAccessorType.DoubleColon)]
        static extern void ScriptingEnterStage(IntPtr objPtr, int stage, string progressMessage, float progress);

        [StaticAccessor("ScriptableBakedReflectionSystem", StaticAccessorType.DoubleColon)]
        static extern void ScriptingExitStage(IntPtr objPtr, int stage);

        [StaticAccessor("ScriptableBakedReflectionSystem", StaticAccessorType.DoubleColon)]
        static extern void ScriptingSetIsDone(IntPtr objPtr, bool isDone);
    }
}
