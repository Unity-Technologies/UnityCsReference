// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Animations.AnimationWindow.TimelineFoundation
{
    interface ICanvas
    {
        DiscreteTime WorldPixelToTime(float worldPixel, bool ignoreSnapToFrame = false);
        float TimeToWorldPixel(DiscreteTime time);

        float DurationToPixelWidth(DiscreteTime duration);
        DiscreteTime PixelWidthToDuration(float width);

        string ToTimeString(DiscreteTime time);
    }
}
