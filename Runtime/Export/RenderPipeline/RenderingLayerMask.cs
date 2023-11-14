// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Rendering
{
    [RequiredByNativeCode(Optional = true, GenerateProxy = true)]
    [NativeHeader("Runtime/Graphics/RenderingLayerMask.h")]
    [NativeHeader("Runtime/BaseClasses/TagManager.h")]
    [NativeClass("RenderingLayerMask", "struct RenderingLayerMask;")]
    public struct RenderingLayerMask
    {
        [NativeName("m_Bits")] uint m_Mask;

        public static implicit operator uint(RenderingLayerMask mask)
        {
            return mask.m_Mask;
        }

        // implicitly converts an integer to a LayerMask
        public static implicit operator RenderingLayerMask(uint intVal)
        {
            RenderingLayerMask mask;
            mask.m_Mask = intVal;
            return mask;
        }

        // Converts a layer mask value to an integer value.
        public uint value
        {
            get => m_Mask;
            set => m_Mask = value;
        }

        // Given a layer number, returns the name of the layer as defined in either a Builtin or a User Layer in the [[wiki:class-TagManager|Tag Manager]]
        [StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        [NativeMethod("RenderingLayerToString")]
        public static extern string RenderingLayerToName(int layer);

        // Given a layer name, returns the layer index as defined by either a Builtin or a User Layer in the [[wiki:class-TagManager|Tag Manager]]
        [StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        [NativeMethod("StringToRenderingLayer")]
        public static extern int NameToRenderingLayer(string layerName);

        // Given a set of layer names, returns the equivalent layer mask for all of them.
        public static uint GetMask(params string[] renderingLayerNames)
        {
            if (renderingLayerNames == null)
                throw new ArgumentNullException(nameof(renderingLayerNames));

            uint mask = 0;
            for (var i = 0; i < renderingLayerNames.Length; i++)
            {
                var layer = NameToRenderingLayer(renderingLayerNames[i]);
                if (layer != -1)
                    mask |= 1u << layer;
            }

            return mask;
        }

        // Given a span of layer names, returns the equivalent layer mask for all of them.
        public static uint GetMask(ReadOnlySpan<string> renderingLayerNames)
        {
            if (renderingLayerNames == null)
                throw new ArgumentNullException(nameof(renderingLayerNames));

            uint mask = 0;
            foreach (var name in renderingLayerNames)
            {
                var layer = NameToRenderingLayer(name);
                if (layer != -1)
                    mask |= 1u << layer;
            }
            return mask;
        }
    }
}
