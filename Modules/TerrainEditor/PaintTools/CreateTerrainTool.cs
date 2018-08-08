// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;


namespace UnityEditor
{
    internal class TerrainMap
    {
        public Terrain GetTerrain(int tileX, int tileZ)
        {
            Terrain result = null;
            m_terrainTiles.TryGetValue(new TileCoord(tileX, tileZ), out result);
            return result;
        }

        public bool TryGetTerrainCoord(Terrain terrain, out int tileX, out int tileZ)
        {
            TileCoord coord = new TileCoord(0, 0);
            bool found = m_tileCoords.TryGetValue(terrain, out coord);
            tileX = coord.tileX;
            tileZ = coord.tileZ;
            return found;
        }

        // create a terrain map of ALL terrains, by using only their placement to fit them to a grid
        // the position and size of centerTerrain defines the grid alignment and origin.  if NULL, we use the first active terrain
        static public TerrainMap CreateFromPlacement(Terrain centerTerrain = null, bool fullValidation = true)
        {
            if (Terrain.activeTerrains == null)
                return null;

            int numTerrains = Terrain.activeTerrains.Length;
            if (numTerrains == 0)
                return null;

            TerrainMap terrainMap = new TerrainMap();

            // if not defined, use the first terrain to define the grid origin
            if (centerTerrain == null)
                centerTerrain = Terrain.activeTerrains[0];

            float gridOriginX = centerTerrain.transform.position.x;
            float gridOriginZ = centerTerrain.transform.position.z;
            float gridScaleX = 1.0f / centerTerrain.terrainData.size.x;
            float gridScaleZ = 1.0f / centerTerrain.terrainData.size.z;

            // iterate all active terrains
            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                // convert position to a grid index, with proper rounding
                Vector3 pos = terrain.transform.position;
                int tileX = Mathf.RoundToInt((pos.x - gridOriginX) * gridScaleX);
                int tileZ = Mathf.RoundToInt((pos.z - gridOriginZ) * gridScaleZ);

                // attempt to add the terrain at that grid position
                terrainMap.TryToAddTerrain(tileX, tileZ, terrain);
            }

            if (fullValidation)
            {
                // run validation to check alignment status
                terrainMap.Validate();
            }
            terrainMap.UpdateConsistency();
            return terrainMap;
        }

        public struct TileCoord
        {
            public readonly int tileX;
            public readonly int tileZ;

            public TileCoord(int tileX, int tileZ)
            {
                this.tileX = tileX;
                this.tileZ = tileZ;
            }
        };

        Vector3 m_patchSize;            // size of the (0,0) terrain (size of ALL terrains if m_sameSize)

        // terrain consistency result
        public bool m_consistent;      // true if all terrains conform to all consistency checks
        public bool m_nonOverlapping;  // true if we didn't encounter any overlapping terrains
        public bool m_validGrid;       // true if we didn't find any terrain connected at multiple different grid coordinates
        public bool m_sameSize;        // true if all terrains are the same size (x,y,z)
        public bool m_alignedEdges;    // true if all terrains are positioned edge-to-edge

        // 2D bounds
        public int m_minTileX;
        public int m_minTileZ;
        public int m_maxTileX;
        public int m_maxTileZ;

        // TODO: convert to native hash map?  terrain manager could track these internally perhaps
        public Dictionary<TileCoord, Terrain> m_terrainTiles;
        public Dictionary<Terrain, TileCoord> m_tileCoords;

        TerrainMap()
        {
            m_consistent = true;
            m_nonOverlapping = true;
            m_validGrid = true;
            m_sameSize = true;
            m_alignedEdges = true;
            m_terrainTiles = new Dictionary<TileCoord, Terrain>();
            m_tileCoords = new Dictionary<Terrain, TileCoord>();
        }

        void AddTerrainInternal(int x, int z, Terrain terrain)
        {
            if (m_terrainTiles.Count == 0)
            {
                // first one added, initialize statistics
                m_patchSize = terrain.terrainData.size;
                m_minTileX = x;
                m_minTileZ = z;
                m_maxTileX = x;
                m_maxTileZ = z;
            }
            else
            {
                // check consistency with existing terrains
                if (terrain.terrainData.size != m_patchSize)
                {
                    // ERROR - terrain is not the same size as other terrains
                    m_sameSize = false;
                }
                m_minTileX = Mathf.Min(m_minTileX, x);
                m_minTileZ = Mathf.Min(m_minTileZ, z);
                m_maxTileX = Mathf.Max(m_maxTileX, x);
                m_maxTileZ = Mathf.Max(m_maxTileZ, z);
            }
            m_terrainTiles.Add(new TileCoord(x, z), terrain);
            m_tileCoords.Add(terrain, new TileCoord(x, z));
        }

