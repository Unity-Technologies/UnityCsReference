// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Localization;

[VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
sealed class StartupSelectorsAttribute : PropertyAttribute
{
    public StartupSelectorsAttribute() : base(applyToCollection: true)
    {
    }
}
