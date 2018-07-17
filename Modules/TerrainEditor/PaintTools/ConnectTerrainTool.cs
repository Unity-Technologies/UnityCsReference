// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    public class ConnectTerrainsTool : TerrainPaintTool<ConnectTerrainsTool>
    {
        enum TerrainNeighbor
        {
            Top = 0,
            Bottom,
            Left,
            Right
        }

        public override bool ShouldShowBrushes()
        {
            return false;
        }

        public override bool DoesPaint()
        {
            return false;
        }

        public override string GetName()
        {
            return "Connect Terrains";
        }

        public override string GetDesc()
        {
            return "Click the edges to connect or use the \"Auto-connect adjacent\" button";
        }

        Terrain CreateNeighbor(Terrain parent, TerrainNeighbor neighbor)
        {
            Vector3 offset;

            if (neighbor == TerrainNeighbor.Bottom)
                offset = Vector3.back * parent.terrainData.size.z;
            else if (neighbor == TerrainNeighbor.Top)
                offset = Vector3.forward * parent.terrainData.size.z;
            else if (neighbor == TerrainNeighbor.Left)
                offset = Vector3.left * parent.terrainData.size.x;
            else //if (neighbor == TerrainNeighbor.Right)
                offset = Vector3.right * parent.terrainData.size.x;

            Vector3 position = parent.transform.position + offset;
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
            terrainData.name = uniqueName;
            terrainData.size = parent.terrainData.size;
            GameObject terrainGO = (GameObject)Terrain.CreateTerrainGameObject(terrainData);

            terrainGO.name = uniqueName;
            terrainGO.transform.position = position;

            Terrain terrain = terrainGO.GetComponent<Terrain>();
            terrain.drawInstanced = parent.drawInstanced;
            AssetDatabase.CreateAsset(terrainData, "Assets/Terrain" + terrainGO.name + ".asset");

            AutoConnect(terrain);
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

        public override void OnSceneGUI(SceneView sceneView, Terrain terrain, Texture brushTexture, float brushStrength, int brushSizeInTerrainUnits)
        {
            Quaternion rot = new Quaternion();
            rot.eulerAngles = new Vector3(90, 00, 0);

            Handles.color = new Color(0.9f, 1.0f, 0.8f, 1.0f);
            Vector3 size = terrain.terrainData.size;
            Vector3 pos = terrain.transform.position + 0.5f * new Vector3(size.x, 0, size.z);

            if (terrain.bottomNeighbor == null)
            {
                if (Handles.Button(pos + new Vector3(0, 0, -size.z), rot, 0.5f * size.x, 0.5f * size.z, Handles.RectangleHandleCap))
                    CreateNeighbor(terrain, TerrainNeighbor.Bottom);
            }
            else
                Handles.DrawWireDisc(pos + new Vector3(0, 0, -0.5f * size.z), Vector3.up, 50);

            if (terrain.rightNeighbor == null)
            {
                if (Handles.Button(pos + new Vector3(size.x, 0, 0), rot, 0.5f * size.x, 0.5f * size.z, Handles.RectangleHandleCap))
                    CreateNeighbor(terrain, TerrainNeighbor.Right);
            }
            else
                Handles.DrawWireDisc(pos + new Vector3(0.5f * size.x, 0, 0), Vector3.up, 50);

            if (terrain.topNeighbor == null)
            {
                if (Handles.Button(pos + new Vector3(0, 0, size.z), rot, 0.5f * size.x, 0.5f * size.z, Handles.RectangleHandleCap))
                    CreateNeighbor(terrain, TerrainNeighbor.Top);
            }
            else
                Handles.DrawWireDisc(pos + new Vector3(0, 0, 0.5f * size.z), Vector3.up, 50);

            if (terrain.leftNeighbor == null)
            {
                if (Handles.Button(pos + new Vector3(-size.x, 0, 0), rot, 0.5f * size.x, 0.5f * size.z, Handles.RectangleHandleCap))
                    CreateNeighbor(terrain, TerrainNeighbor.Left);
            }
            else
                Handles.DrawWireDisc(pos + new Vector3(-0.5f * size.x, 0, 0), Vector3.up, 50);
        }

        public override void OnInspectorGUI(Terrain terrain)
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Auto-connect adjacent"))
                AutoConnect(terrain);

            if (GUILayout.Button("Clear neighboring data"))
                ClearNeighborData();

            GUILayout.EndHorizontal();
        }

        void ClearNeighborData()
        {
            foreach (Terrain t in Terrain.activeTerrains)
                t.SetNeighbors(null, null, null, null);
        }

        Terrain[] CreateGridLayout(Terrain terrain, ref int numXPatches, ref int numZPatches)
        {
            if (Terrain.activeTerrains == null)
                return null;

            Vector3 min, max, patchsize;

            int numTerrains = Terrain.activeTerrains.Length;

            min = max = terrain.transform.position;
            patchsize = terrain.terrainData.size;

            // find extents
            Terrain[] allTerrains = new Terrain[numTerrains];
            for (int i = 0; i < numTerrains; i++)
            {
                allTerrains[i] = Terrain.activeTerrains[i];
                Vector3 pos = allTerrains[i].transform.position;
                if (allTerrains[i].terrainData.size != patchsize)
                {
                    Debug.LogError("Terrain sizes must be the same in order to connect.");
                    ClearNeighborData();
                    return null;
                }

                if (min.x > pos.x)
                    min.x = pos.x;
                if (min.z > pos.z)
                    min.z = pos.z;
                if (max.x < pos.x)
                    max.x = pos.x;
                if (max.z < pos.z)
                    max.z = pos.z;
            }

            numXPatches = 1 + (int)((max.x - min.x) / patchsize.x);
            numZPatches = 1 + (int)((max.z - min.z) / patchsize.z);

            // populate grid
            Terrain[] terrainGrid = new Terrain[numXPatches * numZPatches];
            for (int i = 0; i < terrainGrid.Length; i++)
                terrainGrid[i] = null;

            foreach (Terrain t in allTerrains)
            {
                Vector3 pos = t.transform.position - min;
                int x = Mathf.FloorToInt(pos.x / patchsize.x);
                int z = Mathf.FloorToInt(pos.z / patchsize.z);

                if (terrainGrid[z * numXPatches + x] != null)
                {
                    Debug.Log("Error auto connecting. Duplicate terrain found: " + t.name);
                    return null;
                }

                terrainGrid[z * numXPatches + x] = t;
            }

            return terrainGrid;
        }

        Terrain GetTerrainFromGridChecked(Terrain[] grid, int x, int y, int numXPatches, int numZPatches)
        {
            if (x < 0 || x >= numXPatches || y < 0 || y >= numZPatches)
                return null;

            return grid[y * numXPatches + x];
        }

        void AutoConnect(Terrain terrain)
        {
            int numXPatches = 0, numZPatches = 0;
            Terrain[] terrainGrid = CreateGridLayout(terrain, ref numXPatches, ref numZPatches);
            if (terrainGrid == null)
                return;

            ClearNeighborData();

            for (int j = 0; j < numZPatches; j++)
            {
                for (int i = 0; i < numXPatches; i++)
                {
                    Terrain t = GetTerrainFromGridChecked(terrainGrid, i, j, numXPatches, numZPatches);

                    if (t == null)
                        continue;

                    Terrain left   = GetTerrainFromGridChecked(terrainGrid, i - 1, j, numXPatches, numZPatches);
                    Terrain top    = GetTerrainFromGridChecked(terrainGrid, i, j + 1, numXPatches, numZPatches);
                    Terrain right  = GetTerrainFromGridChecked(terrainGrid, i + 1, j, numXPatches, numZPatches);
                    Terrain bottom = GetTerrainFromGridChecked(terrainGrid, i, j - 1, numXPatches, numZPatches);

                    t.SetNeighbors(left, top, right, bottom);
                }
            }
        }
    }
}
