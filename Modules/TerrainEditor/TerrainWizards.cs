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

namespace UnityEditor
{
    internal class TerrainWizard : ScriptableWizard
    {
        internal const int kMaxResolution = 4097;

        protected Terrain      m_Terrain;
        protected TerrainData    terrainData
        {
            get
            {
                if (m_Terrain != null)
                    return m_Terrain.terrainData;
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
            if (m_Terrain == null || m_Terrain.terrainData == null)
            {
                isValid = false;
                errorString = "Terrain does not exist";
            }
        }

        internal void InitializeDefaults(Terrain terrain)
        {
            m_Terrain = terrain;
            OnWizardUpdate();
        }

        internal void FlushHeightmapModification()
        {
            //@TODO:        m_Terrain.treeDatabase.RecalculateTreePosition();
            m_Terrain.Flush();
        }

        internal static T DisplayTerrainWizard<T>(string title, string button) where T : TerrainWizard
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
            if (m_Terrain == null)
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
            m_Terrain = terrain;
            m_Path = path;
            PickRawDefaults(m_Path);
            helpString = "Raw files must use a single channel and be either 8 or 16 bit.";
            OnWizardUpdate();
        }
    }

    internal class ExportRawHeightmap : TerrainWizard
    {
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
            if (m_Terrain == null)
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

        new void InitializeDefaults(Terrain terrain)
        {
            m_Terrain = terrain;
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
        internal const int kNavMeshLodFirst = -1;
        internal const int kNavMeshLodLast = int.MaxValue;

        public GameObject   m_Tree;
        public float        m_BendFactor;
        public int          m_NavMeshLod;
        private int         m_PrototypeIndex = -1;
        private bool        m_IsValidTree = false;

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
            m_Terrain = terrain;
            m_PrototypeIndex = index;

            if (m_PrototypeIndex == -1)
            {
                m_Tree = null;
                m_BendFactor = 0.0f;
                m_NavMeshLod = kNavMeshLodLast;
            }
            else
            {
                TreePrototype  treePrototype = m_Terrain.terrainData.treePrototypes[m_PrototypeIndex];
                m_Tree =       treePrototype.prefab;
                m_BendFactor = treePrototype.bendFactor;
                m_NavMeshLod = treePrototype.navMeshLod;
            }

            m_IsValidTree = IsValidTree(m_Tree, m_PrototypeIndex, terrain);

            OnWizardUpdate();
        }

        void DoApply()
        {
            if (terrainData == null)
                return;
            TreePrototype[] trees = m_Terrain.terrainData.treePrototypes;
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
                m_Terrain.terrainData.treePrototypes = newTrees;
                PaintTreesTool.instance.selectedTree = m_PrototypeIndex;
            }
            else
            {
                trees[m_PrototypeIndex].prefab = m_Tree;
                trees[m_PrototypeIndex].bendFactor = m_BendFactor;
                trees[m_PrototypeIndex].navMeshLod = m_NavMeshLod;

                m_Terrain.terrainData.treePrototypes = trees;
            }
            m_Terrain.Flush();
            EditorUtility.SetDirty(m_Terrain);
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
            EditorGUI.BeginChangeCheck();

            bool allowSceneObjects = !EditorUtility.IsPersistent(m_Terrain.terrainData); // sometimes user prefers saving terrainData with the scene file
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
                m_IsValidTree = IsValidTree(m_Tree, m_PrototypeIndex, m_Terrain);
            return changed;
        }

