// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Localization;

static class ResourceRelease
{
    /// <summary>
    /// Unloads a <c>Resources</c>-loaded asset, skipping <see cref="GameObject"/> and <see cref="Component"/> which <see cref="Resources.UnloadAsset"/> rejects.
    /// </summary>
    /// <param name="asset">The asset to unload; <see langword="null"/> is ignored.</param>
    public static void Unload(Object asset)
    {
        if (asset != null && asset is not GameObject && asset is not Component)
            Resources.UnloadAsset(asset);
    }
}
