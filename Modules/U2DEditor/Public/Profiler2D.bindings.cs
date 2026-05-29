// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Profiling;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Serialization;

namespace UnityEditor.U2D
{

    [System.Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct SpriteAtlasProfilerInfo
    {
        public EntityId assetEntityId;
        public int assetGuidOffset;
    }

    [System.Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct SpriteAtlasUsageProfilerInfo
    {
        public EntityId atlasEntityId;
        public EntityId spriteEntityId;
        public EntityId textureEntityId;
        public float spriteTextureSizeRatio;
    }

    [System.Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct TilemapChunkInfo
    {
        public EntityId tilemapEntityId;
        public int chunkIdX;
        public int chunkIdY;
        public int meshCount;
    }

    internal static partial class Profiler2D
    {
        /// <summary>
        /// GUID for the 2D Profile Module definition. Category | Protocol Major | Minor
        /// </summary>
        internal static readonly Guid kProfilerU2D = new Guid(UnityEngine.U2D.Profiler2D.GetGUIDString());

        // Extract from Raw Data View
        internal static NativeArray<SpriteAtlasProfilerInfo> GetSpriteAtlasProfilerInfo(RawFrameDataView frameData)
        {
            if (frameData != null)
            {
                var data = frameData.GetFrameMetaData<SpriteAtlasProfilerInfo>(kProfilerU2D, 1);
                return data;
            }
            return new NativeArray<SpriteAtlasProfilerInfo>(0, Allocator.Temp);
        }

        // Extract from Raw Data View
        internal static NativeArray<SpriteAtlasUsageProfilerInfo> GetSpriteAtlasUsageProfilerInfo(RawFrameDataView frameData)
        {
            if (frameData != null)
            {
                var data = frameData.GetFrameMetaData<SpriteAtlasUsageProfilerInfo>(kProfilerU2D, 2);
                return data;
            }
            return new NativeArray<SpriteAtlasUsageProfilerInfo>(0, Allocator.Temp);
        }

        // Extract from Raw Data View
        internal static NativeArray<TilemapChunkInfo> GetTilemapChunkInfo(RawFrameDataView frameData)
        {
            if (frameData != null)
            {
                var data = frameData.GetFrameMetaData<TilemapChunkInfo>(kProfilerU2D, 3);
                return data;
            }
            return new NativeArray<TilemapChunkInfo>(0, Allocator.Temp);
        }

        // Extract from Raw Data View
        internal static string GetSpriteStringByBlobOffset(RawFrameDataView frameData, int offset)
        {
            var returnVal = new string("");
            if (frameData != null)
            {
                var data = frameData.GetFrameMetaData<byte>(kProfilerU2D, 0);
                if (data.Length != 0 && offset < data.Length)
                {
                    int j = 0;
                    var stringBytes = new byte[data.Length];
                    for (int i = offset; i < data.Length && data[i] != 0; i++)
                        stringBytes[j++] = data[i];
                    stringBytes[j] = 0;
                    returnVal = System.Text.Encoding.Default.GetString(stringBytes,0,j);
                }
            }
            return returnVal;
        }

    };

}
