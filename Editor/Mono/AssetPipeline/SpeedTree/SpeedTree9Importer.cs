// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

using UnityEditor.AssetImporters;

using STVertex = UnityEditor.SpeedTree.Importer.Vertex;
using Material = UnityEngine.Material;
using Color = UnityEngine.Color;

using static UnityEditor.SpeedTree.Importer.SpeedTreeImporterCommon;
using static UnityEditor.SpeedTree.Importer.WindConfigSDK;
using static UnityEditor.SpeedTree.Importer.SpeedTree9Importer;
using static UnityEditor.SpeedTree.Importer.SpeedTree9Reader;

namespace UnityEditor.SpeedTree.Importer
{
    [ScriptedImporter(1, "st9", AllowCaching = true)]
    public class SpeedTree9Importer : ScriptedImporter
    {
        const int SPEEDTREE_9_WIND_VERSION = 1;
        const int SPEEDTREE_9_MATERIAL_VERSION = 1;

        internal static class ImporterSettings
        {
            internal const string kGameObjectName = "SpeedTree";
            internal const string kHDRPShaderName = "HDRP/Nature/SpeedTree9_HDRP";
            internal const string kURPShaderName = "Universal Render Pipeline/Nature/SpeedTree9_URP";
            internal const string kLegacyShaderName = "Nature/SpeedTree9";
            internal const string kWindAssetName = "SpeedTreeWind";
            internal const string kSRPDependencyName = "srp/default-pipeline";
            internal const string kMaterialSettingsDependencyname = "SpeedTree9Importer_MaterialSettings";

            // In some very specific scenarios, the Shader cannot be found using "Shader.Find" (e.g project upgrade).
            // Adding an extra-security is necessary to avoid that, by manually forcing the load of the Shader using
            // "AssetDatabase.LoadAssetAtPath". It only happens for SRPs during project upgrades.
            internal const string kHDRPShaderPath = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Nature/SpeedTree9_HDRP.shadergraph";
            internal const string kURPShaderPath = "Packages/com.unity.render-pipelines.universal/Shaders/Nature/SpeedTree9_URP.shadergraph";
        }

        private static class Styles
        {
            internal static readonly Texture2D kIcon = EditorGUIUtility.FindTexture("UnityEditor/SpeedTree9Importer Icon");
        }

        private struct STMeshGeometry
        {
            public Vector3[] vertices;
            public Vector3[] normals;
            public Vector4[] tangents;
            public Color32[] colors32;
            public Vector4[][] uvs;
            public int lodIndex;

            public STMeshGeometry(int vertexCount, int UVCount, int indexLod)
            {
                vertices = new Vector3[vertexCount];
                normals = new Vector3[vertexCount];
                tangents = new Vector4[vertexCount];
                colors32 = new Color32[vertexCount];
                uvs = new Vector4[UVCount][];
                lodIndex = indexLod;

                for (int i = 0; i < UVCount; ++i)
                {
                    uvs[i] = new Vector4[vertexCount];
                }
            }
        }

        [SerializeField]
        internal MeshSettings m_MeshSettings = new MeshSettings();

        [SerializeField]
        internal MaterialSettings m_MaterialSettings = new MaterialSettings();

        [SerializeField]
        internal LightingSettings m_LightingSettings = new LightingSettings();

        [SerializeField]
        internal AdditionalSettings m_AdditionalSettings = new AdditionalSettings();

        [SerializeField]
        internal LODSettings m_LODSettings = new LODSettings();

        [SerializeField]
        internal List<PerLODSettings> m_PerLODSettings = new List<PerLODSettings>();

        [SerializeField]
        internal WindSettings m_WindSettings = new WindSettings();

        [SerializeField]
        internal int m_MaterialVersion = SPEEDTREE_9_MATERIAL_VERSION;

        /// <summary>
        /// Necessary to set default HDRP properties and materials upgrade.
        /// </summary>
        /// <param name="mainObject">The main object used by the importer, containing the data.</param>
        public delegate void OnAssetPostProcess(GameObject mainObject);

        /// <summary>
        /// Exposes the Diffuse Profile property in the importer inspector with compatible render pipelines.
        /// </summary>
        /// <param name="diffusionProfileAsset">The serialized property of the diffusion profile asset.</param>
        /// <param name="diffusionProfileHash">The serialized property of the diffusion profile hash.</param>
        /// <remarks>
        /// Necessary to expose the Diffuse Profile property in the inspector, since the importer is not
        /// aware of HDRP as it's a package. This property is highly used by artists, so exposing it is a big win.
        /// </remarks>
        public delegate void OnCustomEditorSettings(ref SerializedProperty diffusionProfileAsset, ref SerializedProperty diffusionProfileHash);

        [SerializeField]
        internal SpeedTreeImporterOutputData m_OutputImporterData;

        // Cache main objects, created during import process.
        private AssetImportContext m_Context;
        private SpeedTree9Reader m_Tree;
        private Shader m_Shader;
        private SpeedTreeWindAsset m_WindAsset;
        private STRenderPipeline m_RenderPipeline;

        // Values cached at the begining of the import process.
        private bool m_HasFacingData;
        private bool m_HasBranch2Data;
        private bool m_LastLodIsBillboard;
        private bool m_WindEnabled;
        private uint m_LODCount;
        private uint m_CollisionObjectsCount;
        private string m_PathFromDirectory;

        internal bool MaterialsShouldBeRegenerated => m_MaterialVersion != SPEEDTREE_9_MATERIAL_VERSION;

        public override bool SupportsRemappedAssetType(Type type)
        {
            return true;
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            m_Context = ctx;
            m_Tree = new SpeedTree9Reader();

            FileStatus status = m_Tree.Initialize(ctx.assetPath);
            if (status != FileStatus.Valid)
            {
                ctx.LogImportError($"Error while initializing the SpeedTree9 reader: {status}.");
                return;
            }

            m_Tree.ReadContent();

            CacheTreeImporterValues(ctx.assetPath);

            m_RenderPipeline = GetCurrentRenderPipelineType();

            ctx.DependsOnCustomDependency(ImporterSettings.kSRPDependencyName);

            if (!TryGetShaderForCurrentRenderPipeline(m_RenderPipeline, out m_Shader))
            {
                ctx.LogImportError("SpeedTree9 shader is invalid, cannot create Materials for this SpeedTree asset.");
                return;
            }

            m_OutputImporterData = SpeedTreeImporterOutputData.Create();
            m_OutputImporterData.hasBillboard = m_LastLodIsBillboard;

            CalculateScaleFactorFromUnit();

            if (m_WindEnabled)
            {
                m_OutputImporterData.m_WindConfig = CopySpeedTree9WindConfig(m_Tree.Wind, m_MeshSettings.scaleFactor, m_Tree.Bounds);
                SetWindParameters(ref m_OutputImporterData.m_WindConfig);

                m_WindAsset = new SpeedTreeWindAsset(SPEEDTREE_9_WIND_VERSION, m_OutputImporterData.m_WindConfig)
                {
                    name = ImporterSettings.kWindAssetName,
                };
            }

            GameObject mainObject = new GameObject(ImporterSettings.kGameObjectName);

            if (m_AdditionalSettings.generateRigidbody)
            {
                CreateAndAddRigidBodyToAsset(mainObject);
            }

            ctx.AddObjectToAsset(ImporterSettings.kGameObjectName, mainObject);
            ctx.SetMainObject(mainObject);

            SetThumbnailFromTexture2D(Styles.kIcon, mainObject.GetInstanceID());

            m_OutputImporterData.mainObject = mainObject;

            CalculateBillboardAndPerLODSettings();

            CreateMeshAndMaterials();

            CreateAssetIdentifiersAndAddMaterialsToContext();

            if (m_AdditionalSettings.generateColliders && m_CollisionObjectsCount > 0)
            {
                CreateAndAddCollidersToAsset();
            }

            if (m_WindEnabled)
            {
                ctx.AddObjectToAsset(ImporterSettings.kWindAssetName, m_WindAsset);
            }

            ctx.AddObjectToAsset(m_OutputImporterData.name, m_OutputImporterData);
            ctx.DependsOnCustomDependency(ImporterSettings.kMaterialSettingsDependencyname);

            AddDependencyOnExtractedMaterials();

            TriggerAllCabback();
        }

