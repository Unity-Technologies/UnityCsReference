// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor;

internal interface IUIPlatformProperties : IPlatformProperties
{
    // The GameViewSizes.BuildTargetGroupToGameViewSizeGroup method uses this property to set that class's GameViewSizeGroup
    // static variable.
    GameViewSizeGroupType GameViewSizeGroupType => GameViewSizeGroupType.Standalone;
}
