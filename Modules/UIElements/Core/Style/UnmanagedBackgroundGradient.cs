// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.UIElements.Layout;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.UIElements
{
    // Unmanaged mirror of BackgroundGradientStop. C++ counterpart in BackgroundTypes.h.
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    [StructLayout(LayoutKind.Sequential, Size = 24)]
    internal struct UnmanagedBackgroundGradientStop : IEquatable<UnmanagedBackgroundGradientStop>
    {
        public Color color;             // 16 bytes
        public float position;          //  4
        public int positionIsPercent;   //  4  (treat as bool; int for unmanaged + C++ parity)

        public static implicit operator UnmanagedBackgroundGradientStop(BackgroundGradientStop s)
        {
            return new UnmanagedBackgroundGradientStop
            {
                color = s.color,
                position = s.position,
                positionIsPercent = s.positionIsPercent ? 1 : 0,
            };
        }

        public BackgroundGradientStop ToManaged()
        {
            return new BackgroundGradientStop
            {
                color = color,
                position = position,
                positionIsPercent = positionIsPercent != 0,
            };
        }

        public bool Equals(UnmanagedBackgroundGradientStop other)
        {
            return color == other.color
                && position.Equals(other.position)
                && positionIsPercent == other.positionIsPercent;
        }

        public override bool Equals(object obj) => obj is UnmanagedBackgroundGradientStop o && Equals(o);

        public override int GetHashCode()
        {
            var h = color.GetHashCode();
            h = (h * -1521134295) + position.GetHashCode();
            h = (h * -1521134295) + positionIsPercent.GetHashCode();
            return h;
        }
    }

    // Unmanaged mirror of BackgroundGradient. POD with up to MaxStops inline stops; overflow
    // truncates with a warning. C++ counterpart in BackgroundTypes.h.
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    [StructLayout(LayoutKind.Sequential)]
    internal struct UnmanagedBackgroundGradient : IEquatable<UnmanagedBackgroundGradient>
    {
        public const int MaxStops = 4;

        // One-shot per session to avoid flooding the console when a stylesheet with a
        // >MaxStops gradient is applied to many elements.
        [NoAutoStaticsCleanup] // one-shot warning flag; safe to persist across reload
        static bool s_WarnedTruncatedStops;

        public GradientType type;                                       //  4
        public BackgroundGradientShape shape;                           //  4
        public BackgroundGradientSize size;                             //  4
        public float angle;                                             //  4
        public Vector2 position;                                        //  8
        public int stopCount;                                           //  4
        int __padding;                                                  //  4  (8-byte align before stops)
        public FixedBuffer4<UnmanagedBackgroundGradientStop> stops;     // 96

        public bool IsEmpty => stopCount <= 0;

        public static implicit operator UnmanagedBackgroundGradient(BackgroundGradient g)
        {
            var u = new UnmanagedBackgroundGradient
            {
                type = g.type,
                shape = g.shape,
                size = g.size,
                angle = g.angle,
                position = g.position,
                stopCount = 0,
            };

            var src = g.stops;
            if (src != null && src.Length > 0)
            {
                int n = src.Length;
                if (n > MaxStops)
                {
                    if (!s_WarnedTruncatedStops)
                    {
                        s_WarnedTruncatedStops = true;
                        Debug.LogWarning(
                            $"BackgroundGradient has {n} stops; only the first {MaxStops} are kept. " +
                            $"Reduce the stop count to silence this warning.");
                    }
                    n = MaxStops;
                }
                for (int i = 0; i < n; ++i)
                    u.stops[i] = src[i];
                u.stopCount = n;
            }

            return u;
        }

        public BackgroundGradient ToManaged()
        {
            if (stopCount <= 0)
                return default;

            var managedStops = new BackgroundGradientStop[stopCount];
            for (int i = 0; i < stopCount; ++i)
                managedStops[i] = stops[i].ToManaged();

            return new BackgroundGradient
            {
                type = type,
                shape = shape,
                size = size,
                angle = angle,
                position = position,
                stops = managedStops,
            };
        }

        public static implicit operator BackgroundGradient(UnmanagedBackgroundGradient u) => u.ToManaged();

        public bool Equals(UnmanagedBackgroundGradient other)
        {
            if (type != other.type) return false;
            if (shape != other.shape) return false;
            if (size != other.size) return false;
            if (!angle.Equals(other.angle)) return false;
            if (!position.Equals(other.position)) return false;
            if (stopCount != other.stopCount) return false;
            for (int i = 0; i < stopCount; ++i)
            {
                if (!stops[i].Equals(other.stops[i])) return false;
            }
            return true;
        }

        public override bool Equals(object obj) => obj is UnmanagedBackgroundGradient o && Equals(o);

        public override int GetHashCode()
        {
            var h = type.GetHashCode();
            h = (h * -1521134295) + shape.GetHashCode();
            h = (h * -1521134295) + size.GetHashCode();
            h = (h * -1521134295) + angle.GetHashCode();
            h = (h * -1521134295) + position.GetHashCode();
            for (int i = 0; i < stopCount; ++i)
                h = (h * -1521134295) + stops[i].GetHashCode();
            return h;
        }

        public static bool operator ==(UnmanagedBackgroundGradient a, UnmanagedBackgroundGradient b) => a.Equals(b);
        public static bool operator !=(UnmanagedBackgroundGradient a, UnmanagedBackgroundGradient b) => !a.Equals(b);

        // Cross-type ops resolve the ambiguity from the implicit conversions in both directions.
        public static bool operator ==(UnmanagedBackgroundGradient a, BackgroundGradient b) => a.Equals((UnmanagedBackgroundGradient)b);
        public static bool operator !=(UnmanagedBackgroundGradient a, BackgroundGradient b) => !a.Equals((UnmanagedBackgroundGradient)b);
        public static bool operator ==(BackgroundGradient a, UnmanagedBackgroundGradient b) => ((UnmanagedBackgroundGradient)a).Equals(b);
        public static bool operator !=(BackgroundGradient a, UnmanagedBackgroundGradient b) => !((UnmanagedBackgroundGradient)a).Equals(b);
    }
}
