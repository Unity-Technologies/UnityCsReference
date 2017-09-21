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
    [NativeHeader("Runtime/Export/Director/MaterialEffectPlayable.bindings.h")]
    [NativeHeader("Runtime/Shaders/Director/MaterialEffectPlayable.h")]
    [NativeHeader("Runtime/Director/Core/HPlayable.h")]
    [StaticAccessor("MaterialEffectPlayableBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public struct MaterialEffectPlayable : IPlayable, IEquatable<MaterialEffectPlayable>
    {
        PlayableHandle m_Handle;

        public static MaterialEffectPlayable Create(PlayableGraph graph, Material material, int pass = -1)
        {
            var handle = CreateHandle(graph, material, pass);
            return new MaterialEffectPlayable(handle);
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph, Material material, int pass)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!InternalCreateMaterialEffectPlayable(ref graph, material, pass, ref handle))
                return PlayableHandle.Null;
            return handle;
        }

        internal MaterialEffectPlayable(PlayableHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<MaterialEffectPlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an MaterialEffectPlayable.");
            }

            m_Handle = handle;
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        public static implicit operator Playable(MaterialEffectPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        public static explicit operator MaterialEffectPlayable(Playable playable)
        {
            return new MaterialEffectPlayable(playable.GetHandle());
        }

        public bool Equals(MaterialEffectPlayable other)
        {
            return GetHandle() == other.GetHandle();
        }


        public Material GetMaterial()
        {
            return GetMaterialInternal(ref m_Handle);
        }

        public void SetMaterial(Material value)
        {
            SetMaterialInternal(ref m_Handle, value);
        }

        public int GetPass()
        {
            return GetPassInternal(ref m_Handle);
        }

        public void SetPass(int value)
        {
            SetPassInternal(ref m_Handle, value);
        }

        // Bindings methods.
        extern private static Material GetMaterialInternal(ref PlayableHandle hdl);
        extern private static void SetMaterialInternal(ref PlayableHandle hdl, Material material);
        extern private static int GetPassInternal(ref PlayableHandle hdl);
        extern private static void SetPassInternal(ref PlayableHandle hdl, int pass);
        extern private static bool InternalCreateMaterialEffectPlayable(ref PlayableGraph graph, Material material, int pass, ref PlayableHandle handle);
        extern private static bool ValidateType(ref PlayableHandle hdl);

    }
}
