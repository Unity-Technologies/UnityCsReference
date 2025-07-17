// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Profile.Internal
{
    /// <summary>
    /// Settings dropdown data provider. Unique key is passed to selection callback.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
    internal interface IAddSettingsDataProvider
    {
        IEnumerable<(int key, string displayName)> FetchSettings();
    }
}
