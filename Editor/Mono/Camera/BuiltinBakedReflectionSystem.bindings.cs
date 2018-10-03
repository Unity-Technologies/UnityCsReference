// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Experimental.Rendering
{
    [NativeHeader("Editor/Src/Camera/BuiltinBakedReflectionSystem.h")]
    [NativeHeader("Editor/Src/Camera/SceneStateHash.h")]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    class BuiltinBakedReflectionSystem : IScriptableBakedReflectionSystem
    {
        IntPtr m_Ptr = IntPtr.Zero;
        IScriptableBakedReflectionSystemStageNotifier m_Handle;

        public int stageCount { get { return 3; } }
        [NativeProperty("StateHash")]
        extern Hash128 _stateHash { get; }
        public Hash128 stateHash { get { return _stateHash; } }

        bool disposed { get { return m_Ptr == IntPtr.Zero; } }

        BuiltinBakedReflectionSystem()
        {
            m_Ptr = Internal_GetPtr();
        }

        ~BuiltinBakedReflectionSystem()
        {
            Dispose(false);
        }

        public void Tick(SceneStateHash dependencies, IScriptableBakedReflectionSystemStageNotifier handle)
        {
            if (disposed)
                throw new ObjectDisposedException("BuiltinBakedReflectionSystem");

            m_Handle = handle;
            Assert.IsNotNull(m_Handle);

            Internal_Tick(dependencies);
        }

        public void SynchronizeReflectionProbes()
        {
            if (disposed)
                throw new ObjectDisposedException("BuiltinBakedReflectionSystem");

            Internal_SynchronizeReflectionProbes();
        }

        public void Clear()
        {
            if (disposed)
                throw new ObjectDisposedException("BuiltinBakedReflectionSystem");

            Internal_Clear();
        }

        public bool BakeAllReflectionProbes()
        {
            if (disposed)
                throw new ObjectDisposedException("BuiltinBakedReflectionSystem");

            return Internal_BakeAllReflectionProbes();
        }

        public void Cancel()
        {
            // Cancel is empty on purpose
            // We need to have this callback on the C# side as it is the only way the C# implementation
            // is notified when the user cancelled the bake
        }

        public void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
            m_Ptr = IntPtr.Zero;
        }

        [RequiredByNativeCode]
        static BuiltinBakedReflectionSystem Internal_BuiltinBakedReflectionSystem_New()
        {
            var instance = new BuiltinBakedReflectionSystem();
            instance.Internal_SetPtr(instance);
            return instance;
        }

        [RequiredByNativeCode]
        void Internal_BuiltinBakedReflectionSystem_EnterStage(int stage, string progressMessage, float progress)
        {
            Assert.IsNotNull(m_Handle);

            m_Handle.EnterStage(stage, progressMessage, progress);
        }

        [RequiredByNativeCode]
        void Internal_BuiltinBakedReflectionSystem_ExitStage(int stage)
        {
            Assert.IsNotNull(m_Handle);

            m_Handle.ExitStage(stage);
        }

        [RequiredByNativeCode]
        void Internal_BuiltinBakedReflectionSystem_SetIsDone(bool isDone)
        {
            Assert.IsNotNull(m_Handle);

            m_Handle.SetIsDone(isDone);
        }

        [NativeMethod("Get")]
        static extern IntPtr Internal_GetPtr();

        extern void Internal_Tick(SceneStateHash dependencies);
        extern void Internal_SynchronizeReflectionProbes();
        extern void Internal_Clear();
        extern void Internal_SetPtr(BuiltinBakedReflectionSystem ptr);
        extern bool Internal_BakeAllReflectionProbes();
    }
}