        private void TriggerAllCabback()
        {
            var allMethods = AttributeHelper.GetMethodsWithAttribute<MaterialSettingsCallbackAttribute>().methodsWithAttributes;
            foreach (var method in allMethods)
            {
                var callback = Delegate.CreateDelegate(typeof(OnAssetPostProcess), method.info) as OnAssetPostProcess;
                callback?.Invoke(m_OutputImporterData.mainObject);
            }
        }

        private void CacheTreeImporterValues(string assetPath)
        {
            // Variables used a lot are cached, since accessing any Reader array has a non-negligeable cost. 
            m_HasFacingData = TreeHasFacingData();
            m_HasBranch2Data = m_Tree.Wind.DoBranch2;
            m_LastLodIsBillboard = m_Tree.BillboardInfo.LastLodIsBillboard;
            m_LODCount = (uint)m_Tree.Lod.Length;
            m_CollisionObjectsCount = (uint)m_Tree.CollisionObjects.Length;

            WindConfigSDK windCfg = m_Tree.Wind;
            m_WindEnabled = (windCfg.DoShared || windCfg.DoBranch1 || windCfg.DoBranch2 || windCfg.DoRipple)
                && m_WindSettings.enableWind;

            m_PathFromDirectory = Path.GetDirectoryName(assetPath) + "/";
        }

