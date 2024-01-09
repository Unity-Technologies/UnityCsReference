// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements.StyleSheets
{
    // Stores both a image file and its potential @2x variant
    // Both are guaranteed to be non-null
    [Serializable]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal struct ScalableImage
    {
        public Texture2D normalImage;
        public Texture2D highResolutionImage;

        public override string ToString()
        {
            return $"{nameof(normalImage)}: {normalImage}, {nameof(highResolutionImage)}: {highResolutionImage}";
        }
    }
}
