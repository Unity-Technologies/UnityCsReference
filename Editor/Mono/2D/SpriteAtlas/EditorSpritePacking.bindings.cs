// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEditor.U2D.SpritePacking
{
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct SpritePackInfoInternal
    {
        public int guid;
        public int texIndex;
        public int indexCount;
        public int vertexCount;
        public RectInt rect;
        public IntPtr indices;
        public IntPtr vertices;
    };

    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct SpritePackTextureInfoInternal
    {
        public int width;
        public int height;
        public IntPtr buffer;
    };

    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct SpritePackDatasetInternal
    {
        public SpritePackInfoInternal spriteData;
        public SpritePackTextureInfoInternal textureData;
    };

    [StructLayout(LayoutKind.Sequential)]
    internal struct SpritePackInfo
    {
        public int guid;
        public int texIndex;
        public int indexCount;
        public int vertexCount;
        public RectInt rect;
        public NativeArray<int> indices;
        public NativeArray<Vector3> vertices;
    };

    [StructLayout(LayoutKind.Sequential)]
    internal struct SpritePackTextureInfo
    {
        public int width;
        public int height;
        public NativeArray<Color32> buffer;
    };

    internal struct SpritePackDataset
    {
        public List<SpritePackInfo> spriteData;
        public List<SpritePackTextureInfo> textureData;
    };

    internal struct SpritePackConfig
    {
        public int padding;
    };

    [NativeHeader("Runtime/2D/SpriteAtlas/SpriteAtlas.h")]
    [NativeHeader("Editor/Src/2D/SpriteAtlas/SpriteAtlasPackingUtilities.h")]
    internal class SpritePackUtility
    {
        internal unsafe static SpritePackDataset PackCustomSpritesWrapper(SpritePackDataset input, SpritePackConfig packConfig, Allocator alloc)
        {
            var output = new SpritePackDataset();
            var spriteCount = input.spriteData.Count;
            if (0 == spriteCount)
                return output;

            var data = new NativeArray<SpritePackDatasetInternal>(spriteCount, Allocator.Temp, NativeArrayOptions.ClearMemory);
            for (int i = 0; i < spriteCount; ++i)
            {
                SpritePackDatasetInternal rawData = data[i];
                rawData.spriteData.guid = input.spriteData[i].guid;
                int texIndex = input.spriteData[i].texIndex;
                if (texIndex >= input.textureData.Count)
                {
                    data.Dispose();
                    throw new ArgumentOutOfRangeException("texIndex", "texIndex must point to a valid index in textureData list.");
                }
                rawData.spriteData.texIndex = texIndex;
                rawData.spriteData.indexCount = input.spriteData[i].indexCount;
                rawData.spriteData.vertexCount = input.spriteData[i].vertexCount;
                rawData.spriteData.rect = input.spriteData[i].rect;
                rawData.spriteData.indices = input.spriteData[i].indices.IsCreated ? (IntPtr)input.spriteData[i].indices.GetUnsafePtr() : (IntPtr)0;
                rawData.spriteData.vertices = input.spriteData[i].vertices.IsCreated  ? (IntPtr)input.spriteData[i].vertices.GetUnsafePtr() : (IntPtr)0;
                rawData.textureData.width = input.textureData[texIndex].width;
                rawData.textureData.height = input.textureData[texIndex].height;
                rawData.textureData.buffer = input.textureData[texIndex].buffer.IsCreated ? (IntPtr)input.textureData[texIndex].buffer.GetUnsafePtr() : (IntPtr)0;
                data[i] = rawData;
            }

            var spriteOutput = (SpritePackDatasetInternal*)PackCustomSpritesInternal(spriteCount, (SpritePackDatasetInternal*)data.GetUnsafePtr(), packConfig);
            if (null != spriteOutput)
            {
                var colorBufferArray = new SpritePackTextureInfo[spriteCount];
                for (int i = 0; i < spriteCount; ++i)
                {
                    SpritePackTextureInfoInternal rawBuffer = spriteOutput[i].textureData;
                    int index = spriteOutput[i].spriteData.texIndex;
                    SpritePackTextureInfo outputBuffer = colorBufferArray[index];
                    // New Texture. Copy.
                    if (!outputBuffer.buffer.IsCreated)
                    {
                        outputBuffer.width = rawBuffer.width;
                        outputBuffer.height = rawBuffer.height;
                        Color32* rawColor = (Color32*)rawBuffer.buffer;
                        if (null != rawColor)
                        {
                            outputBuffer.buffer = new NativeArray<Color32>(rawBuffer.width * rawBuffer.height, alloc);
                            UnsafeUtility.MemCpy(outputBuffer.buffer.GetUnsafePtr(), rawColor, rawBuffer.width * rawBuffer.height * sizeof(Color32));
                        }
                        UnsafeUtility.Free((void*)rawBuffer.buffer, Allocator.Persistent);
                    }
                    colorBufferArray[index] = outputBuffer;
                }
                output.textureData = new List<SpritePackTextureInfo>(colorBufferArray);

                var spriteDataArray = new SpritePackInfo[spriteCount];
                for (int i = 0; i < spriteCount; ++i)
                {
                    SpritePackInfo spriteData = spriteDataArray[i];
                    spriteData.guid = spriteOutput[i].spriteData.guid;
                    spriteData.indexCount = spriteOutput[i].spriteData.indexCount;
                    spriteData.vertexCount = spriteOutput[i].spriteData.vertexCount;
                    spriteData.rect = spriteOutput[i].spriteData.rect;
                    if (0 != spriteData.indexCount && 0 != spriteData.vertexCount)
                    {
                        int* rawIndices = (int*)spriteOutput[i].spriteData.indices;
                        if (null != rawIndices)
                        {
                            spriteData.indices = new NativeArray<int>(spriteOutput[i].spriteData.indexCount, alloc);
                            UnsafeUtility.MemCpy(spriteData.indices.GetUnsafePtr(), rawIndices, spriteOutput[i].spriteData.indexCount * sizeof(int));
                        }
                        Vector3* rawVertices = (Vector3*)spriteOutput[i].spriteData.vertices;
                        if (null != rawVertices)
                        {
                            spriteData.vertices = new NativeArray<Vector3>(spriteOutput[i].spriteData.vertexCount, alloc);
                            UnsafeUtility.MemCpy(spriteData.vertices.GetUnsafePtr(), rawVertices, spriteOutput[i].spriteData.vertexCount * sizeof(Vector3));
                        }
                        UnsafeUtility.Free((void*)spriteOutput[i].spriteData.indices, Allocator.Persistent);
                        UnsafeUtility.Free((void*)spriteOutput[i].spriteData.vertices, Allocator.Persistent);
                    }
                    spriteData.texIndex = spriteOutput[i].spriteData.texIndex;
                    spriteDataArray[i] = spriteData;
                }
                output.spriteData = new List<SpritePackInfo>(spriteDataArray);
                UnsafeUtility.Free((void*)spriteOutput, Allocator.Persistent);
            }

            data.Dispose();
            return output;
        }

        internal static SpritePackDataset PackCustomSprites(SpritePackDataset spriteDataInput, SpritePackConfig packConfig, Allocator outputAlloc)
        {
            return PackCustomSpritesWrapper(spriteDataInput, packConfig, outputAlloc);
        }

        [NativeThrows]
        [FreeFunction("PackCustomSprites")]
        extern internal unsafe static IntPtr PackCustomSpritesInternal(int spriteCount, SpritePackDatasetInternal* data, SpritePackConfig packConfig);
    }
}
