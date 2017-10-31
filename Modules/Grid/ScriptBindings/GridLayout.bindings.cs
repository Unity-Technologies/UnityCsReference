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

        public extern Vector3 cellSize
        {
            [FreeFunction("GridLayoutBindings::GetCellSize", HasExplicitThis = true)]
            get;
        }

        public extern Vector3 cellGap
        {
            [FreeFunction("GridLayoutBindings::GetCellGap", HasExplicitThis = true)]
            get;
        }

        public extern CellLayout cellLayout
        {
            get;
        }

        public extern CellSwizzle cellSwizzle
        {
            get;
        }

        [FreeFunction("GridLayoutBindings::GetBoundsLocal", HasExplicitThis = true)]
        public extern Bounds GetBoundsLocal(Vector3Int cellPosition);

        [FreeFunction("GridLayoutBindings::CellToLocal", HasExplicitThis = true)]
        public extern Vector3 CellToLocal(Vector3Int cellPosition);

        [FreeFunction("GridLayoutBindings::LocalToCell", HasExplicitThis = true)]
        public extern Vector3Int LocalToCell(Vector3 localPosition);

        [FreeFunction("GridLayoutBindings::CellToLocalInterpolated", HasExplicitThis = true)]
        public extern Vector3 CellToLocalInterpolated(Vector3 cellPosition);

        [FreeFunction("GridLayoutBindings::LocalToCellInterpolated", HasExplicitThis = true)]
        public extern Vector3 LocalToCellInterpolated(Vector3 localPosition);

        [FreeFunction("GridLayoutBindings::CellToWorld", HasExplicitThis = true)]
        public extern Vector3 CellToWorld(Vector3Int cellPosition);

        [FreeFunction("GridLayoutBindings::WorldToCell", HasExplicitThis = true)]
        public extern Vector3Int WorldToCell(Vector3 worldPosition);

        [FreeFunction("GridLayoutBindings::LocalToWorld", HasExplicitThis = true)]
        public extern Vector3 LocalToWorld(Vector3 localPosition);

        [FreeFunction("GridLayoutBindings::WorldToLocal", HasExplicitThis = true)]
        public extern Vector3 WorldToLocal(Vector3 worldPosition);

        [FreeFunction("GridLayoutBindings::GetLayoutCellCenter", HasExplicitThis = true)]
        public extern Vector3 GetLayoutCellCenter();
    }
}
