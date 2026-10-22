// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.UIElements.UIR
{
    // What a clipped filtered subtree must still render around its visible area so its backdrop-filters read complete content
    static class BackdropReadRegions
    {
        struct BackdropRead
        {
            public RenderData owner;
            public Rect bounds;
            public Rect capture;
            public float reach;
            public Rect read;
            public bool reads;
            public bool isWhole;
        }

        [NoAutoStaticsCleanup] // scratch reused by every tree each frame; Extend clears it
        static readonly List<BackdropRead> s_Backdrops = new();

        public static void Add(RenderData owner)
        {
            BackdropFilterHelper.ComputeReadGeometryInRenderTree(owner, out Rect bounds, out Rect capture, out float reach);
            s_Backdrops.Add(new BackdropRead { owner = owner, bounds = bounds, capture = capture, reach = reach });
        }

        public static int addedCount => s_Backdrops.Count;

        public static bool IsAdded(RenderData owner)
        {
            int count = s_Backdrops.Count;
            BackdropRead[] backdrops = NoAllocHelpers.ExtractArrayFromList(s_Backdrops);
            for (int i = 0; i < count; ++i)
            {
                if (backdrops[i].owner == owner)
                    return true;
            }
            return false;
        }

        // Needs the pre-order labels of the owners to be up to date
        public static Rect Extend(Rect visible)
        {
            Rect extended = visible;
            int count = s_Backdrops.Count;
            BackdropRead[] backdrops = NoAllocHelpers.ExtractArrayFromList(s_Backdrops);

            for (int i = 0; i < count; ++i)
            {
                ref BackdropRead backdrop = ref backdrops[i];
                Rect? visiblePart = UIRUtility.Intersect(backdrop.bounds, visible);
                if (visiblePart == null)
                    continue;

                // Blur taps clamp at the capture's edge, so nothing past it is ever read
                backdrop.read = UIRUtility.Intersect(UIRUtility.Inflate(visiblePart.Value, backdrop.reach), backdrop.capture).Value;
                backdrop.reads = true;
                extended = UIRUtility.Encapsulate(extended, backdrop.read);
            }

            // A capture holds what earlier backdrop-filters drew, so one read where its own capture was cut short is rendered whole
            bool madeWhole;
            do
            {
                madeWhole = false;
                for (int i = 0; i < count; ++i)
                {
                    ref BackdropRead backdrop = ref backdrops[i];
                    if (backdrop.isWhole || !IsReadPastItsCompleteArea(backdrops, count, i, extended))
                        continue;

                    backdrop.read = backdrop.capture;
                    backdrop.reads = true;
                    backdrop.isWhole = true;
                    extended = UIRUtility.Encapsulate(extended, backdrop.read);
                    madeWhole = true;
                }
            }
            while (madeWhole);

            s_Backdrops.Clear();
            return extended;
        }

        static bool IsReadPastItsCompleteArea(BackdropRead[] backdrops, int count, int index, Rect extended)
        {
            ref BackdropRead backdrop = ref backdrops[index];
            Rect complete = GetCompleteArea(in backdrop, extended);
            for (int j = 0; j < count; ++j)
            {
                ref BackdropRead reader = ref backdrops[j];

                // Only a backdrop drawn later captures this one's output
                if (j == index || !reader.reads || reader.owner.preOrderLabel < backdrop.owner.preOrderLabel)
                    continue;

                Rect? readPart = UIRUtility.Intersect(reader.read, backdrop.bounds);
                if (readPart != null && !Contains(complete, readPart.Value))
                    return true;
            }
            return false;
        }

        // Where its output matches the unclipped one: on each side, either its capture is rendered up to that side or the point reads no further than what is
        static Rect GetCompleteArea(in BackdropRead backdrop, Rect extended)
        {
            Rect capture = backdrop.capture;
            float reach = backdrop.reach;
            return Rect.MinMaxRect(
                capture.xMin >= extended.xMin ? capture.xMin : extended.xMin + reach,
                capture.yMin >= extended.yMin ? capture.yMin : extended.yMin + reach,
                capture.xMax <= extended.xMax ? capture.xMax : extended.xMax - reach,
                capture.yMax <= extended.yMax ? capture.yMax : extended.yMax - reach);
        }

        static bool Contains(Rect outer, Rect inner)
        {
            return inner.xMin >= outer.xMin && inner.yMin >= outer.yMin && inner.xMax <= outer.xMax && inner.yMax <= outer.yMax;
        }
    }
}
