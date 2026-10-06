// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // NOTE: keep in sync with TerrainLayer::SmoothnessSource in TerrainLayer.h
    ///<summary>Source of smoothness value used in the underlying splat material of a <see cref="TerrainLayer" /> when <see cref="TerrainLayer.diffuseTexture" /> has an alpha channel.</summary>
    ///<remarks>When <see cref="TerrainLayer.diffuseTexture" /> has an alpha channel, you can set smoothness by <see cref="TerrainLayer.smoothness" /> or through the alpha channel of the diffuse texture.</remarks>
    public enum TerrainLayerSmoothnessSource
    {
        ///<summary>Smoothness value comes from a mix of <see cref="TerrainLayer.smoothness" /> and the alpha channel of <see cref="TerrainLayer.diffuseTexture" /></summary>
        [InspectorName("Constant * Diffuse Alpha")]
        ConstantMultipliedByDiffuseAlpha = 0,

        ///<summary>Smoothness value comes from the alpha channel of <see cref="TerrainLayer.diffuseTexture" />.</summary>
        [InspectorName("Diffuse Alpha Channel")]
        DiffuseAlphaChannel = 1,

        ///<summary>Smoothness value comes from <see cref="TerrainLayer.smoothness" />.</summary>
        [InspectorName("Constant Only")]
        ConstantOnly = 2
    }

    ///<summary>Description of a terrain layer.</summary>
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [NativeHeader("TerrainScriptingClasses.h")]
    [NativeHeader("Modules/Terrain/Public/TerrainLayerScriptingInterface.h")]
    [NativeClass("TerrainLayer", PersistentTypeId = 0x746C6179)]
    public sealed partial class TerrainLayer : Object
    {
        internal TerrainLayer(global::UnityEngine.EntityId id) : base(id) {}
        ///<exclude />
        public TerrainLayer() { SetEntityIdFromConstructor(Internal_Create()); }

        [FreeFunction("TerrainLayerScriptingInterface::Create")]
        extern private static EntityId Internal_Create();

        ///<summary>The diffuse texture used by the terrain layer.</summary>
        extern public Texture2D diffuseTexture { get; set; }
        ///<summary>Normal map texture used by the terrain layer.</summary>
        extern public Texture2D normalMapTexture { get; set; }
        ///<summary>The mask map texture used by the terrain layer.</summary>
        ///<remarks>The content of each channel varies by the terrain shader. For HDRP TerrainLit shader, the channels are:
        ///
        ///R: Height - used when doing height based blending
        ///
        ///G: Smoothness
        ///
        ///B: Metallic.</remarks>
        extern public Texture2D maskMapTexture { get; set; }
        ///<summary>UV Tiling size.</summary>
        extern public Vector2 tileSize { get; set; }
        ///<summary>UV tiling offset.</summary>
        extern public Vector2 tileOffset { get; set; }

        ///<summary>Specular color.</summary>
        [NativeProperty("SpecularColor")] extern public Color specular { get; set; }

        ///<summary>Metallic factor used by the terrain layer.</summary>
        ///<remarks>How much does the surface behave like a metallic surface ( 0..1 ).</remarks>
        extern public float metallic { get; set; }
        ///<summary>Smoothness of the specular reflection.</summary>
        ///<remarks>Specifies the surface smoothness : 0..1 where 0 is rough and 1 is mirror polish.</remarks>
        extern public float smoothness { get; set; }
        ///<summary>A float value that scales the normal vector. The minimum value is 0, the maximum value is 1.</summary>
        ///<remarks>Specifying a value of 0 neutrializes the normal vector ((0,0,1) in the tangent space). Specifying a value greater than 1 exaggerates the normal further from the neutral position.</remarks>
        extern public float normalScale { get; set; }
        ///<summary>A Vector4 value specifying the minimum RGBA value that the diffuse texture maps to when the value of the channel is 0.</summary>
        extern public Vector4 diffuseRemapMin { get; set; }
        ///<summary>A Vector4 value specifying the maximum RGBA value that the diffuse texture maps to when the value of the channel is 1.</summary>
        extern public Vector4 diffuseRemapMax { get; set; }
        ///<summary>A Vector4 value specifying the minimum RGBA value that the mask map texture maps to when the value of the channel is 0.</summary>
        extern public Vector4 maskMapRemapMin { get; set; }
        ///<summary>A Vector4 value specifying the maximum RGBA value that the mask map texture maps to when the value of the channel is 1.</summary>
        extern public Vector4 maskMapRemapMax { get; set; }
        ///<summary>Choose the source for smoothness value.</summary>
        ///<remarks>Used for choosing where smoothness value comes from when <see cref="TerrainLayer.diffuseTexture" /> has an alpha channel.
        ///                    Setting to <see cref="TerrainLayerSmoothnessSource.ConstantMultipliedByDiffuseAlpha" /> results in using a mix of the smoothness value set in <see cref="TerrainLayer.smoothness" /> and of the <see cref="TerrainLayer.diffuseTexture" /> alpha channel.
        ///                    Setting to <see cref="TerrainLayerSmoothnessSource.DiffuseAlphaChannel" /> results in using the smoothness value set from the alpha channel of <see cref="TerrainLayer.diffuseTexture" />.
        ///                    Setting to <see cref="TerrainLayerSmoothnessSource.ConstantOnly" /> results in using the smoothness value set in <see cref="TerrainLayer.smoothness" />.</remarks>
        extern public TerrainLayerSmoothnessSource smoothnessSource { get; set; }
    }
}