        internal void RegenerateMaterials()
        {
            m_OutputImporterData = AssetDatabase.LoadAssetAtPath<SpeedTreeImporterOutputData>(assetPath);

            if (m_OutputImporterData.hasEmbeddedMaterials)
            {
                // TODO: Verify if we could only generate the embedded materials
                // instead of reimporting entirely the asset.
                SaveAndReimport();
                return;
            }

            try
            {
                AssetDatabase.StartAssetEditing();

                RegenerateAndPopulateExternalMaterials(this.assetPath);

                TriggerAllCabback();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        #region Mesh Geometry & Renderers
        private Mesh CreateMeshAndGeometry(Lod lod, int lodIndex)
        {
            bool isBillboard = m_LastLodIsBillboard && (lodIndex == (m_LODCount - 1));
            int vertexCount = (int)lod.Vertices.Length;
            int numUVs = CalculateNumUVs(isBillboard);

            STMeshGeometry sTMeshGeometry = new STMeshGeometry(vertexCount, numUVs, lodIndex);

            CalculateMeshGeometry(sTMeshGeometry, lod, isBillboard);

            Mesh mesh = new Mesh()
            {
                name = "LOD" + sTMeshGeometry.lodIndex + "_Mesh",
                indexFormat = (sTMeshGeometry.vertices.Length > 65535) ? IndexFormat.UInt32 : IndexFormat.UInt16,
                subMeshCount = (int)lod.DrawCalls.Length,
                vertices = sTMeshGeometry.vertices,
                normals = sTMeshGeometry.normals,
                tangents = sTMeshGeometry.tangents,
                colors32 = sTMeshGeometry.colors32
            };

            mesh.SetUVs(0, sTMeshGeometry.uvs[0]);
            mesh.SetUVs(1, sTMeshGeometry.uvs[1]);

            if (!isBillboard)
            {
                if (m_HasBranch2Data || m_HasFacingData)
                {
                    mesh.SetUVs(2, sTMeshGeometry.uvs[2]);
                }
                if (m_HasBranch2Data && m_HasFacingData)
                {
                    mesh.SetUVs(3, sTMeshGeometry.uvs[3]);
                }
            }

            return mesh;
        }

        private void SetMeshIndices(Mesh mesh, Lod lod, DrawCall draw, int drawIndex)
        {
            int[] indices = new int[draw.IndexCount];

            uint[] lodIndices = lod.Indices;

            for (int index = 0; index < draw.IndexCount; ++index)
            {
                indices[index] = unchecked((int)lodIndices[unchecked((int)(draw.IndexStart + index))]);
            }
            mesh.SetIndices(indices, MeshTopology.Triangles, drawIndex, true);
        }

        private void CreateMeshAndLODObjects(Mesh mesh, int lodIndex, ref LOD[] lods)
        {
            mesh.RecalculateUVDistributionMetrics();
            GameObject lodObject = new GameObject("LOD" + lodIndex);
            lodObject.transform.parent = m_OutputImporterData.mainObject.transform;

            MeshFilter meshFilter = lodObject.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            MeshRenderer renderer = lodObject.AddComponent<MeshRenderer>();
            {
                lods[lodIndex] = new LOD(m_PerLODSettings[lodIndex].height, new Renderer[] { renderer });

                SetMeshRendererSettings(renderer, lodIndex);
            }

            AddTreeComponentToLODObject(lodObject);

            m_Context.AddObjectToAsset(mesh.name, mesh);
        }

        private void CalculateMeshGeometry(STMeshGeometry sTMeshGeometry, Lod lod, bool isBillboard)
        {
            STVertex[] vertices = lod.Vertices;

            for (int i = 0; i < sTMeshGeometry.vertices.Length; ++i)
            {
                STVertex vertex = vertices[i];

                sTMeshGeometry.vertices[i].Set(
                    vertex.Anchor.X + vertex.Offset.X,
                    vertex.Anchor.Y + vertex.Offset.Y,
                    vertex.Anchor.Z + vertex.Offset.Z);

                sTMeshGeometry.vertices[i] *= m_MeshSettings.scaleFactor;

                if (vertex.CameraFacing)
                {
                    sTMeshGeometry.vertices[i].x = vertex.Anchor.X - vertex.Offset.X;
                }

                sTMeshGeometry.normals[i].Set(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z);

                Vector3 vertexTangent = new Vector3(vertex.Tangent.X, vertex.Tangent.Y, vertex.Tangent.Z);
                Vector3 binormal = Vector3.Cross(sTMeshGeometry.normals[i], vertexTangent);
                Vector2 vertexBinormal = new Vector3(vertex.Binormal.X, vertex.Binormal.Y, vertex.Binormal.Z);

                float dot = Vector3.Dot(binormal, vertexBinormal);
                float flip = (dot < 0.0f) ? -1.0f : 1.0f;

                sTMeshGeometry.tangents[i].Set(vertex.Tangent.X, vertex.Tangent.Y, vertex.Tangent.Z, flip);

                sTMeshGeometry.colors32[i] = new Color(
                    vertex.Color.X * vertex.AmbientOcclusion,
                    vertex.Color.Y * vertex.AmbientOcclusion,
                    vertex.Color.Z * vertex.AmbientOcclusion,
                    vertex.BlendWeight);

                // Texcoord setup:
                // 0        Diffuse UV, Branch1Pos, Branch1Dir
                // 1        Lightmap UV, Branch1Weight, RippleWeight

                // If Branch2 is available:
                // 2        Branch2Pos, Branch2Dir, Branch2Weight, <Unused>

                // If camera-facing geom is available:
                // 2/3      Anchor XYZ, FacingFlag

                int currentUV = 0;
                sTMeshGeometry.uvs[currentUV++][i].Set(
                    vertex.TexCoord.X,
                    vertex.TexCoord.Y,
                    vertex.BranchWind1.X,
                    vertex.BranchWind1.Y);

                sTMeshGeometry.uvs[currentUV++][i].Set(
                    vertex.LightmapTexCoord.X,
                    vertex.LightmapTexCoord.Y,
                    vertex.BranchWind1.Z,
                    vertex.RippleWeight);

                if (!isBillboard)
                {
                    if (m_HasBranch2Data)
                    {
                        sTMeshGeometry.uvs[currentUV++][i].Set(
                            vertex.BranchWind2.X,
                            vertex.BranchWind2.Y,
                            vertex.BranchWind2.Z,
                            0.0f);
                    }

                    if (m_HasFacingData)
                    {
                        sTMeshGeometry.uvs[currentUV++][i].Set(
                            vertex.Anchor.X * m_MeshSettings.scaleFactor,
                            vertex.Anchor.Y * m_MeshSettings.scaleFactor,
                            vertex.Anchor.Z * m_MeshSettings.scaleFactor,
                            vertex.CameraFacing ? 1.0f : 0.0f);
                    }
                }
            }
        }

        private void SetMeshRendererSettings(MeshRenderer renderer, int lodIndex)
        {
            ShadowCastingMode castMode = (m_LightingSettings.enableShadowCasting) ? ShadowCastingMode.On : ShadowCastingMode.Off;
            LightProbeUsage probeUsage = (m_LightingSettings.enableLightProbes) ? LightProbeUsage.BlendProbes : LightProbeUsage.Off;
            ReflectionProbeUsage reflectionProbe = m_LightingSettings.reflectionProbeEnumValue;
            bool receiveShadows = m_LightingSettings.enableShadowReceiving;

            if (m_PerLODSettings[lodIndex].enableSettingOverride)
            {
                castMode = m_PerLODSettings[lodIndex].castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                probeUsage = m_PerLODSettings[lodIndex].useLightProbes ? LightProbeUsage.BlendProbes : LightProbeUsage.Off;
                receiveShadows = m_PerLODSettings[lodIndex].receiveShadows;
                reflectionProbe = m_PerLODSettings[lodIndex].reflectionProbeUsage;
            }

            renderer.sharedMaterials = RetrieveMaterialsForCurrentLod(lodIndex);
            renderer.motionVectorGenerationMode = m_AdditionalSettings.motionVectorModeEnumValue;
            renderer.receiveShadows = receiveShadows;
            renderer.shadowCastingMode = castMode;
            renderer.lightProbeUsage = probeUsage;
            renderer.reflectionProbeUsage = reflectionProbe;
        }

        private int CalculateNumUVs(bool isBillboard)
        {
            int numUVs = 2;
            if (!isBillboard)
            {
                if (m_HasBranch2Data)
                {
                    numUVs += 1;
                }

                if (m_HasFacingData)
                {
                    numUVs += 1;
                }
            }
            return numUVs;
        }
        #endregion

        #region LODs
        private PerLODSettings InstantiateAndInitializeLODSettingsObject(bool isBillboardLOD)
        {
            return new PerLODSettings()
            {
                enableSettingOverride = isBillboardLOD,
                enableBump = m_MaterialSettings.enableBumpMapping,
                enableHue = m_MaterialSettings.enableHueVariation,
                enableSubsurface = m_MaterialSettings.enableSubsurfaceScattering,
                castShadows = true,
                receiveShadows = true,
                useLightProbes = !isBillboardLOD,
                reflectionProbeUsage = isBillboardLOD ? ReflectionProbeUsage.Off : ReflectionProbeUsage.BlendProbes,
            };
        }

        private void CalculateBillboardAndPerLODSettings()
        {
            if (m_PerLODSettings.Count > m_LODCount)
            {
                m_PerLODSettings.RemoveRange((int)m_LODCount, m_PerLODSettings.Count);
            }
            else if (m_PerLODSettings.Count < m_LODCount)
            {
                for (int i = 0; i < m_LODCount; ++i)
                {
                    bool isBillboardLOD = m_LastLodIsBillboard && i == m_LODCount - 1;

                    PerLODSettings lodSettings = InstantiateAndInitializeLODSettingsObject(isBillboardLOD);

                    m_PerLODSettings.Add(lodSettings);
                }

                // Always reset LOD heights if size doesn't match.
                for (int i = 0; i < m_LODCount; ++i)
                {
                    m_PerLODSettings[i].height = (i == 0) ? 0.5f : m_PerLODSettings[i - 1].height * 0.5f;
                }

                if (m_LODCount > 0)
                {
                    // Using this pattern to avoid using 'Last' from Linq.
                    m_PerLODSettings[^1].height = 0.01f;
                }
            }

            Debug.Assert(m_PerLODSettings.Count == m_LODCount);
        }

        private Material[] RetrieveMaterialsForCurrentLod(int lodIndex)
        {
            List<MaterialInfo> materials = m_OutputImporterData.lodMaterials.materials;

            if (materials == null || materials.Count == 0)
                return null;

            List<int> matIDs = m_OutputImporterData.lodMaterials.lodToMaterials[lodIndex];

            // Using this pattern to avoid using 'Select' from Linq.
            Material[] lodMaterials = new Material[matIDs.Count];
            for (int i = 0; i < matIDs.Count; ++i)
            {
                lodMaterials[i] = materials[matIDs[i]].material;
            }

            return lodMaterials;
        }

        private void AddLODGroupToMainObjectAndSetTransition(LOD[] lods)
        {
            LODGroup lodGroup = m_OutputImporterData.mainObject.AddComponent<LODGroup>();
            lodGroup.SetLODs(lods);

            int numLODs = lods.Length;

            if (m_LODSettings.enableSmoothLODTransition && numLODs > 0)
            {
                lodGroup.fadeMode = LODFadeMode.CrossFade;
                lodGroup.animateCrossFading = m_LODSettings.animateCrossFading;
                lodGroup.lastLODBillboard = m_LastLodIsBillboard;

                if (!m_LODSettings.animateCrossFading)
                {
                    int lastLod = numLODs - 1;
                    lods[lastLod].fadeTransitionWidth = m_LODSettings.fadeOutWidth;

                    if (m_LastLodIsBillboard && numLODs > 2)
                    {
                        int lastMeshLOD = numLODs - 2;
                        lods[lastMeshLOD].fadeTransitionWidth = m_LODSettings.billboardTransitionCrossFadeWidth;
                    }
                }
            }

            lodGroup.RecalculateBounds();
        }

        private void AddTreeComponentToLODObject(GameObject mainObject)
        {
            // Register the wind asset ptr through Tree component.
            Tree treeComponent = mainObject.AddComponent<Tree>();
            if (m_WindEnabled)
            {
                treeComponent.windAsset = m_WindAsset;
            }
        }
        #endregion

        #region Materials
        private void CreateMeshAndMaterials(bool regenerateMaterials = false)
        {
            LOD[] lods = new LOD[m_LODCount];

            // Loop each LOD (mesh) of the asset.
            for (int lodIndex = 0; lodIndex < m_LODCount; ++lodIndex)
            {
                Lod lod = m_Tree.Lod[lodIndex];
                Mesh mesh = CreateMeshAndGeometry(lod, lodIndex);

                // Loop each DrawCall (material) of the current mesh LOD.
                for (int drawIndex = 0; drawIndex < lod.DrawCalls.Length; ++drawIndex)
                {
                    DrawCall draw = lod.DrawCalls[drawIndex];

                    STMaterial stMaterial = m_Tree.Materials[(int)draw.MaterialIndex];

                    CreateMaterialsForCurrentLOD(stMaterial, lodIndex, regenerateMaterials);

                    SetMeshIndices(mesh, lod, draw, drawIndex);
                }

                CreateMeshAndLODObjects(mesh, lodIndex, ref lods);
            }

            AddLODGroupToMainObjectAndSetTransition(lods);
        }

        private void CreateMaterialsForCurrentLOD(STMaterial stMaterial, int lodIndex, bool regenerateMaterials)
        {
            bool matOverrided = m_PerLODSettings[lodIndex].enableSettingOverride;
            bool isBillboard = m_LastLodIsBillboard && (lodIndex == (m_LODCount - 1));

            // Overrided LODs have they own unique material.
            string stMatName = stMaterial.Name;
            if (matOverrided && !isBillboard)
                stMatName += String.Format("LOD{0}", lodIndex);

            // Retrieve previously extracted material and update serialized index.
            if (TryGetExternalMaterial(stMatName, out var extractedMat))
            {
                // Explicity regenerate materials, should happen when bumping the material version for example.
                if (regenerateMaterials)
                {
                    extractedMat = CreateMaterial(stMaterial, lodIndex, extractedMat.name, m_PathFromDirectory);

                    SetMaterialTextureAndColorProperties(stMaterial, extractedMat, lodIndex, m_PathFromDirectory);
                }
                else
                {
                    RetrieveMaterialSpecialProperties(extractedMat);
                }

                var existedMatIndex = m_OutputImporterData.lodMaterials.materials.FindIndex(m => m.defaultName == stMatName);
                if (existedMatIndex == -1)
                {
                    m_OutputImporterData.lodMaterials.materials.Add(new MaterialInfo { material = extractedMat, defaultName = stMatName, exported = true });
                    m_OutputImporterData.lodMaterials.matNameToIndex[stMatName] = m_OutputImporterData.lodMaterials.materials.Count - 1;
                }
                else
                {
                    m_OutputImporterData.lodMaterials.matNameToIndex[stMatName] = existedMatIndex;
                }
            }
            // Create material if it doesn't exist yet.
            else if (!m_OutputImporterData.lodMaterials.matNameToIndex.ContainsKey(stMatName))
            {
                Material newMat = CreateMaterial(stMaterial, lodIndex, stMatName, m_PathFromDirectory);

                m_OutputImporterData.lodMaterials.materials.Add(new MaterialInfo { material = newMat, defaultName = stMatName, exported = false });
                m_OutputImporterData.lodMaterials.matNameToIndex.Add(stMatName, m_OutputImporterData.lodMaterials.materials.Count - 1);
            }

            // Map the material id to the current LOD.
            int indexMat = m_OutputImporterData.lodMaterials.matNameToIndex[stMatName];
            m_OutputImporterData.lodMaterials.AddLodMaterialIndex(lodIndex, indexMat);
        }

        private void CreateAssetIdentifiersAndAddMaterialsToContext()
        {
            m_OutputImporterData.materialsIdentifiers.Clear();

            foreach (MaterialInfo matInfo in m_OutputImporterData.lodMaterials.materials)
            {
                m_OutputImporterData.hasEmbeddedMaterials |= !matInfo.exported;

                if (!matInfo.exported)
                {
                    m_Context.AddObjectToAsset(matInfo.material.name, matInfo.material);

                    // It looks like a limitation from the default AssetImporter system. When deleting extracted materials manually,
                    // the external object map still contains a null reference, even after a reimport of the asset.
                    if (TryGetSourceAssetIdentifierFromName(matInfo.material.name, out var assetIdentifier))
                    {
                        RemoveRemap(assetIdentifier);
                    }
                }

                m_OutputImporterData.materialsIdentifiers.Add(new AssetIdentifier(matInfo.material.GetType(), matInfo.defaultName));
            }
        }

        private void RegenerateMaterialsFromTree()
        {
            for (int lodIndex = 0; lodIndex < m_LODCount; lodIndex++)
            {
                Lod stLOD = m_Tree.Lod[lodIndex];

                // Loop necessary materials for current LOD.
                for (int drawIndex = 0; drawIndex < stLOD.DrawCalls.Length; ++drawIndex)
                {
                    int matIndex = (int)stLOD.DrawCalls[drawIndex].MaterialIndex;
                    STMaterial stMaterial = m_Tree.Materials[matIndex];

                    CreateMaterialsForCurrentLOD(stMaterial, lodIndex, regenerateMaterials: true);
                }
            }
        }

        private void RegenerateAndPopulateExternalMaterials(string assetPath)
        {
            // This object could potentially be cached, but this function is rarely triggered (only when bumping the material version)
            // so the cost of caching it is not really interesting.
            m_Tree = new SpeedTree9Reader();

            FileStatus status = m_Tree.Initialize(assetPath);
            if (status != FileStatus.Valid)
            {
                Debug.LogError($"Error while initializing the SpeedTree9 reader: {status}.");
                return;
            }

            m_Tree.ReadContent();

            CacheTreeImporterValues(assetPath);

            m_RenderPipeline = GetCurrentRenderPipelineType();

            if (!TryGetShaderForCurrentRenderPipeline(m_RenderPipeline, out m_Shader))
            {
                Debug.LogError("SpeedTree9 shader is invalid, cannot create Materials for this SpeedTree asset.");
                return;
            }

            m_OutputImporterData = AssetDatabase.LoadAssetAtPath<SpeedTreeImporterOutputData>(assetPath);
            m_OutputImporterData.lodMaterials.materials.Clear();

            if (m_WindEnabled)
            {
                m_OutputImporterData.m_WindConfig = CopySpeedTree9WindConfig(m_Tree.Wind, m_MeshSettings.scaleFactor, m_Tree.Bounds);
            }

            RegenerateMaterialsFromTree();

            foreach (MaterialInfo matInfo in m_OutputImporterData.lodMaterials.materials)
            {
                m_OutputImporterData.hasEmbeddedMaterials |= !matInfo.exported;

                m_OutputImporterData.materialsIdentifiers.Add(new AssetIdentifier(matInfo.material.GetType(), matInfo.material.name));

                // Remap the new material to the importer 'ExternalObjectMap'.
                if (TryGetExternalMaterial(matInfo.defaultName, out var extractedMat))
                {
                    string newMatPath = AssetDatabase.GetAssetPath(extractedMat);

                    // Not ideal, but it's safer to regenerate entirely the material (in case the pipeline has changed)
                    // and to avoid any potential issue with the 'ExternalObject' system (material null during next import)
                    if (File.Exists(newMatPath))
                    {
                        File.Delete(newMatPath);
                        AssetDatabase.CreateAsset(matInfo.material, newMatPath);
                    }

                    if (TryGetSourceAssetIdentifierFromName(matInfo.defaultName, out var assetIdentifier))
                    {
                        AddRemap(assetIdentifier, matInfo.material);
                    }
                }
            }
        }

        private bool TryGetExternalMaterial(string name, out Material material)
        {
            var externalObjMap = GetExternalObjectMap();

            foreach (var obj in externalObjMap)
            {
                if (obj.Key.name == name)
                {
                    material = obj.Value as Material;
                    return material != null;
                }
            }

            material = null;
            return false;
        }

        private bool TryGetSourceAssetIdentifierFromName(string name, out SourceAssetIdentifier assetIdentifier)
        {
            var externalObjMap = GetExternalObjectMap();

            foreach (var obj in externalObjMap)
            {
                if (obj.Key.name == name)
                {
                    assetIdentifier = obj.Key;
                    return true;
                }
            }

            assetIdentifier = new SourceAssetIdentifier();
            return false;
        }

        private Material CreateMaterial(STMaterial stMaterial, int lod, string matName, string path)
        {
            Material mat = new Material(m_Shader)
            {
                name = matName
            };

            RetrieveMaterialSpecialProperties(mat);

            SetMaterialTextureAndColorProperties(stMaterial, mat, lod, path);

            SetMaterialOtherProperties(stMaterial, mat);

            SetWindKeywords(mat, stMaterial.Billboard);

            return mat;
        }

        private bool SetMaterialTexture(Material mat, STMaterial stMaterial, int indexMap, string path, int property)
        {
            if (stMaterial.Maps.Length > indexMap)
            {
                MaterialMap stMatMap = stMaterial.Maps[indexMap];
                string mapPath = stMatMap.Path;

                if (!stMatMap.Used)
                    return false;

                if (!string.IsNullOrEmpty(mapPath))
                {
                    Texture2D texture = LoadTexture(mapPath, path);

                    if (texture != null)
                    {
                        mat.SetTexture(property, texture);
                        return true;
                    }
                }
            }

            return false;
        }

        private Texture2D LoadTexture(string mapPath, string path)
        {
            string texturePath = path + mapPath;

            Texture2D texture = (m_Context != null)
                ? m_Context.GetReferenceToAssetMainObject(texturePath) as Texture2D
                : AssetDatabase.LoadAssetAtPath(texturePath, typeof(Texture2D)) as Texture2D;

            if (texture != null)
                return texture;

            // Textures are not located near the asset, let's check if they were moved somewhere else.
            string mapPathWithoutExtension = Path.GetFileNameWithoutExtension(mapPath);
            string[] textureAssets = AssetDatabase.FindAssets(mapPathWithoutExtension);

            if (textureAssets != null && textureAssets.Length > 0)
            {
                string assetPathFromGUID = AssetDatabase.GUIDToAssetPath(textureAssets[0]);

                texture = (m_Context != null)
                    ? m_Context.GetReferenceToAssetMainObject(assetPathFromGUID) as Texture2D
                    : AssetDatabase.LoadAssetAtPath(assetPathFromGUID, typeof(Texture2D)) as Texture2D;

                return texture;
            }

            return null;
        }

        private bool TryGetInstanceIDFromMaterialProperty(Material material, int propertyName, out int id)
        {
            if (!material.HasProperty(propertyName))
            {
                id = 0;
                return false;
            }

            var property = material.GetTexture(propertyName);
            id = property.GetInstanceID();

            return true;
        }

        // Not all pipelines support the following properties, so we don't draw them in the inspector if that's the case.
        private void RetrieveMaterialSpecialProperties(Material mat)
        {
            m_OutputImporterData.hasAlphaClipThreshold |= mat.HasProperty(MaterialProperties.AlphaClipThresholdID);
            m_OutputImporterData.hasTransmissionScale |= mat.HasProperty(MaterialProperties.TransmissionScaleID);
        }

        private void SetMaterialTextureAndColorProperties(STMaterial stMaterial, Material mat, int lodIndex, string path)
        {
            bool enableHueVariation = m_MaterialSettings.enableHueVariation;
            bool enableBumpMapping = m_MaterialSettings.enableBumpMapping;
            bool enableSubsurfaceScattering = m_MaterialSettings.enableSubsurfaceScattering;

            if (m_PerLODSettings[lodIndex].enableSettingOverride)
            {
                enableHueVariation = m_PerLODSettings[lodIndex].enableHue;
                enableBumpMapping = m_PerLODSettings[lodIndex].enableBump;
                enableSubsurfaceScattering = m_PerLODSettings[lodIndex].enableSubsurface;
            }

            // MainTex and Color
            {
                bool colorTex = SetMaterialTexture(mat, stMaterial, 0, path, MaterialProperties.MainTexID);

                if (colorTex && TryGetInstanceIDFromMaterialProperty(mat, MaterialProperties.MainTexID, out int id) && id != 0)
                {
                    mat.SetColor(MaterialProperties.ColorTintID, m_MaterialSettings.mainColor);
                }
                else if (colorTex)
                {
                    Vec4 stColorVec = stMaterial.Maps[3].Color;
                    Color rpColor = new Color(stColorVec.X, stColorVec.Y, stColorVec.Z, stColorVec.W);

                    mat.SetColor(MaterialProperties.ColorTintID, m_MaterialSettings.mainColor * rpColor);
                }
            }

            // Bump map
            {
                bool hasNormalMap = SetMaterialTexture(mat, stMaterial, 1, path, MaterialProperties.NormalMapID);
                bool enableFeature = hasNormalMap && enableBumpMapping;

                mat.SetFloat(MaterialProperties.NormalMapKwToggleID, (enableFeature) ? 1.0f : 0.0f);
            }

            // Glossiness, metallic, AO
            {
                bool foundExtra = SetMaterialTexture(mat, stMaterial, 2, path, MaterialProperties.ExtraTexID);

                int id = 0;
                if (foundExtra && TryGetInstanceIDFromMaterialProperty(mat, MaterialProperties.ExtraTexID, out id) && id != 0)
                {
                    // _Glossiness (== _Smoothness) is multipled in the shader with the texture values if ExtraTex is present.
                    // Set default value 1.0f to override the default value 0.5, otherwise, the original texture values will
                    // be scaled down to half as much. Same goes for _Metallic
                    mat.SetFloat(MaterialProperties.GlossinessID, 1.0f);
                    mat.SetFloat(MaterialProperties.MetallicID, 1.0f);
                }
                else if (foundExtra)
                {
                    Vec4 stColor = stMaterial.Maps[2].Color;
                    mat.SetFloat(MaterialProperties.GlossinessID, stColor.X);
                    mat.SetFloat(MaterialProperties.MetallicID, stColor.Y);
                }

                mat.SetFloat(MaterialProperties.ExtraMapKwToggleID, (foundExtra && id != 0) ? 1.0f : 0.0f);
            }

            // Extra and SSS
            if (stMaterial.TwoSided || stMaterial.Billboard)
            {
                bool hasSSSTex = SetMaterialTexture(mat, stMaterial, 3, path, MaterialProperties.SubsurfaceTexID);
                bool setToggle = hasSSSTex && enableSubsurfaceScattering;

                // TODO: To implement in ST9 Shader.
                mat.SetFloat(MaterialProperties.SubsurfaceKwToggleID, (setToggle) ? 1.0f : 0.0f);

                if (hasSSSTex && TryGetInstanceIDFromMaterialProperty(mat, MaterialProperties.SubsurfaceTexID, out int id) && id != 0)
                {
                    mat.SetColor(MaterialProperties.SubsurfaceColorID, new Color(1.0f, 1.0f, 1.0f, 1.0f));
                }
                else if (hasSSSTex)
                {
                    Vec4 stColor = stMaterial.Maps[3].Color;
                    mat.SetColor(MaterialProperties.SubsurfaceColorID, new Color(stColor.X, stColor.Y, stColor.Z, stColor.W));
                }

                if (m_OutputImporterData.hasAlphaClipThreshold && mat.HasFloat(MaterialProperties.AlphaClipThresholdID))
                {
                    mat.SetFloat(MaterialProperties.AlphaClipThresholdID, m_MaterialSettings.alphaClipThreshold);
                }

                if (m_OutputImporterData.hasTransmissionScale && mat.HasFloat(MaterialProperties.TransmissionScaleID))
                {
                    mat.SetFloat(MaterialProperties.TransmissionScaleID, m_MaterialSettings.transmissionScale);
                }
            }

            // Hue effect
            {
                mat.SetFloat(MaterialProperties.HueVariationKwToggleID, enableHueVariation ? 1.0f : 0.0f);
                mat.SetColor(MaterialProperties.HueVariationColorID, m_MaterialSettings.hueVariation);
            }
        }

        private void SetMaterialOtherProperties(STMaterial stMaterial, Material mat)
        {
            bool isBillboard = stMaterial.Billboard;

            // Other properties
            mat.SetFloat(MaterialProperties.BillboardKwToggleID, isBillboard ? 1.0f : 0.0f);
            if (isBillboard)
            {
                mat.EnableKeyword(MaterialKeywords.BillboardID);
            }
            mat.SetFloat(MaterialProperties.LeafFacingKwToggleID, m_HasFacingData ? 1.0f : 0.0f);

            if (m_RenderPipeline == STRenderPipeline.HDRP)
            {
                mat.SetFloat(MaterialProperties.DoubleSidedToggleID, stMaterial.TwoSided ? 1.0f : 0.0f);
                mat.SetFloat(MaterialProperties.DoubleSidedNormalModeID, stMaterial.FlipNormalsOnBackside ? 0.0f : 2.0f);

                mat.SetVector(MaterialProperties.DiffusionProfileAssetID, m_MaterialSettings.diffusionProfileAssetID);
                mat.SetFloat(MaterialProperties.DiffusionProfileID, m_MaterialSettings.diffusionProfileID);
            }
            else if (m_RenderPipeline == STRenderPipeline.URP)
            {
                mat.SetFloat(MaterialProperties.BackfaceNormalModeID, stMaterial.FlipNormalsOnBackside ? 0.0f : 2.0f);
            }
            else // legacy rendering pipeline
            {
                mat.SetFloat(MaterialProperties.TwoSidedID, stMaterial.TwoSided ? 0.0f : 2.0f); // matches cull mode. 0: no cull
            }
            mat.enableInstancing = true;
            mat.doubleSidedGI = stMaterial.TwoSided;
        }

        private void SetWindKeywords(Material material, bool isBillboardMat)
        {
            if (material == null)
                return;

            //------------------------------------------------------------------------
            // Note:
            //   mat.SetFloat(...)      : Legacy rendering pipeline keyword toggle
            //   mat.EnableKeyword(...) : SRP keyword toggle
            //------------------------------------------------------------------------
            SpeedTreeWindConfig9 windCfg = m_OutputImporterData.m_WindConfig;

            if (windCfg.doShared != 0)
            {
                material.SetFloat(MaterialProperties.WindSharedKwToggle, 1.0f);
            }
            if (!isBillboardMat)
            {
                if (windCfg.doBranch2 != 0)
                {
                    material.SetFloat(MaterialProperties.WindBranch2KwToggle, 1.0f);
                }
                if (windCfg.doBranch1 != 0)
                {
                    material.SetFloat(MaterialProperties.WindBranch1KwToggle, 1.0f);
                }
                if (windCfg.doRipple != 0)
                {
                    material.SetFloat(MaterialProperties.WindRippleKwToggle, 1.0f);
                    if (windCfg.doShimmer != 0)
                    {
                        material.SetFloat(MaterialProperties.WindShimmerKwToggle, 1.0f);
                    }
                }
            }
        }

        internal bool SearchAndRemapMaterials(string materialFolderPath)
        {
            bool changedMappings = false;

            if (materialFolderPath == null)
                throw new ArgumentNullException("materialFolderPath");

            if (string.IsNullOrEmpty(materialFolderPath))
                throw new ArgumentException(string.Format("Invalid material folder path: {0}.", materialFolderPath), "materialFolderPath");

            string[] guids = AssetDatabase.FindAssets("t:Material", new string[] { materialFolderPath });
            List<Tuple<string, Material>> materials = new List<Tuple<string, Material>>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                // ensure that we only load material assets, not embedded materials
                Material material = AssetDatabase.LoadMainAssetAtPath(path) as Material;
                if (material)
                    materials.Add(new Tuple<string, Material>(path, material));
            }

            m_OutputImporterData = AssetDatabase.LoadAssetAtPath<SpeedTreeImporterOutputData>(assetPath);
            AssetIdentifier[] importedMaterials = m_OutputImporterData.materialsIdentifiers.ToArray();

            foreach (Tuple<string, Material> material in materials)
            {
                string materialName = material.Item2.name;
                string materialFile = material.Item1;

                // the legacy materials have the LOD in the path, while the new materials have the LOD as part of the name
                bool isLegacyMaterial = !materialName.Contains("LOD") && !materialName.Contains("Billboard");
                bool hasLOD = isLegacyMaterial && materialFile.Contains("LOD");
                string lod = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(materialFile));
                AssetIdentifier importedMaterial = Array.Find(importedMaterials, x => x.name.Contains(materialName) && (!hasLOD || x.name.Contains(lod)));

                if (!string.IsNullOrEmpty(importedMaterial.name))
                {
                    SourceAssetIdentifier importedIdentifier = new SourceAssetIdentifier(material.Item2);

                    AddRemap(importedIdentifier, material.Item2);
                    changedMappings = true;
                }
            }

            return changedMappings;
        }

