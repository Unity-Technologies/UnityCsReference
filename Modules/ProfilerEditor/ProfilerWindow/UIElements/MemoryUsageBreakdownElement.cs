// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor
{
    internal class MemoryUsageBreakdownElement : VisualElement
    {
        // adaptation helper. MemoryUsageBreakdownElement is copied over from the memory profiler package which contains this helper
        static class UIElementsHelper
        {
            public static void SetVisibility(VisualElement element, bool visible)
            {
                element.visible = visible;
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        static class Content
        {
            public static readonly string SelectedFormatStringPartOfTooltip = L10n.Tr("\nSelected: {0}\n({1:0.0}% of {2})");
            public static readonly string Reserved = L10n.Tr("Reserved");
            public static readonly string UsedFormatStringPartOfTooltip = L10n.Tr(" Used: {0}\n({1:0.0}% of {2})");
            public static readonly string ReservedFormatStringPartOfTooltip = L10n.Tr("{0}: {1}\n({2:0.0}% of {3})");
            public static readonly string ReservedClarificationForReservedPartOfTooltip = L10n.Tr("\nReserved");
        }

        public string Text { get; private set;}

        ulong m_TotalBytes;
        public long TotalBytes
        {
            get { return (long)m_TotalBytes; }
            private set { m_TotalBytes = (ulong)value; }
        }

        ulong m_UsedBytes;
        public long UsedBytes
        {
            get { return (long)m_UsedBytes; }
            private set { m_UsedBytes = (ulong)value; }
        }
        public bool ShowUsed { get; private set; }

        ulong m_SelectedBytes;
        public long SelectedBytes
        {
            get { return (long)m_SelectedBytes; }
            private set { m_SelectedBytes = (ulong)value; }
        }
        public bool ShowSelected { get; private set; }

        public float PercentageUsed
        {
            get { return m_TotalBytes == 0 ? 100 : m_UsedBytes / (float)m_TotalBytes * 100; }
        }
        public float PercentageSelected
        {
            get { return m_TotalBytes == 0 ? 100 : m_SelectedBytes / (float)m_TotalBytes * 100; }
        }

        public string BackgroundColorClass { get; private set; }

        VisualElement m_ReservedElement;
        VisualElement m_BackgroundElement;
        VisualElement m_UsedElement;
        VisualElement m_SelectedElement;

        MemoryUsageBreakdown m_BreakdownParent;
        MemoryUsageBreakdown.RowElement m_Row;

        public MemoryUsageBreakdownElement(string text, string backgroundColorClass, bool showUsed = false, bool showSelected = false) : this()
        {
            Text = text;
            BackgroundColorClass = backgroundColorClass;
            ShowUsed = showUsed;
            ShowSelected = showSelected;

            if (!string.IsNullOrEmpty(backgroundColorClass))
                AddToClassList(backgroundColorClass);
        }

        public MemoryUsageBreakdownElement() : base()
        {
            AddToClassList("memory-usage-bar__element");
            m_BackgroundElement = new VisualElement();
            m_BackgroundElement.AddToClassList("memory-usage-breakdown__memory-usage-bar__background");
            hierarchy.Add(m_BackgroundElement);

            m_ReservedElement = new VisualElement();
            m_ReservedElement.AddToClassList("memory-usage-breakdown__memory-usage-bar__reserved");
            m_BackgroundElement.Add(m_ReservedElement);

            m_UsedElement = new VisualElement();
            m_UsedElement.AddToClassList("memory-usage-breakdown__memory-usage-bar__used-portion");
            m_ReservedElement.Add(m_UsedElement);

            m_SelectedElement = new VisualElement();
            m_SelectedElement.AddToClassList("memory-usage-breakdown__memory-usage-bar__selected-portion");
            m_ReservedElement.Add(m_SelectedElement);
        }

        void Init(string text, bool showUsed, ulong used, ulong total, bool showSelected, ulong selected, string backgroundColorClass)
        {
            Text = text;
            ShowUsed = showUsed;
            m_UsedBytes = used;
            m_TotalBytes = total;
            ShowSelected = showSelected;
            m_SelectedBytes = selected;
            BackgroundColorClass = backgroundColorClass;
            m_ReservedElement.AddToClassList(backgroundColorClass);

            m_BackgroundElement.style.backgroundColor = Color.black;

            UIElementsHelper.SetVisibility(m_UsedElement, ShowUsed);

            RegisterCallback<GeometryChangedEvent>(OnGeometryChangedEvent);

            m_UsedElement.style.width = new Length(PercentageUsed, LengthUnit.Percent);
            m_UsedElement.AddToClassList(BackgroundColorClass);
            UIElementsHelper.SetVisibility(m_SelectedElement, ShowSelected);
            m_SelectedElement.style.width = new Length(m_SelectedBytes, LengthUnit.Percent);
        }

        void OnGeometryChangedEvent(GeometryChangedEvent e)
        {
            var backgroundColor = m_ReservedElement.resolvedStyle.backgroundColor;
            var outlineColor = backgroundColor;
            if (ShowUsed)
            {
                backgroundColor.a = 0.3f;
            }
            m_ReservedElement.style.backgroundColor = backgroundColor;
            m_ReservedElement.style.borderBottomColor = outlineColor;
            m_ReservedElement.style.borderTopColor = outlineColor;
            m_ReservedElement.style.borderLeftColor = outlineColor;
            m_ReservedElement.style.borderRightColor = outlineColor;
            UnregisterCallback<GeometryChangedEvent>(OnGeometryChangedEvent);
        }

        public void SetValues(ulong totalBytes, ulong usedBytes)
        {
            SetValues(totalBytes, usedBytes, 0, false);
        }

        public void SetValues(ulong totalBytes, ulong usedBytes, ulong selectedBytes)
        {
            SetValues(totalBytes, usedBytes, selectedBytes, true);
        }

        public void SetValues(ulong totalBytes, ulong usedBytes, ulong selectedBytes, bool showSelected)
        {
            ShowSelected = showSelected;
            m_SelectedBytes = selectedBytes;

            m_TotalBytes = totalBytes;
            m_UsedBytes = usedBytes;
            var tooltipText = BuildTooltipText(m_BreakdownParent.HeaderText, Text, (ulong)m_BreakdownParent.TotalBytes, m_TotalBytes, ShowUsed, m_UsedBytes, ShowSelected, m_SelectedBytes);
            tooltip = tooltipText;
            m_Row.RowSize.text = BuildRowSizeText(totalBytes, usedBytes, ShowUsed, showSelected);
            m_Row.Root.tooltip = tooltip;
            SetBarElements();
        }

        public static string BuildTooltipText(string memoryBreakDownName, string elementName, ulong totalBytes, ulong reservedBytes, bool showUsed = false, ulong usedBytes = 0, bool showSelected = false, ulong selectedBytes = 0)
        {
            // Unity/Other Used: 27MB
            // (90% of Reserved)
            // Reserved: 30 MB
            // (90% of Total Memory)
            // Selected: 1MB
            // (XX% of Unity/Other)

            // or

            // Unity/Other: 30 MB
            // (90% of Total Memory)
            // Selected: 1MB
            // (XX% of Unity/Other)
            var selectedText = showSelected ? string.Format(Content.SelectedFormatStringPartOfTooltip, EditorUtility.FormatBytes((long)selectedBytes), selectedBytes / (float)reservedBytes * 100, showUsed ? Content.Reserved : elementName) : "";
            var usedText = showUsed ? string.Format(Content.UsedFormatStringPartOfTooltip, EditorUtility.FormatBytes((long)usedBytes), usedBytes / (float)reservedBytes * 100, showUsed ? Content.Reserved : elementName) : "";
            var reservedText = string.Format(Content.ReservedFormatStringPartOfTooltip, showUsed ? Content.ReservedClarificationForReservedPartOfTooltip : "", EditorUtility.FormatBytes((long)reservedBytes), reservedBytes / (float)totalBytes * 100, memoryBreakDownName);
            return string.Format("{0}{1}{2}{3}",
                elementName,
                usedText,
                reservedText,
                selectedText);
        }

        public static string BuildRowSizeText(ulong totalBytes, ulong usedBytes, bool showUsed, bool showSelected = false, ulong selectedBytes = 0)
        {
            if (showUsed)
            {
                var total = EditorUtility.FormatBytes((long)totalBytes);
                var used = EditorUtility.FormatBytes((long)usedBytes);

                // check if the last two characters are the same (i.e. " B" "KB" ...) so we can drop unnecesary unit qualifiers
                if (total[total.Length - 1] == used[used.Length - 1]
                    && total[total.Length - 2] == used[used.Length - 2])
                {
                    used = used.Substring(0, used.Length - (used[used.Length - 2] == ' ' ? 2 : 3));
                }
                return string.Format("{0} / {1}", used, total);
            }
            else
            {
                return EditorUtility.FormatBytes((long)totalBytes);
            }
        }

        void SetBarElements()
        {
            UIElementsHelper.SetVisibility(m_UsedElement, ShowUsed);
            m_UsedElement.style.width = new Length(PercentageUsed, LengthUnit.Percent);
            UIElementsHelper.SetVisibility(m_SelectedElement, ShowSelected);
            m_SelectedElement.style.width = new Length(PercentageSelected, LengthUnit.Percent);
        }

        public void SetupRow(MemoryUsageBreakdown breakdownParent, MemoryUsageBreakdown.RowElement row)
        {
            m_Row = row;
            m_BreakdownParent = breakdownParent;
            m_Row.Root.tooltip = tooltip;
            // copy the color
            if (!string.IsNullOrEmpty(BackgroundColorClass))
                m_Row.ColorBox.AddToClassList(BackgroundColorClass);
            m_Row.RowName.text = Text;
            m_Row.ShowUsed = ShowUsed;

            SetValues(m_TotalBytes, m_UsedBytes);
        }

        /// <summary>
        /// Instantiates a <see cref="MemoryUsageBreakdownElement"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<MemoryUsageBreakdownElement, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="MemoryUsageBreakdownElement"/>.
        /// </summary>
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text", defaultValue = "Other" };
            UxmlStringAttributeDescription m_ColorClass = new UxmlStringAttributeDescription { name = "background-color-class", defaultValue = "" };
            UxmlBoolAttributeDescription m_ShowUsed = new UxmlBoolAttributeDescription { name = "show-used", defaultValue = false };
            UxmlLongAttributeDescription m_Used = new UxmlLongAttributeDescription { name = "used-bytes", defaultValue = 50 };
            UxmlLongAttributeDescription m_Total = new UxmlLongAttributeDescription { name = "total-bytes", defaultValue = 100 };
            UxmlBoolAttributeDescription m_ShowSelected = new UxmlBoolAttributeDescription { name = "show-selected", defaultValue = false };
            UxmlLongAttributeDescription m_Selected = new UxmlLongAttributeDescription { name = "selected-bytes", defaultValue = 0};

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var text = m_Text.GetValueFromBag(bag, cc);
                var showUsed = m_ShowUsed.GetValueFromBag(bag, cc);
                var total = m_Total.GetValueFromBag(bag, cc);
                var showSelected = m_ShowSelected.GetValueFromBag(bag, cc);
                var used = Mathf.Clamp(m_Used.GetValueFromBag(bag, cc), 0, total);
                var selected = Mathf.Clamp(m_Selected.GetValueFromBag(bag, cc), 0, total);
                var color = m_ColorClass.GetValueFromBag(bag, cc);

                ((MemoryUsageBreakdownElement)ve).Init(text, showUsed, (ulong)used, (ulong)total, showSelected, (ulong)selected, color);
            }
        }
    }
}
