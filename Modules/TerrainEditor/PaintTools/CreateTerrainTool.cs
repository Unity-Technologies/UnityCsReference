// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.TerrainAPI;


namespace UnityEditor.Experimental.TerrainAPI
{
    internal class CreateTerrainTool : TerrainPaintTool<CreateTerrainTool>
    {
        private class Styles
        {
            public GUIContent fillHeightmapUsingNeighbors = EditorGUIUtility.TrTextContent("Fill Heightmap Using Neighbors", "If selected, it will fill heightmap of the new terrain performing cross blend of heightmaps of its neighbors.");
            public GUIContent fillAddressMode = EditorGUIUtility.TrTextContent("Fill Heightmap Address Mode", "Type of the terrain's neighbors sampling address mode.");
        }

        private enum FillAddressMode
        {
            Clamp = 0,
            Mirror = 1
        }

        private enum TerrainNeighbor
        {
            Top = 0,
            Bottom,
            Left,
            Right
        }

        private class TerrainNeighborInfo
        {
            public TerrainData terrainData;
            public Texture texture;
            public float offset;
        }

        private static Styles s_Styles;

        [SerializeField] private bool m_FillHeightmapUsingNeighbors = true;
        [SerializeField] private FillAddressMode m_FillAddressMode;
        private Material m_CrossBlendMaterial;


        private Material GetOrCreateCrossBlendMaterial()
        {
            if (m_CrossBlendMaterial == null)
                m_CrossBlendMaterial = new Material(Shader.Find("Hidden/TerrainEngine/CrossBlendNeighbors"));
            return m_CrossBlendMaterial;
        }

        public override string GetName()
        {
            return "Create Neighbor Terrains";
        }

        public override string GetDesc()
        {
            return "Click the edges to create neighbor terrains";
        }

        public override void OnEnable()
        {
            LoadInspectorSettings();
        }

        public override void OnDisable()
        {
            SaveInspectorSettings();
        }

        private void LoadInspectorSettings()
        {
            m_FillHeightmapUsingNeighbors = EditorPrefs.GetBool("TerrainFillHeightmapUsingNeighbors", true);
            m_FillAddressMode = (FillAddressMode)EditorPrefs.GetInt("TerrainFillAddressMode", 0);
        }

        private void SaveInspectorSettings()
        {
            EditorPrefs.SetBool("TerrainFillHeightmapUsingNeighbors", m_FillHeightmapUsingNeighbors);
            EditorPrefs.SetInt("TerrainFillAddressMode", (int)m_FillAddressMode);
        }

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            base.OnInspectorGUI(terrain, editContext);

            if (s_Styles == null)
            {
                s_Styles = new Styles();
            }

            m_FillHeightmapUsingNeighbors = EditorGUILayout.Toggle(s_Styles.fillHeightmapUsingNeighbors, m_FillHeightmapUsingNeighbors);

            EditorGUI.BeginDisabledGroup(!m_FillHeightmapUsingNeighbors);
            m_FillAddressMode = (FillAddressMode)EditorGUILayout.EnumPopup(s_Styles.fillAddressMode, m_FillAddressMode);
            EditorGUI.EndDisabledGroup();
        }

        Terrain CreateNeighbor(Terrain parent, Vector3 position)
        {
            string uniqueName = "Terrain_" + position.ToString();

            if (null != GameObject.Find(uniqueName))
            {
                Debug.LogWarning("Already have a neighbor on that side");
                return null;
            }

            TerrainData terrainData = new TerrainData();
            terrainData.baseMapResolution = parent.terrainData.baseMapResolution;
            terrainData.heightmapResolution = parent.terrainData.heightmapResolution;
            terrainData.alphamapResolution = parent.terrainData.alphamapResolution;
            if (parent.terrainData.terrainLayers != null && parent.terrainData.terrainLayers.Length > 0)
            {
                var newarray = new TerrainLayer[1];
                newarray[0] = parent.terrainData.terrainLayers[0];
                terrainData.terrainLayers = newarray;
            }
            terrainData.SetDetailResolution(parent.terrainData.detailResolution, parent.terrainData.detailResolutionPerPatch);
            terrainData.name = Guid.NewGuid().ToString();
            terrainData.size = parent.terrainData.size;
            GameObject terrainGO = Terrain.CreateTerrainGameObject(terrainData);

            terrainGO.name = uniqueName;
            terrainGO.transform.position = position;

            Terrain terrain = terrainGO.GetComponent<Terrain>();
            terrain.groupingID = parent.groupingID;
            terrain.drawInstanced = parent.drawInstanced;
            terrain.allowAutoConnect = parent.allowAutoConnect;

            string parentTerrainDataDir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(parent.terrainData));
            AssetDatabase.CreateAsset(terrainData, Path.Combine(parentTerrainDataDir, "TerrainData_" + terrainData.name + ".asset"));
            if (m_FillHeightmapUsingNeighbors)
                FillHeightmapUsingNeighbors(terrain);

            Undo.RegisterCreatedObjectUndo(terrainGO, "Add New neighbor");

