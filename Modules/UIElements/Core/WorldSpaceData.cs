// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    // Some data only make sense in a panel configured in world-space mode. Instead of inflating
    // every VisualElement with world-space specific data, we keep them in
    // a separate cache managed here.

    internal struct WorldSpaceData
    {
        public static readonly Bounds k_Empty3DBounds = new(Vector3.zero, Vector3.one * Mathf.NegativeInfinity);

        public Bounds localBounds3D;
        public Bounds localBoundsPicking3D;
        public Bounds localBoundsWithoutNested3D;

        // This value is technically a flat 2D rect, but it is only useful in world-space mode.
        // For this reason, we store it in this struct to avoid inflating the VisualElement class.
        public Rect boundingBoxWithoutNested;
    }

    internal static class WorldSpaceDataStore
    {
        private static Dictionary<uint, WorldSpaceData> m_WorldSpaceData = new();

        public static void SetWorldSpaceData(VisualElement ve, WorldSpaceData data)
        {
            m_WorldSpaceData[ve.controlid] = data;
        }

        public static WorldSpaceData GetWorldSpaceData(VisualElement ve)
        {
            if (m_WorldSpaceData.TryGetValue(ve.controlid, out var data))
                return data;
            return new WorldSpaceData();
        }

        public static void ClearWorldSpaceData(VisualElement ve)
        {
            ve.isLocalBounds3DDirty = true;
            ve.needs3DBounds = false;

            m_WorldSpaceData.Remove(ve.controlid);

            for (var i = ve.hierarchy.childCount - 1; i >= 0; --i)
                ClearWorldSpaceData(ve.hierarchy[i]);
        }

        public static void ClearLocalBounds3DData(VisualElement ve)
        {
            var data = GetWorldSpaceData(ve);
            data.localBounds3D = WorldSpaceData.k_Empty3DBounds;
            data.localBoundsPicking3D = WorldSpaceData.k_Empty3DBounds;
            data.localBoundsWithoutNested3D = WorldSpaceData.k_Empty3DBounds;
            SetWorldSpaceData(ve, data);
        }
    }
}
