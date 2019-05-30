// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEngine
{
    [ExcludeFromPreset]
    [ExcludeFromObjectFactory]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("GUIText has been removed. Use UI.Text instead.", true)]
    public sealed class GUIText
    {
        static void FeatureRemoved() { throw new Exception("GUIText has been removed from Unity. Use UI.Text instead."); }

        [Obsolete("GUIText has been removed. Use UI.Text instead.", true)]
        public bool text
        {
            get
            {
                FeatureRemoved();
                return false;
            }
            set { FeatureRemoved(); }
        }

        [Obsolete("GUIText has been removed. Use UI.Text instead.", true)]
        public Material material
        {
            get
            {
                FeatureRemoved();
                return null;
            }
            set { FeatureRemoved(); }
        }

        [Obsolete("GUIText has been removed. Use UI.Text instead.", true)]
        public Font font
        {
            get
            {
                FeatureRemoved();
                return null;
            }
            set { FeatureRemoved(); }
        }

        [Obsolete("GUIText has been removed. Use UI.Text instead.", true)]
        public TextAlignment alignment
        {
            get
            {
                FeatureRemoved();
                return 0;
            }
            set { FeatureRemoved(); }
        }

        [Obsolete("GUIText has been removed. Use UI.Text instead.", true)]
        public TextAnchor anchor
        {
            get
            {
                FeatureRemoved();
                return 0;
            }
            set { FeatureRemoved(); }
        }

        [Obsolete("GUIText has been removed. Use UI.Text instead.", true)]
        public float lineSpacing
        {
            get
            {
                FeatureRemoved();
                return 0.0f;
            }
            set { FeatureRemoved(); }
        }

        [Obsolete("GUIText has been removed. Use UI.Text instead.", true)]
        public float tabSize
        {
            get
            {
                FeatureRemoved();
                return 0.0f;
            }
            set { FeatureRemoved(); }
        }

        [Obsolete("GUIText has been removed. Use UI.Text instead.", true)]
        public int fontSize
        {
            get
            {
                FeatureRemoved();
                return 0;
            }
            set { FeatureRemoved(); }
        }

        [Obsolete("GUIText has been removed. Use UI.Text instead.", true)]
        public FontStyle fontStyle
        {
            get
            {
                FeatureRemoved();
                return 0;
            }
            set { FeatureRemoved(); }
        }

        [Obsolete("GUIText has been removed. Use UI.Text instead.", true)]
        public bool richText
        {
            get
            {
                FeatureRemoved();
                return false;
            }
            set { FeatureRemoved(); }
        }

        [Obsolete("GUIText has been removed. Use UI.Text instead.", true)]
        public Color color
        {
            get
            {
                FeatureRemoved();
                return new Color(0.0f, 0.0f, 0.0f);
            }
            set { FeatureRemoved(); }
        }

        [Obsolete("GUIText has been removed. Use UI.Text instead.", true)]
        public Vector2 pixelOffset
        {
            get
            {
                FeatureRemoved();
                return new Vector2(0.0f, 0.0f);
            }
            set { FeatureRemoved(); }
        }
    }
}