            return terrain;
        }

        private void FillHeightmapUsingNeighbors(Terrain terrain)
        {
            TerrainUtility.AutoConnect();

            Terrain[] nbrTerrains = new Terrain[4] { terrain.topNeighbor, terrain.bottomNeighbor, terrain.leftNeighbor, terrain.rightNeighbor };

            // Position of the terrain must be lowest
            Vector3 position = terrain.transform.position;
            foreach (Terrain nbrTerrain in nbrTerrains)
            {
                if (nbrTerrain)
                    position.y = Mathf.Min(position.y, nbrTerrain.transform.position.y);
            }
            terrain.transform.position = position;

            TerrainNeighborInfo top = new TerrainNeighborInfo();
            TerrainNeighborInfo bottom = new TerrainNeighborInfo();
            TerrainNeighborInfo left = new TerrainNeighborInfo();
            TerrainNeighborInfo right = new TerrainNeighborInfo();
            TerrainNeighborInfo[] nbrInfos = new TerrainNeighborInfo[4] { top, bottom, left, right };

            const float kNeightNormFactor = 2.0f;
            for (int i = 0; i < 4; ++i)
            {
                TerrainNeighborInfo nbrInfo = nbrInfos[i];
                Terrain nbrTerrain = nbrTerrains[i];
                if (nbrTerrain)
                {
                    nbrInfo.terrainData = nbrTerrain.terrainData;
                    if (nbrInfo.terrainData)
                    {
                        nbrInfo.texture = nbrInfo.terrainData.heightmapTexture;
                        nbrInfo.offset = (nbrTerrain.transform.position.y - terrain.transform.position.y) / (nbrInfo.terrainData.size.y * kNeightNormFactor);
                    }
                }
            }

            RenderTexture heightmap = terrain.terrainData.heightmapTexture;
            Vector4 texCoordOffsetScale = new Vector4(-0.5f / heightmap.width, -0.5f / heightmap.height,
                (float)heightmap.width / (heightmap.width - 1), (float)heightmap.height / (heightmap.height - 1));

            Material crossBlendMat = GetOrCreateCrossBlendMaterial();
            Vector4 slopeEnableFlags = new Vector4(bottom.texture ? 0.0f : 1.0f, top.texture ? 0.0f : 1.0f, left.texture ? 0.0f : 1.0f, right.texture ? 0.0f : 1.0f);
            crossBlendMat.SetVector("_SlopeEnableFlags", slopeEnableFlags);
            crossBlendMat.SetVector("_TexCoordOffsetScale", texCoordOffsetScale);
            crossBlendMat.SetVector("_Offsets", new Vector4(bottom.offset, top.offset, left.offset, right.offset));
            crossBlendMat.SetFloat("_AddressMode", (float)m_FillAddressMode);
            crossBlendMat.SetTexture("_TopTex", top.texture);
            crossBlendMat.SetTexture("_BottomTex", bottom.texture);
            crossBlendMat.SetTexture("_LeftTex", left.texture);
            crossBlendMat.SetTexture("_RightTex", right.texture);

            Graphics.Blit(null, heightmap, crossBlendMat);

            terrain.terrainData.DirtyHeightmapRegion(new RectInt(0, 0, heightmap.width, heightmap.height), TerrainHeightmapSyncControl.HeightAndLod);
        }

        private bool RaycastTerrain(Terrain terrain, Ray mouseRay, out RaycastHit hit, out Terrain hitTerrain)
        {
            if (terrain.GetComponent<Collider>().Raycast(mouseRay, out hit, Mathf.Infinity))
            {
                hitTerrain = terrain;
                return true;
            }

            hitTerrain = null;
            return false;
        }

        public override void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext)
        {
            if ((Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseDown) &&
                (Event.current.button == 2 || Event.current.alt))
            {
                return;
            }

            Quaternion rot = new Quaternion();
            rot.eulerAngles = new Vector3(90, 00, 0);

            Handles.color = new Color(0.9f, 1.0f, 0.8f, 1.0f);
            Vector3 size = terrain.terrainData.size;

            TerrainUtility.TerrainMap mapGroup = TerrainUtility.TerrainMap.CreateFromPlacement(terrain);
            if (mapGroup == null)
                return;

            foreach (TerrainUtility.TerrainMap.TileCoord coord in mapGroup.m_terrainTiles.Keys)
            {
                int x = coord.tileX;
                int y = coord.tileZ;

                Terrain t = mapGroup.GetTerrain(x, y);

                if (t == null)
                    continue;

                Terrain left = mapGroup.GetTerrain(x - 1, y);
                Terrain right = mapGroup.GetTerrain(x + 1, y);
                Terrain top = mapGroup.GetTerrain(x, y + 1);
                Terrain bottom = mapGroup.GetTerrain(x, y - 1);

                Vector3 pos = t.transform.position + 0.5f * new Vector3(size.x, 0, size.z);

                if ((bottom == null) && Handles.Button(pos + new Vector3(0, 0, -size.z), rot, 0.5f * size.x, 0.5f * size.x, Handles.RectangleHandleCapWorldSpace))
                    CreateNeighbor(terrain, t.transform.position + Vector3.back * size.z);
                if ((top == null) && Handles.Button(pos + new Vector3(0, 0, size.z), rot, 0.5f * size.x, 0.5f * size.x, Handles.RectangleHandleCapWorldSpace))
                    CreateNeighbor(terrain, t.transform.position + Vector3.forward * size.z);
                if ((right == null) && Handles.Button(pos + new Vector3(size.x, 0, 0), rot, 0.5f * size.x, 0.5f * size.x, Handles.RectangleHandleCapWorldSpace))
                    CreateNeighbor(terrain, t.transform.position + Vector3.right * size.x);
                if ((left == null) && Handles.Button(pos + new Vector3(-size.x, 0, 0), rot, 0.5f * size.x, 0.5f * size.x, Handles.RectangleHandleCapWorldSpace))
                    CreateNeighbor(terrain, t.transform.position + Vector3.left * size.x);
            }
        }
    }
}