        internal string GetMaterialFolderPath()
        {
            return FileUtil.DeleteLastPathNameComponent(assetPath) + "/";
        }

        internal string GetShaderNameFromPipeline(STRenderPipeline renderPipeline)
        {
            switch (renderPipeline)
            {
                case STRenderPipeline.HDRP:
                    return ImporterSettings.kHDRPShaderName;
                case STRenderPipeline.URP:
                    return ImporterSettings.kURPShaderName;
                case STRenderPipeline.Legacy:
                default:
                    return ImporterSettings.kLegacyShaderName;
            }
        }

        internal void SetMaterialsVersionToCurrent()
        {
            m_MaterialVersion = SPEEDTREE_9_MATERIAL_VERSION;
            MarkDirty();
        }

        private void AddDependencyOnExtractedMaterials()
        {
            Dictionary<SourceAssetIdentifier, UnityEngine.Object> extMap = GetExternalObjectMap();

            foreach (var entry in extMap)
            {
                if (entry.Value != null)
                {
                    string matPath = AssetDatabase.GetAssetPath(entry.Value);

                    m_Context.DependsOnImportedAsset(matPath);

                    // Necessary to avoid the warning "Import of asset setup artifact dependency to but dependency isn't used
                    // and therefore not registered in the asset database".
                    AssetDatabase.LoadAssetAtPath(matPath, typeof(Material));
                }
            }
        }
        #endregion

