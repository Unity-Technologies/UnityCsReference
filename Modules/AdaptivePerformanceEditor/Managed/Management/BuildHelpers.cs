// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.AdaptivePerformance.Editor
{
    internal static class BuildHelpers
    {
        internal static void CleanOldSettings<T>()
        {
            Object[] preloadedAssets = PlayerSettings.GetPreloadedAssets();
            if (preloadedAssets == null)
                return;

            List<Object> curSettings = new List<Object>();

            foreach (Object asset in preloadedAssets)
            {
                if (asset != null && asset.GetType() != typeof(T))
                {
                    curSettings.Add(asset);
                }
            }

            if (curSettings.Count != preloadedAssets.Length)
            {
                PlayerSettings.SetPreloadedAssets(curSettings.ToArray());
            }
        }
    }
}
