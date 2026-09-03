// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace Unity.Localization;

/// <summary>
/// Selects a variant by the current <see cref="Application.platform"/>.
/// </summary>
[Serializable]
internal class PlatformVariantSelector : IVariantSelector
{
    static readonly string k_CurrentPlatform = Application.platform.ToString();
    [NoAutoStaticsCleanup] // cached enum names; the same value for the life of the process
    static string[] s_AvailableKeys;

    /// <inheritdoc/>
    public string SelectorId => "platform";

    /// <inheritdoc/>
    public string CurrentKey => k_CurrentPlatform;

    /// <inheritdoc/>
    public IEnumerable<string> AvailableKeys => s_AvailableKeys ??= Enum.GetNames(typeof(RuntimePlatform));
}