        #region Wind
        private unsafe SpeedTreeWindConfig9 CopySpeedTree9WindConfig(WindConfigSDK wind, float scaleFactor, in Bounds3 treeBounds)
        {
            const bool CHECK_ZERO = true;
            const bool DONT_CHECK_ZERO = false;

            void CopyCurve(in float[] src, float* dst)
            {
                const int NUM_CURVE_ELEMENTS = 20;
                Debug.Assert(src.Length == NUM_CURVE_ELEMENTS);
                for (global::System.Int32 i = 0; i < NUM_CURVE_ELEMENTS; i++)
                {
                    dst[i] = src[i];
                }
            }

            void CopyCurveScale(in float[] src, float* dst, float scaleFactor)
            {
                const int NUM_CURVE_ELEMENTS = 20;
                Debug.Assert(src.Length == NUM_CURVE_ELEMENTS);
                for (global::System.Int32 i = 0; i < NUM_CURVE_ELEMENTS; i++)
                {
                    dst[i] = src[i] * scaleFactor;
                }
            }

            bool ValidCurve(float[] curve, bool bCheckZero = CHECK_ZERO)
            {
                bool bNonZero = false;
                for (int i = 0; i < curve.Length; ++i)
                {
                    bNonZero |= curve[i] != 0.0f;
                    if (float.IsNaN(curve[i]))
                    {
                        return false;
                    }
                }

                if (bCheckZero)
                {
                    return bNonZero;
                }
                return true;
            }

            bool BranchHasAllCurvesValid(in WindBranch b)
            {
                return ValidCurve(b.Bend)
                    && ValidCurve(b.Oscillation)
                    && ValidCurve(b.Speed, CHECK_ZERO)
                    && ValidCurve(b.Turbulence)
                    && ValidCurve(b.Flexibility, DONT_CHECK_ZERO
                );
            }

            bool RippleHasAllCurvesValid(in WindRipple r)
            {
                return ValidCurve(r.Planar)
                    && ValidCurve(r.Directional)
                    && ValidCurve(r.Speed)
                    && ValidCurve(r.Flexibility, DONT_CHECK_ZERO
                );
            }

            SpeedTreeWindConfig9 cfg = new SpeedTreeWindConfig9();

            // common
            WindConfigCommon common = wind.Common;
            cfg.strengthResponse = common.StrengthResponse;
            cfg.directionResponse = common.DirectionResponse;
            cfg.gustFrequency = common.GustFrequency;
            cfg.gustStrengthMin = common.GustStrengthMin;
            cfg.gustStrengthMax = common.GustStrengthMax;
            cfg.gustDurationMin = common.GustDurationMin;
            cfg.gustDurationMax = common.GustDurationMax;
            cfg.gustRiseScalar = common.GustRiseScalar;
            cfg.gustFallScalar = common.GustFallScalar;

            // st9
            cfg.branch1StretchLimit = wind.Branch1StretchLimit * scaleFactor;
            cfg.branch2StretchLimit = wind.Branch2StretchLimit * scaleFactor;
            cfg.treeExtentX = (treeBounds.Max.X - treeBounds.Min.X) * scaleFactor;
            cfg.treeExtentY = (treeBounds.Max.Y - treeBounds.Min.Y) * scaleFactor;
            cfg.treeExtentZ = (treeBounds.Max.Z - treeBounds.Min.Z) * scaleFactor;

            if (wind.DoShared)
            {
                WindConfigSDK.WindBranch shared = wind.Shared;
                CopyCurveScale(shared.Bend, cfg.bendShared, scaleFactor);
                CopyCurveScale(shared.Oscillation, cfg.oscillationShared, scaleFactor);
                CopyCurve(shared.Speed, cfg.speedShared);
                CopyCurve(shared.Turbulence, cfg.turbulenceShared);
                CopyCurve(shared.Flexibility, cfg.flexibilityShared);
                cfg.independenceShared = shared.Independence;
                cfg.sharedHeightStart = wind.SharedStartHeight * scaleFactor;
                if (BranchHasAllCurvesValid(in shared))
                {
                    cfg.doShared = 1;
                }
            }

            if (wind.DoBranch1)
            {
                WindConfigSDK.WindBranch branch1 = wind.Branch1;
                CopyCurveScale(branch1.Bend, cfg.bendBranch1, scaleFactor);
                CopyCurveScale(branch1.Oscillation, cfg.oscillationBranch1, scaleFactor);
                CopyCurve(branch1.Speed, cfg.speedBranch1);
                CopyCurve(branch1.Turbulence, cfg.turbulenceBranch1);
                CopyCurve(branch1.Flexibility, cfg.flexibilityBranch1);
                cfg.independenceBranch1 = branch1.Independence;
                if (BranchHasAllCurvesValid(in branch1))
                {
                    cfg.doBranch1 = 1;
                }
            }

            if (wind.DoBranch2)
            {
                WindConfigSDK.WindBranch branch2 = wind.Branch2;
                CopyCurveScale(branch2.Bend, cfg.bendBranch2, scaleFactor);
                CopyCurveScale(branch2.Oscillation, cfg.oscillationBranch2, scaleFactor);
                CopyCurve(branch2.Speed, cfg.speedBranch2);
                CopyCurve(branch2.Turbulence, cfg.turbulenceBranch2);
                CopyCurve(branch2.Flexibility, cfg.flexibilityBranch2);
                cfg.independenceBranch2 = branch2.Independence;
                if (BranchHasAllCurvesValid(in branch2))
                {
                    cfg.doBranch2 = 1;
                }
            }

            if (wind.DoRipple)
            {
                WindConfigSDK.WindRipple ripple = wind.Ripple;
                CopyCurveScale(ripple.Planar, cfg.planarRipple, scaleFactor);
                CopyCurveScale(ripple.Directional, cfg.directionalRipple, scaleFactor);
                CopyCurve(ripple.Speed, cfg.speedRipple);
                CopyCurve(ripple.Flexibility, cfg.flexibilityRipple);
                cfg.independenceRipple = ripple.Independence;
                if (RippleHasAllCurvesValid(in ripple))
                {
                    cfg.doRipple = 1;
                    if (wind.DoShimmer)
                    {
                        cfg.doShimmer = 1;
                        cfg.shimmerRipple = ripple.Shimmer;
                    }
                }
            }
            return cfg;
        }

