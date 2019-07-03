// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal class HeightmapFilters
    {
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
            int r = terrain.heightmapResolution;

            float[,] heights = terrain.GetHeights(0, 0, r, r);

            Smooth(heights, terrain);

            terrain.SetHeights(0, 0, heights);
        }

        public static void Flatten(TerrainData terrain, float height)
        {
            int r = terrain.heightmapResolution;

            float[,] heights = new float[r, r];
            for (int y = 0; y < r; y++)
            {
                for (int x = 0; x < r; x++)
                {
                    heights[y, x] = height;
                }
            }
            terrain.SetHeights(0, 0, heights);
        }
    }
} //namespace
