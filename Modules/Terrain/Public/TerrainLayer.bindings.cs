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
        private const string k_ScriptingInterfaceName = "TerrainLayerScriptingInterface";
        private const string k_ScriptingInterfacePrefix = k_ScriptingInterfaceName + "::";

        public TerrainLayer() { Internal_Create(this); }

        [FreeFunction(k_ScriptingInterfacePrefix + "Create")]
        extern private static void Internal_Create([Writable] TerrainLayer layer);

        extern public Texture2D diffuseTexture
        {
            [NativeName("GetDiffuseTexture")]
            get;
            [NativeName("SetDiffuseTexture")]
            set;
        }

        extern public Texture2D normalMapTexture
        {
            [NativeName("GetNormalMapTexture")]
            get;
            [NativeName("SetNormalMapTexture")]
            set;
        }

        extern public Vector2 tileSize
        {
            [NativeName("GetTileSize")]
            get;
            [NativeName("SetTileSize")]
            set;
        }

        extern public Vector2 tileOffset
        {
            [NativeName("GetTileOffset")]
            get;
            [NativeName("SetTileOffset")]
            set;
        }

        extern public Color specular
        {
            [NativeName("GetSpecularColor")]
            get;
            [NativeName("SetSpecularColor")]
            set;
        }

        extern public float metallic
        {
            [NativeName("GetMetallic")]
            get;
            [NativeName("SetMetallic")]
            set;
        }

        extern public float smoothness
        {
            [NativeName("GetSmoothness")]
            get;
            [NativeName("SetSmoothness")]
            set;
        }
    }
}
