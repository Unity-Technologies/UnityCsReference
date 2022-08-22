// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    public partial class LightingSettings
    {
        [Obsolete("LightingSettings.filteringGaussRadiusDirect is deprecated. Use filteringGaussianRadiusDirect instead.", false)]
        public int filteringGaussRadiusDirect { get { return (int)filteringGaussianRadiusDirect; } set { filteringGaussianRadiusDirect = (float)value; } }

        [Obsolete("LightingSettings.filteringGaussRadiusIndirect is deprecated. Use filteringGaussianRadiusIndirect instead.", false)]
        public int filteringGaussRadiusIndirect { get { return (int)filteringGaussianRadiusIndirect; } set { filteringGaussianRadiusIndirect = (float)value; } }

        [Obsolete("LightingSettings.filteringGaussRadiusAO is deprecated. Use filteringGaussianRadiusAO instead.", false)]
        public int filteringGaussRadiusAO { get { return (int)filteringGaussianRadiusAO; } set { filteringGaussianRadiusAO = (float)value; } }

    }
}
