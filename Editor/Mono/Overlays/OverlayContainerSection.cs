// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    enum OverlayContainerSection
    {
        BeforeSpacer,
        AfterSpacer,
        Middle
    }

    sealed class ContainerSection<TData> : ContainerSection where TData : struct
    {
        readonly List<TData> m_Data = new List<TData>();

        public ContainerSection()
        {
            overlayInserted += (overlay, index, hint) => m_Data.Insert(index, new TData());
            overlayRemoved += (overlay, index) => m_Data.RemoveAt(index);
        }

        public TData GetData(int index)
        {
            if (index < 0 || index >= m_Data.Count)
                return default;

            return m_Data[index];
        }

        public TData GetData(Overlay overlay)
        {
            return GetData(GetOverlayIndex(overlay));
        }

        public void SetData(int index, TData data)
        {
            if (index < 0 || index >= m_Data.Count)
                return;

            m_Data[index] = data;
        }

        public void SetData(Overlay overlay, TData data)
        {
            var index = GetOverlayIndex(overlay);
            SetData(index, data);
        }
    }

    class ContainerSection : VisualElement
    {
        readonly List<Overlay> m_Overlays = new List<Overlay>();
        readonly VisualElement m_Content;

        public event Action<Overlay, int, DockingHint> overlayInserted;
        public event Action<Overlay, int> overlayRemoved;

        public override VisualElement contentContainer => m_Content;

        public int overlayCount => m_Overlays.Count;

        public ContainerSection()
        {
            hierarchy.Add(m_Content = new VisualElement());
        }

        public bool ContainsOverlay(Overlay overlay)
        {
            return m_Overlays.Contains(overlay);
        }

        public int GetOverlayIndex(Overlay overlay)
        {
            return m_Overlays.IndexOf(overlay);
        }

        public int GetOverlayHierarchyIndex(Overlay overlay)
        {
            return m_Content.IndexOf(overlay.rootVisualElement);
        }

        public void InsertOverlay(Overlay overlay, int index, DockingHint hint)
        {
            if (overlay == null)
                return;

            int realIndex = -1;

            //Insert relative to another element in case other visual elements are added to hierarchy
            if (index < m_Overlays.Count)
            {
                realIndex = m_Content.IndexOf(m_Overlays[index].rootVisualElement);
            }
            else if (index == m_Overlays.Count)
            {
                realIndex = m_Content.childCount;
            }

            realIndex = Mathf.Max(realIndex, 0);

            m_Content.Insert(realIndex, overlay.rootVisualElement);
            m_Overlays.Insert(index, overlay);
            overlayInserted?.Invoke(overlay, index, hint);
        }

        public bool RemoveOverlay(Overlay overlay)
        {
            var index = m_Overlays.IndexOf(overlay);
            if (index >= 0)
            {
                m_Overlays.RemoveAt(index);
                overlay.rootVisualElement.RemoveFromHierarchy();
                overlayRemoved?.Invoke(overlay, index);
                return true;
            }

            return false;
        }

        public int GetVisibleCount()
        {
            int count = 0;
            foreach (var overlay in m_Overlays)
            {
                if (overlay != null && overlay.displayed)
                    ++count;
            }

            return count;
        }

        public Overlay GetFirstVisible()
        {
            foreach (var overlay in m_Overlays)
            {
                if (overlay != null && overlay.displayed)
                    return overlay;
            }

            return null;
        }

        public Overlay GetLastVisible()
        {
            for (int i = m_Overlays.Count - 1; i >= 0; --i)
            {
                var overlay = m_Overlays[i];
                if (overlay != null && overlay.displayed)
                    return overlay;
            }

            return null;
        }

        public bool HasVisibleOverlays()
        {
            return GetFirstVisible() != null;
        }

        public Overlay GetOverlay(int index)
        {
            return m_Overlays[index];
        }
    }
}
