// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor;

internal interface IVRPlatformProperties : IPlatformProperties
{
    bool SupportSinglePassStereoRendering => false;
    bool SupportStereoInstancingRendering => false;
    bool SupportStereoMultiviewRendering  => false;
    bool SupportStereo360Capture          => false;
}
