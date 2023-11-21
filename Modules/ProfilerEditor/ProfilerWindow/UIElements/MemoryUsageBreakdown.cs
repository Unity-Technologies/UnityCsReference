// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor
{
    internal class MemoryUsageBreakdown : VisualElement
    {
        // adaptation helper. MemoryUsageBreakdown is copied over from the memory profiler package which contains this helper
        static class UIElementsHelper
        {
            public static void SetVisibility(VisualElement element, bool visible)
            {
                element.visible = visible;
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        static class ResourcePaths
        {
            public const string MemoryUsageBreakdownUxmlPath = "Profiler/Modules/Memory/MemoryUsageBreakdown.uxml";
            public const string MemoryUsageBreakdownUssPath = "Profiler/Modules/Memory/MemoryUsageBreakdown.uss";
            public const string MemoryUsageBreakdownLegendRowUxmlPath = "Profiler/Modules/Memory/MemoryUsageBreakdownLegendRow.uxml";
        }

        static class Content
        {
            public static readonly string DefaultUnkownRowName = L10n.Tr("Unknown");
            public static readonly string UnknownkSizeLabel = L10n.Tr("Unknown");
            public static readonly string TotalFormatString = L10n.Tr("Total: {0}");
            public static readonly string TotalLabelTooltip = L10n.Tr("The Total memory usage.");
            public static readonly string TotalAndMaxFormatString = L10n.Tr("Total: {0} | Max: {1}");
            public static readonly string TotalAndMaxLabelTooltip = L10n.Tr("The bar is scaled in relation to the frame with the highest total memory usage (Max).");
            public static readonly string TotalAndMaxLabelTooltipForMaxValue = L10n.Tr("This is one of the frames with the highest total memory usage (Max).");
        }

        public string HeaderText { get; private set; }

        ulong m_TotalBytes;
        public long TotalBytes
        {
            get { return (long)m_TotalBytes; }
            private set { SetTotalUsed((ulong)value, m_Normalized, m_MaxTotalBytesToNormalizeTo); }
        }

        bool m_Normalized;
        ulong m_MaxTotalBytesToNormalizeTo;

        public bool ShowUnknown { get; private set; }

        public string UnknownName { get; private set; } = Content.DefaultUnkownRowName;

        VisualTreeAsset m_MemoryUsageBreakdownLegednRowViewTree;

        List<MemoryUsageBreakdownElement> m_Elements = new List<MemoryUsageBreakdownElement>();

        VisualElement m_Root;
        VisualElement m_Content;
        VisualElement m_MemoryUsageBar;
        VisualElement m_MemoryUsageTable;
        Label m_HeaderNamne;
        Label m_HeaderSize;

        VisualElement m_UnknownBar;
        RowElement m_UnknownRow;

        public struct RowElement
        {
            public bool ShowUsed
            {
                set
                {
                    if (RowResevedUsed != null)
                        UIElementsHelper.SetVisibility(RowResevedUsed, value);
                    if (ColorBoxUnused != null)
                        UIElementsHelper.SetVisibility(ColorBoxUnused, value);
                }
            }
            public VisualElement Root;
            public VisualElement ColorBox;
            public VisualElement ColorBoxUnused;
            public Label RowName;
            public Label RowResevedUsed;
            public Label RowSize;

            public RowElement(VisualElement row)
            {
                Root = row.Q("memory-usage-breakdown__legend__row");
                ColorBox = row.Q("memory-usage-breakdown__legend__color-box");
                ColorBoxUnused = row.Q("memory-usage-breakdown__legend__color-box__unused");
                RowName = row.Q<Label>("memory-usage-breakdown__legend__name");
                RowResevedUsed = row.Q<Label>("memory-usage-breakdown__legend__used-reserved");
                RowSize = row.Q<Label>("memory-usage-breakdown__legend__size-column");
            }

            public void SetupAsLastRow()
            {
                if (Root != null)
                    Root.AddToClassList("legend__last-row");
            }
        }

        public override VisualElement contentContainer
        {
            get { return m_Content; }
        }

        public MemoryUsageBreakdown(string headerText, bool showUnknown = false) : this()
        {
            ShowUnknown = showUnknown;
            HeaderText = headerText;
            Init(headerText, m_TotalBytes, showUnknown, UnknownName);
        }

        public MemoryUsageBreakdown() : base()
        {
            VisualTreeAsset memoryUsageBreakdownViewTree;
            memoryUsageBreakdownViewTree = EditorGUIUtility.Load(ResourcePaths.MemoryUsageBreakdownUxmlPath) as VisualTreeAsset;

            m_MemoryUsageBreakdownLegednRowViewTree = EditorGUIUtility.Load(ResourcePaths.MemoryUsageBreakdownLegendRowUxmlPath) as VisualTreeAsset;

            var styleSheet = EditorGUIUtility.Load(ResourcePaths.MemoryUsageBreakdownUssPath) as StyleSheet;

            m_Root = memoryUsageBreakdownViewTree.CloneTree();

            m_Root.styleSheets.Add(styleSheet);
            //m_Root.AddStyleSheetPath(ResourcePath.MemoryUsageBreakdownUssPath);

            style.flexShrink = 0;
            m_Root.style.flexGrow = 1;

            hierarchy.Add(m_Root);
            m_Root.parent.style.flexDirection = FlexDirection.Row;

            m_HeaderNamne = m_Root.Q<Label>("memory-usage-breakdown__header__title");
            m_HeaderSize = m_Root.Q<Label>("memory-usage-breakdown__header__total-value");

            m_MemoryUsageBar = m_Root.Q("memory-usage-breakdown__memory-usage-bar");
            m_MemoryUsageTable = m_Root.Q("memory-usage-breakdown__legend");
            m_Content = m_MemoryUsageBar.Q("memory-usage-breakdown__memory-usage-bar__known-parts");

            m_UnknownBar = m_MemoryUsageBar.Q("memory-usage-breakdown__memory-usage-bar__unknown");
            Setup();
        }

        public void SetVaules(ulong totalBytes, ulong[] reserved, ulong[] used)
        {
            SetVaules(totalBytes, reserved, used, true, m_MaxTotalBytesToNormalizeTo);
        }

        public void SetVaules(ulong totalBytes, ulong[] reserved, ulong[] used, bool normalized, ulong maxTotalBytesToNormalizeTo, bool totalIsKnown = true)
        {
            m_TotalBytes = totalBytes;
            for (int i = 0; i < m_Elements.Count && i < reserved.Length; i++)
            {
                if (used == null || i >= used.Length)
                    m_Elements[i].SetValues(reserved[i], 0);
                else
                    m_Elements[i].SetValues(reserved[i], used[i]);
            }
            SetTotalUsed(totalBytes, normalized, maxTotalBytesToNormalizeTo, force: true, totalIsKnown: totalIsKnown);
        }

        void SetTotalUsed(ulong totalBytes, bool normalized, ulong maxTotalBytesToNormalizeTo, bool force = false, bool totalIsKnown = true)
        {
            if (!force && m_TotalBytes == totalBytes && normalized == m_Normalized && maxTotalBytesToNormalizeTo == m_MaxTotalBytesToNormalizeTo)
                return;

            m_TotalBytes = totalBytes;
            m_Normalized = normalized;
            m_MaxTotalBytesToNormalizeTo = Math.Max(m_TotalBytes, maxTotalBytesToNormalizeTo);

            UpdateTotalSizeText();

            var knownBytes = 0ul;
            for (int i = 0; i < m_Elements.Count; i++)
            {
                knownBytes += (ulong)m_Elements[i].TotalBytes;
            }

            var unknownUnknown = !totalIsKnown;
            var unknownSize = m_TotalBytes;
            for (int i = 0; i < m_Elements.Count; i++)
            {
                var percentage = (m_Elements[i].TotalBytes / (float)knownBytes) * 100;
                m_Elements[i].style.width = new Length(percentage, LengthUnit.Percent);
                var elementSize = (ulong)m_Elements[i].TotalBytes;
                if (elementSize > unknownSize)
                {
                    unknownSize = 0ul;
                    unknownUnknown = true;
                }
                else
                    unknownSize -= Math.Min(elementSize, unknownSize);
            }

            var totalBarByteAmount = knownBytes;
            if (!ShowUnknown && m_Normalized)
                totalBarByteAmount = m_TotalBytes;

            if (!m_Normalized && m_MaxTotalBytesToNormalizeTo > totalBarByteAmount)
                totalBarByteAmount = m_MaxTotalBytesToNormalizeTo;

            if (ShowUnknown || !m_Normalized)
            {
                var percentage = totalBarByteAmount > 0 ? knownBytes / (float)totalBarByteAmount * 100 : 100;
                m_Content.style.width = new Length(Mathf.RoundToInt(percentage), LengthUnit.Percent);
            }
            else
            {
                m_Content.style.width = new Length(100, LengthUnit.Percent);
            }
            if (m_UnknownBar.visible != ShowUnknown)
                UIElementsHelper.SetVisibility(m_UnknownBar, ShowUnknown);
            if (ShowUnknown)
            {
                var percentage = totalBarByteAmount > 0 ? unknownSize / (float)totalBarByteAmount * 100 : 100;
                m_UnknownBar.style.width = new Length(Mathf.RoundToInt(percentage), LengthUnit.Percent);

                m_UnknownBar.tooltip = MemoryUsageBreakdownElement.BuildTooltipText(HeaderText, UnknownName, (ulong)TotalBytes, unknownSize);
                m_UnknownRow.Root.tooltip = m_UnknownBar.tooltip;
                if (unknownUnknown)
                    m_UnknownRow.RowSize.text = Content.UnknownkSizeLabel;
                else
                    m_UnknownRow.RowSize.text = MemoryUsageBreakdownElement.BuildRowSizeText(unknownSize, unknownSize, false);
            }

            if (m_UnknownRow.Root != null && m_UnknownRow.Root.visible != ShowUnknown)
                UIElementsHelper.SetVisibility(m_UnknownRow.Root, ShowUnknown);
        }

        void UpdateTotalSizeText()
        {
            if (!m_Normalized && m_TotalBytes < m_MaxTotalBytesToNormalizeTo)
            {
                m_HeaderSize.text = string.Format(Content.TotalAndMaxFormatString, EditorUtility.FormatBytes((long)m_TotalBytes), EditorUtility.FormatBytes((long)m_MaxTotalBytesToNormalizeTo));
                m_HeaderSize.tooltip = Content.TotalAndMaxLabelTooltip;
            }
            else
            {
                m_HeaderSize.text = string.Format(Content.TotalFormatString, EditorUtility.FormatBytes((long)m_TotalBytes));
                m_HeaderSize.tooltip = m_Normalized ? Content.TotalLabelTooltip : Content.TotalAndMaxLabelTooltipForMaxValue;
            }
        }

        void Init(string headerText, ulong totalMemory, bool showUnknown, string unknownName)
        {
            ShowUnknown = showUnknown;
            m_TotalBytes = totalMemory;
            UnknownName = unknownName;
            if (m_HeaderNamne != null)
            {
                m_HeaderNamne.text = HeaderText = headerText;
                UpdateTotalSizeText();
            }
            Setup();
        }

        public void Setup()
        {
            GatherElements();

            if (m_Elements.Count > 0)
            {
                UnregisterCallback<GeometryChangedEvent>(OnPostDisplaySetup);
                SetupElements();
            }
            else
                RegisterCallback<GeometryChangedEvent>(OnPostDisplaySetup);
        }

        void GatherElements()
        {
            m_Elements.Clear();
            if (m_Content == null)
                return;
            for (int i = 0; i < m_Content.childCount; i++)
            {
                if (m_Content[i] is MemoryUsageBreakdownElement)
                {
                    m_Elements.Add(m_Content[i] as MemoryUsageBreakdownElement);
                }
            }
        }

        void OnPostDisplaySetup(GeometryChangedEvent evt)
        {
            GatherElements();
            if (m_Elements.Count > 0)
            {
                UnregisterCallback<GeometryChangedEvent>(OnPostDisplaySetup);
                SetupElements();
            }
        }

        void SetupElements()
        {
            m_MemoryUsageTable.Clear();
            RowElement latestRow = default(RowElement);
            for (int i = 0; i < m_Elements.Count; i++)
            {
                var row = m_MemoryUsageBreakdownLegednRowViewTree.CloneTree();
                m_MemoryUsageTable.Add(row);

                latestRow = new RowElement(row);
                m_Elements[i].SetupRow(this, latestRow);
            }
            if (ShowUnknown)
            {
                var unkownRow = m_MemoryUsageBreakdownLegednRowViewTree.CloneTree();
                m_MemoryUsageTable.Add(unkownRow);
                m_UnknownRow = new RowElement(unkownRow);
                m_UnknownRow.ShowUsed = false;
                m_UnknownRow.RowName.text = UnknownName;
                m_UnknownRow.ColorBox.AddToClassList("background-color__memory-summary-category__unknown");
                m_UnknownRow.RowSize.text = "?? B";
                latestRow = m_UnknownRow;
            }
            latestRow.SetupAsLastRow();

            SetTotalUsed(m_TotalBytes, m_Normalized, m_MaxTotalBytesToNormalizeTo, force: true);
        }

        /// <summary>
        /// Instantiates a <see cref="MemoryUsageBreakdown"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlFactory : UxmlFactory<MemoryUsageBreakdown, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="MemoryUsageBreakdown"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_HeaderText = new UxmlStringAttributeDescription { name = "header-text", defaultValue = "Memory Usage" };
            UxmlIntAttributeDescription m_TotalMemory = new UxmlIntAttributeDescription { name = "total-bytes", defaultValue = (int)(1024 * 1024 * 1024 * 1.2f) };
            UxmlBoolAttributeDescription m_ShowUnkown = new UxmlBoolAttributeDescription { name = "show-unknown", defaultValue = false };
            UxmlStringAttributeDescription m_UnknownName = new UxmlStringAttributeDescription { name = "unknown-name", defaultValue = "Unknown" };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get
                {
                    // Can only contain MemoryUsageBreakdownElements
                    yield return new UxmlChildElementDescription(typeof(MemoryUsageBreakdownElement));
                }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var totalMemory = m_TotalMemory.GetValueFromBag(bag, cc);
                var headerText = m_HeaderText.GetValueFromBag(bag, cc);
                var showUnknown = m_ShowUnkown.GetValueFromBag(bag, cc);
                var unknownName = m_UnknownName.GetValueFromBag(bag, cc);


                ((MemoryUsageBreakdown)ve).Init(headerText, (ulong)totalMemory, showUnknown, unknownName);
            }
        }
    }
}
