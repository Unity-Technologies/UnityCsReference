// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    [Serializable, Obsolete]
    public class ColorPickerHDRConfig
    {
        public float minBrightness;
        public float maxBrightness;
        public float minExposureValue;
        public float maxExposureValue;

        public ColorPickerHDRConfig(
            float minBrightness, float maxBrightness, float minExposureValue, float maxExposureValue
            )
        {
            this.minBrightness = minBrightness;
            this.maxBrightness = maxBrightness;
            this.minExposureValue = minExposureValue;
            this.maxExposureValue = maxExposureValue;
        }
    }
}
