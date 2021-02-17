// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/AssetBundle/Public/AssetBundleLoadingCache.h")]
    static class AssetBundleLoadingCache
    {
        internal const int kMinAllowedBlockCount = 2;
        internal const int kMinAllowedMaxBlocksPerFile = 2;
        internal static extern uint maxBlocksPerFile { get; set; }
        internal static extern uint blockCount { get; set; }
        internal static extern uint blockSize { get; }

        internal static uint memoryBudgetKB
        {
            get
            {
                return blockCount * blockSize;
            }
            set
            {
                uint newBlockCount = Math.Max(value / blockSize, kMinAllowedBlockCount);
                uint newMaxBlocksPerFile = Math.Max(blockCount / 4, kMinAllowedMaxBlocksPerFile);
                if (newBlockCount != blockCount || newMaxBlocksPerFile != maxBlocksPerFile)
                {
                    blockCount = newBlockCount;
                    maxBlocksPerFile = newMaxBlocksPerFile;
                }
            }
        }
    }
}
