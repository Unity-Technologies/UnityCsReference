// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class ZIndexOverlaysDemo : ElementSnippet<ZIndexOverlaysDemo>
    {
        internal override void Apply(VisualElement container)
        {
            container.AddToClassList(EditorGUIUtility.isProSkin ? "dark" : "light");

            Initialize(container);
        }

        /// <sample>
        #region sample
        VisualElement m_Root;
        VisualElement m_Tooltip;
        Label m_TooltipText;
        VisualElement m_DropdownList;
        VisualElement m_ContextMenu;

        static readonly string[] k_Tooltips =
        {
            "Current connection status.\nGreen = live, red = disconnected.",
            "Page view trends for the last 30 days.\nClick to learn more.",
            "3 unread alerts since last login.\nClick to review and acknowledge.",
        };

        void Initialize(VisualElement root)
        {
            m_Root = root;
            m_Tooltip = root.Q("tooltip");
            m_TooltipText = root.Q<Label>("tooltip-text");
            m_DropdownList = root.Q("dropdown-list");
            m_ContextMenu = root.Q("context-menu");

            SetupTooltips(root);
            SetupDropdown(root);
            SetupContextMenu(root);

            root.RegisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);
        }

        void SetupTooltips(VisualElement root)
        {
            var cards = root.Q("tooltip-cards");
            int i = 0;
            foreach (var card in cards.Children())
            {
                var tip = k_Tooltips[i++];
                card.RegisterCallback<PointerEnterEvent>(evt => ShowTooltip(tip, evt.position));
                card.RegisterCallback<PointerMoveEvent>(evt => MoveTooltip(evt.position));
                card.RegisterCallback<PointerLeaveEvent>(_ => m_Tooltip.style.display = DisplayStyle.None);
            }
        }

        void ShowTooltip(string text, Vector2 worldPos)
        {
            m_TooltipText.text = text;
            m_Tooltip.style.display = DisplayStyle.Flex;
            MoveTooltip(worldPos);
        }

        void MoveTooltip(Vector2 worldPos)
        {
            var p = m_Root.WorldToLocal(worldPos);
            m_Tooltip.style.left = p.x + 16f;
            m_Tooltip.style.top = p.y - 65f;
        }

        void SetupDropdown(VisualElement root)
        {
            var btn = root.Q<Button>("dropdown-btn");

            foreach (var row in m_DropdownList.Children())
            {
                var fruit = row.Q<Label>().text;
                row.RegisterCallback<PointerDownEvent>(evt =>
                {
                    btn.text = fruit + " ▼";
                    m_DropdownList.style.display = DisplayStyle.None;
                    evt.StopPropagation();
                });
            }

            btn.clicked += () =>
            {
                var wb = btn.worldBound;
                var pos = m_Root.WorldToLocal(new Vector2(wb.x, wb.yMax));
                m_DropdownList.style.left = pos.x;
                m_DropdownList.style.top = pos.y;
                m_DropdownList.style.display = DisplayStyle.Flex;
            };
        }

        void SetupContextMenu(VisualElement root)
        {
            var btn = root.Q<Button>("context-btn");

            foreach (var row in m_ContextMenu.Children())
            {
                if (!row.ClassListContains("context-menu-item")) continue;
                row.RegisterCallback<PointerDownEvent>(evt =>
                {
                    m_ContextMenu.style.display = DisplayStyle.None;
                    evt.StopPropagation();
                });
            }

            btn.clicked += () =>
            {
                var wb = btn.worldBound;
                var pos = m_Root.WorldToLocal(new Vector2(wb.xMax, wb.y));
                m_ContextMenu.style.left = pos.x;
                m_ContextMenu.style.top = pos.y;
                m_ContextMenu.style.display = DisplayStyle.Flex;
            };
        }

        void OnRootPointerDown(PointerDownEvent evt)
        {
            if (m_DropdownList.style.display == DisplayStyle.Flex &&
                !m_DropdownList.worldBound.Contains(evt.position))
                m_DropdownList.style.display = DisplayStyle.None;

            if (m_ContextMenu.style.display == DisplayStyle.Flex &&
                !m_ContextMenu.worldBound.Contains(evt.position))
                m_ContextMenu.style.display = DisplayStyle.None;
        }
        #endregion
        /// </sample>
    }
}
