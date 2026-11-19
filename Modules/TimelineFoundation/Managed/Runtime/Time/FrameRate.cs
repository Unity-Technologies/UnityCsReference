// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Bindings;

//Copy of UnityEngine.Playables.FrameRate
namespace Unity.Timeline.Foundation.Time
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal struct FrameRate : IEquatable<FrameRate>
    {
        public static readonly FrameRate k_24Fps = new FrameRate(24U);
        public static readonly FrameRate k_23_976Fps = new FrameRate(24U, true);
        public static readonly FrameRate k_25Fps = new FrameRate(25U);
        public static readonly FrameRate k_30Fps = new FrameRate(30U);
        public static readonly FrameRate k_29_97Fps = new FrameRate(30U, true);
        public static readonly FrameRate k_50Fps = new FrameRate(50U);
        public static readonly FrameRate k_60Fps = new FrameRate(60U);
        public static readonly FrameRate k_59_94Fps = new FrameRate(60U, true);
        [SerializeField]
        private int m_Rate;

        public bool dropFrame => this.m_Rate < 0;

        public double rate => !this.dropFrame ? (double)this.m_Rate : (double)-this.m_Rate * 0.999000999000999;

        public FrameRate(uint frameRate = 0, bool drop = false) => this.m_Rate = (drop ? -1 : 1) * (int)frameRate;

        public bool IsValid() => (uint)this.m_Rate > 0U;

        public bool Equals(FrameRate other) => this.m_Rate == other.m_Rate;

        public override bool Equals(object obj) => obj is FrameRate other && this.Equals(other);

        public static bool operator ==(FrameRate a, FrameRate b) => a.Equals(b);

        public static bool operator !=(FrameRate a, FrameRate b) => !a.Equals(b);

        public static bool operator <(FrameRate a, FrameRate b) => a.rate < b.rate;

        public static bool operator <=(FrameRate a, FrameRate b) => a.rate <= b.rate;

        public static bool operator >(FrameRate a, FrameRate b) => a.rate > b.rate;

        public static bool operator >=(FrameRate a, FrameRate b) => a.rate <= b.rate;

        public override int GetHashCode() => this.m_Rate;

        public override string ToString() => this.ToString((string)null, (IFormatProvider)null);

        public string ToString(string format) => this.ToString(format, (IFormatProvider)null);

        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = this.dropFrame ? "F2" : "F0";
            if (formatProvider == null)
                formatProvider = (IFormatProvider)CultureInfo.InvariantCulture.NumberFormat;
            return $"{rate.ToString(format, formatProvider)} Fps";
        }

        public static int FrameRateToInt(FrameRate framerate) => framerate.m_Rate;

        public static FrameRate DoubleToFrameRate(double framerate)
        {
            uint frameRate1 = (uint)Math.Ceiling(framerate);
            if (frameRate1 <= 0U)
                return new FrameRate(1U);
            FrameRate frameRate2 = new FrameRate(frameRate1, true);
            return Math.Abs(framerate - frameRate2.rate) < Math.Abs(framerate - (double)frameRate1) ? frameRate2 : new FrameRate(frameRate1);
        }
    }
}
