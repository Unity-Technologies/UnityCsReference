// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using UnityEngine.Rendering;

using System.Collections.Generic;
using static UnityEditor.SpeedTree.Importer.SpeedTreeImporterCommon;

namespace UnityEditor.SpeedTree.Importer
{
    [Serializable]
    internal class MeshSettings
    {
        public STUnitConversion unitConversion = STUnitConversion.kFeetToMeters;
        public float scaleFactor = SpeedTreeConstants.kFeetToMetersRatio;
    }

    [Serializable]
    internal class MaterialSettings
    {
        public Color mainColor = Color.white;
        public Color hueVariation = new Color(1.0f, 0.5f, 0.0f, 0.1f);
        public float alphaClipThreshold = 0.10f;
        public float transmissionScale = 0.0f;

        public bool enableHueVariation = false;
        public bool enableBumpMapping = true;
        public bool enableSubsurfaceScattering = true;

        public Vector4 diffusionProfileAssetID = Vector4.zero;
        public uint diffusionProfileID = 0;
    }

    [Serializable]
    internal class LightingSettings
    {
        public bool enableShadowCasting = true;
        public bool enableShadowReceiving = true;
        public bool enableLightProbes = true;
        public ReflectionProbeUsage reflectionProbeEnumValue = ReflectionProbeUsage.BlendProbes;
    }

    [Serializable]
    internal class AdditionalSettings
    {
        public MotionVectorGenerationMode motionVectorModeEnumValue = MotionVectorGenerationMode.Object;
        public bool generateColliders = true;
        public bool generateRigidbody = true;
    }

    [Serializable]
    internal class LODSettings
    {
        public bool enableSmoothLODTransition = true;
        public bool animateCrossFading = true;
        public float billboardTransitionCrossFadeWidth = 0.25f;
        public float fadeOutWidth = 0.25f;
        public bool hasBillboard = false;
    }

    [Serializable]
    internal class PerLODSettings
    {
        public bool enableSettingOverride = false;

        // LOD
        public float height = 0.5f;

        // Lighting
        public bool castShadows = false;
        public bool receiveShadows = false;
        public bool useLightProbes = false;
        public ReflectionProbeUsage reflectionProbeUsage = ReflectionProbeUsage.Off;

        // Material
        public bool enableBump = false;
        public bool enableHue = false;
        public bool enableSubsurface = false;
    };

    [Serializable]
    internal class MaterialInfo
    {
        public Material material = null;
        public string defaultName = null;
        public bool exported = false;
    }

    [Serializable]
    internal class LODMaterials
    {
        public List<MaterialInfo> materials = new List<MaterialInfo>();
        public Dictionary<int, List<int>> lodToMaterials = new Dictionary<int, List<int>>();
        public Dictionary<string, int> matNameToIndex = new Dictionary<string, int>();

        public void AddLodMaterialIndex(int lod, int matIndex)
        {
            if (lodToMaterials.ContainsKey(lod))
            {
                lodToMaterials[lod].Add(matIndex);
            }
            else
            {
                lodToMaterials.Add(lod, new List<int>() { matIndex});
            }
        }
    }

    [Serializable]
    internal class WindSettings
    {
        public bool enableWind = true;

        [Range(1.0f, 20.0f)]
        public float strenghResponse = 5.0f;

        [Range(1.0f, 20.0f)]
        public float directionResponse = 2.5f;

        [Range(0.0f, 1.0f)]
        public float randomness = 0.5f;
    }

    // Ideally, we should use 'AssetImporter.SourceAssetIdentifier', but this struct has many problems:
    // 1 - 'Type' is not a serializable type, so it's always equal to null in our context.
    // 2 - The C# struct is not matching the C++ class.
    // 3 - Missing the 'Serializable' attribute on top of the struct.
    [Serializable]
    internal class AssetIdentifier
    {
        public string type;
        public string name;

        public AssetIdentifier(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                throw new ArgumentNullException("asset");
            }

            this.type = asset.GetType().ToString();
            this.name = asset.name;
        }

        public AssetIdentifier(Type type, string name)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("The name is empty", "name");
            }

            this.type = type.ToString();
            this.name = name;
        }
    }

    /// <summary>
    /// This attribute is used as a callback to set SRP specific properties from the importer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class MaterialSettingsCallbackAttribute : Attribute
    {
        /// <summary>
        /// The version of the method.
        /// </summary>
        public int MethodVersion;

        /// <summary>
        /// Initializes a new instance of the MaterialSettingsCallbackAttribute with the given method version.
        /// </summary>
        /// <param name="methodVersion">The given method version.</param>
        public MaterialSettingsCallbackAttribute(int methodVersion)
        {
            MethodVersion = methodVersion;
        }

        [RequiredSignature]
        static void SignatureExample(GameObject mainObject)
        {
            throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// This attribute is used as a callback to extend the inspector by adding
    /// the Diffuse Profile property when the HDRP package is in use.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class DiffuseProfileCallbackAttribute : Attribute
    {
        /// <summary>
        /// The version of the method.
        /// </summary>
        public int MethodVersion;

        /// <summary>
        /// Initializes a new instance of the DiffuseProfileCallbackAttribute with the given method version.
        /// </summary>
        /// <param name="methodVersion">The given method version.</param>
        public DiffuseProfileCallbackAttribute(int methodVersion)
        {
            MethodVersion = methodVersion;
        }

        [RequiredSignature]
        static void SignatureExample(ref SerializedProperty diffusionProfileAsset, ref SerializedProperty diffusionProfileHash)
        {
            throw new InvalidOperationException();
        }
    }
}
