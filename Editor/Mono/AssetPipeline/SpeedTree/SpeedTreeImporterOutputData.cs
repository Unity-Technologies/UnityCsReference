// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.SpeedTree.Importer
{
    internal class SpeedTreeImporterOutputData : ScriptableObject
    {
        /// <summary>
        /// The main object of the asset.
        /// </summary>
        public GameObject mainObject;

        /// <summary>
        /// Contains the materials data and materials are assigned per LOD with an index.
        /// </summary>
        public LODMaterials lodMaterials = new LODMaterials();

        /// <summary>
        /// The materials metadata used for the materials extraction code.
        /// </summary>
        public List<AssetIdentifier> materialsIdentifiers = new List<AssetIdentifier>();

        /// <summary>
        /// Represents the wind configuration parameters.
        /// </summary>
        public SpeedTreeWindConfig9 m_WindConfig = new SpeedTreeWindConfig9();

        /// <summary>
        /// Determines if the current asset has embedded materials or not.
        /// </summary>
        public bool hasEmbeddedMaterials = false;

        /// <summary>
        /// Determines if the current Shader supports alpha clip threshold.
        /// </summary>
        public bool hasAlphaClipThreshold = false;

        /// <summary>
        /// Determines if the current Shader supports transmision scale.
        /// </summary>
        public bool hasTransmissionScale = false;

        /// <summary>
        /// Determines if the current asset has a biilboard or not.
        /// </summary>
        public bool hasBillboard = false;

        /// <summary>
        /// Create an instance of SpeedTreeImporterOutputData with default settings.
        /// </summary>
        /// <returns>The newly created SpeedTreeImporterOutputData instance.</returns>
        static public SpeedTreeImporterOutputData Create()
        {
            SpeedTreeImporterOutputData outputImporterData = CreateInstance<SpeedTreeImporterOutputData>();
            outputImporterData.name = "ImporterOutputData";

            // Ideally, we should use this flag so the file is not visible in the asset prefab.
            // Though, it breaks the 'AssetDatabase.LoadAssetAtPath' function (the file is not loaded anymore).
            // outputImporterData.hideFlags = HideFlags.HideInHierarchy;

            return outputImporterData;
        }
    }
}
