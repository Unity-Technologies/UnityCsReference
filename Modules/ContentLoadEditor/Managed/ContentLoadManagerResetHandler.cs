// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using Unity.Loading;
using UnityEngine.Scripting;

namespace UnityEditor
{
    /// <summary>
    /// Editor-only: When native requests a reset (domain reload / build), unregisters all content directories
    /// by querying GetContentDirectories() and calling UnregisterContentDirectory for each. Single source of truth.
    /// Called from native via [RequiredByNativeCode].
    /// </summary>
    internal static class ContentLoadManagerResetHandler
    {
        /// <summary>
        /// Called by native code before domain reload or build to unload all managed content directory state.
        /// </summary>
        [RequiredByNativeCode]
        public static void OnContentResetRequested()
        {
            ContentDirectoryHandle[] handles = ContentLoadManager.GetContentDirectories();
            foreach (var handle in handles)
            {
                try
                {
                    ContentLoadManager.UnregisterContentDirectory(handle);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