        private void SetWindParameters(ref SpeedTreeWindConfig9 cfg)
        {
            cfg.strengthResponse = m_WindSettings.strenghResponse;
            cfg.directionResponse = m_WindSettings.directionResponse;
            cfg.windIndependence = m_WindSettings.randomness;
        }

        #endregion

        #region Others
        private void CalculateScaleFactorFromUnit()
        {
            float scaleFactor = m_MeshSettings.scaleFactor;

            switch (m_MeshSettings.unitConversion)
            {
                // Use units in the imported file without any conversion.
                case STUnitConversion.kLeaveAsIs:
                    scaleFactor = 1.0f;
                    break;
                case STUnitConversion.kFeetToMeters:
                    scaleFactor = SpeedTreeConstants.kFeetToMetersRatio;
                    break;
                case STUnitConversion.kCentimetersToMeters:
                    scaleFactor = SpeedTreeConstants.kCentimetersToMetersRatio;
                    break;
                case STUnitConversion.kInchesToMeters:
                    scaleFactor = SpeedTreeConstants.kInchesToMetersRatio;
                    break;
                case STUnitConversion.kCustomConversion:
                    /* no-op */
                    break;
            }

            m_MeshSettings.scaleFactor = scaleFactor;
        }

        private bool TreeHasFacingData()
        {
            for (int lodIndex = 0; lodIndex < m_LODCount; ++lodIndex)
            {
                Lod lod = m_Tree.Lod[lodIndex];

                for (int drawIndex = 0; drawIndex < lod.DrawCalls.Length; ++drawIndex)
                {
                    DrawCall draw = lod.DrawCalls[drawIndex];
                    if(draw.ContainsFacingGeometry)
                        return true;
                }
            }
            return false;
        }

