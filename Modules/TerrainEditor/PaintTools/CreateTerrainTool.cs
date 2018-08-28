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
    public class CreateTerrainTool : TerrainPaintTool<CreateTerrainTool>
    {
        enum TerrainNeighbor
        {
            Top = 0,
            Bottom,
            Left,
            Right
        }

        public override string GetName()
        {
            return "Create Neighbor Terrains";
        }

        public override string GetDesc()
        {
            return "Click the edges to create neighbor terrains";
        }

        Terrain CreateNeighbor(Terrain parent, Vector3 position)
        {
            string uniqueName = "Terrain_" + position.ToString();

            if (null != GameObject.Find(uniqueName))
            {
                Debug.LogWarning("Already have a neighbor on that side");
                return (Terrain)null;
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
            GameObject terrainGO = (GameObject)Terrain.CreateTerrainGameObject(terrainData);

            terrainGO.name = uniqueName;
            terrainGO.transform.position = position;

            Terrain terrain = terrainGO.GetComponent<Terrain>();
            terrain.groupingID = parent.groupingID;
            terrain.drawInstanced = parent.drawInstanced;
            terrain.allowAutoConnect = parent.allowAutoConnect;

            string parentTerrainDataDir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(parent.terrainData));
            AssetDatabase.CreateAsset(terrainData, Path.Combine(parentTerrainDataDir, "TerrainData_" + terrainData.name + ".asset"));

            Undo.RegisterCreatedObjectUndo(terrainGO, "Add New neighbor");

            return terrain;
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

                if ((bottom == null) && Handles.Button(pos + new Vector3(0, 0, -size.z), rot, 0.5f * size.x, 0.5f * size.x, Handles.RectangleHandleCap))
                    CreateNeighbor(terrain, t.transform.position + Vector3.back * size.z);
                if ((top == null) && Handles.Button(pos + new Vector3(0, 0, size.z), rot, 0.5f * size.x, 0.5f * size.x, Handles.RectangleHandleCap))
                    CreateNeighbor(terrain, t.transform.position + Vector3.forward * size.z);
                if ((right == null) && Handles.Button(pos + new Vector3(size.x, 0, 0), rot, 0.5f * size.x, 0.5f * size.x, Handles.RectangleHandleCap))
                    CreateNeighbor(terrain, t.transform.position + Vector3.right * size.x);
                if ((left == null) && Handles.Button(pos + new Vector3(-size.x, 0, 0), rot, 0.5f * size.x, 0.5f * size.x, Handles.RectangleHandleCap))
                    CreateNeighbor(terrain, t.transform.position + Vector3.left * size.x);
            }
        }
    }
}
