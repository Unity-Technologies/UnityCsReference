// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [ExcludeFromPreset]
    [ExcludeFromObjectFactory]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("GUITexture has been removed. Use UI.Image instead.", true)]
    public sealed class GUITexture
    {
        static void FeatureRemoved() { throw new Exception("GUITexture has been removed from Unity. Use UI.Image instead."); }

        [Obsolete("GUITexture has been removed. Use UI.Image instead.", true)]
        public Color color
        {
            get
            {
                FeatureRemoved();
                return new Color(0.0f, 0.0f, 0.0f);
            }
            set { FeatureRemoved(); }
        }

        [Obsolete("GUITexture has been removed. Use UI.Image instead.", true)]
        public Texture texture
        {
            get
            {
                FeatureRemoved();
                return null;
            }
            set { FeatureRemoved(); }
        }

        [Obsolete("GUITexture has been removed. Use UI.Image instead.", true)]
        public Rect pixelInset
        {
            get
            {
                FeatureRemoved();
                return new Rect();
            }
            set { FeatureRemoved(); }
        }

        [Obsolete("GUITexture has been removed. Use UI.Image instead.", true)]
        public RectOffset border
        {
            get
            {
                FeatureRemoved();
                return null;
            }
            set { FeatureRemoved(); }
        }
    }
}
