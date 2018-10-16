// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Tilemaps;
using UnityEngine;
using System.Collections;

namespace UnityEditor
{
    public class GridBrush : GridBrushBase
    {
        [SerializeField]
        [HideInInspector]
        private BrushCell[] m_Cells;

        [SerializeField]
        [HideInInspector]
        private Vector3Int m_Size;

        [SerializeField]
        [HideInInspector]
        private Vector3Int m_Pivot;

        private ArrayList m_Locations;
        private ArrayList m_Tiles;

        private static readonly Matrix4x4 s_Clockwise = new Matrix4x4(new Vector4(0f, 1f, 0f, 0f), new Vector4(-1f, 0f, 0f, 0f), new Vector4(0f, 0f, 1f, 0f), new Vector4(0f, 0f, 0f, 1f));
        private static readonly Matrix4x4 s_CounterClockwise = new Matrix4x4(new Vector4(0f, -1f, 0f, 0f), new Vector4(1f, 0f, 0f, 0f), new Vector4(0f, 0f, 1f, 0f), new Vector4(0f, 0f, 0f, 1f));
        private static readonly Matrix4x4 s_180Rotate = new Matrix4x4(new Vector4(-1f, 0f, 0f, 0f), new Vector4(0f, -1f, 0f, 0f), new Vector4(0f, 0f, 1f, 0f), new Vector4(0f, 0f, 0f, 1f));

        public Vector3Int size { get { return m_Size; } set { m_Size = value; SizeUpdated(); } }
        public Vector3Int pivot { get { return m_Pivot; } set { m_Pivot = value; } }
        public BrushCell[] cells { get { return m_Cells; } }
        public int cellCount { get { return m_Cells != null ? m_Cells.Length : 0; } }

        private ArrayList locations
        {
            get
            {
                if (m_Locations == null)
                    m_Locations = new ArrayList();
                return m_Locations;
            }
        }

        private ArrayList tiles
        {
            get
            {
                if (m_Tiles == null)
                    m_Tiles = new ArrayList();
                return m_Tiles;
            }
        }

        public GridBrush()
        {
            Init(Vector3Int.one, Vector3Int.zero);
            SizeUpdated();
        }

        public void Init(Vector3Int size)
        {
            Init(size, Vector3Int.zero);
            SizeUpdated();
        }

        public void Init(Vector3Int size, Vector3Int pivot)
        {
            m_Size = size;
            m_Pivot = pivot;
            SizeUpdated();
        }

        public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
        {
            Vector3Int min = position - pivot;
            BoundsInt bounds = new BoundsInt(min, m_Size);
            BoxFill(gridLayout, brushTarget, bounds);
        }

