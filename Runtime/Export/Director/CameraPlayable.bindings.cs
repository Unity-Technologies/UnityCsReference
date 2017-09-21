// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;

using UnityObject = UnityEngine.Object;

namespace UnityEngine.Experimental.Playables
{
    [NativeHeader("Runtime/Export/Director/CameraPlayable.bindings.h")]
    [NativeHeader("Runtime/Camera//Director/CameraPlayable.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [StaticAccessor("CameraPlayableBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public struct CameraPlayable : IPlayable, IEquatable<CameraPlayable>
    {
        PlayableHandle m_Handle;

        public static CameraPlayable Create(PlayableGraph graph, Camera camera)
        {
            var handle = CreateHandle(graph, camera);
            return new CameraPlayable(handle);
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph, Camera camera)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!InternalCreateCameraPlayable(ref graph, camera, ref handle))
                return PlayableHandle.Null;
            return handle;
        }

        internal CameraPlayable(PlayableHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<CameraPlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an CameraPlayable.");
            }

            m_Handle = handle;
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        public static implicit operator Playable(CameraPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        public static explicit operator CameraPlayable(Playable playable)
        {
            return new CameraPlayable(playable.GetHandle());
        }

        public bool Equals(CameraPlayable other)
        {
            return GetHandle() == other.GetHandle();
        }


        public Camera GetCamera()
        {
            return GetCameraInternal(ref m_Handle);
        }

        public void SetCamera(Camera value)
        {
            SetCameraInternal(ref m_Handle, value);
        }

        // Bindings methods.
        extern private static Camera GetCameraInternal(ref PlayableHandle hdl);
        extern private static void SetCameraInternal(ref PlayableHandle hdl, Camera camera);
        extern private static bool InternalCreateCameraPlayable(ref PlayableGraph graph, Camera camera, ref PlayableHandle handle);
        extern private static bool ValidateType(ref PlayableHandle hdl);

    }
}
