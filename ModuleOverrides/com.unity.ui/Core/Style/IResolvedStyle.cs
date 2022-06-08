// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    public partial interface IResolvedStyle
    {
        /// <summary>
        /// Background image scaling in the element's box.
        /// </summary>
        [Obsolete("unityBackgroundScaleMode is deprecated. Use background-* properties instead.")]
        StyleEnum<ScaleMode> unityBackgroundScaleMode { get; }
    }
}
