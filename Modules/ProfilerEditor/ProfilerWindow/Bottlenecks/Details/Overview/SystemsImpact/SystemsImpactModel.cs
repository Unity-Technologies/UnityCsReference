// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.Profiling.Editor.UI
{
    readonly struct SystemsImpactModel
    {
        public SystemsImpactModel(Range frameRange, SystemImpact[] data)
        {
            FrameRange = frameRange;
            Data = data;
        }

        public Range FrameRange { get; }

        public SystemImpact[] Data { get; }

        public readonly struct SystemImpact
        {
            public SystemImpact(
                string name,
                Color color,
                Color colorBlindColor,
                ulong durationNs)
            {
                Name = name;
                Color = color;
                ColorBlindColor = colorBlindColor;
                DurationNs = durationNs;
            }

            public string Name { get; }
            public Color Color { get; }
            public Color ColorBlindColor { get; }
            public ulong DurationNs { get; }
        }
    }
}