        public override void Erase(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
        {
            Vector3Int min = position - pivot;
            BoundsInt bounds = new BoundsInt(min, m_Size);
            BoxErase(gridLayout, brushTarget, bounds);
        }

        public override void BoxFill(GridLayout gridLayout, GameObject brushTarget, BoundsInt position)
        {
            if (brushTarget == null)
                return;

            Tilemap map = brushTarget.GetComponent<Tilemap>();
            if (map == null)
                return;

            locations.Clear();
            tiles.Clear();
            foreach (Vector3Int location in position.allPositionsWithin)
            {
                Vector3Int local = location - position.min;
                BrushCell cell = m_Cells[GetCellIndexWrapAround(local.x, local.y, local.z)];
                if (cell.tile == null)
                    continue;

                locations.Add(location);
                tiles.Add(cell.tile);
            }
            map.SetTiles((Vector3Int[])locations.ToArray(typeof(Vector3Int)), (TileBase[])tiles.ToArray(typeof(TileBase)));
            foreach (Vector3Int location in position.allPositionsWithin)
            {
                Vector3Int local = location - position.min;
                BrushCell cell = m_Cells[GetCellIndexWrapAround(local.x, local.y, local.z)];
                if (cell.tile == null)
                    continue;

                map.SetTransformMatrix(location, cell.matrix);
                map.SetColor(location, cell.color);
            }
        }

        public override void BoxErase(GridLayout gridLayout, GameObject brushTarget, BoundsInt position)
        {
            if (brushTarget == null)
                return;

            Tilemap map = brushTarget.GetComponent<Tilemap>();
            if (map == null)
                return;

            var emptyTiles = new TileBase[position.size.x * position.size.y * position.size.z];
            map.SetTilesBlock(position, emptyTiles);
            foreach (Vector3Int location in position.allPositionsWithin)
            {
                map.SetTransformMatrix(location, Matrix4x4.identity);
                map.SetColor(location, Color.white);
            }
        }

        public override void FloodFill(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
        {
            if (cellCount == 0)
                return;

            if (brushTarget == null)
                return;

            Tilemap map = brushTarget.GetComponent<Tilemap>();
            if (map == null)
                return;

            map.FloodFill(position, cells[0].tile);
        }

        public override void Rotate(RotationDirection direction, Grid.CellLayout layout)
        {
            switch (layout)
            {
                case GridLayout.CellLayout.Hexagon:
                    RotateHexagon(direction);
                    break;
                case Grid.CellLayout.Isometric:
                case Grid.CellLayout.IsometricZAsY:
                case GridLayout.CellLayout.Rectangle:
                {
                    Vector3Int oldSize = m_Size;
                    BrushCell[] oldCells = m_Cells.Clone() as BrushCell[];
                    size = new Vector3Int(oldSize.y, oldSize.x, oldSize.z);
                    BoundsInt oldBounds = new BoundsInt(Vector3Int.zero, oldSize);

                    foreach (Vector3Int oldPos in oldBounds.allPositionsWithin)
                    {
                        int newX = direction == RotationDirection.Clockwise ? oldSize.y - oldPos.y - 1 : oldPos.y;
                        int newY = direction == RotationDirection.Clockwise ? oldPos.x : oldSize.x - oldPos.x - 1;
                        int toIndex = GetCellIndex(newX, newY, oldPos.z);
                        int fromIndex = GetCellIndex(oldPos.x, oldPos.y, oldPos.z, oldSize.x, oldSize.y, oldSize.z);
                        m_Cells[toIndex] = oldCells[fromIndex];
                    }

                    int newPivotX = direction == RotationDirection.Clockwise ? oldSize.y - pivot.y - 1 : pivot.y;
                    int newPivotY = direction == RotationDirection.Clockwise ? pivot.x : oldSize.x - pivot.x - 1;
                    pivot = new Vector3Int(newPivotX, newPivotY, pivot.z);

                    Matrix4x4 rotation = direction == RotationDirection.Clockwise ? s_Clockwise : s_CounterClockwise;
                    Matrix4x4 counterRotation = direction != RotationDirection.Clockwise ? s_Clockwise : s_CounterClockwise;
                    foreach (BrushCell cell in m_Cells)
                    {
                        Matrix4x4 oldMatrix = cell.matrix;
                        bool counter = (oldMatrix.lossyScale.x < 0) ^ (oldMatrix.lossyScale.y < 0);
                        cell.matrix = oldMatrix * (counter ? counterRotation : rotation);
                    }
                }
                break;
            }
        }

        private static Vector3Int RotateHexagonPosition(RotationDirection direction, Vector3Int position)
        {
            var cube = HexagonToCube(position);
            Vector3Int rotatedCube = Vector3Int.zero;
            if (RotationDirection.Clockwise == direction)
            {
                rotatedCube.x = -cube.z;
                rotatedCube.y = -cube.x;
                rotatedCube.z = -cube.y;
            }
            else
            {
                rotatedCube.x = -cube.y;
                rotatedCube.y = -cube.z;
                rotatedCube.z = -cube.x;
            }
            return CubeToHexagon(rotatedCube);
        }

        private void RotateHexagon(RotationDirection direction)
        {
            BrushCell[] oldCells = m_Cells.Clone() as BrushCell[];
            Vector3Int oldPivot = new Vector3Int(pivot.x, pivot.y, pivot.z);
            Vector3Int oldSize = new Vector3Int(size.x, size.y, size.z);
            Vector3Int minSize = Vector3Int.zero;
            Vector3Int maxSize = Vector3Int.zero;
            BoundsInt oldBounds = new BoundsInt(Vector3Int.zero, oldSize);
            foreach (Vector3Int oldPos in oldBounds.allPositionsWithin)
            {
                if (oldCells[GetCellIndex(oldPos.x, oldPos.y, oldPos.z, oldSize.x, oldSize.y, oldSize.z)].tile == null)
                    continue;
                var pos = RotateHexagonPosition(direction, oldPos - oldPivot);
                minSize.x = Mathf.Min(minSize.x, pos.x);
                minSize.y = Mathf.Min(minSize.y, pos.y);
                maxSize.x = Mathf.Max(maxSize.x, pos.x);
                maxSize.y = Mathf.Max(maxSize.y, pos.y);
            }
            Vector3Int newSize = new Vector3Int(1 + maxSize.x - minSize.x, 1 + maxSize.y - minSize.y, oldSize.z);
            Vector3Int newPivot = new Vector3Int(-minSize.x, -minSize.y, oldPivot.z);
            UpdateSizeAndPivot(newSize, new Vector3Int(newPivot.x, newPivot.y, newPivot.z));
            foreach (Vector3Int oldPos in oldBounds.allPositionsWithin)
            {
                if (oldCells[GetCellIndex(oldPos.x, oldPos.y, oldPos.z, oldSize.x, oldSize.y, oldSize.z)].tile == null)
                    continue;
                Vector3Int newPos = RotateHexagonPosition(direction, new Vector3Int(oldPos.x, oldPos.y, oldPos.z) - oldPivot) + newPivot;
                m_Cells[GetCellIndex(newPos.x, newPos.y, newPos.z)] = oldCells[GetCellIndex(oldPos.x, oldPos.y, oldPos.z, oldSize.x, oldSize.y, oldSize.z)];
            }
            // Do not rotate hexagon cell matrix, as hexagon cells are not perfect hexagons
        }

        private static Vector3Int HexagonToCube(Vector3Int position)
        {
            Vector3Int cube = Vector3Int.zero;
            cube.x = position.x - (position.y - (position.y & 1)) / 2;
            cube.z = position.y;
            cube.y = -cube.x - cube.z;
            return cube;
        }

        private static Vector3Int CubeToHexagon(Vector3Int position)
        {
            Vector3Int hexagon = Vector3Int.zero;
            hexagon.x = position.x + (position.z - (position.z & 1)) / 2;
            hexagon.y = position.z;
            hexagon.z = 0;
            return hexagon;
        }

        public override void Flip(FlipAxis flip, Grid.CellLayout layout)
        {
            if (flip == FlipAxis.X)
                FlipX(layout);
            else
                FlipY(layout);
        }

        public override void Pick(GridLayout gridLayout, GameObject brushTarget, BoundsInt position, Vector3Int pickStart)
        {
            Reset();
            UpdateSizeAndPivot(new Vector3Int(position.size.x, position.size.y, 1), new Vector3Int(pickStart.x, pickStart.y, 0));

            Tilemap tilemap = brushTarget.GetComponent<Tilemap>();
            foreach (Vector3Int pos in position.allPositionsWithin)
            {
                Vector3Int brushPosition = new Vector3Int(pos.x - position.x, pos.y - position.y, 0);
                PickCell(pos, brushPosition, tilemap);
            }
        }

        private void PickCell(Vector3Int position, Vector3Int brushPosition, Tilemap tilemap)
        {
            if (tilemap == null)
                return;

            SetTile(brushPosition, tilemap.GetTile(position));
            SetMatrix(brushPosition, tilemap.GetTransformMatrix(position));
            SetColor(brushPosition, tilemap.GetColor(position));
        }

        public override void MoveStart(GridLayout gridLayout, GameObject brushTarget, BoundsInt position)
        {
            Reset();
            UpdateSizeAndPivot(new Vector3Int(position.size.x, position.size.y, 1), Vector3Int.zero);

            Tilemap tilemap = brushTarget.GetComponent<Tilemap>();
            if (tilemap == null)
                return;

            foreach (Vector3Int pos in position.allPositionsWithin)
            {
                Vector3Int brushPosition = new Vector3Int(pos.x - position.x, pos.y - position.y, 0);
                PickCell(pos, brushPosition, tilemap);
                tilemap.SetTile(pos, null);
            }
        }

        public override void MoveEnd(GridLayout gridLayout, GameObject brushTarget, BoundsInt position)
        {
            Paint(gridLayout, brushTarget, position.min);
            Reset();
        }

        public void Reset()
        {
            UpdateSizeAndPivot(Vector3Int.one, Vector3Int.zero);
        }

        private void FlipX(Grid.CellLayout layout)
        {
            BrushCell[] oldCells = m_Cells.Clone() as BrushCell[];
            BoundsInt oldBounds = new BoundsInt(Vector3Int.zero, m_Size);

            foreach (Vector3Int oldPos in oldBounds.allPositionsWithin)
            {
                int newX = m_Size.x - oldPos.x - 1;
                int toIndex = GetCellIndex(newX, oldPos.y, oldPos.z);
                int fromIndex = GetCellIndex(oldPos);
                m_Cells[toIndex] = oldCells[fromIndex];
            }

            int newPivotX = m_Size.x - pivot.x - 1;
            pivot = new Vector3Int(newPivotX, pivot.y, pivot.z);
            FlipCells(ref m_Cells, new Vector3(-1f, 1f, 1f), layout == GridLayout.CellLayout.Hexagon);
        }

        private void FlipY(Grid.CellLayout layout)
        {
            BrushCell[] oldCells = m_Cells.Clone() as BrushCell[];
            BoundsInt oldBounds = new BoundsInt(Vector3Int.zero, m_Size);

            foreach (Vector3Int oldPos in oldBounds.allPositionsWithin)
            {
                int newY = m_Size.y - oldPos.y - 1;
                int toIndex = GetCellIndex(oldPos.x, newY, oldPos.z);
                int fromIndex = GetCellIndex(oldPos);
                m_Cells[toIndex] = oldCells[fromIndex];
            }

            int newPivotY = m_Size.y - pivot.y - 1;
            pivot = new Vector3Int(pivot.x, newPivotY, pivot.z);
            FlipCells(ref m_Cells, new Vector3(1f, -1f, 1f), layout == GridLayout.CellLayout.Hexagon);
        }

        private static void FlipCells(ref BrushCell[] cells, Vector3 scale, bool skipRotation)
        {
            Matrix4x4 flip = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, scale);
            foreach (BrushCell cell in cells)
            {
                Matrix4x4 oldMatrix = cell.matrix;
                if (skipRotation || Mathf.Approximately(oldMatrix.rotation.x + oldMatrix.rotation.y + oldMatrix.rotation.z + oldMatrix.rotation.w, 1.0f))
                    cell.matrix = oldMatrix * flip;
                else
                    cell.matrix = oldMatrix * s_180Rotate * flip;
            }
        }

        public void UpdateSizeAndPivot(Vector3Int size, Vector3Int pivot)
        {
            m_Size = size;
            m_Pivot = pivot;
            SizeUpdated();
        }

        public void SetTile(Vector3Int position, TileBase tile)
        {
            if (ValidateCellPosition(position))
                m_Cells[GetCellIndex(position)].tile = tile;
        }

        public void SetMatrix(Vector3Int position, Matrix4x4 matrix)
        {
            if (ValidateCellPosition(position))
                m_Cells[GetCellIndex(position)].matrix = matrix;
        }

        public void SetColor(Vector3Int position, Color color)
        {
            if (ValidateCellPosition(position))
                m_Cells[GetCellIndex(position)].color = color;
        }

        public int GetCellIndex(Vector3Int brushPosition)
        {
            return GetCellIndex(brushPosition.x, brushPosition.y, brushPosition.z);
        }

        public int GetCellIndex(int x, int y, int z)
        {
            return x + m_Size.x * y + m_Size.x * m_Size.y * z;
        }

        public int GetCellIndex(int x, int y, int z, int sizex, int sizey, int sizez)
        {
            return x + sizex * y + sizex * sizey * z;
        }

        public int GetCellIndexWrapAround(int x, int y, int z)
        {
            return (x % m_Size.x) + m_Size.x * (y % m_Size.y) + m_Size.x * m_Size.y * (z % m_Size.z);
        }

        private bool ValidateCellPosition(Vector3Int position)
        {
            var valid =
                position.x >= 0 && position.x < size.x &&
                position.y >= 0 && position.y < size.y &&
                position.z >= 0 && position.z < size.z;
            if (!valid)
                throw new ArgumentException(string.Format("Position {0} is an invalid cell position. Valid range is between [{1}, {2}).", position, Vector3Int.zero, size));
            return valid;
        }

        private void SizeUpdated()
        {
            m_Cells = new BrushCell[m_Size.x * m_Size.y * m_Size.z];
            BoundsInt bounds = new BoundsInt(Vector3Int.zero, m_Size);
            foreach (Vector3Int pos in bounds.allPositionsWithin)
            {
                m_Cells[GetCellIndex(pos)] = new BrushCell();
            }
        }

        public override int GetHashCode()
        {
            int hash = 0;
            unchecked
            {
                foreach (var cell in cells)
                {
                    hash = hash * 33 + cell.GetHashCode();
                }
            }
            return hash;
        }

        [Serializable]
        public class BrushCell
        {
            public TileBase tile { get { return m_Tile; } set { m_Tile = value; } }
            public Matrix4x4 matrix { get { return m_Matrix; } set { m_Matrix = value; } }
            public Color color { get { return m_Color; } set { m_Color = value; } }

            [SerializeField] private TileBase m_Tile;
            [SerializeField] Matrix4x4 m_Matrix = Matrix4x4.identity;
            [SerializeField] private Color m_Color = Color.white;

            public override int GetHashCode()
            {
                int hash = 0;
                unchecked
                {
                    hash = tile != null ? tile.GetInstanceID() : 0;
                    hash = hash * 33 + matrix.GetHashCode();
                    hash = hash * 33 + matrix.rotation.GetHashCode();
                    hash = hash * 33 + color.GetHashCode();
                }
                return hash;
            }
        }
    }
}
