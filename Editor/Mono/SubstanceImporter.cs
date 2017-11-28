// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// SubstanceImporter C# class description
// SUBSTANCE HOOK

using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.IO;

#pragma warning disable CS0618  // Due to Obsolete attribute on Predural classes

namespace UnityEditor
{
    // Material informations for Procedural Material textures generation
    // I have hidden this odd implementation detail from the public interface.
    struct ScriptingProceduralMaterialInformation
    {
        // Offset and scaling
        Vector2 m_TextureOffsets;
        Vector2 m_TextureScales;
        int     m_GenerateAllOutputs;
        int     m_AnimationUpdateRate;
        bool    m_GenerateMipMaps;

        // Gets or sets the texture offsets
        public Vector2 offset { get { return m_TextureOffsets; } set { m_TextureOffsets = value; } }

        // Gets or sets the texture scale factors
        public Vector2 scale { get { return m_TextureScales; } set { m_TextureScales = value; } }

        // Gets or sets if all the outputs are generated independently of the current shader
        public bool generateAllOutputs { get { return m_GenerateAllOutputs != 0; } set { m_GenerateAllOutputs = value ? 1 : 0; } }

        // Gets or sets the animation update rate in millisecond
        public int animationUpdateRate { get { return m_AnimationUpdateRate; } set { m_AnimationUpdateRate = value; } }

        // Gets or sets the mipmap generation mode
        public bool generateMipMaps { get { return m_GenerateMipMaps; } set { m_GenerateMipMaps = value; } }
    }

    // Substance importer lets you access the imported Procedural Material instances.
    public partial class SubstanceImporter : AssetImporter
    {
        // Get the material offset, which is used for all the textures that are part of this Procedural Material.
        public Vector2 GetMaterialOffset(ProceduralMaterial material)
        {
            if (material == null) throw new ArgumentException("Invalid ProceduralMaterial");
            return GetMaterialInformation(material).offset;
        }

        // Set the material offset, which is used for all the textures that are part of this Procedural Material.
        public void SetMaterialOffset(ProceduralMaterial material, Vector2 offset)
        {
            if (material == null) throw new ArgumentException("Invalid ProceduralMaterial");
            ScriptingProceduralMaterialInformation information = GetMaterialInformation(material);
            information.offset = offset;
            SetMaterialInformation(material, information);
        }

        // Get the material scale, which is used for all the textures that are part of this Procedural Material.
        public Vector2 GetMaterialScale(ProceduralMaterial material)
        {
            if (material == null) throw new ArgumentException("Invalid ProceduralMaterial");
            return GetMaterialInformation(material).scale;
        }

        // Set the material scale, which is used for all the textures that are part of this Procedural Material.
        public void SetMaterialScale(ProceduralMaterial material, Vector2 scale)
        {
            if (material == null) throw new ArgumentException("Invalid ProceduralMaterial");
            ScriptingProceduralMaterialInformation information = GetMaterialInformation(material);
            information.scale = scale;
            SetMaterialInformation(material, information);
        }

        // Checks if the Procedural Material need to force generation of all its outputs.
        public bool GetGenerateAllOutputs(ProceduralMaterial material)
        {
            if (material == null) throw new ArgumentException("Invalid ProceduralMaterial");
            return GetMaterialInformation(material).generateAllOutputs;
        }

        // Specifies if the Procedural Material need to force generation of all its outputs.
        public void SetGenerateAllOutputs(ProceduralMaterial material, bool generated)
        {
            if (material == null) throw new ArgumentException("Invalid ProceduralMaterial");
            ScriptingProceduralMaterialInformation information = GetMaterialInformation(material);
            information.generateAllOutputs = generated;
            SetMaterialInformation(material, information);
        }

        // Get the Procedural Material animation update rate in millisecond.
        public int GetAnimationUpdateRate(ProceduralMaterial material)
        {
            if (material == null) throw new ArgumentException("Invalid ProceduralMaterial");
            return GetMaterialInformation(material).animationUpdateRate;
        }

        // Set the Procedural Material animation update rate in millisecond.
        public void SetAnimationUpdateRate(ProceduralMaterial material, int animation_update_rate)
        {
            if (material == null) throw new ArgumentException("Invalid ProceduralMaterial");
            ScriptingProceduralMaterialInformation information = GetMaterialInformation(material);
            information.animationUpdateRate = animation_update_rate;
            SetMaterialInformation(material, information);
        }

        // Returns whether mipmaps are generated for the given material
        public bool GetGenerateMipMaps(ProceduralMaterial material)
        {
            if (material == null) throw new ArgumentException("Invalid ProceduralMaterial");
            return GetMaterialInformation(material).generateMipMaps;
        }

        // Sets the Mipmap generation mode for the given material
        public void SetGenerateMipMaps(ProceduralMaterial material, bool mode)
        {
            if (material == null) throw new ArgumentException("Invalid ProceduralMaterial");
            ScriptingProceduralMaterialInformation information = GetMaterialInformation(material);
            information.generateMipMaps = mode;
            SetMaterialInformation(material, information);
        }

        // Compares a texture and a property name and returns true if the texture is a ProceduralTexture locked to that property
        internal static bool IsProceduralTextureSlot(Material material, Texture tex, string name)
        {
            return (material is ProceduralMaterial && tex is ProceduralTexture
                    && CanShaderPropertyHostProceduralOutput(name, ((tex as ProceduralTexture).GetProceduralOutputType()))
                    && SubstanceImporter.IsSubstanceParented(tex as ProceduralTexture, material as ProceduralMaterial));
        }

        public void ExportBitmaps(ProceduralMaterial material, string exportPath, bool alphaRemap)
        {
            if (material == null) throw new ArgumentException("Invalid ProceduralMaterial");
            if (exportPath == "") throw new ArgumentException("Invalid export path specified");

            DirectoryInfo dirInfo = Directory.CreateDirectory(exportPath);
            if (!dirInfo.Exists) throw new ArgumentException("Export folder " + exportPath + " doesn't exist and cannot be created.");

            ExportBitmapsInternal(material, exportPath, alphaRemap);
        }

        public void ExportPreset(ProceduralMaterial material, string exportPath)
        {
            if (material == null) throw new ArgumentException("Invalid ProceduralMaterial");
            if (exportPath == "") throw new ArgumentException("Invalid export path specified");

            DirectoryInfo dirInfo = Directory.CreateDirectory(exportPath);
            if (!dirInfo.Exists) throw new ArgumentException("Export folder " + exportPath + " doesn't exist and cannot be created.");

            File.WriteAllText(Path.Combine(exportPath, material.name + ".sbsprs"), material.preset);
        }
    }
} // namespace UnityEditor

#pragma warning restore CS0618  // Due to Obsolete attribute on Predural classes
