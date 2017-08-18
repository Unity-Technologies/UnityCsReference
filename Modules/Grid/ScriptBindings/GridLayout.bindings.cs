// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Grid/Public/GridMarshalling.h")]
    [NativeType(Header = "Modules/Grid/Public/Grid.h")]
    public partial class GridLayout : Behaviour
    {
        // Enums.
        public enum CellLayout
        {
            Rectangle = 0,
        }

        public enum CellSwizzle
        {
            XYZ = 0,
            XZY = 1,
            YXZ = 2,
            YZX = 3,
            ZXY = 4,
            ZYX = 5
        };

        public Vector3 cellSize
        {
            get { return GetCellSize(this); }
        }
        [FreeFunction("GridLayoutBindings::GetCellSize")]
        private extern static Vector3 GetCellSize(GridLayout self);

        public Vector3 cellGap
        {
            get { return GetCellGap(this); }
        }
        [FreeFunction("GridLayoutBindings::GetCellGap")]
        private extern static Vector3 GetCellGap(GridLayout self);

        public extern CellLayout cellLayout
        {
            get;
        }

        public extern CellSwizzle cellSwizzle
        {
            get;
        }

        public Bounds GetBoundsLocal(Vector3Int cellPosition) { return GetBoundsLocal(this, cellPosition); }

        [FreeFunction("GridLayoutBindings::GetBoundsLocal")]
        private extern static Bounds GetBoundsLocal(GridLayout self, Vector3Int cellPosition);

        public Vector3 CellToLocal(Vector3Int cellPosition) { return CellToLocal(this, cellPosition); }

        [FreeFunction("GridLayoutBindings::CellToLocal")]
        private extern static Vector3 CellToLocal(GridLayout self, Vector3Int cellPosition);

        public Vector3Int LocalToCell(Vector3 localPosition) { return LocalToCell(this, localPosition); }

        [FreeFunction("GridLayoutBindings::LocalToCell")]
        private extern static Vector3Int LocalToCell(GridLayout self, Vector3 localPosition);

        public Vector3 CellToLocalInterpolated(Vector3 cellPosition) { return CellToLocalInterpolated(this, cellPosition); }

        [FreeFunction("GridLayoutBindings::CellToLocalInterpolated")]
        private extern static Vector3 CellToLocalInterpolated(GridLayout self, Vector3 cellPosition);

        public Vector3 LocalToCellInterpolated(Vector3 localPosition) { return LocalToCellInterpolated(this, localPosition); }

        [FreeFunction("GridLayoutBindings::LocalToCellInterpolated")]
        private extern static Vector3 LocalToCellInterpolated(GridLayout self, Vector3 localPosition);

        public Vector3 CellToWorld(Vector3Int cellPosition) { return CellToWorld(this, cellPosition); }

        [FreeFunction("GridLayoutBindings::CellToWorld")]
        private extern static Vector3 CellToWorld(GridLayout self, Vector3Int cellPosition);

        public Vector3Int WorldToCell(Vector3 worldPosition) { return WorldToCell(this, worldPosition); }

        [FreeFunction("GridLayoutBindings::WorldToCell")]
        private extern static Vector3Int WorldToCell(GridLayout self, Vector3 worldPosition);

        public Vector3 LocalToWorld(Vector3 localPosition) { return LocalToWorld(this, localPosition); }

        [FreeFunction("GridLayoutBindings::LocalToWorld")]
        private extern static Vector3 LocalToWorld(GridLayout self, Vector3 localPosition);

        public Vector3 WorldToLocal(Vector3 worldPosition) { return WorldToLocal(this, worldPosition); }

        [FreeFunction("GridLayoutBindings::WorldToLocal")]
        private extern static Vector3 WorldToLocal(GridLayout self, Vector3 worldPosition);

        public Vector3 GetLayoutCellCenter() { return GetLayoutCellCenter(this); }

        [FreeFunction("GridLayoutBindings::GetLayoutCellCenter")]
        private extern static Vector3 GetLayoutCellCenter(GridLayout self);
    }
}
