// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.UIElements
{

    internal interface StopRecordingInterface
    {
        public void StopRecording();
    }

    internal class LayoutDebuggerItem
    {
        public LayoutDebuggerItem(int frameIndex, int passIndex, int layoutLoop, LayoutDebuggerVisualElement ve)
        {
            m_FrameIndex = frameIndex;
            m_PassIndex = passIndex;
            m_LayoutLoop = layoutLoop;
            m_VE = ve;
        }

        public int m_FrameIndex;
        public int m_PassIndex;
        public int m_LayoutLoop;

        public LayoutDebuggerVisualElement m_VE;
    }

    internal class LayoutDebuggerVisualElement
    {
        public List<LayoutDebuggerVisualElement> m_Children = null;
        public VisualElement m_OriginalVisualElement;
        public string name;
        public Rect layout = new Rect();
        public bool visible = false;
        public bool enable = false;
        public bool enabledInHierarchy = false;
        public bool isDirty = false;
        public LayoutDebuggerVisualElement parent = null;

        public bool IsVisualElementVisible()
        {
            return visible && enable && enabledInHierarchy;
        }

        public int CountTotalElement()
        {
            int count = 0;
            CountTotalElement(this, ref count);
            return count;
        }

        private static void CountTotalElement(LayoutDebuggerVisualElement ve, ref int count)
        {
            if (!ve.IsVisualElementVisible())
            {
                return;
            }

            count++;

            for (int i = 0; i < ve.m_Children.Count; ++i)
            {
                var child = ve.m_Children[i];
                CountTotalElement(child, ref count);
            }
        }

        public static void CopyLayout(VisualElement source, LayoutDebuggerVisualElement dest, List<VisualElement> currentDirtyVE)
        {
            dest.name = source.name;
            dest.layout = source.layout;
            dest.visible = source.visible;
            dest.enable = source.enabledSelf;
            dest.enabledInHierarchy = source.enabledInHierarchy;
            dest.m_OriginalVisualElement = source;
            dest.isDirty = currentDirtyVE?.Contains(source) ?? false;

            var childCount = source.hierarchy.childCount;

            dest.m_Children = new List<LayoutDebuggerVisualElement>(childCount);

            for (int i = 0; i < childCount; ++i)
            {
                var child = source.hierarchy[i];
                LayoutDebuggerVisualElement ve = new LayoutDebuggerVisualElement();
                CopyLayout(child, ve, currentDirtyVE);
                ve.parent = dest;
                dest.m_Children.Add(ve);
            }
        }

        public static void TrackDirtyElement(VisualElement ve, List<VisualElement> dirtyVE)
        {
            if (ve.layoutNode.IsDirty)
            {
                dirtyVE.Add(ve);
            }
            else
            {
                return;
            }

            foreach (var child in ve.hierarchy.children)
            {
                TrackDirtyElement(child, dirtyVE);
            }
        }
    }

}
