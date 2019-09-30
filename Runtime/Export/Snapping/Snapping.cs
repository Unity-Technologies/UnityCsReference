// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine
{
    public static class Snapping
    {
        internal static bool IsCardinalDirection(Vector3 direction)
        {
            return
                Mathf.Abs(direction.x) > 0f && Mathf.Approximately(direction.y, 0f) && Mathf.Approximately(direction.z, 0f)
                || Mathf.Abs(direction.y) > 0f && Mathf.Approximately(direction.x, 0f) && Mathf.Approximately(direction.z, 0f)
                || Mathf.Abs(direction.z) > 0f && Mathf.Approximately(direction.x, 0f) && Mathf.Approximately(direction.y, 0f);
        }

        public static float Snap(float val, float snap)
        {
            if (snap == 0)
                return val;

            return snap * Mathf.Round(val / snap);
        }

        public static Vector2 Snap(Vector2 val, Vector2 snap)
        {
            return new Vector3(
                Mathf.Abs(snap.x) < Mathf.Epsilon ? val.x : snap.x * Mathf.Round(val.x / snap.x),
                Mathf.Abs(snap.y) < Mathf.Epsilon ? val.y : snap.y * Mathf.Round(val.y / snap.y)
            );
        }

        public static Vector3 Snap(Vector3 val, Vector3 snap, SnapAxis axis = SnapAxis.All)
        {
            return new Vector3(
                (axis & SnapAxis.X) == SnapAxis.X ? Snap(val.x, snap.x) : val.x,
                (axis & SnapAxis.Y) == SnapAxis.Y ? Snap(val.y, snap.y) : val.y,
                (axis & SnapAxis.Z) == SnapAxis.Z ? Snap(val.z, snap.z) : val.z
            );
        }
    }
}
