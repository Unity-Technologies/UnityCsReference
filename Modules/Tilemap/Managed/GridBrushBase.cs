// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine
{
    public abstract class GridBrushBase : ScriptableObject
    {
        public enum Tool { Select, Move, Paint, Box, Pick, Erase, FloodFill }
        public enum RotationDirection { Clockwise = 0, CounterClockwise = 1 }
        public enum FlipAxis { X = 0, Y = 1 }

        public virtual void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position) {}
        public virtual void Erase(GridLayout gridLayout, GameObject brushTarget, Vector3Int position) {}

        public virtual void BoxFill(GridLayout gridLayout, GameObject brushTarget, BoundsInt position)
        {
            for (int z = position.zMin; z < position.zMax; z++)
            {
                for (int y = position.yMin; y < position.yMax; y++)
                {
                    for (int x = position.xMin; x < position.xMax; x++)
                    {
                        Paint(gridLayout, brushTarget, new Vector3Int(x, y, z));
                    }
                }
            }
        }

        public virtual void BoxErase(GridLayout gridLayout, GameObject brushTarget, BoundsInt position)
        {
            for (int z = position.zMin; z < position.zMax; z++)
            {
                for (int y = position.yMin; y < position.yMax; y++)
                {
                    for (int x = position.xMin; x < position.xMax; x++)
                    {
                        Erase(gridLayout, brushTarget, new Vector3Int(x, y, z));
                    }
                }
            }
        }

        public virtual void Select(GridLayout gridLayout, GameObject brushTarget, BoundsInt position) {}
        public virtual void FloodFill(GridLayout gridLayout, GameObject brushTarget, Vector3Int position) {}
        public virtual void Rotate(RotationDirection direction, GridLayout.CellLayout layout) {}
        public virtual void Flip(FlipAxis flip, GridLayout.CellLayout layout) {}
        public virtual void Pick(GridLayout gridLayout, GameObject brushTarget, BoundsInt position, Vector3Int pivot) {}
        public virtual void Move(GridLayout gridLayout, GameObject brushTarget, BoundsInt from, BoundsInt to) {}
        public virtual void MoveStart(GridLayout gridLayout, GameObject brushTarget, BoundsInt position) {}
        public virtual void MoveEnd(GridLayout gridLayout, GameObject brushTarget, BoundsInt position) {}
    }
}