        private bool TryGetShaderForCurrentRenderPipeline(STRenderPipeline renderPipeline, out Shader shader)
        {
            switch (renderPipeline)
            {
                case STRenderPipeline.URP:
                    shader = Shader.Find(ImporterSettings.kURPShaderName);
                    if (shader == null)
                    {
                        shader = m_Context.GetReferenceToAssetMainObject(ImporterSettings.kURPShaderPath) as Shader;
                    }
                    break;
                case STRenderPipeline.HDRP:
                    shader = Shader.Find(ImporterSettings.kHDRPShaderName);
                    if (shader == null)
                    {
                        shader = m_Context.GetReferenceToAssetMainObject(ImporterSettings.kHDRPShaderPath) as Shader;
                    }
                    break;
                default:
                    shader = Shader.Find(ImporterSettings.kLegacyShaderName);
                    break;
            }

            return shader != null;
        }

        private void CreateAndAddRigidBodyToAsset(GameObject mainObject)
        {
            Rigidbody rb = mainObject.AddComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false;
                rb.isKinematic = true;
            }
        }

        private void CreateAndAddCollidersToAsset()
        {
            for (int iCollider = 0; iCollider < m_CollisionObjectsCount; ++iCollider)
            {
                CollisionObject stCollider = m_Tree.CollisionObjects[iCollider];

                GameObject collisionObject = new GameObject("Collider" + (iCollider + 1));
                collisionObject.transform.parent = m_OutputImporterData.mainObject.transform;

                Vector3 vOne = new Vector3(stCollider.Position.X, stCollider.Position.Y, stCollider.Position.Z);
                Vector3 vTwo = new Vector3(stCollider.Position2.X, stCollider.Position2.Y, stCollider.Position2.Z);

                vOne *= m_MeshSettings.scaleFactor;
                vTwo *= m_MeshSettings.scaleFactor;

                collisionObject.transform.position = (vOne + vTwo) * 0.5f;

                if ((vOne - vTwo).sqrMagnitude < 0.001f)
                {
                    SphereCollider collider = collisionObject.AddComponent<SphereCollider>();
                    collider.radius = stCollider.Radius * m_MeshSettings.scaleFactor;
                }
                else
                {
                    CapsuleCollider collider = collisionObject.AddComponent<CapsuleCollider>();
                    collider.direction = 2;
                    collider.radius = stCollider.Radius * m_MeshSettings.scaleFactor;
                    collider.height = (vOne - vTwo).magnitude;
                    collisionObject.transform.LookAt(vTwo);
                }

                m_Context.AddObjectToAsset(collisionObject.name, collisionObject);
            }
        }

