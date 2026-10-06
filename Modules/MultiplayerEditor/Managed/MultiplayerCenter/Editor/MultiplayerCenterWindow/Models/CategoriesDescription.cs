// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Multiplayer.Center.Common;
using Unity.Properties;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Center.Editor
{
    /// <summary>
    /// Description of each <see cref="OnboardingSectionCategory"/>.
    /// </summary>
    /// <remarks>
    /// This is used to change the Multiplayer Center UI by ordering, changing the name,
    /// and changing the visibility of the categories without touching the public enum itself.
    /// </remarks>
    class CategoriesDescription : EnumBasedDescription<OnboardingSectionCategory, Category>, IDataSourceViewHashProvider
    {
        protected override Category CreateNewDescription(OnboardingSectionCategory type)
        {
            return new Category
            {
                CategoryType = type,
                Name = type.ToString(),
                Hidden = false,
            };
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            FilteredCategories = Descriptions;
        }

        void OnEnable()
        {
            m_HasChanged = 0;
            FilteredCategories = Descriptions;
        }

        int m_HasChanged;
        List<Category> m_FilteredCategories = new();
        [CreateProperty]
        public List<Category> FilteredCategories
        {
            get => m_FilteredCategories;
            private set
            {
                bool changed = false;
                int filterIndex = 0;
                for (var i = 0; i < value.Count; i++)
                {
                    var category = value[i];
                    if (!category.Hidden)
                    {
                        if (m_FilteredCategories.Count <= filterIndex)
                        {
                            changed = true;
                            m_FilteredCategories.Add(category);
                        }
                        else if(category.CategoryType != m_FilteredCategories[filterIndex].CategoryType
                                || category.DisplayName != m_FilteredCategories[filterIndex].DisplayName)
                        {
                            changed = true;
                            m_FilteredCategories[filterIndex] = category;
                        }

                        filterIndex++;
                    }
                }

                if (filterIndex < m_FilteredCategories.Count)
                {
                    m_FilteredCategories.RemoveRange(filterIndex, m_FilteredCategories.Count - filterIndex);
                    changed = true;
                }

                if (changed)
                {
                    m_HasChanged++;
                }
            }
        }

        public long GetViewHashCode()
        {
            return m_HasChanged;
        }
    }

    [Serializable]
    struct Category : IEnumDescription<OnboardingSectionCategory>, IListViewDisplayName
    {
        [ReadOnlyInspector]
        public OnboardingSectionCategory CategoryType;
        public string Name;
        public bool Hidden;

        public OnboardingSectionCategory Type => CategoryType;
        [CreateProperty]
        public string DisplayName => Name;
    }
}
