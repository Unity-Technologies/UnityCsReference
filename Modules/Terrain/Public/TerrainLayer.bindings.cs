// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [NativeHeader("TerrainScriptingClasses.h")]
    [NativeHeader("Modules/Terrain/Public/TerrainLayerScriptingInterface.h")]
    public sealed partial class TerrainLayer : Object
    {
        public TerrainLayer() { Internal_Create(this); }

        [FreeFunction("TerrainLayerScriptingInterface::Create")]
        extern private static void Internal_Create([Writable] TerrainLayer layer);

        extern public Texture2D diffuseTexture { get; set; }
        extern public Texture2D normalMapTexture { get; set; }
        extern public Texture2D maskMapTexture { get; set; }
        extern public Vector2 tileSize { get; set; }
        extern public Vector2 tileOffset { get; set; }

        [NativeProperty("SpecularColor")] extern public Color specular { get; set; }

        extern public float metallic { get; set; }
        extern public float smoothness { get; set; }
        extern public float normalScale { get; set; }
        extern public Vector4 diffuseRemapMin { get; set; }
        extern public Vector4 diffuseRemapMax { get; set; }
        extern public Vector4 maskMapRemapMin { get; set; }
        extern public Vector4 maskMapRemapMax { get; set; }
    }
}
