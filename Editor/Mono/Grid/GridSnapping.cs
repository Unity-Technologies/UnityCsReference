// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal static class GridSnapping
    {
        public static bool active
        {
            get { return activeGrid != null; }
        }

        public static Grid activeGrid
        {
            get { return Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<Grid>() : null; }
        }

        public static Vector3 Snap(Vector3 position)
        {
            return Snap(activeGrid, position);
        }

        public static Vector3 Snap(Grid grid, Vector3 position)
        {
            Vector3 result = position;
            if (grid != null && !EditorGUI.actionKey)
            {
                Vector3 local = grid.WorldToLocal(position);
                Vector3 interpolatedCell = grid.LocalToCellInterpolated(local);
                Vector3 roundedCell = new Vector3(
                        Mathf.Round(2.0f * interpolatedCell.x) / 2,
                        Mathf.Round(2.0f * interpolatedCell.y) / 2,
                        Mathf.Round(2.0f * interpolatedCell.z) / 2
                        );
                local = grid.CellToLocalInterpolated(roundedCell);
                result = grid.LocalToWorld(local);
            }
            return result;
        }

    }
}
