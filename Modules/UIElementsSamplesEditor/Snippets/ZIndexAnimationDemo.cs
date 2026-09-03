// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class ZIndexAnimationDemo : ElementSnippet<ZIndexAnimationDemo>
    {
        internal override void Apply(VisualElement container)
        {
            container.AddToClassList(EditorGUIUtility.isProSkin ? "dark" : "light");

            Initialize(container);
        }

        /// <sample>
        #region sample
        VisualElement m_Root;
        VisualElement m_ExpandedView;
        VisualElement m_TransferItem;
        VisualElement m_SlotsA;
        VisualElement m_SlotsB;

        bool m_IsAnimating;
        Rect m_CollapseRect;

        const long k_FrameMs = 32;
        const long k_ExpandMs = 400;
        const long k_TransferMs = 500;

        static readonly string[] k_CardDescs =
        {
            "Choose your fighter.\nEnter the arena.",
            "Forge new equipment.\nUpgrade your gear.",
            "Discover new lands.\nFind hidden treasures.",
            "Buy low, sell high.\nBuild your fortune.",
        };

        void Initialize(VisualElement root)
        {
            m_Root = root;
            m_ExpandedView = root.Q("expanded-view");
            m_TransferItem = root.Q("transfer-item");
            m_SlotsA = root.Q("slots-a");
            m_SlotsB = root.Q("slots-b");
            m_IsAnimating = false;

            root.Q<Button>("collapse-btn").clicked += CollapseView;

            SetupExpandCards(root);
            SetupInventories();
        }

        void SetupExpandCards(VisualElement root)
        {
            var grid = root.Q("card-grid");
            int i = 0;
            foreach (var card in grid.Children())
            {
                var desc = k_CardDescs[i++];
                card.RegisterCallback<PointerDownEvent>(_ => ExpandCard(card, desc));
            }
        }

        void ExpandCard(VisualElement card, string desc)
        {
            if (m_IsAnimating) return;
            m_IsAnimating = true;

            m_CollapseRect = card.worldBound;
            var startPos = m_Root.WorldToLocal(m_CollapseRect.position);

            m_Root.Q<Label>("expanded-title").text = card.Q<Label>().text;
            m_Root.Q<Label>("expanded-body").text = desc;
            m_ExpandedView.style.backgroundColor = card.resolvedStyle.backgroundColor;

            m_ExpandedView.style.left = startPos.x;
            m_ExpandedView.style.top = startPos.y;
            m_ExpandedView.style.width = m_CollapseRect.width;
            m_ExpandedView.style.height = m_CollapseRect.height;
            m_ExpandedView.style.opacity = 0f;
            m_ExpandedView.style.display = DisplayStyle.Flex;

            m_ExpandedView.schedule.Execute(() =>
            {
                var rs = m_Root.resolvedStyle;
                m_ExpandedView.style.left = 0f;
                m_ExpandedView.style.top = 0f;
                m_ExpandedView.style.width = rs.width;
                m_ExpandedView.style.height = rs.height;
                m_ExpandedView.style.opacity = 1f;
            }).ExecuteLater(k_FrameMs);
        }

        void CollapseView()
        {
            if (m_ExpandedView.style.display != DisplayStyle.Flex) return;

            var collapsePos = m_Root.WorldToLocal(m_CollapseRect.position);
            m_ExpandedView.style.left = collapsePos.x;
            m_ExpandedView.style.top = collapsePos.y;
            m_ExpandedView.style.width = m_CollapseRect.width;
            m_ExpandedView.style.height = m_CollapseRect.height;
            m_ExpandedView.style.opacity = 0f;

            m_ExpandedView.schedule.Execute(() =>
            {
                m_ExpandedView.style.display = DisplayStyle.None;
                m_IsAnimating = false;
            }).ExecuteLater(k_ExpandMs + 50);
        }

        void SetupInventories()
        {
            m_SlotsA.Query<VisualElement>(className: "inv-item").ForEach(item =>
                item.RegisterCallback<PointerDownEvent>(_ => TransferItem(item)));
        }

        void TransferItem(VisualElement item)
        {
            if (m_IsAnimating) return;

            var inA = IsDescendant(item, m_SlotsA);
            var dstGrid = inA ? m_SlotsB : m_SlotsA;

            VisualElement destSlot = null;
            foreach (var slot in dstGrid.Children())
            {
                if (slot.childCount == 0) { destSlot = slot; break; }
            }
            if (destSlot == null) return;

            m_IsAnimating = true;

            var srcWorld = item.worldBound;
            var dstWorld = destSlot.worldBound;
            var srcPos = m_Root.WorldToLocal(srcWorld.position);
            var offset = m_Root.WorldToLocal(dstWorld.position) - srcPos;

            m_Root.Q<Label>("transfer-item-label").text = item.Q<Label>().text;

            m_TransferItem.style.left = srcPos.x;
            m_TransferItem.style.top = srcPos.y;
            m_TransferItem.style.width = srcWorld.width;
            m_TransferItem.style.height = srcWorld.height;
            m_TransferItem.style.translate = new StyleTranslate(new Translate(0f, 0f));
            m_TransferItem.style.display = DisplayStyle.Flex;
            item.style.opacity = 0f;

            m_TransferItem.schedule.Execute(() =>
            {
                m_TransferItem.style.translate = new StyleTranslate(new Translate(offset.x, offset.y));
            }).ExecuteLater(k_FrameMs);

            m_TransferItem.schedule.Execute(() =>
            {
                m_TransferItem.style.display = DisplayStyle.None;
                m_TransferItem.style.translate = new StyleTranslate(new Translate(0f, 0f));

                item.style.opacity = 1f;
                item.RemoveFromHierarchy();
                destSlot.Add(item);
                m_IsAnimating = false;
            }).ExecuteLater(k_FrameMs + k_TransferMs + 50);
        }

        static bool IsDescendant(VisualElement element, VisualElement ancestor)
        {
            var current = element.parent;
            while (current != null)
            {
                if (current == ancestor) return true;
                current = current.parent;
            }
            return false;
        }
        #endregion
        /// </sample>
    }
}
