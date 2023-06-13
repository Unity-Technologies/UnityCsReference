// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/GI/InteractiveLightBaking.Bindings.h")]
    internal class InteractiveLightBaking
    {
        public struct InteractiveLightmapData
        {
            public Int32 numLightmaps;
            public Int32 lightmapsMode;
            public Vector4 decodeValues;
            public Texture2D[] lightmaps;
            public Texture2D[] directionalities;
            public Texture2D[] shadowmasks;
            public LightmapData[] ToLightmapData()
            {
                LightmapData[] lightmapData = new LightmapData[numLightmaps];
                for (int i = 0; i < numLightmaps; i++)
                {
                    lightmapData[i] = new LightmapData();
                    lightmapData[i].lightmapColor = lightmaps[i];
                    lightmapData[i].lightmapDir = directionalities[i];
                    lightmapData[i].shadowMask = shadowmasks[i];
                }

                return lightmapData;
            }
        }

        [FreeFunction]
        public static extern InteractiveLightmapData GetLightmapData();
        [FreeFunction]
        public static extern bool IsBakingDone();
        [FreeFunction]
        [NativeName("GetInteractiveLightingSettings")]
        public static extern LightingSettings GetLightingSettings();
    }
}
