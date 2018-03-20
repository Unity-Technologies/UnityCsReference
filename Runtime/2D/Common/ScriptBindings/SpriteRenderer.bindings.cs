// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine
{
    public enum SpriteDrawMode
    {
        Simple,
        Sliced,
        Tiled
    }

    public enum SpriteTileMode
    {
        Continuous,
        Adaptive
    }

    public enum SpriteMaskInteraction
    {
        None = 0,
        VisibleInsideMask = 1,
        VisibleOutsideMask = 2
    }

    [NativeType("Runtime/Graphics/Mesh/SpriteRenderer.h")]
    [RequireComponent(typeof(Transform))]
    public sealed partial class SpriteRenderer : Renderer
    {
        internal extern bool shouldSupportTiling
        {
            [NativeMethod("ShouldSupportTiling")]
            get;
        }

        public extern Sprite sprite
        {
            get;
            set;
        }

        public extern SpriteDrawMode drawMode
        {
            get;
            set;
        }

        public extern Vector2 size
        {
            get;
            set;
        }

        public extern float adaptiveModeThreshold
        {
            get;
            set;
        }

        public extern SpriteTileMode tileMode
        {
            get;
            set;
        }

        public extern Color color
        {
            get;
            set;
        }

        public extern SpriteMaskInteraction maskInteraction
        {
            get;
            set;
        }

        public extern bool flipX
        {
            get;
            set;
        }

        public extern bool flipY
        {
            get;
            set;
        }

        public extern SpriteSortPoint spriteSortPoint
        {
            get;
            set;
        }

        [NativeMethod(Name = "GetSpriteBounds")]
        internal extern Bounds Internal_GetSpriteBounds(SpriteDrawMode mode);
        internal Bounds GetSpriteBounds()
        {
            return Internal_GetSpriteBounds(drawMode);
        }
    }
}
