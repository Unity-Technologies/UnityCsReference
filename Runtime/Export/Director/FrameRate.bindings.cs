// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Playables
{
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode("FrameRate")]
    [NativeHeader("Runtime/Director/Core/FrameRate.h")]
    internal struct FrameRate : IEquatable<FrameRate>
    {
        [Ignore] public static readonly FrameRate k_24Fps = new FrameRate(24U, false);
        [Ignore] public static readonly FrameRate k_23_976Fps = new FrameRate(24U, true);
        [Ignore] public static readonly FrameRate k_25Fps = new FrameRate(25U, false);

        [Ignore] public static readonly FrameRate k_30Fps = new FrameRate(30U, false);
        [Ignore] public static readonly FrameRate k_29_97Fps = new FrameRate(30U, true);

        [Ignore] public static readonly FrameRate k_50Fps = new FrameRate(50U, false);
        [Ignore] public static readonly FrameRate k_60Fps = new FrameRate(60U, false);
        [Ignore] public static readonly FrameRate k_59_94Fps = new FrameRate(60U, true);

        [SerializeField]
        int m_Rate;

        public bool dropFrame => m_Rate < 0;
        public double rate => dropFrame ? -m_Rate * (1000.0 / 1001.0) : m_Rate;

        public FrameRate(uint frameRate = 0, bool drop = false)
        {
            m_Rate = (drop ? -1 : 1) * (int)frameRate;
        }

        public bool IsValid()
        {
            return m_Rate != 0;
        }

        public bool Equals(FrameRate other)
        {
            return m_Rate == other.m_Rate;
        }

        public override bool Equals(object obj)
        {
            return obj is FrameRate && Equals((FrameRate)obj);
        }

        public static bool operator==(FrameRate a, FrameRate b) => a.Equals(b);
        public static bool operator!=(FrameRate a, FrameRate b) => !a.Equals(b);
        public static bool operator<(FrameRate a, FrameRate b) => a.rate < b.rate;
        public static bool operator<=(FrameRate a, FrameRate b) => a.rate <= b.rate;
        public static bool operator>(FrameRate a, FrameRate b) => a.rate > b.rate;
        public static bool operator>=(FrameRate a, FrameRate b) => a.rate <= b.rate;

        public override int GetHashCode()
        {
            return m_Rate;
        }

        public override string ToString()
        {
            return ToString(null, null);
        }

        public string ToString(string format)
        {
            return ToString(format, null);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format =  dropFrame ? "F2" : "F0";
            if (formatProvider == null)
                formatProvider = CultureInfo.InvariantCulture.NumberFormat;
            return UnityString.Format("{0} Fps", rate.ToString(format, formatProvider));
        }

        internal static int FrameRateToInt(FrameRate framerate)
        {
            return framerate.m_Rate;
        }

        internal static FrameRate DoubleToFrameRate(double framerate)
        {
            var fullFrameRate = (uint)Math.Ceiling(framerate);
            if (fullFrameRate <= 0)
                return new FrameRate(1U);
            var dropFrameRate = new FrameRate(fullFrameRate, true);
            if (Math.Abs(framerate - dropFrameRate.rate) < Math.Abs(framerate - fullFrameRate))
                return dropFrameRate;
            return new FrameRate(fullFrameRate);
        }
    }
}
