// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    static class GridSnapping
    {
        public static Func<Vector3, Vector3> snapPosition = null;
        public static Func<bool> activeFunc = null;

        public static bool active
        {
            get { return (activeFunc != null ? activeFunc() : false); }
        }

        public static Vector3 Snap(Vector3 position)
        {
            if (snapPosition != null)
                return snapPosition(position);
            return position;
        }

        public static void SnapSelectionToGrid(SnapAxis axis = SnapAxis.All)
        {
            var selections = Selection.transforms;
            if (selections != null && selections.Length > 0)
            {
                Undo.RecordObjects(selections, L10n.Tr("Snap to Grid"));
                Handles.SnapToGrid(selections, axis);
            }
        }
    }
}