        internal override void OnWizardUpdate()
        {
            base.OnWizardUpdate();

            if (m_Tree == null)
            {
                errorString = "Please assign a tree";
                isValid = false;
            }
            else if (!m_IsValidTree)
            {
                errorString = "Tree has already been selected as a prototype";
                isValid = false;
            }
            else if (m_PrototypeIndex != -1)
            {
                DoApply();
            }
        }
    }

    internal class DetailWizardSharedStyles
    {
        public readonly GUIStyle helpBoxBig;
        public readonly GUIContent noiseSeed = EditorGUIUtility.TrTextContent("Noise Seed", "Specifies the random seed value for detail object placement.");
        public readonly GUIContent noiseSpread = EditorGUIUtility.TrTextContent("Noise Spread", "Controls the spatial frequency of the noise pattern used to vary the scale and color of the detail objects.");
        public readonly GUIContent detailDensity = EditorGUIUtility.TrTextContent("Detail density", "Controls detail density for this detail prototype, relative to it's size. Only enabled in \"Coverage\" detail scatter mode.");
        public readonly GUIContent holeEdgePadding = EditorGUIUtility.TrTextContent("Hole Edge Padding (%)", "Controls how far away detail objects are from the edge of the hole area.\n\nSpecify this value as a percentage of the detail width, which determines the radius of the circular area around the detail object used for hole testing.");
        public readonly GUIContent useDensityScaling = EditorGUIUtility.TrTextContent("Affected by Density Scale", "Toggles whether or not this detail prototype should be affected by the global density scaling setting in the Terrain settings.");
        public readonly GUIContent alignToGround = EditorGUIUtility.TrTextContent("Align To Ground (%)", "Rotate detail axis to ground normal direction.");
        public readonly GUIContent positionOrderliness = EditorGUIUtility.TrTextContent("Position Orderliness (%)", "Controls how to generate position between random and quasirandom. \n\nquasirandom prevents instances from overlapping each other.");

        public DetailWizardSharedStyles()
        {
            helpBoxBig = new GUIStyle("HelpBox")
            {
                fontSize = EditorStyles.label.fontSize
            };
        }

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

    enum DetailMeshRenderMode
    {
        VertexLit,
        Grass
    }

    class DetailMeshWizard : TerrainWizard
    {
        public GameObject   m_DetailPrefab;
        public float        m_MinWidth;
        public float        m_MaxWidth;
        public float        m_MinHeight;
        public float        m_MaxHeight;
        public int          m_NoiseSeed;
        public float        m_NoiseSpread;
        public float        m_DetailDensity;
        public float        m_HoleEdgePadding;
        public Color        m_HealthyColor;
        public Color        m_DryColor;
        public DetailMeshRenderMode m_RenderMode;
        public bool         m_UseInstancing;
        public bool         m_UseDensityScaling;
        public float        m_AlignToGround;
        public float        m_PositionOrderliness;

        private int     m_PrototypeIndex = -1;

        public void OnEnable()
        {
            minSize = new Vector2(400, 400);
        }

        internal void InitializeDefaults(Terrain terrain, int index)
        {
            m_Terrain = terrain;
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
                prototype = m_Terrain.terrainData.detailPrototypes[m_PrototypeIndex];

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
                    m_RenderMode = DetailMeshRenderMode.Grass;
                    break;
                case DetailRenderMode.Grass:
                    m_RenderMode = DetailMeshRenderMode.Grass;
                    break;
                case DetailRenderMode.VertexLit:
                    m_RenderMode = DetailMeshRenderMode.VertexLit;
                    break;
            }
            m_UseInstancing = prototype.useInstancing;
            m_UseDensityScaling = prototype.useDensityScaling;
            m_AlignToGround = Mathf.Clamp01(prototype.alignToGround) * 100.0f;
            m_PositionOrderliness = Mathf.Clamp01(prototype.positionOrderliness) * 100.0f;

            OnWizardUpdate();
        }

        private DetailRenderMode ComputeRenderMode()
            => m_RenderMode == DetailMeshRenderMode.Grass && !m_UseInstancing ? DetailRenderMode.Grass : DetailRenderMode.VertexLit;

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
                positionOrderliness = m_PositionOrderliness / 100.0f
            };
        }

        void DoApply()
        {
            if (terrainData == null)
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
            base.OnWizardUpdate();

            if (!isValid)
                return;

            if (!MakePrototype().Validate(out var errorMessage))
            {
                errorString = errorMessage;
                isValid = false;
            }
            else if (m_PrototypeIndex != -1)
            {
                DoApply();
            }
        }

        protected override bool DrawWizardGUI()
        {
            EditorGUI.BeginChangeCheck();

            m_DetailPrefab = EditorGUILayout.ObjectField("Detail Prefab", m_DetailPrefab, typeof(GameObject), !TerrainDataIsPersistent) as GameObject;
            m_AlignToGround = EditorGUILayout.Slider(DetailWizardSharedStyles.Instance.alignToGround, m_AlignToGround, 0, 100);
            m_PositionOrderliness = EditorGUILayout.Slider(DetailWizardSharedStyles.Instance.positionOrderliness, m_PositionOrderliness, 0, 100);
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
                EditorGUILayout.EnumPopup("Render Mode", DetailMeshRenderMode.VertexLit);
                EditorGUI.EndDisabled();
            }
            else
                m_RenderMode = (DetailMeshRenderMode)EditorGUILayout.EnumPopup("Render Mode", m_RenderMode);

            m_UseInstancing = EditorGUILayout.Toggle("Use GPU Instancing", m_UseInstancing);
            if (m_UseInstancing)
                EditorGUILayout.HelpBox("Using GPU Instancing would enable using the Material you set on the prefab.", MessageType.Info);

            m_UseDensityScaling = EditorGUILayout.Toggle(DetailWizardSharedStyles.Instance.useDensityScaling, m_UseDensityScaling);

            if (!DetailPrototype.IsModeSupportedByRenderPipeline(ComputeRenderMode(), m_UseInstancing, out var message))
                EditorGUILayout.LabelField(EditorGUIUtility.TempContent(message, EditorGUIUtility.GetHelpIcon(MessageType.Error)), DetailWizardSharedStyles.Instance.helpBoxBig);

            return EditorGUI.EndChangeCheck();
        }
    }

    class DetailTextureWizard : TerrainWizard
    {
        public Texture2D    m_DetailTexture;
        public float        m_MinWidth;
        public float        m_MaxWidth;
        public float        m_MinHeight;
        public float        m_MaxHeight;
        public int          m_NoiseSeed;
        public float        m_NoiseSpread;
        public float        m_DetailDensity;
        public float        m_HoleEdgePadding;
        public Color        m_HealthyColor;
        public Color        m_DryColor;
        public bool         m_Billboard;
        public bool         m_UseDensityScaling;
        public float        m_AlignToGround;
        public float        m_PositionOrderliness;

        private int      m_PrototypeIndex = -1;

        public void OnEnable()
        {
            minSize = new Vector2(400, 400);
        }

        internal void InitializeDefaults(Terrain terrain, int index)
        {
            m_Terrain = terrain;

            m_PrototypeIndex = index;
            DetailPrototype prototype;
            if (m_PrototypeIndex == -1)
            {
                prototype = new DetailPrototype();
                prototype.noiseSeed = UnityEngine.Random.Range(1, int.MaxValue);
                prototype.renderMode = DetailRenderMode.GrassBillboard;
                prototype.useDensityScaling = true;
                if (GraphicsSettings.currentRenderPipeline != null && GraphicsSettings.currentRenderPipeline.terrainDetailGrassBillboardShader == null)
                    prototype.renderMode = DetailRenderMode.Grass;
            }
            else
                prototype = m_Terrain.terrainData.detailPrototypes[m_PrototypeIndex];

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
            m_PositionOrderliness = Mathf.Clamp01(prototype.positionOrderliness) * 100.0f;

            OnWizardUpdate();
        }

        DetailRenderMode ComputeRenderMode()
            => m_Billboard ? DetailRenderMode.GrassBillboard : DetailRenderMode.Grass;

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
                renderMode = ComputeRenderMode(),
                usePrototypeMesh = false,
                useInstancing = false,
                useDensityScaling = m_UseDensityScaling,
                alignToGround = m_AlignToGround / 100.0f,
                positionOrderliness = m_PositionOrderliness / 100.0f
            };
        }

        void DoApply()
        {
            if (terrainData == null)
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

            base.OnWizardUpdate();

            if (!isValid)
                return;

            if (!MakePrototype().Validate(out var errorMessage))
            {
                errorString = errorMessage;
                isValid = false;
            }
            else if (m_PrototypeIndex != -1)
            {
                DoApply();
            }
        }

        protected override bool DrawWizardGUI()
        {
            EditorGUI.BeginChangeCheck();

            Rect r = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            m_DetailTexture = EditorGUI.ObjectField(r, "Detail Texture", m_DetailTexture, typeof(Texture2D), !TerrainDataIsPersistent) as Texture2D;

            m_AlignToGround = EditorGUILayout.Slider(DetailWizardSharedStyles.Instance.alignToGround, m_AlignToGround, 0, 100);
            m_PositionOrderliness = EditorGUILayout.Slider(DetailWizardSharedStyles.Instance.positionOrderliness, m_PositionOrderliness, 0, 100);
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

            if (!DetailPrototype.IsModeSupportedByRenderPipeline(ComputeRenderMode(), false, out var message))
                EditorGUILayout.LabelField(EditorGUIUtility.TempContent(message, EditorGUIUtility.GetHelpIcon(MessageType.Error)), DetailWizardSharedStyles.Instance.helpBoxBig);

            return EditorGUI.EndChangeCheck();
        }
    }

    class PlaceTreeWizard : TerrainWizard
    {
        public int numberOfTrees = 10000;
        public bool keepExistingTrees = true;
        private const int kMaxNumberOfTrees = 1000000;

        public void OnEnable()
        {
            minSize = new Vector2(250, 150);
        }

        void OnWizardCreate()
        {
            if (numberOfTrees > kMaxNumberOfTrees)
            {
                isValid = false;
                errorString = String.Format("Mass placing more than {0} trees is not supported", kMaxNumberOfTrees);
                Debug.LogError(errorString);
                return;
            }
            PaintTreesTool.instance.MassPlaceTrees(m_Terrain.terrainData, numberOfTrees, true, keepExistingTrees);
            m_Terrain.Flush();
        }
    }

    class FlattenHeightmap : TerrainWizard
    {
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
