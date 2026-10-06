// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Multiplayer.Center.Common;
using Unity.Properties;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Center.Editor
{
    /// <summary>
    /// This panel creates new VisualElements for each existing <see cref="OnboardingSectionCategory"/>
    /// and fill them based on registered <see cref="OnboardingGUIProvider{T}"/>
    /// </summary>
    [UxmlElement]
    partial class CategoriesContainer : ScrollView
    {
        Dictionary<OnboardingSectionCategory, VisualElement> m_CategoryToElement = new();
        OnboardingSectionCategory m_DisplayedCategory;

        /// <summary>
        /// Currently displayed category panel.
        /// </summary>
        /// <remarks>
        /// This property is bound to <see cref="MultiplayerCenterWindow.SelectedCategoryType"/> in the UXML.
        /// </remarks>
        [CreateProperty]
        public OnboardingSectionCategory DisplayedCategory
        {
            get => m_DisplayedCategory;
            set
            {
                if (m_DisplayedCategory != value)
                {
                    m_CategoryToElement[m_DisplayedCategory].style.display = DisplayStyle.None;
                    m_DisplayedCategory = value;
                    m_CategoryToElement[m_DisplayedCategory].style.display = DisplayStyle.Flex;
                }
            }
        }

        public CategoriesContainer()
        {
            // fetch all onboarding sections and create a child object for each of them
            m_DisplayedCategory = OnboardingSectionCategory.Intro;
            foreach (OnboardingSectionCategory value in Enum.GetValues(typeof(OnboardingSectionCategory)))
            {
                var element = new VisualElement() { name = $"OnboardingSection--{value}" };
                element.style.display =
                    value == OnboardingSectionCategory.Intro ? DisplayStyle.Flex : DisplayStyle.None;
                m_CategoryToElement.Add(value, element);
                base.contentContainer.Add(element);
            }

            // fetch all onboarding GUI providers, instantiate them and add them to the correct category in priority order.
            var newSections = TypeCache.GetTypesDerivedFrom(typeof(OnboardingGUIProvider<>));
            var orderedSections = new Dictionary<OnboardingSectionCategory, SortedSet<IOnboardingGUIProvider>>();
            foreach (var sectionType in newSections)
            {
                if (sectionType.IsAbstract || sectionType.ContainsGenericParameters)
                    continue;

                var section = (IOnboardingGUIProvider)Activator.CreateInstance(sectionType);
                // does all category exists?
                foreach (var sectionCategory in section.Categories)
                {
                    if (!orderedSections.ContainsKey(sectionCategory.Item1))
                    {
                        orderedSections.Add(sectionCategory.Item1,
                            new SortedSet<IOnboardingGUIProvider>(new PriorityComparer(sectionCategory.Item1)));
                    }

                    orderedSections[sectionCategory.Item1].Add(section);
                }
            }

            foreach (var orderedSection in orderedSections)
            {
                foreach (var section in orderedSection.Value)
                {
                    var sectionContainer = section.CreateGUI();
                    sectionContainer.name = $"{orderedSection.Key}--{section.GetType().Name}";
                    sectionContainer.AddToClassList(StyleClasses.CategorySection);
                    m_CategoryToElement[orderedSection.Key].Add(sectionContainer);
                }
            }

#pragma warning disable CS0618 // Type or member is obsolete
            // Handling the obsolete sections for backward compatibility
            var sections = SectionsFinder.FindSectionTypes();
            foreach (var element in m_CategoryToElement)
            {
                if (sections.TryGetValue(element.Key, out var types))
                {
                    foreach (var sectionType in types)
                    {
                        var instance = Activator.CreateInstance(sectionType) as IOnboardingSection;
                        if (instance != null)
                        {
                            element.Value.Add(new DeprecatedOnboardingSections(instance));
                        }
                    }
                }
            }
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }

    /// <summary>
    /// Used to sort <see cref="IOnboardingGUIProvider"/> by priority
    /// when adding them to the <see cref="CategoriesContainer"/>.
    /// </summary>
    class PriorityComparer : IComparer<IOnboardingGUIProvider>
    {
        OnboardingSectionCategory m_Section;

        public PriorityComparer(OnboardingSectionCategory section)
        {
            m_Section = section;
        }

        public int Compare(IOnboardingGUIProvider x, IOnboardingGUIProvider y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (y is null) return 1;
            if (x is null) return -1;

            var priorityCompare = x[m_Section].Priority.CompareTo(y[m_Section].Priority);
            if (priorityCompare != 0)
                return priorityCompare;
            return String.Compare(x.GetType().Name, y.GetType().Name, StringComparison.Ordinal);
        }
    }
}
