// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

namespace UnityEditor
{
    internal class HeightmapFilters
    {
        static void WobbleStuff(float[,] heights, TerrainData terrain)
        {
            for (int y = 0; y < heights.GetLength(0); y++)
                for (int x = 0; x < heights.GetLength(1); x++)
                    heights[y, x] = (heights[y, x] + 1) / 2;
        }

        static void Noise(float[,] heights, TerrainData terrain)
        {
            for (int y = 0; y < heights.GetLength(0); y++)
                for (int x = 0; x < heights.GetLength(1); x++)
                    heights[y, x] += Random.value * 0.01F;
        }

        /*
        public static void WobbleStuff () {
            int w = GetActiveTerrainData ().heightmapWidth;
            int h = GetActiveTerrainData ().heightmapHeight;

            // Grab terrain
            float[,] heights = GetActiveTerrainData ().GetHeights(0, 0, w, h);

            // Apply filter
            WobbleStuff (heights, GetActiveTerrainData ());

            // Apply back
            GetActiveTerrainData ().SetHeights(0, 0, heights);
            FlushHeightmapModification ();
        }
    */
        public static void Smooth(float[,] heights, TerrainData terrain)
        {
            float[,] oldHeights = heights.Clone() as float[, ];
            int width = heights.GetLength(1);
            int height = heights.GetLength(0);
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    float h = 0.0F;
                    h += oldHeights[y, x];

                    h += oldHeights[y, x - 1];
                    h += oldHeights[y, x + 1];

                    h += oldHeights[y - 1, x];
                    h += oldHeights[y + 1, x];

                    h /= 5.0F;

                    heights[y, x] = h;
                }
            }
        }

        public static void Smooth(TerrainData terrain)
        {
            int w = terrain.heightmapWidth;
            int h = terrain.heightmapHeight;

            float[,] heights = terrain.GetHeights(0, 0, w, h);

            Smooth(heights, terrain);

            terrain.SetHeights(0, 0, heights);
        }

        public static void Flatten(TerrainData terrain, float height)
        {
            int w = terrain.heightmapWidth;
            int h = terrain.heightmapHeight;

            float[,] heights = new float[h, w];
            for (int y = 0; y < heights.GetLength(0); y++)
            {
                for (int x = 0; x < heights.GetLength(1); x++)
                {
                    heights[y, x] = height;
                }
            }
            terrain.SetHeights(0, 0, heights);
        }
    }
} //namespace
