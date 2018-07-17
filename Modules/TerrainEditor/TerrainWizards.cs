// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

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
        public int m_Width = 1;
        public int m_Height = 1;
        public ByteOrder m_ByteOrder = ByteOrder.Windows;
        public bool m_FlipVertically = false;
        public Vector3 m_TerrainSize = new Vector3(2000, 600, 2000);
        private string m_Path;

        void PickRawDefaults(string path)
        {
            FileStream file = File.Open(path, FileMode.Open, FileAccess.Read);
            int fileSize = (int)file.Length;
            file.Close();

            m_TerrainSize = terrainData.size;

            if (terrainData.heightmapWidth * terrainData.heightmapHeight == fileSize)
            {
                m_Width = terrainData.heightmapWidth;
                m_Height = terrainData.heightmapHeight;
                m_Depth = Depth.Bit8;
            }
            else if (terrainData.heightmapWidth * terrainData.heightmapHeight * 2 == fileSize)
            {
                m_Width = terrainData.heightmapWidth;
                m_Height = terrainData.heightmapHeight;
                m_Depth = Depth.Bit16;
            }
            else
            {
                m_Depth = Depth.Bit16;

                int pixels = fileSize / (int)m_Depth;
                int width = Mathf.RoundToInt(Mathf.Sqrt(pixels));
                int height = Mathf.RoundToInt(Mathf.Sqrt(pixels));
                if ((width * height * (int)m_Depth) == fileSize)
                {
                    m_Width = width;
                    m_Height = height;
                    return;
                }


                m_Depth = Depth.Bit8;

                pixels = fileSize / (int)m_Depth;
                width = Mathf.RoundToInt(Mathf.Sqrt(pixels));
                height = Mathf.RoundToInt(Mathf.Sqrt(pixels));
                if ((width * height * (int)m_Depth) == fileSize)
                {
                    m_Width = width;
                    m_Height = height;
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

            if (m_Width > kMaxResolution || m_Height > kMaxResolution)
            {
                isValid = false;
                errorString = "Heightmaps above 4097x4097 in resolution are not supported";
                Debug.LogError(errorString);
            }

            if (File.Exists(m_Path) && isValid)
            {
                Undo.RegisterCompleteObjectUndo(terrainData, "Import Raw heightmap");

                terrainData.heightmapResolution = Mathf.Max(m_Width, m_Height);
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
                data = br.ReadBytes(m_Width * m_Height * (int)m_Depth);
                br.Close();
            }

            int heightmapWidth = terrainData.heightmapWidth;
            int heightmapHeight = terrainData.heightmapHeight;
            float[,] heights = new float[heightmapHeight, heightmapWidth];
            if (m_Depth == Depth.Bit16)
            {
                float normalize = 1.0F / (1 << 16);
                for (int y = 0; y < heightmapHeight; ++y)
                {
                    for (int x = 0; x < heightmapWidth; ++x)
                    {
                        int index = Mathf.Clamp(x, 0, m_Width - 1) + Mathf.Clamp(y, 0, m_Height - 1) * m_Width;
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
                        int destY = m_FlipVertically ? heightmapHeight - 1 - y : y;
                        heights[destY, x] = height;
                    }
                }
            }
            else
            {
                float normalize =  1.0F / (1 << 8);
                for (int y = 0; y < heightmapHeight; ++y)
                {
                    for (int x = 0; x < heightmapWidth; ++x)
                    {
                        int index = Mathf.Clamp(x, 0, m_Width - 1) + Mathf.Clamp(y, 0, m_Height - 1) * m_Width;
                        byte compressedHeight = data[index];
                        float height = compressedHeight * normalize;
                        int destY = m_FlipVertically ? heightmapHeight - 1 - y : y;
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
            }
        }

        internal override void OnWizardUpdate()
        {
            base.OnWizardUpdate();
            if (terrainData)
                helpString = "Width " + terrainData.heightmapWidth + "\nHeight " + terrainData.heightmapHeight;
        }

        void WriteRaw(string path)
        {
            // Write data
            int heightmapWidth = terrainData.heightmapWidth;
            int heightmapHeight = terrainData.heightmapHeight;
            float[,] heights = terrainData.GetHeights(0, 0, heightmapWidth, heightmapHeight);
            byte[] data = new byte[heightmapWidth * heightmapHeight * (int)m_Depth];

            if (m_Depth == Depth.Bit16)
            {
                float normalize = (1 << 16);
                for (int y = 0; y < heightmapHeight; ++y)
                {
                    for (int x = 0; x < heightmapWidth; ++x)
                    {
                        int index = x + y * heightmapWidth;
                        int srcY = m_FlipVertically ? heightmapHeight - 1 - y : y;
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
                for (int y = 0; y < heightmapHeight; ++y)
                {
                    for (int x = 0; x < heightmapWidth; ++x)
                    {
                        int index = x + y * heightmapWidth;
                        int srcY = m_FlipVertically ? heightmapHeight - 1 - y : y;
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
            helpString = "Width " + terrain.terrainData.heightmapWidth + " Height " + terrain.terrainData.heightmapHeight;
            OnWizardUpdate();
        }
    }


    class TreeWizard : TerrainWizard
    {
        public GameObject   m_Tree;
        public float        m_BendFactor;
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
            }
            else
            {
                m_Tree = m_Terrain.terrainData.treePrototypes[m_PrototypeIndex].prefab;
                m_BendFactor = m_Terrain.terrainData.treePrototypes[m_PrototypeIndex].bendFactor;
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
                m_PrototypeIndex = trees.Length;
                m_Terrain.terrainData.treePrototypes = newTrees;
                TreePainter.selectedTree = m_PrototypeIndex;
            }
            else
            {
                trees[m_PrototypeIndex].prefab = m_Tree;
                trees[m_PrototypeIndex].bendFactor = m_BendFactor;

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
            if (!TerrainEditorUtility.IsLODTreePrototype(m_Tree))
                m_BendFactor = EditorGUILayout.FloatField("Bend Factor", m_BendFactor);

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


    enum DetailMeshRenderMode
    {
        VertexLit,
        Grass
    }

    class DetailMeshWizard : TerrainWizard
    {
        public GameObject   m_Detail;
        public float        m_NoiseSpread;
        public float        m_MinWidth;
        public float        m_MaxWidth;
        public float        m_MinHeight;
        public float        m_MaxHeight;
        public Color        m_HealthyColor;
        public Color        m_DryColor;
        public DetailMeshRenderMode m_RenderMode;
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
                prototype = new DetailPrototype();
            else
                prototype = m_Terrain.terrainData.detailPrototypes[m_PrototypeIndex];

            m_Detail = prototype.prototype;

            m_NoiseSpread = prototype.noiseSpread;
            m_MinWidth = prototype.minWidth;
            m_MaxWidth = prototype.maxWidth;
            m_MinHeight = prototype.minHeight;
            m_MaxHeight = prototype.maxHeight;
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

            OnWizardUpdate();
        }

        void DoApply()
        {
            if (terrainData == null)
                return;
            DetailPrototype[] prototypes = m_Terrain.terrainData.detailPrototypes;

            if (m_PrototypeIndex == -1)
            {
                // Add a new detailprototype to the prototype arrays
                DetailPrototype[] newarray = new DetailPrototype[prototypes.Length + 1];
                System.Array.Copy(prototypes, 0, newarray, 0, prototypes.Length);
                m_PrototypeIndex = prototypes.Length;

                prototypes = newarray;
                prototypes[m_PrototypeIndex] = new DetailPrototype();
            }
            prototypes[m_PrototypeIndex].renderMode = DetailRenderMode.VertexLit;
            prototypes[m_PrototypeIndex].usePrototypeMesh = true;
            prototypes[m_PrototypeIndex].prototype = m_Detail;
            prototypes[m_PrototypeIndex].prototypeTexture = null;
            prototypes[m_PrototypeIndex].noiseSpread = m_NoiseSpread;
            prototypes[m_PrototypeIndex].minWidth = m_MinWidth;
            prototypes[m_PrototypeIndex].maxWidth = m_MaxWidth;
            prototypes[m_PrototypeIndex].minHeight = m_MinHeight;
            prototypes[m_PrototypeIndex].maxHeight = m_MaxHeight;
            prototypes[m_PrototypeIndex].healthyColor = m_HealthyColor;
            prototypes[m_PrototypeIndex].dryColor = m_DryColor;

            if (m_RenderMode == DetailMeshRenderMode.Grass)
                prototypes[m_PrototypeIndex].renderMode = DetailRenderMode.Grass;
            else
                prototypes[m_PrototypeIndex].renderMode = DetailRenderMode.VertexLit;

            m_Terrain.terrainData.detailPrototypes = prototypes;
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

        internal override void OnWizardUpdate()
        {
            base.OnWizardUpdate();

            if (m_Detail == null)
            {
                errorString = "Please assign a detail prefab";
                isValid = false;
            }
            else if (m_PrototypeIndex != -1)
            {
                DoApply();
            }
        }
    }


    class DetailTextureWizard : TerrainWizard
    {
        public Texture2D    m_DetailTexture;
        public float        m_MinWidth;
        public float        m_MaxWidth;
        public float        m_MinHeight;
        public float        m_MaxHeight;
        public float        m_NoiseSpread;
        public Color        m_HealthyColor;
        public Color        m_DryColor;
        public bool         m_Billboard;
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
                prototype.renderMode = DetailRenderMode.GrassBillboard;
            }
            else
                prototype = m_Terrain.terrainData.detailPrototypes[m_PrototypeIndex];

            m_DetailTexture = prototype.prototypeTexture;
            m_MinWidth = prototype.minWidth;
            m_MaxWidth = prototype.maxWidth;
            m_MinHeight = prototype.minHeight;
            m_MaxHeight = prototype.maxHeight;

            m_NoiseSpread = prototype.noiseSpread;
            m_HealthyColor = prototype.healthyColor;
            m_DryColor = prototype.dryColor;
            m_Billboard = prototype.renderMode == DetailRenderMode.GrassBillboard;

            OnWizardUpdate();
        }

        void DoApply()
        {
            if (terrainData == null)
                return;

            DetailPrototype[] prototypes = m_Terrain.terrainData.detailPrototypes;
            if (m_PrototypeIndex == -1)
            {
                // Add a new detailprototype to the prototype arrays
                DetailPrototype[] newarray = new DetailPrototype[prototypes.Length + 1];
                System.Array.Copy(prototypes, 0, newarray, 0, prototypes.Length);
                m_PrototypeIndex = prototypes.Length;
                prototypes = newarray;
                prototypes[m_PrototypeIndex] = new DetailPrototype();
            }

            prototypes[m_PrototypeIndex].prototype = null;
            prototypes[m_PrototypeIndex].prototypeTexture = m_DetailTexture;
            prototypes[m_PrototypeIndex].minWidth = m_MinWidth;
            prototypes[m_PrototypeIndex].maxWidth = m_MaxWidth;
            prototypes[m_PrototypeIndex].minHeight = m_MinHeight;
            prototypes[m_PrototypeIndex].maxHeight = m_MaxHeight;
            prototypes[m_PrototypeIndex].noiseSpread = m_NoiseSpread;
            prototypes[m_PrototypeIndex].healthyColor = m_HealthyColor;
            prototypes[m_PrototypeIndex].dryColor = m_DryColor;
            prototypes[m_PrototypeIndex].renderMode = m_Billboard ? DetailRenderMode.GrassBillboard : DetailRenderMode.Grass;
            prototypes[m_PrototypeIndex].usePrototypeMesh = false;

            m_Terrain.terrainData.detailPrototypes = prototypes;
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

        internal override void OnWizardUpdate()
        {
            m_MinHeight = Mathf.Max(0f, m_MinHeight);
            m_MaxHeight = Mathf.Max(m_MinHeight, m_MaxHeight);
            m_MinWidth = Mathf.Max(0f, m_MinWidth);
            m_MaxWidth = Mathf.Max(m_MinWidth, m_MaxWidth);
            base.OnWizardUpdate();
            if (m_DetailTexture == null)
            {
                errorString = "Please assign a detail texture";
                isValid = false;
            }
            else if (m_PrototypeIndex != -1)
            {
                DoApply();
            }
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
            TreePainter.MassPlaceTrees(m_Terrain.terrainData, numberOfTrees, true, keepExistingTrees);
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
