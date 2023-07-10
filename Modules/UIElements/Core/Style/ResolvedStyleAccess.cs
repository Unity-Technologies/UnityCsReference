// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;

namespace UnityEngine.UIElements
{
    internal partial class ResolvedStyleAccess
    {
        private VisualElement ve { get; }

        public ResolvedStyleAccess(VisualElement ve)
        {
            this.ve = ve;
        }

        [Obsolete("unityBackgroundScaleMode is deprecated. Use background-* properties instead.")]
        public StyleEnum<ScaleMode> unityBackgroundScaleMode => BackgroundPropertyHelper.ResolveUnityBackgroundScaleMode(
            ve.computedStyle.backgroundPositionX, ve.computedStyle.backgroundPositionY,
            ve.computedStyle.backgroundRepeat, ve.computedStyle.backgroundSize, out _);
    }
}
