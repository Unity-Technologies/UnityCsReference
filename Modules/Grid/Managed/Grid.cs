// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine
{
    public partial class Grid
    {
        public Vector3 GetCellCenterLocal(Vector3Int position) 
        { 
            Vector3 cs = cellSize;
            Vector3 ics = inverseCellStride;
            Vector3 localCellCenter = GetLayoutCellCenter();
            Vector3 relativeCellCenter = new Vector3(localCellCenter.x * cs.x * ics.x, localCellCenter.y * cs.y * ics.y, localCellCenter.z * cs.z * ics.z);
            return CellToLocalInterpolated(position) + CellToLocalInterpolated(relativeCellCenter); 
        }
        public Vector3 GetCellCenterWorld(Vector3Int position) { return LocalToWorld(GetCellCenterLocal(position)); }
    }
}
