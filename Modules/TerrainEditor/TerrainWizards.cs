// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.TerrainTools;
using UnityEngine.Rendering;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor
{
    public class TerrainWizard : ScriptableWizard
    {
        #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
        public TerrainWizard() { }
        #pragma warning restore UAL0015

        internal const int kMaxResolution = 4097;

        internal Terrain      terrain;
        internal TerrainData    terrainData
        {
            get
            {
                if (terrain != null)
                    return terrain.terrainData;
                else
                    return null;
            }
        }

        internal bool TerrainDataIsPersistent
            => EditorUtility.IsPersistent(terrainData);

        internal virtual void  OnWizardUpdate()
        {
            isValid = true;
            errorString = "";
            if (terrain == null || terrain.terrainData == null)
            {
                isValid = false;
                errorString = "Terrain does not exist";
            }
        }

        public void ResetDefaults(Terrain terrain)
        {
            this.terrain = terrain;
            OnWizardUpdate();
        }

        // Removing or restoring prototypes while a wizard is open moves the edited prototype, so wizards look for it again
        // by content. The old index is trusted only while the array keeps its length. The prototype is missing if no
        // prototype matches or more than one does. Returns true if it moved to another index or went missing since the last call.
        internal static bool UpdateEditedPrototype<T>(T[] prototypes, ref T edited, ref int index, ref int count, ref bool missing, Func<T, T, bool> isSame) where T : class
        {
            // Prototypes are not serializable, so a domain reload keeps the index but clears the edited copy.
            if (edited == null && !missing && index < prototypes.Length)
            {
                edited = prototypes[index];
                count = prototypes.Length;
            }

            T target = edited;
            Predicate<T> isEdited = p => isSame(p, target);
            int found;
            if (prototypes.Length == count && index < prototypes.Length && isEdited(prototypes[index]))
                found = index;
            else
            {
                found = Array.FindIndex(prototypes, isEdited);
                if (found != Array.FindLastIndex(prototypes, isEdited))
                    found = -1;
            }

            bool isMissing = found == -1;
            if (!isMissing)
                count = prototypes.Length;
            if (isMissing == missing && (isMissing || found == index))
                return false;

            missing = isMissing;
            if (!isMissing)
                index = found;
            return true;
        }

        // DetailPrototype.Equals does not compare alignToGround and positionJitter.
        internal static bool IsSameDetail(DetailPrototype a, DetailPrototype b)
            => a.Equals(b) && a.alignToGround == b.alignToGround && a.positionJitter == b.positionJitter;

        internal void FlushHeightmapModification()
        {
            //@TODO:        m_Terrain.treeDatabase.RecalculateTreePosition();
            terrain.Flush();
        }

        public static T DisplayTerrainWizard<T>(string title, string button) where T : TerrainWizard
        {
            var treeWizards = Resources.FindObjectsOfTypeAll<T>();
            if (treeWizards.Length > 0)
            {
                var wizard = (T)treeWizards[0];
                wizard.titleContent = EditorGUIUtility.TextContent(title);
                wizard.createButtonName = button;
                wizard.otherButtonName = "";
                wizard.Focus();
                return wizard;
            }
            return ScriptableWizard.DisplayWizard<T>(title, button);
        }
    }

    internal class ImportRawHeightmap : TerrainWizard
    {
        #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
        internal ImportRawHeightmap() { }
        #pragma warning restore UAL0015

        internal enum Depth { Bit8 = 1, Bit16 = 2 }
        internal enum ByteOrder { Mac = 1, Windows = 2 }

        public Depth m_Depth = Depth.Bit16;
        public int m_Resolution = 1;
        public ByteOrder m_ByteOrder = ByteOrder.Windows;
        public bool m_FlipVertically = false;
        public Vector3 m_TerrainSize = new Vector3(2000, 600, 2000);
        private string m_Path;

        public void OnEnable()
        {
            minSize = new Vector2(400, 250);
        }

        void PickRawDefaults(string path)
        {
            FileStream file = File.Open(path, FileMode.Open, FileAccess.Read);
            int fileSize = (int)file.Length;
            file.Close();

            m_TerrainSize = terrainData.size;

            if (terrainData.heightmapResolution * terrainData.heightmapResolution == fileSize)
            {
                m_Resolution = terrainData.heightmapResolution;
                m_Depth = Depth.Bit8;
            }
            else if (terrainData.heightmapResolution * terrainData.heightmapResolution * 2 == fileSize)
            {
                m_Resolution = terrainData.heightmapResolution;
                m_Depth = Depth.Bit16;
            }
            else
            {
                m_Depth = Depth.Bit16;

                int pixels = fileSize / (int)m_Depth;
                int resolution = Mathf.RoundToInt(Mathf.Sqrt(pixels));
                if ((resolution * resolution * (int)m_Depth) == fileSize)
                {
                    m_Resolution = resolution;
                    return;
                }


                m_Depth = Depth.Bit8;

                pixels = fileSize / (int)m_Depth;
                resolution = Mathf.RoundToInt(Mathf.Sqrt(pixels));
                if ((resolution * resolution * (int)m_Depth) == fileSize)
                {
                    m_Resolution = resolution;
                    return;
                }

                m_Depth = Depth.Bit16;
            }
        }

        internal void OnWizardCreate()
        {
            if (terrain == null)
            {
                isValid = false;
                errorString = "Terrain does not exist";
            }

            if (m_Resolution > kMaxResolution)
            {
                isValid = false;
                errorString = "Heightmaps above 4097x4097 in resolution are not supported";
                Debug.LogError(errorString);
            }

            if (File.Exists(m_Path) && isValid)
            {
                Undo.RegisterCompleteObjectUndo(terrainData, "Import Raw heightmap");

                terrainData.heightmapResolution = m_Resolution;
                terrainData.size = m_TerrainSize;
                ReadRaw(m_Path);

                FlushHeightmapModification();
            }
        }

        void ReadRaw(string path)
        {
            // Read data
            byte[] data;
            using (BinaryReader br = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read)))
            {
                data = br.ReadBytes(m_Resolution * m_Resolution * (int)m_Depth);
                br.Close();
            }

            int heightmapRes = terrainData.heightmapResolution;
            float[,] heights = new float[heightmapRes, heightmapRes];
            if (m_Depth == Depth.Bit16)
            {
                float normalize = 1.0F / (1 << 16);
                for (int y = 0; y < heightmapRes; ++y)
                {
                    for (int x = 0; x < heightmapRes; ++x)
                    {
                        int index = Mathf.Clamp(x, 0, m_Resolution - 1) + Mathf.Clamp(y, 0, m_Resolution - 1) * m_Resolution;
                        if ((m_ByteOrder == ByteOrder.Mac) == System.BitConverter.IsLittleEndian)
                        {
                            // Yay, seems like this is the easiest way to swap bytes in C#. NUTS
                            byte temp;
                            temp = data[index * 2];
                            data[index * 2 + 0] = data[index * 2 + 1];
                            data[index * 2 + 1] = temp;
                        }

                        ushort compressedHeight = System.BitConverter.ToUInt16(data, index * 2);

                        float height = compressedHeight * normalize;
                        int destY = m_FlipVertically ? heightmapRes - 1 - y : y;
                        heights[destY, x] = height;
                    }
                }
            }
            else
            {
                float normalize =  1.0F / (1 << 8);
                for (int y = 0; y < heightmapRes; ++y)
                {
                    for (int x = 0; x < heightmapRes; ++x)
                    {
                        int index = Mathf.Clamp(x, 0, m_Resolution - 1) + Mathf.Clamp(y, 0, m_Resolution - 1) * m_Resolution;
                        byte compressedHeight = data[index];
                        float height = compressedHeight * normalize;
                        int destY = m_FlipVertically ? heightmapRes - 1 - y : y;
                        heights[destY, x] = height;
                    }
                }
            }

            terrainData.SetHeights(0, 0, heights);
        }

        internal void InitializeImportRaw(Terrain terrain, string path)
        {
            base.terrain = terrain;
            m_Path = path;
            PickRawDefaults(m_Path);
            helpString = "Raw files must use a single channel and be either 8 or 16 bit.";
            OnWizardUpdate();
        }
    }

    internal class ExportRawHeightmap : TerrainWizard
    {
        #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
        internal ExportRawHeightmap() { }
        #pragma warning restore UAL0015

        internal enum Depth { Bit8 = 1, Bit16 = 2 }

        public Depth m_Depth = Depth.Bit16;
        internal enum ByteOrder { Mac = 1, Windows = 2 }
        public ByteOrder m_ByteOrder = ByteOrder.Windows;
        public bool m_FlipVertically = false;

        public void OnEnable()
        {
            minSize = new Vector2(400, 200);
        }

        internal void OnWizardCreate()
        {
            if (terrain == null)
            {
                isValid = false;
                errorString = "Terrain does not exist";
            }

            string saveLocation = EditorUtility.SaveFilePanel("Save Raw Heightmap", "", "terrain", "raw");
            if (saveLocation != "")
            {
                WriteRaw(saveLocation);
                AssetDatabase.Refresh();
            }
        }

        internal override void OnWizardUpdate()
        {
            base.OnWizardUpdate();
            if (terrainData)
                helpString = "Resolution " + terrainData.heightmapResolution;
        }

        void WriteRaw(string path)
        {
            // Write data
            int heightmapRes = terrainData.heightmapResolution;
            float[,] heights = terrainData.GetHeights(0, 0, heightmapRes, heightmapRes);
            byte[] data = new byte[heightmapRes * heightmapRes * (int)m_Depth];

            if (m_Depth == Depth.Bit16)
            {
                float normalize = (1 << 16);
                for (int y = 0; y < heightmapRes; ++y)
                {
                    for (int x = 0; x < heightmapRes; ++x)
                    {
                        int index = x + y * heightmapRes;
                        int srcY = m_FlipVertically ? heightmapRes - 1 - y : y;
                        int height = Mathf.RoundToInt(heights[srcY, x] * normalize);
                        ushort compressedHeight = (ushort)Mathf.Clamp(height, 0, ushort.MaxValue);

                        byte[] byteData = System.BitConverter.GetBytes(compressedHeight);
                        // Yay, seems like this is the easiest way to swap bytes in C#. NUTS
                        if ((m_ByteOrder == ByteOrder.Mac) == System.BitConverter.IsLittleEndian)
                        {
                            data[index * 2 + 0] = byteData[1];
                            data[index * 2 + 1] = byteData[0];
                        }
                        else
                        {
                            data[index * 2 + 0] = byteData[0];
                            data[index * 2 + 1] = byteData[1];
                        }
                    }
                }
            }
            else
            {
                float normalize = (1 << 8);
                for (int y = 0; y < heightmapRes; ++y)
                {
                    for (int x = 0; x < heightmapRes; ++x)
                    {
                        int index = x + y * heightmapRes;
                        int srcY = m_FlipVertically ? heightmapRes - 1 - y : y;
                        int height = Mathf.RoundToInt(heights[srcY, x] * normalize);
                        byte compressedHeight = (byte)Mathf.Clamp(height, 0, byte.MaxValue);
                        data[index] = compressedHeight;
                    }
                }
            }

            FileStream fs = new FileStream(path, FileMode.Create);
            fs.Write(data, 0, data.Length);
            fs.Close();
        }

        new void ResetDefaults(Terrain terrain)
        {
            base.terrain = terrain;
            helpString = "Resolution " + terrain.terrainData.heightmapResolution;
            OnWizardUpdate();
        }
    }


    internal enum NavMeshLodIndex
    {
        First,
        Last,
        Custom
    }

    class TreeWizard : TerrainWizard
    {
        #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
        TreeWizard() { }
        #pragma warning restore UAL0015

        internal const int kNavMeshLodFirst = -1;
        internal const int kNavMeshLodLast = int.MaxValue;

        public GameObject   m_Tree;
        public float        m_BendFactor;
        public int          m_NavMeshLod;
        private int         m_PrototypeIndex = -1;
        private bool        m_IsValidTree = false;
        private TreePrototype m_EditedPrototype;
        private int         m_PrototypeCount;
        private bool        m_EditedPrototypeMissing = false;

        public void OnEnable()
        {
            minSize = new Vector2(400, 150);
        }

        private static bool IsValidTree(GameObject tree, int prototypeIndex, Terrain terrain)
        {
            if (tree == null)
                return false;
            var prototypes = terrain.terrainData.treePrototypes;
            for (int i = 0; i < prototypes.Length; ++i)
            {
                if (i != prototypeIndex && prototypes[i].m_Prefab == tree)
                    return false;
            }
            return true;
        }

        internal void InitializeDefaults(Terrain terrain, int index)
        {
            base.terrain = terrain;
            m_PrototypeIndex = index;
            m_EditedPrototypeMissing = false;

            if (m_PrototypeIndex == -1)
            {
                m_Tree = null;
                m_BendFactor = 0.0f;
                m_NavMeshLod = kNavMeshLodLast;
                m_EditedPrototype = null;
            }
            else
            {
                TreePrototype[] treePrototypes = base.terrain.terrainData.treePrototypes;
                TreePrototype treePrototype = treePrototypes[m_PrototypeIndex];
                m_Tree =            treePrototype.prefab;
                m_BendFactor =      treePrototype.bendFactor;
                m_NavMeshLod =      treePrototype.navMeshLod;
                m_EditedPrototype = treePrototype;
                m_PrototypeCount =  treePrototypes.Length;
            }

            m_IsValidTree = IsValidTree(m_Tree, m_PrototypeIndex, terrain);

            OnWizardUpdate();
        }

        private bool UpdatePrototypeIndex()
        {
            if (m_PrototypeIndex == -1 || terrainData == null)
                return false;
            if (!UpdateEditedPrototype(terrainData.treePrototypes, ref m_EditedPrototype, ref m_PrototypeIndex, ref m_PrototypeCount, ref m_EditedPrototypeMissing, (a, b) => a.Equals(b)))
                return false;

            m_IsValidTree = IsValidTree(m_Tree, m_PrototypeIndex, terrain);
            return true;
        }

        void DoApply()
        {
            if (terrainData == null)
                return;

            UpdatePrototypeIndex();
            if (m_EditedPrototypeMissing)
                return;

            var paintTrees = (PaintTreesTool)EditorTools.EditorToolManager.GetSingleton(typeof(PaintTreesTool));

            TreePrototype[] trees = terrain.terrainData.treePrototypes;
            if (m_PrototypeIndex == -1)
            {
                TreePrototype[] newTrees = new TreePrototype[trees.Length + 1];
                for (int i = 0; i < trees.Length; i++)
                    newTrees[i] = trees[i];
                newTrees[trees.Length] = new TreePrototype();
                newTrees[trees.Length].prefab = m_Tree;
                newTrees[trees.Length].bendFactor = m_BendFactor;
                newTrees[trees.Length].navMeshLod = m_NavMeshLod;
                m_PrototypeIndex = trees.Length;
                terrain.terrainData.treePrototypes = newTrees;
                paintTrees.selectedTree = m_PrototypeIndex;
            }
            else
            {
                trees[m_PrototypeIndex].prefab = m_Tree;
                trees[m_PrototypeIndex].bendFactor = m_BendFactor;
                trees[m_PrototypeIndex].navMeshLod = m_NavMeshLod;
                terrain.terrainData.treePrototypes = trees;
            }
            trees = terrain.terrainData.treePrototypes;
            m_EditedPrototype = trees[m_PrototypeIndex];
            m_PrototypeCount = trees.Length;
            terrain.Flush();
            EditorUtility.SetDirty(terrain);
        }

        void OnWizardCreate()
        {
            DoApply();
        }

        void OnWizardOtherButton()
        {
            DoApply();
        }

        protected override bool DrawWizardGUI()
        {
            if (UpdatePrototypeIndex())
                UpdateErrorState();

            EditorGUI.BeginChangeCheck();

            bool allowSceneObjects = !EditorUtility.IsPersistent(terrain.terrainData); // sometimes user prefers saving terrainData with the scene file
            m_Tree = (GameObject)EditorGUILayout.ObjectField("Tree Prefab", m_Tree, typeof(GameObject), allowSceneObjects);
            if (m_Tree)
            {
                MeshRenderer meshRenderer = m_Tree.GetComponent<MeshRenderer>();
                if (meshRenderer)
                {
                    EditorGUI.BeginDisabled(true);
                    EditorGUILayout.EnumPopup("Cast Shadows", meshRenderer.shadowCastingMode);
                    EditorGUI.EndDisabled();
                }
            }
            if (!TerrainEditorUtility.IsLODTreePrototype(m_Tree))
            {
                m_BendFactor = EditorGUILayout.FloatField("Bend Factor", m_BendFactor);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();

                LODGroup lodGroup = m_Tree.GetComponent<LODGroup>();

                NavMeshLodIndex navMeshLodIndex = NavMeshLodIndex.Custom;
                if (m_NavMeshLod == kNavMeshLodLast)
                    navMeshLodIndex = NavMeshLodIndex.Last;
                else if (m_NavMeshLod == kNavMeshLodFirst)
                    navMeshLodIndex = NavMeshLodIndex.First;

                navMeshLodIndex = (NavMeshLodIndex)EditorGUILayout.EnumPopup("NavMesh LOD Index", navMeshLodIndex, GUILayout.MinWidth(250));

                if (navMeshLodIndex == NavMeshLodIndex.First)
                    m_NavMeshLod = kNavMeshLodFirst;
                else if (navMeshLodIndex == NavMeshLodIndex.Last)
                    m_NavMeshLod = kNavMeshLodLast;
                else
                    m_NavMeshLod = EditorGUILayout.IntSlider(m_NavMeshLod, 0, Mathf.Max(0, lodGroup.lodCount - 1));

                EditorGUILayout.EndHorizontal();
            }

            bool changed = EditorGUI.EndChangeCheck();

            if (changed)
                m_IsValidTree = IsValidTree(m_Tree, m_PrototypeIndex, terrain);
            return changed;
        }

        void UpdateErrorState()
        {
            base.OnWizardUpdate();

            if (m_EditedPrototypeMissing)
            {
                errorString = "The tree being edited was removed or changed outside this window";
                isValid = false;
            }
            else if (m_Tree == null)
            {
                errorString = "Please assign a tree";
                isValid = false;
            }
            else if (!m_IsValidTree)
            {
                errorString = "Tree has already been selected as a prototype";
                isValid = false;
            }
        }

        internal override void OnWizardUpdate()
        {
            UpdatePrototypeIndex();
            UpdateErrorState();

            if (isValid && m_PrototypeIndex != -1)
                DoApply();
        }
    }

    internal class DetailWizardSharedStyles
    {
        public readonly GUIStyle helpBoxBig;
        public readonly GUIContent noiseSeed = L10n.TextContent("Noise Seed", "Specifies the random seed value for detail object placement.", null, null);
        public readonly GUIContent noiseSpread = L10n.TextContent("Noise Spread", "Controls the spatial frequency of the noise pattern used to vary the scale and color of the detail objects.", null, null);
        public readonly GUIContent detailDensity = L10n.TextContent("Detail density", "Controls detail density for this detail prototype, relative to it's size. Only enabled in \"Coverage\" detail scatter mode.", null, null);
        public readonly GUIContent holeEdgePadding = L10n.TextContent("Hole Edge Padding (%)", "Controls how far away detail objects are from the edge of the hole area.\n\nSpecify this value as a percentage of the detail width, which determines the radius of the circular area around the detail object used for hole testing.", null, null);
        public readonly GUIContent useDensityScaling = L10n.TextContent("Affected by Density Scale", "Toggles whether or not this detail prototype should be affected by the global density scaling setting in the Terrain settings.", null, null);
        public readonly GUIContent alignToGround = L10n.TextContent("Align To Ground (%)", "Rotate detail axis to ground normal direction.", null, null);
        public readonly GUIContent positionJitter = L10n.TextContent("Position Jitter (%)", "Controls the randomness of the detail distribution, from ordered to random. Only available when legacy distribution in Quality Settings is turned off.", null, null);

        public DetailWizardSharedStyles()
        {
            helpBoxBig = new GUIStyle("HelpBox")
            {
                fontSize = EditorStyles.label.fontSize
            };
        }

        [NoAutoStaticsCleanup] // lazy GUIContent/GUIStyle styles holder; editor infra, no user refs
        private static DetailWizardSharedStyles s_Styles = null;

        public static DetailWizardSharedStyles Instance
        {
            get
            {
                if (s_Styles == null)
                    s_Styles = new DetailWizardSharedStyles();
                return s_Styles;
            }
        }
    };

    public enum TerrainDetailMeshRenderMode
    {
        VertexLit,
        Grass
    }

    public class TerrainDetailMeshWizard : TerrainWizard
    {
        #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
        public TerrainDetailMeshWizard() { }
        #pragma warning restore UAL0015

        GameObject   m_DetailPrefab;
        float        m_MinWidth;
        float        m_MaxWidth;
        float        m_MinHeight;
        float        m_MaxHeight;
        int          m_NoiseSeed;
        float        m_NoiseSpread;
        float        m_DetailDensity;
        float        m_HoleEdgePadding;
        Color        m_HealthyColor;
        Color        m_DryColor;
        TerrainDetailMeshRenderMode m_RenderMode;
        bool         m_UseInstancing;
        bool         m_UseDensityScaling;
        float        m_AlignToGround;
        float        positionJitter;
        float        m_TargetCoverage;
        int          m_PrototypeIndex = -1;
        DetailPrototype m_EditedPrototype;
        int          m_PrototypeCount;
        bool         m_EditedPrototypeMissing = false;

        public void OnEnable()
        {
            minSize = new Vector2(400, 400);
        }

        public void ResetDefaults(Terrain terrain, int index)
        {
            base.terrain = terrain;
            m_PrototypeIndex = index;
            DetailPrototype prototype;
            if (m_PrototypeIndex == -1)
            {
                prototype = new DetailPrototype()
                {
                    renderMode = DetailRenderMode.VertexLit,
                    noiseSeed = UnityEngine.Random.Range(1, int.MaxValue),
                    useInstancing = true,
                    useDensityScaling = true
                };
            }
            else
            {
                DetailPrototype[] prototypes = base.terrain.terrainData.detailPrototypes;
                prototype = prototypes[m_PrototypeIndex];
                m_PrototypeCount = prototypes.Length;
            }
            m_EditedPrototype = m_PrototypeIndex == -1 ? null : prototype;
            m_EditedPrototypeMissing = false;

            m_DetailPrefab = prototype.prototype;
            m_MinWidth = prototype.minWidth;
            m_MaxWidth = prototype.maxWidth;
            m_MinHeight = prototype.minHeight;
            m_MaxHeight = prototype.maxHeight;
            m_NoiseSeed = prototype.noiseSeed;
            m_NoiseSpread = prototype.noiseSpread;
            m_DetailDensity = prototype.density;
            m_HoleEdgePadding = Mathf.Clamp01(prototype.holeEdgePadding) * 100.0f;
            m_HealthyColor = prototype.healthyColor;
            m_DryColor = prototype.dryColor;
            switch (prototype.renderMode)
            {
                case DetailRenderMode.GrassBillboard:
                    Debug.LogError("Detail meshes can't be rendered as billboards");
                    m_RenderMode = TerrainDetailMeshRenderMode.Grass;
                    break;
                case DetailRenderMode.Grass:
                    m_RenderMode = TerrainDetailMeshRenderMode.Grass;
                    break;
                case DetailRenderMode.VertexLit:
                    m_RenderMode = TerrainDetailMeshRenderMode.VertexLit;
                    break;
            }
            m_UseInstancing = prototype.useInstancing;
            m_UseDensityScaling = prototype.useDensityScaling;
            m_AlignToGround = Mathf.Clamp01(prototype.alignToGround) * 100.0f;
            positionJitter = Mathf.Clamp01(prototype.positionJitter) * 100.0f;
            m_TargetCoverage = prototype.targetCoverage;

            OnWizardUpdate();
        }

        private DetailRenderMode ComputeRenderMode()
            => m_RenderMode == TerrainDetailMeshRenderMode.Grass && !m_UseInstancing ? DetailRenderMode.Grass : DetailRenderMode.VertexLit;

        DetailPrototype MakePrototype()
        {
            return new DetailPrototype
            {
                prototype = m_DetailPrefab,
                prototypeTexture = null,
                minWidth = m_MinWidth,
                maxWidth = m_MaxWidth,
                minHeight = m_MinHeight,
                maxHeight = m_MaxHeight,
                noiseSeed = m_NoiseSeed,
                noiseSpread = m_NoiseSpread,
                holeEdgePadding = m_HoleEdgePadding / 100.0f,
                density = m_DetailDensity,
                healthyColor = m_HealthyColor,
                dryColor = m_DryColor,
                renderMode = ComputeRenderMode(),
                usePrototypeMesh = true,
                useInstancing = m_UseInstancing,
                useDensityScaling = m_UseDensityScaling,
                alignToGround = m_AlignToGround / 100.0f,
                positionJitter = positionJitter / 100.0f,
                targetCoverage = m_TargetCoverage
            };
        }

        private bool UpdatePrototypeIndex()
            => m_PrototypeIndex != -1 && terrainData != null
            && UpdateEditedPrototype(terrainData.detailPrototypes, ref m_EditedPrototype, ref m_PrototypeIndex, ref m_PrototypeCount, ref m_EditedPrototypeMissing, IsSameDetail);

        void DoApply()
        {
            if (terrainData == null)
                return;

            UpdatePrototypeIndex();
            if (m_EditedPrototypeMissing)
                return;

            DetailPrototype[] prototypes = terrainData.detailPrototypes;
            if (m_PrototypeIndex == -1)
            {
                // Add a new detailprototype to the prototype arrays
                DetailPrototype[] newarray = new DetailPrototype[prototypes.Length + 1];
                System.Array.Copy(prototypes, 0, newarray, 0, prototypes.Length);
                m_PrototypeIndex = prototypes.Length;
                prototypes = newarray;
            }
            prototypes[m_PrototypeIndex] = MakePrototype();

            terrainData.detailPrototypes = prototypes;
            prototypes = terrainData.detailPrototypes;
            m_EditedPrototype = prototypes[m_PrototypeIndex];
            m_PrototypeCount = prototypes.Length;
            EditorUtility.SetDirty(terrainData);
        }

        void OnWizardCreate()
        {
            DoApply();
        }

        void OnWizardOtherButton()
        {
            DoApply();
        }

        void UpdateErrorState()
        {
            base.OnWizardUpdate();

            if (!isValid)
                return;

            if (m_EditedPrototypeMissing)
            {
                errorString = "The detail being edited was removed or changed outside this window";
                isValid = false;
            }
            else if (!MakePrototype().Validate(out var errorMessage))
            {
                errorString = errorMessage;
                isValid = false;
            }
        }

        internal override void OnWizardUpdate()
        {
            UpdatePrototypeIndex();
            UpdateErrorState();

            if (isValid && m_PrototypeIndex != -1)
                DoApply();
        }

        protected override bool DrawWizardGUI()
        {
            if (UpdatePrototypeIndex())
                UpdateErrorState();

            EditorGUI.BeginChangeCheck();

            m_DetailPrefab = EditorGUILayout.ObjectField("Detail Prefab", m_DetailPrefab, typeof(GameObject), !TerrainDataIsPersistent) as GameObject;
            GUI.enabled = terrainData.detailScatterMode == DetailScatterMode.CoverageMode;
            GUI.enabled = true;
            m_AlignToGround = EditorGUILayout.Slider(DetailWizardSharedStyles.Instance.alignToGround, m_AlignToGround, 0, 100);
            GUI.enabled = !QualitySettings.useLegacyDetailDistribution;
            positionJitter = EditorGUILayout.Slider(DetailWizardSharedStyles.Instance.positionJitter, positionJitter, 0, 100);
            GUI.enabled = true;
            m_MinWidth = EditorGUILayout.FloatField("Min Width", m_MinWidth);
            m_MaxWidth = EditorGUILayout.FloatField("Max Width", m_MaxWidth);
            m_MinHeight = EditorGUILayout.FloatField("Min Height", m_MinHeight);
            m_MaxHeight = EditorGUILayout.FloatField("Max Height", m_MaxHeight);
            m_NoiseSeed = EditorGUILayout.IntField(DetailWizardSharedStyles.Instance.noiseSeed, m_NoiseSeed);
            m_NoiseSpread = EditorGUILayout.FloatField(DetailWizardSharedStyles.Instance.noiseSpread, m_NoiseSpread);
            m_HoleEdgePadding = EditorGUILayout.Slider(DetailWizardSharedStyles.Instance.holeEdgePadding, m_HoleEdgePadding, 0, 100);

            GUI.enabled = terrainData.detailScatterMode == DetailScatterMode.CoverageMode;
            m_DetailDensity = EditorGUILayout.Slider(DetailWizardSharedStyles.Instance.detailDensity, m_DetailDensity, 0, 5);
            GUI.enabled = true;

            if (!m_UseInstancing)
            {
                m_HealthyColor = EditorGUILayout.ColorField("Healthy Color", m_HealthyColor);
                m_DryColor = EditorGUILayout.ColorField("Dry Color", m_DryColor);
            }

            if (m_UseInstancing)
            {
                EditorGUI.BeginDisabled(true);
                EditorGUILayout.EnumPopup("Render Mode", TerrainDetailMeshRenderMode.VertexLit);
                EditorGUI.EndDisabled();
            }
            else
                m_RenderMode = (TerrainDetailMeshRenderMode)EditorGUILayout.EnumPopup("Render Mode", m_RenderMode);

            m_UseInstancing = EditorGUILayout.Toggle("Use GPU Instancing", m_UseInstancing);
            if (m_UseInstancing)
                EditorGUILayout.HelpBox("Using GPU Instancing would enable using the Material you set on the prefab.", MessageType.Info);

            m_UseDensityScaling = EditorGUILayout.Toggle(DetailWizardSharedStyles.Instance.useDensityScaling, m_UseDensityScaling);

            if (!DetailPrototype.IsModeSupportedByRenderPipeline(ComputeRenderMode(), m_UseInstancing, out var message))
                EditorGUILayout.LabelField(EditorGUIUtility.TempContent(message, EditorGUIUtility.GetHelpIcon(MessageType.Error)), DetailWizardSharedStyles.Instance.helpBoxBig);

            return EditorGUI.EndChangeCheck();
        }
    }

    public class TerrainDetailTextureWizard : TerrainWizard
    {
        #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
        public TerrainDetailTextureWizard() { }
        #pragma warning restore UAL0015

        Texture2D    m_DetailTexture;
        float        m_MinWidth;
        float        m_MaxWidth;
        float        m_MinHeight;
        float        m_MaxHeight;
        int          m_NoiseSeed;
        float        m_NoiseSpread;
        float        m_DetailDensity;
        float        m_HoleEdgePadding;
        Color        m_HealthyColor;
        Color        m_DryColor;
        bool         m_Billboard;
        bool         m_UseDensityScaling;
        float        m_AlignToGround;
        float        m_PositionJitter;

        int      m_PrototypeIndex = -1;
        DetailPrototype m_EditedPrototype;
        int      m_PrototypeCount;
        bool     m_EditedPrototypeMissing = false;

        public void OnEnable()
        {
            minSize = new Vector2(400, 400);
        }

        public void ResetDefaults(Terrain terrain, int index)
        {
            base.terrain = terrain;

            m_PrototypeIndex = index;
            DetailPrototype prototype;
            if (m_PrototypeIndex == -1)
            {
                prototype = new DetailPrototype();
                prototype.noiseSeed = UnityEngine.Random.Range(1, int.MaxValue);
                prototype.renderMode = DetailRenderMode.GrassBillboard;
                prototype.useDensityScaling = true;
                if (GraphicsSettings.currentRenderPipeline != null && GraphicsSettings.GetDefaultShader(DefaultShaderType.TerrainDetailGrassBillboard) == null)
                    prototype.renderMode = DetailRenderMode.Grass;
            }
            else
            {
                DetailPrototype[] prototypes = base.terrain.terrainData.detailPrototypes;
                prototype = prototypes[m_PrototypeIndex];
                m_PrototypeCount = prototypes.Length;
            }
            m_EditedPrototype = m_PrototypeIndex == -1 ? null : prototype;
            m_EditedPrototypeMissing = false;

            m_DetailTexture = prototype.prototypeTexture;
            m_MinWidth = prototype.minWidth;
            m_MaxWidth = prototype.maxWidth;
            m_MinHeight = prototype.minHeight;
            m_MaxHeight = prototype.maxHeight;
            m_NoiseSeed = prototype.noiseSeed;
            m_NoiseSpread = prototype.noiseSpread;
            m_DetailDensity = prototype.density;
            m_HoleEdgePadding = Mathf.Clamp01(prototype.holeEdgePadding) * 100.0f;
            m_HealthyColor = prototype.healthyColor;
            m_DryColor = prototype.dryColor;
            m_Billboard = prototype.renderMode == DetailRenderMode.GrassBillboard;
            m_UseDensityScaling = prototype.useDensityScaling;
            m_AlignToGround = Mathf.Clamp01(prototype.alignToGround) * 100.0f;
            m_PositionJitter = Mathf.Clamp01(prototype.positionJitter) * 100.0f;

            OnWizardUpdate();
        }

        private static DetailRenderMode ComputeRenderMode(bool billboard)
            => billboard ? DetailRenderMode.GrassBillboard : DetailRenderMode.Grass;

        DetailPrototype MakePrototype()
        {
            return new DetailPrototype
            {
                prototype = null,
                prototypeTexture = m_DetailTexture,
                minWidth = m_MinWidth,
                maxWidth = m_MaxWidth,
                minHeight = m_MinHeight,
                maxHeight = m_MaxHeight,
                noiseSeed = m_NoiseSeed,
                noiseSpread = m_NoiseSpread,
                density = m_DetailDensity,
                holeEdgePadding = m_HoleEdgePadding / 100.0f,
                healthyColor = m_HealthyColor,
                dryColor = m_DryColor,
                renderMode = ComputeRenderMode(m_Billboard),
                usePrototypeMesh = false,
                useInstancing = false,
                useDensityScaling = m_UseDensityScaling,
                alignToGround = m_AlignToGround / 100.0f,
                positionJitter = m_PositionJitter / 100.0f
            };
        }

        private bool UpdatePrototypeIndex()
            => m_PrototypeIndex != -1 && terrainData != null
            && UpdateEditedPrototype(terrainData.detailPrototypes, ref m_EditedPrototype, ref m_PrototypeIndex, ref m_PrototypeCount, ref m_EditedPrototypeMissing, IsSameDetail);

        void DoApply()
        {
            if (terrainData == null)
                return;

            UpdatePrototypeIndex();
            if (m_EditedPrototypeMissing)
                return;

            DetailPrototype[] prototypes = terrainData.detailPrototypes;
            if (m_PrototypeIndex == -1)
            {
                // Add a new detailprototype to the prototype arrays
                DetailPrototype[] newarray = new DetailPrototype[prototypes.Length + 1];
                System.Array.Copy(prototypes, 0, newarray, 0, prototypes.Length);
                m_PrototypeIndex = prototypes.Length;
                prototypes = newarray;
            }
            prototypes[m_PrototypeIndex] = MakePrototype();

            terrainData.detailPrototypes = prototypes;
            prototypes = terrainData.detailPrototypes;
            m_EditedPrototype = prototypes[m_PrototypeIndex];
            m_PrototypeCount = prototypes.Length;
            EditorUtility.SetDirty(terrainData);
        }

        void OnWizardCreate()
        {
            DoApply();
        }

        void OnWizardOtherButton()
        {
            DoApply();
        }

        internal override void OnWizardUpdate()
        {
            m_MinHeight = Mathf.Max(0f, m_MinHeight);
            m_MaxHeight = Mathf.Max(m_MinHeight, m_MaxHeight);
            m_MinWidth = Mathf.Max(0f, m_MinWidth);
            m_MaxWidth = Mathf.Max(m_MinWidth, m_MaxWidth);

            UpdatePrototypeIndex();
            UpdateErrorState();

            if (isValid && m_PrototypeIndex != -1)
                DoApply();
        }

        void UpdateErrorState()
        {
            base.OnWizardUpdate();

            if (!isValid)
                return;

            if (m_EditedPrototypeMissing)
            {
                errorString = "The detail being edited was removed or changed outside this window";
                isValid = false;
            }
            else if (!MakePrototype().Validate(out var errorMessage))
            {
                errorString = errorMessage;
                isValid = false;
            }
        }

        protected override bool DrawWizardGUI()
        {
            if (UpdatePrototypeIndex())
                UpdateErrorState();

            EditorGUI.BeginChangeCheck();

            Rect r = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            m_DetailTexture = EditorGUI.ObjectField(r, "Detail Texture", m_DetailTexture, typeof(Texture2D), !TerrainDataIsPersistent) as Texture2D;

            m_AlignToGround = EditorGUILayout.Slider(DetailWizardSharedStyles.Instance.alignToGround, m_AlignToGround, 0, 100);
            m_PositionJitter = EditorGUILayout.Slider(DetailWizardSharedStyles.Instance.positionJitter, m_PositionJitter, 0, 100);
            m_MinWidth = EditorGUILayout.FloatField("Min Width", m_MinWidth);
            m_MaxWidth = EditorGUILayout.FloatField("Max Width", m_MaxWidth);
            m_MinHeight = EditorGUILayout.FloatField("Min Height", m_MinHeight);
            m_MaxHeight = EditorGUILayout.FloatField("Max Height", m_MaxHeight);
            m_NoiseSeed = EditorGUILayout.IntField(DetailWizardSharedStyles.Instance.noiseSeed, m_NoiseSeed);
            m_NoiseSpread = EditorGUILayout.FloatField(DetailWizardSharedStyles.Instance.noiseSpread, m_NoiseSpread);
            GUI.enabled = terrainData.detailScatterMode == DetailScatterMode.CoverageMode;
            m_DetailDensity = EditorGUILayout.Slider(DetailWizardSharedStyles.Instance.detailDensity, m_DetailDensity, 0, 5);
            GUI.enabled = true;
            m_HoleEdgePadding = EditorGUILayout.Slider(DetailWizardSharedStyles.Instance.holeEdgePadding, m_HoleEdgePadding, 0, 100);

            m_HealthyColor = EditorGUILayout.ColorField("Healthy Color", m_HealthyColor);
            m_DryColor = EditorGUILayout.ColorField("Dry Color", m_DryColor);

            m_Billboard = EditorGUILayout.Toggle("Billboard", m_Billboard);

            m_UseDensityScaling = EditorGUILayout.Toggle(DetailWizardSharedStyles.Instance.useDensityScaling, m_UseDensityScaling);

            if (!DetailPrototype.IsModeSupportedByRenderPipeline(ComputeRenderMode(m_Billboard), false, out var message))
                EditorGUILayout.LabelField(EditorGUIUtility.TempContent(message, EditorGUIUtility.GetHelpIcon(MessageType.Error)), DetailWizardSharedStyles.Instance.helpBoxBig);

            return EditorGUI.EndChangeCheck();
        }
    }

    class PlaceTreeWizard : TerrainWizard
    {
        #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
        PlaceTreeWizard() { }
        #pragma warning restore UAL0015

        public int numberOfTrees = 10000;
        public bool keepExistingTrees = true;
        private const int kMaxNumberOfTrees = 1000000;

        public void OnEnable()
        {
            minSize = new Vector2(250, 150);
        }

        void OnWizardCreate()
        {
            var paintTrees = (PaintTreesTool)EditorTools.EditorToolManager.GetSingleton(typeof(PaintTreesTool));
            if (numberOfTrees > kMaxNumberOfTrees)
            {
                isValid = false;
                errorString = String.Format("Mass placing more than {0} trees is not supported", kMaxNumberOfTrees);
                Debug.LogError(errorString);
                return;
            }
            paintTrees.MassPlaceTrees(terrain.terrainData, numberOfTrees, true, keepExistingTrees);
            terrain.Flush();
        }
    }

    class FlattenHeightmap : TerrainWizard
    {
        #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
        FlattenHeightmap() { }
        #pragma warning restore UAL0015

        public float height = 0.0F;

        internal override void OnWizardUpdate()
        {
            if (terrainData)
                helpString = height + " meters (" + height / terrainData.size.y * 100 + "%)";
        }

        void OnWizardCreate()
        {
            Undo.RegisterCompleteObjectUndo(terrainData, "Flatten Heightmap");
            HeightmapFilters.Flatten(terrainData, height / terrainData.size.y);
        }
    }

    ///Shut up
    internal class TerrainWizards {}

    /*

    class ImportTextureHeightmap : TerrainWizard
    {
        public Vector3     m_TerrainSize = new Vector3 (2000, 600, 2000);
        public Texture2D   m_Heightmap;

        /// Creates a heightmap from a heightmap texture and a given size of the terrrain
        static public void ImportHeightmap (TerrainData theTerrain, Texture2D texture, Vector3 importSize)
        {
            theTerrain.ResetHeightmap(Mathf.Max(texture.width, texture.height));

            // use clamp wrap mode. Avoid high tesselation at borders
            TextureWrapMode wrapMode = texture.wrapMode;
            texture.wrapMode = TextureWrapMode.Clamp;
    // @TODO:
    //      if (texture.format != TextureFormat.Alpha8 && texture.format != TextureFormat.RGB24 && texture.format != TextureFormat.ARGB32 && texture.format != TextureFormat.RGBA32) {
    //          Debug.Log ("Heightmap texture must be an uncompressed texture");
    //          return;
    //      }

            float[,] heights = new float[theTerrain.heightmapHeight, theTerrain.heightmapWidth];
            for (int y=0;y<theTerrain.heightmapHeight;y++)
            {
                for (int x=0;x<theTerrain.heightmapWidth;x++)
                {
                    heights[y,x] = texture.GetPixel(x, y).grayscale;
                }
            }

            texture.wrapMode = wrapMode;
            theTerrain.size = importSize;
            theTerrain.SetHeights (0, 0, heights);
        }


        void OnWizardCreate ()
        {
            ImportHeightmap (m_Terrain.terrainData, m_Heightmap, m_TerrainSize);
            FlushHeightmapModification();
        }
    }
    */
} //namespace
