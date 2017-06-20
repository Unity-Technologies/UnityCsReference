// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine
{
    public partial class Grid
    {
        public Vector3 GetCellCenterLocal(Vector3Int position) { return CellToLocalInterpolated(position + GetLayoutCellCenter()); }
        public Vector3 GetCellCenterWorld(Vector3Int position) { return LocalToWorld(CellToLocalInterpolated(position + GetLayoutCellCenter())); }
    }
}
