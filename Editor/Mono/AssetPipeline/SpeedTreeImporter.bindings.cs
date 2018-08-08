// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/AssetPipeline/SpeedTreeImporter.h")]
    [NativeHeader("Editor/Src/AssetPipeline/SpeedTreeImporter.bindings.h")]
    [NativeHeader("Runtime/Camera/ReflectionProbeTypes.h")]
    public partial class SpeedTreeImporter : AssetImporter
    {
        public enum MaterialLocation
        {
            External = 0,
            InPrefab = 1
        }

        public extern bool hasImported
        {
            [FreeFunction(Name = "SpeedTreeImporterBindings::HasImported", HasExplicitThis = true)]
            get;
        }

        public extern string materialFolderPath
        {
            get;
        }

        public extern MaterialLocation materialLocation
        {
            get;
            set;
        }

        public extern bool isV8
        {
            get;
        }

        /////////////////////////////////////////////////////////////////////////////
        // Mesh properties

        public extern float scaleFactor { get; set; }


        /////////////////////////////////////////////////////////////////////////////
        // Common material properties

        public extern Color mainColor { get; set; }

        // The below properties (specColor and shininess) were first made obsolete in 5.4, they didn't work anyway, AND SpeedTreeImporter should rarely be scripted by anyone
        // because of that I would say they can be safely removed for 5.6
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("specColor is no longer used and has been deprecated.", true)]
        public Color specColor {  get; set; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("shininess is no longer used and has been deprecated.", true)]
        public float shininess {  get; set; }

        public extern Color hueVariation { get; set; }
        public extern float alphaTestRef { get; set; }


        /////////////////////////////////////////////////////////////////////////////
        // LOD settings

        public extern bool hasBillboard
        {
            [NativeName("HasBillboard")]
            get;
        }

        public extern bool enableSmoothLODTransition { get; set; }
        public extern bool animateCrossFading { get; set; }
        public extern float billboardTransitionCrossFadeWidth { get; set; }
        public extern float fadeOutWidth { get; set; }

        public extern float[] LODHeights
        {
            [FreeFunction(Name = "SpeedTreeImporterBindings::GetLODHeights", HasExplicitThis = true)]
            get;
            [NativeThrows]
            [FreeFunction(Name = "SpeedTreeImporterBindings::SetLODHeights", HasExplicitThis = true)]
            set;
        }

        public extern bool[] castShadows
        {
            [FreeFunction(Name = "SpeedTreeImporterBindings::GetCastShadows", HasExplicitThis = true)]
            get;
            [NativeThrows]
            [FreeFunction(Name = "SpeedTreeImporterBindings::SetCastShadows", HasExplicitThis = true)]
            set;
        }

        public extern bool[] receiveShadows
        {
            [FreeFunction(Name = "SpeedTreeImporterBindings::GetReceiveShadows", HasExplicitThis = true)]
            get;
            [NativeThrows]
            [FreeFunction(Name = "SpeedTreeImporterBindings::SetReceiveShadows", HasExplicitThis = true)]
            set;
        }

        public extern bool[] useLightProbes
        {
            [FreeFunction(Name = "SpeedTreeImporterBindings::GetUseLightProbes", HasExplicitThis = true)]
            get;
            [NativeThrows]
            [FreeFunction(Name = "SpeedTreeImporterBindings::SetUseLightProbes", HasExplicitThis = true)]
            set;
        }

        public extern ReflectionProbeUsage[] reflectionProbeUsages
        {
            [FreeFunction(Name = "SpeedTreeImporterBindings::GetReflectionProbeUsages", HasExplicitThis = true)]
            get;
            [NativeThrows]
            [FreeFunction(Name = "SpeedTreeImporterBindings::SetReflectionProbeUsages", HasExplicitThis = true)]
            set;
        }

        public extern bool[] enableBump
        {
            [FreeFunction(Name = "SpeedTreeImporterBindings::GetEnableBump", HasExplicitThis = true)]
            get;
            [NativeThrows]
            [FreeFunction(Name = "SpeedTreeImporterBindings::SetEnableBump", HasExplicitThis = true)]
            set;
        }

        public extern bool[] enableHue
        {
            [FreeFunction(Name = "SpeedTreeImporterBindings::GetEnableHue", HasExplicitThis = true)]
            get;
            [NativeThrows]
            [FreeFunction(Name = "SpeedTreeImporterBindings::SetEnableHue", HasExplicitThis = true)]
            set;
        }

        public extern bool[] enableSubsurface
        {
            [FreeFunction(Name = "SpeedTreeImporterBindings::GetEnableSubsurface", HasExplicitThis = true)]
            get;
            [NativeThrows]
            [FreeFunction(Name = "SpeedTreeImporterBindings::SetEnableSubsurface", HasExplicitThis = true)]
            set;
        }

        public static readonly string[] windQualityNames = new[] { "None", "Fastest", "Fast", "Better", "Best", "Palm" };

        public extern int bestWindQuality { get; }

        public extern int[] windQualities
        {
            [FreeFunction(Name = "SpeedTreeImporterBindings::GetWindQuality", HasExplicitThis = true)]
            get;
            [NativeThrows]
            [FreeFunction(Name = "SpeedTreeImporterBindings::SetWindQuality", HasExplicitThis = true)]
            set;
        }

        /////////////////////////////////////////////////////////////////////////////

        public extern void GenerateMaterials();

        internal extern bool materialsShouldBeRegenerated
        {
            [NativeName("MaterialsShouldBeRegenerated")]
            get;
        }

        internal extern void SetMaterialVersionToCurrent();

        internal extern SourceAssetIdentifier[] sourceMaterials
        {
            [FreeFunction(Name = "SpeedTreeImporterBindings::GetSourceMaterials", HasExplicitThis = true)]
            get;
        }

        public bool SearchAndRemapMaterials(string materialFolderPath)
        {
            bool changedMappings = false;

            if (materialFolderPath == null)
                throw new ArgumentNullException("materialFolderPath");

            if (string.IsNullOrEmpty(materialFolderPath))
                throw new ArgumentException(string.Format("Invalid material folder path: {0}.", materialFolderPath), "materialFolderPath");

            var guids = AssetDatabase.FindAssets("t:Material", new string[] { materialFolderPath });
            List<Tuple<string, Material>> materials = new List<Tuple<string, Material>>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                // ensure that we only load material assets, not embedded materials
                var material = AssetDatabase.LoadMainAssetAtPath(path) as Material;
                if (material)
                    materials.Add(new Tuple<string, Material>(path, material));
            }

            var importedMaterials = sourceMaterials;
            foreach (var material in materials)
            {
                var materialName = material.Item2.name;
                var materialFile = material.Item1;

                // the legacy materials have the LOD in the path, while the new materials have the LOD as part of the name
                var isLegacyMaterial = !materialName.Contains("LOD") && !materialName.Contains("Billboard");
                var hasLOD = isLegacyMaterial && materialFile.Contains("LOD");
                var lod = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(materialFile));
                var importedMaterial = Array.Find(importedMaterials, x => x.name.Contains(materialName) && (!hasLOD || x.name.Contains(lod)));

                if (!string.IsNullOrEmpty(importedMaterial.name))
                {
                    AddRemap(importedMaterial, material.Item2);
                    changedMappings = true;
                }
            }

            return changedMappings;
        }
    }
}