        internal float[] GetPerLODSettingsHeights()
        {
            float[] heightsArray = new float[m_PerLODSettings.Count];

            for (int i = 0; i < m_PerLODSettings.Count; ++i)
            {
                heightsArray[i] = m_PerLODSettings[i].height;
            }

            return heightsArray;
        }
        #endregion
    }

    // Use a postprocessor to set various settings on the textures since these dont stick during first import.
    class SpeedTree9Postprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths,
            bool didDomainReload)
        {
            foreach (string assetFilename in importedAssets)
            {
                if (Path.GetExtension(assetFilename) == ".st9")
                {
                    try
                    {
                        AssetDatabase.StartAssetEditing();

                        ChangeTextureImporterSettingsForSt9Files(assetFilename);
                    }
                    finally
                    {
                        AssetDatabase.StopAssetEditing();
                    }
                }
            }

            if (didDomainReload)
            {
                if (TryGetHashSpeedTreeAttributeMaterialSettings(out List<string> strToHash))
                {
                    Hash128 hash = new Hash128();

                    foreach (string str in strToHash)
                    {
                        hash.Append(str);
                    }

                    AssetDatabase.RegisterCustomDependency(ImporterSettings.kMaterialSettingsDependencyname, hash);
                }
                else
                {
                    AssetDatabase.UnregisterCustomDependencyPrefixFilter(ImporterSettings.kMaterialSettingsDependencyname);
                }
            }
        }

        private static bool TryGetHashSpeedTreeAttributeMaterialSettings(out List<string> strToHash)
        {
            var allMethods = AttributeHelper.GetMethodsWithAttribute<MaterialSettingsCallbackAttribute>().methodsWithAttributes;

            strToHash = new List<string>();

            foreach (var method in allMethods)
            {
                MaterialSettingsCallbackAttribute attribute = method.attribute as MaterialSettingsCallbackAttribute;

                strToHash.Add($"{method.info.Name}-{method.info.DeclaringType.AssemblyQualifiedName}-{attribute.MethodVersion.ToString()}");
            }

            strToHash.Sort();
            return strToHash.Count > 0;
        }

        private static void ChangeTextureImporterSettingsForSt9Files(string assetPath)
        {
            SpeedTree9Reader tree = new SpeedTree9Reader();
            
            FileStatus status = tree.Initialize(assetPath);
            if (status != FileStatus.Valid)
            {
                Debug.LogError($"Error while initializing the SpeedTree9 reader: {status}.");
                return;
            }

            tree.ReadContent();

            string path = Path.GetDirectoryName(assetPath) + "/";
            for (int matIndex = 0; matIndex < tree.Materials.Length; ++matIndex)
            {
                STMaterial stMaterial = tree.Materials[matIndex];

                if (TryGetTextureImporterFromIndex(stMaterial, 0, path, out TextureImporter texImporterColor))
                    ApplyColorTextureSettings(texImporterColor);

                if (TryGetTextureImporterFromIndex(stMaterial, 1, path, out TextureImporter texImporterNormal))
                    ApplyNormalTextureSettings(texImporterNormal);

                if (TryGetTextureImporterFromIndex(stMaterial, 2, path, out TextureImporter texImporterExtra))
                    ApplyExtraTextureSettings(texImporterExtra);
            }
        }

        private static bool TryGetTextureImporterFromIndex(
            STMaterial stMaterial,
            int index,
            string directoryPath,
            out TextureImporter textureImporter)
        {
            textureImporter = null;

            if (stMaterial.Maps.Length <= index)
                return false;

            MaterialMap mat = stMaterial.Maps[index];
            if (!mat.Used || string.IsNullOrEmpty(mat.Path))
                return false;

            TextureImporter texImporter = TextureImporter.GetAtPath(directoryPath + mat.Path) as TextureImporter;
            if (texImporter == null)
                return false;

            textureImporter = texImporter;
            return true;
        }

        private static void ApplyColorTextureSettings(TextureImporter texImporter)
        {
            if (texImporter.alphaIsTransparency &&
                texImporter.mipMapsPreserveCoverage &&
                texImporter.alphaTestReferenceValue == 0.1f)
                return;

            texImporter.alphaIsTransparency = true;
            texImporter.mipMapsPreserveCoverage = true;
            texImporter.alphaTestReferenceValue = 0.1f;

            EditorUtility.SetDirty(texImporter);
            texImporter.SaveAndReimport();
        }

        private static void ApplyNormalTextureSettings(TextureImporter texImporter)
        {
            if (texImporter.textureType == TextureImporterType.NormalMap)
                return;

            texImporter.textureType = TextureImporterType.NormalMap;

            EditorUtility.SetDirty(texImporter);
            texImporter.SaveAndReimport();
        }

        private static void ApplyExtraTextureSettings(TextureImporter texImporter)
        {
            if (texImporter.sRGBTexture == false)
                return;

            texImporter.sRGBTexture = false;

            EditorUtility.SetDirty(texImporter);
            texImporter.SaveAndReimport();
        }
    }
}