        // attempt to place the specified terrain tile at the specified (x,z) position, with consistency checks
        bool TryToAddTerrain(int tileX, int tileZ, Terrain terrain)
        {
            bool added = false;
            if (terrain != null)
            {
                Terrain existing = GetTerrain(tileX, tileZ);
                if (existing != null)
                {
                    // already a terrain in the location -- check it is the same tile
                    if (existing != terrain)
                    {
                        // ERROR - multiple different terrains at the same coordinate!
                        m_nonOverlapping = false;
                    }
                }
                else
                {
                    // first double check this terrain doesn't exist elsewhere in the terrain map
                    // this is doubly important to stop infinite recursions if terrains are connected in multiple places
                    int existingCoordX, existingCoordZ;
                    if (TryGetTerrainCoord(terrain, out existingCoordX, out existingCoordZ))
                    {
                        // ERROR -- terrain is already located at a different coordinate
                        m_validGrid = false;
                    }
                    else
                    {
                        // add terrain to the terrain map
                        AddTerrainInternal(tileX, tileZ, terrain);
                        added = true;
                    }
                }
            }
            return added;
        }

        bool ValidateTerrain(int tileX, int tileZ)
        {
            bool consistent = true;
            Terrain terrain = GetTerrain(tileX, tileZ);
            if (terrain != null)
            {
                // grab neighbors (according to grid)
                Terrain left = GetTerrain(tileX - 1, tileZ);
                Terrain right = GetTerrain(tileX + 1, tileZ);
                Terrain top = GetTerrain(tileX, tileZ + 1);
                Terrain bottom = GetTerrain(tileX, tileZ - 1);

                // check the tile is fully connected
                {
                    if ((left != terrain.leftNeighbor) ||
                        (right != terrain.rightNeighbor) ||
                        (top != terrain.topNeighbor) ||
                        (bottom != terrain.bottomNeighbor))
                    {
                        consistent = false;
                    }
                }

                // check edge alignment
                {
                    if (left)
                    {
                        if (!Mathf.Approximately(terrain.transform.position.x, left.transform.position.x + left.terrainData.size.x) ||
                            !Mathf.Approximately(terrain.transform.position.z, left.transform.position.z))
                        {
                            // unaligned edge, tile doesn't match expected location
                            consistent = false;
                            m_alignedEdges = false;
                        }
                    }
                    if (right)
                    {
                        if (!Mathf.Approximately(terrain.transform.position.x + terrain.terrainData.size.x, right.transform.position.x) ||
                            !Mathf.Approximately(terrain.transform.position.z, right.transform.position.z))
                        {
                            // unaligned edge, tile doesn't match expected location
                            consistent = false;
                            m_alignedEdges = false;
                        }
                    }
                    if (top)
                    {
                        if (!Mathf.Approximately(terrain.transform.position.x, top.transform.position.x) ||
                            !Mathf.Approximately(terrain.transform.position.z + terrain.terrainData.size.z, top.transform.position.z))
                        {
                            // unaligned edge, tile doesn't match expected location
                            consistent = false;
                            m_alignedEdges = false;
                        }
                    }
                    if (bottom)
                    {
                        if (!Mathf.Approximately(terrain.transform.position.x, bottom.transform.position.x) ||
                            !Mathf.Approximately(terrain.transform.position.z, bottom.transform.position.z + bottom.terrainData.size.z))
                        {
                            // unaligned edge, tile doesn't match expected location
                            consistent = false;
                            m_alignedEdges = false;
                        }
                    }
                }
            }
            return consistent;
        }

        void UpdateConsistency()
        {
            m_consistent = m_nonOverlapping && m_validGrid && m_sameSize && m_alignedEdges;
        }

        // perform all validation checks on the terrain map
        bool Validate()
        {
            // iterate all tiles and validate them
            foreach (TileCoord coord in m_terrainTiles.Keys)
            {
                ValidateTerrain(coord.tileX, coord.tileZ);
            }
            UpdateConsistency();

            return m_consistent;
        }
    };


    public class CreateTerrainTool : TerrainPaintTool<CreateTerrainTool>
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
            terrain.drawInstanced = parent.drawInstanced;
            terrain.allowAutoConnect = true;

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

        public override void OnSceneGUI(SceneView sceneView, Terrain terrain, Texture brushTexture, float brushStrength, int brushSizeInTerrainUnits)
        {
            Quaternion rot = new Quaternion();
            rot.eulerAngles = new Vector3(90, 00, 0);

            Handles.color = new Color(0.9f, 1.0f, 0.8f, 1.0f);
            Vector3 size = terrain.terrainData.size;

            TerrainMap map = TerrainMap.CreateFromPlacement(terrain);
            foreach (TerrainMap.TileCoord coord in map.m_terrainTiles.Keys)
            {
                int x = coord.tileX;
                int y = coord.tileZ;

                Terrain t = map.GetTerrain(x, y);

                if (t == null)
                    continue;

                Terrain left = map.GetTerrain(x - 1, y);
                Terrain right = map.GetTerrain(x + 1, y);
                Terrain top = map.GetTerrain(x, y + 1);
                Terrain bottom = map.GetTerrain(x, y - 1);

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
