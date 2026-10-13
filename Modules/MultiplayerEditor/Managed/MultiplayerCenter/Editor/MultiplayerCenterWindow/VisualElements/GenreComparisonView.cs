// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Center.Editor
{
    /// <summary>
    /// Shows the game genres side by side, one card each, with the genre's illustration,
    /// description, Architecture, Complexity and Cost, and a Select button.
    /// </summary>
    /// <remarks>
    /// Call <see cref="Build"/> with the genre list, then listen to <see cref="genreSelected"/>.
    /// Each card's Show details foldout shows that card's attribute descriptions. Matching sections
    /// are kept level across the cards showing them, and the cards scroll together.
    /// </remarks>
    [UxmlElement]
    partial class GenreComparisonView : VisualElement
    {
        const string k_ComparisonTemplatePath = "Multiplayer/MultiplayerCenter/UI/GenreComparison.uxml";
        const string k_CardTemplatePath = "Multiplayer/MultiplayerCenter/UI/GenreCard.uxml";

        const string k_DetailsShownClass = "genre-card--details-shown";

        const string k_ArchitectureSummaryPath = nameof(GameGenreDescription.ArchitectureSummary);
        const string k_ComplexityPath = nameof(GameGenreDescription.Complexity);
        const string k_ComplexitySummaryPath = nameof(GameGenreDescription.ComplexitySummary);
        const string k_CostPath = nameof(GameGenreDescription.Cost);
        const string k_CostSummaryPath = nameof(GameGenreDescription.CostSummary);

        static readonly string k_ArchitectureLabel = L10n.Tr("Architecture", null);
        static readonly string k_ComplexityLabel = L10n.Tr("Complexity", null);
        static readonly string k_CostLabel = L10n.Tr("Cost", null);
        static readonly string k_ShowDetailsLabel = L10n.Tr("Show details", null);
        static readonly string k_HideDetailsLabel = L10n.Tr("Hide details", null);
        static readonly string k_SelectLabel = L10n.Tr("Select", null);

        /// <summary>
        /// Raised with the genre's index in the list passed to <see cref="Build"/> when its Select
        /// button is clicked.
        /// </summary>
        public event Action<int> genreSelected;

        readonly List<ScrollView> m_CardScrollViews = new();
        readonly List<Label[]> m_LevelledSections = new();

        VisualTreeAsset m_ComparisonTemplate;
        VisualTreeAsset m_CardTemplate;
        bool m_IsSyncingScroll;

        /// <summary>
        /// Replaces the view's content with one card per entry in <paramref name="genres"/>.
        /// </summary>
        /// <param name="genres">The genres to show, in order. Nothing is shown if this is null.</param>
        public void Build(GameGenreList genres)
        {
            Clear();
            m_CardScrollViews.Clear();
            m_LevelledSections.Clear();

            if (genres?.Descriptions == null)
                return;

            ComparisonTemplate.CloneTree(this);

            var cards = this.Q<VisualElement>("genre-cards");

            for (var genreIndex = 0; genreIndex < genres.Descriptions.Count; ++genreIndex)
            {
                cards.Add(CreateCard(genres.Descriptions[genreIndex], genreIndex));
            }
        }

        VisualElement CreateCard(GameGenreDescription description, int genreIndex)
        {
            var cell = new VisualElement { dataSource = description };
            cell.AddToClassList(StyleClasses.ComparisonCell);
            CardTemplate.CloneTree(cell);

            var architectureDescription = CreateDescription(description.Architecture);
            var complexityDescription = CreateDescription(description.ComplexityDescription);
            var costDescription = CreateDescription(description.CostDescription);

            var specs = cell.Q<VisualElement>("genre-card-specs");
            specs.Add(CreateArchitectureSpec(description));
            specs.Add(architectureDescription);
            specs.Add(CreateMeterSpec(k_ComplexityLabel, k_ComplexitySummaryPath, k_ComplexityPath));
            specs.Add(complexityDescription);
            specs.Add(CreateMeterSpec(k_CostLabel, k_CostSummaryPath, k_CostPath));
            specs.Add(costDescription);

            var sections = new[]
            {
                cell.Q<Label>("genre-card-description"),
                architectureDescription,
                complexityDescription,
                costDescription,
            };
            m_LevelledSections.Add(sections);
            foreach (var section in sections)
            {
                section.RegisterCallback<GeometryChangedEvent>(_ => LevelSections());
            }

            var card = cell.Q<VisualElement>("genre-card");
            var details = cell.Q<Foldout>("genre-card-details");
            details.text = k_ShowDetailsLabel;
            details.RegisterValueChangedCallback(changed =>
            {
                if (changed.target != details)
                    return;

                card.EnableInClassList(k_DetailsShownClass, changed.newValue);
                details.text = changed.newValue ? k_HideDetailsLabel : k_ShowDetailsLabel;
            });

            var scrollView = cell.Q<ScrollView>("genre-card-scroll");
            scrollView.verticalScroller.valueChanged += offset => SyncScroll(scrollView, offset);
            m_CardScrollViews.Add(scrollView);
            foreach (var child in scrollView.contentContainer.Children())
            {
                child.RegisterCallback<GeometryChangedEvent>(_ => LevelScrollContent());
            }

            var select = cell.Q<Button>("genre-card-select");
            select.text = k_SelectLabel;
            select.clicked += () => genreSelected?.Invoke(genreIndex);

            return cell;
        }

        void LevelSections()
        {
            if (m_LevelledSections.Count == 0)
                return;

            var sectionCount = m_LevelledSections[0].Length;

            for (var section = 0; section < sectionCount; ++section)
            {
                var tallest = 0f;

                foreach (var card in m_LevelledSections)
                {
                    var label = card[section];
                    var width = label.contentRect.width;

                    if (label.resolvedStyle.display == DisplayStyle.None || width <= 0f)
                        continue;

                    var measured = label.MeasureTextSize(label.text, width, MeasureMode.Exactly, 0f, MeasureMode.Undefined);
                    var box = label.resolvedStyle;
                    var height = measured.y + box.paddingTop + box.paddingBottom + box.borderTopWidth + box.borderBottomWidth;
                    tallest = Mathf.Max(tallest, height);
                }

                foreach (var card in m_LevelledSections)
                {
                    card[section].style.minHeight = tallest;
                }
            }
        }

        // every card gets the same scrollable height, otherwise each ScrollView clamps the synced
        // offset to its own range and the cards drift apart
        void LevelScrollContent()
        {
            var tallest = 0f;

            foreach (var scrollView in m_CardScrollViews)
            {
                tallest = Mathf.Max(tallest, NaturalContentHeight(scrollView.contentContainer));
            }

            foreach (var scrollView in m_CardScrollViews)
            {
                var content = scrollView.contentContainer;
                if (!Mathf.Approximately(content.style.minHeight.value.value, tallest))
                    content.style.minHeight = tallest;
            }
        }

        static float NaturalContentHeight(VisualElement content)
        {
            var bottom = 0f;

            foreach (var child in content.Children())
            {
                if (child.resolvedStyle.display != DisplayStyle.None)
                    bottom = Mathf.Max(bottom, child.layout.yMax + child.resolvedStyle.marginBottom);
            }

            return bottom + content.resolvedStyle.paddingBottom + content.resolvedStyle.borderBottomWidth;
        }

        void SyncScroll(ScrollView source, float offset)
        {
            if (m_IsSyncingScroll)
                return;

            m_IsSyncingScroll = true;

            foreach (var scrollView in m_CardScrollViews)
            {
                if (scrollView != source)
                    scrollView.scrollOffset = new Vector2(scrollView.scrollOffset.x, offset);
            }

            m_IsSyncingScroll = false;
        }

        static Label CreateDescription(string prose)
        {
            var description = new Label(prose) { pickingMode = PickingMode.Ignore };
            description.AddToClassList(StyleClasses.SpecRowDescription);
            return description;
        }

        static VisualElement CreateArchitectureSpec(GameGenreDescription description)
        {
            var row = CreateSpecRow(k_ArchitectureLabel);

            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList(StyleClasses.ArchitectureCellIcon);
            if (description.ArchitectureIcon != null)
                icon.style.backgroundImage = Background.FromTexture2D(description.ArchitectureIcon);
            row.Add(icon);

            var summary = new Label { pickingMode = PickingMode.Ignore };
            summary.AddToClassList(StyleClasses.ArchitectureCellLabel);
            BindText(summary, k_ArchitectureSummaryPath);
            row.Add(summary);

            return row;
        }

        static VisualElement CreateMeterSpec(string label, string summaryPath, string valuePath)
        {
            var row = CreateSpecRow(label);

            var summary = new Label { pickingMode = PickingMode.Ignore };
            summary.AddToClassList(StyleClasses.SpecRowSummary);
            BindText(summary, summaryPath);
            row.Add(summary);

            var meter = new SegmentedRangeMeter();
            meter.AddToClassList(StyleClasses.SegmentedRangeMeter);
            meter.SetBinding(nameof(meter.Range), ToTarget(valuePath));
            row.Add(meter);

            return row;
        }

        static VisualElement CreateSpecRow(string label)
        {
            var row = new VisualElement();
            row.AddToClassList(StyleClasses.SpecRow);

            var name = new Label(label) { pickingMode = PickingMode.Ignore };
            name.AddToClassList(StyleClasses.SpecRowLabel);
            row.Add(name);

            return row;
        }

        static void BindText(Label label, string dataSourcePath)
        {
            label.SetBinding(nameof(label.text), ToTarget(dataSourcePath));
        }

        static DataBinding ToTarget(string dataSourcePath)
        {
            return new DataBinding
            {
                dataSourcePath = PropertyPath.FromName(dataSourcePath),
                bindingMode = BindingMode.ToTarget,
            };
        }

        VisualTreeAsset ComparisonTemplate =>
            m_ComparisonTemplate ??= EditorGUIUtility.LoadRequired(k_ComparisonTemplatePath) as VisualTreeAsset;

        VisualTreeAsset CardTemplate =>
            m_CardTemplate ??= EditorGUIUtility.LoadRequired(k_CardTemplatePath) as VisualTreeAsset;
    }
}
