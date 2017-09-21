// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;

namespace UnityEngine.Experimental.Playables
{
    [NativeHeader("Runtime/Export/Director/TexturePlayableOutput.bindings.h")]
    [NativeHeader("Runtime/Graphics/Director/TexturePlayableOutput.h")]
    [NativeHeader("Runtime/Graphics/RenderTexture.h")]
    [StaticAccessor("TexturePlayableOutputBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public struct TexturePlayableOutput : IPlayableOutput
    {
        private PlayableOutputHandle m_Handle;

        public static TexturePlayableOutput Create(PlayableGraph graph, string name, RenderTexture target)
        {
            PlayableOutputHandle handle;
            if (!TexturePlayableGraphExtensions.InternalCreateTextureOutput(ref graph, name, out handle))
                return TexturePlayableOutput.Null;

            TexturePlayableOutput output = new TexturePlayableOutput(handle);
            output.SetTarget(target);

            return output;
        }

        internal TexturePlayableOutput(PlayableOutputHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOutputOfType<TexturePlayableOutput>())
                    throw new InvalidCastException("Can't set handle: the playable is not an TexturePlayableOutput.");
            }

            m_Handle = handle;
        }

        public static TexturePlayableOutput Null
        {
            get { return new TexturePlayableOutput(PlayableOutputHandle.Null); }
        }

        public PlayableOutputHandle GetHandle()
        {
            return m_Handle;
        }

        public static implicit operator PlayableOutput(TexturePlayableOutput output)
        {
            return new PlayableOutput(output.GetHandle());
        }

        public static explicit operator TexturePlayableOutput(PlayableOutput output)
        {
            return new TexturePlayableOutput(output.GetHandle());
        }


        public RenderTexture GetTarget()
        {
            return InternalGetTarget(ref m_Handle);
        }

        public void SetTarget(RenderTexture value)
        {
            InternalSetTarget(ref m_Handle, value);
        }

        // Bindings methods.
        extern private static RenderTexture InternalGetTarget(ref PlayableOutputHandle output);
        extern private static void InternalSetTarget(ref PlayableOutputHandle output, RenderTexture target);

    }
}
