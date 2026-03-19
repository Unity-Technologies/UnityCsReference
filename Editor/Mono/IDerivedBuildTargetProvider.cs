// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.Modules;
using UnityEngine;

namespace UnityEditor
{
    using DiscoveredTargetInfo = BuildTargetDiscovery.DiscoveredTargetInfo;

    internal interface IDerivedBuildTargetProvider
    {
        GUID GetBasePlatformGuid();
        IEnumerable<IDerivedBuildTarget> GetDerivedBuildTargets();
        void RemoveDerivedBuildTargetExtension(GUID buildTarget);
        IBuildProfileExtension CreateBuildProfileExtension(GUID buildTarget);
        ISettingEditorExtension CreateSettingsEditorExtension(GUID buildTarget);
        bool TryGetDiscoveredTargetInfo(GUID buildTarget, out DiscoveredTargetInfo discoveredTargetInfo);
    }
}
