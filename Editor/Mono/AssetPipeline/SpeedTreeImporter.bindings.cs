// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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
        public extern bool hasImported
        {
            [FreeFunction(Name = "SpeedTreeImporterBindings::HasImported", HasExplicitThis = true)]
            get;
        }

        public extern string materialFolderPath
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
        [Obsolete("specColor is no longer used and has been deprecated.", true)]
        public Color specColor {  get; set; }

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
    }
}
