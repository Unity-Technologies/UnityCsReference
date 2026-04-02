// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;

namespace UnityEditor.Search
{
    [Serializable]
    [VisibleToOtherModules]
    struct SearchResultViewDescriptor
    {
        [SerializeField] string m_Id;
        public string Id { get => m_Id; set => m_Id = value; }

        [SerializeField] string m_Description;
        public string Description { get => m_Description; set => m_Description = value; }

        [SerializeField] string m_ButtonClassName;
        public string ButtonClassName { get => m_ButtonClassName; set => m_ButtonClassName = value; }

        [SerializeField] float m_SizeMin;
        public float SizeMin { get => m_SizeMin; set => m_SizeMin = value; }

        [SerializeField] float m_SizeMax;
        public float SizeMax { get => m_SizeMax; set => m_SizeMax = value; }

        [SerializeField] float m_SizeDefault;
        public float SizeDefault { get => m_SizeDefault; set => m_SizeDefault = value; }

        public Func<ISearchView, IResultView> ViewCreator => m_ViewCreator.handler;
        [SerializeField] private SearchFunctor<Func<ISearchView, IResultView>> m_ViewCreator;

        public Func<Texture2D> FetchIcon => m_FetchIcon.handler;
        [SerializeField] private SearchFunctor<Func<Texture2D>> m_FetchIcon;

        public SearchResultViewDescriptor(string id, Func<ISearchView, IResultView> creator, Func<Texture2D> fetchIcon, float sizeDefault, string description = null, string buttonClassName = null)
            : this(id, creator, fetchIcon, sizeDefault, sizeDefault, sizeDefault, description, buttonClassName)
        {
        }

        public SearchResultViewDescriptor(string id, Func<ISearchView, IResultView> creator, Func<Texture2D> fetchIcon, float sizeMin, float sizeMax, float sizeDefault, string description = null, string buttonClassName = null)
        {
            m_Id = id;
            m_ViewCreator = new SearchFunctor<Func<ISearchView, IResultView>>(creator);
            m_FetchIcon = new SearchFunctor<Func<Texture2D>>(fetchIcon);
            m_SizeMin = sizeMin;
            m_SizeMax = sizeMax;
            m_SizeDefault = sizeDefault;
            m_Description = description;
            m_ButtonClassName = buttonClassName;
        }

        public bool SupportsSizeSlider => SizeMax != SizeMin;
        public bool IsValid => !string.IsNullOrEmpty(m_Id) &&
            ViewCreator != null &&
            FetchIcon != null &&
            SizeMin > 0 &&
            SizeDefault > 0 &&
            SizeMax >= SizeMin;

        public override string ToString()
        {
            return $"{Id} - {SizeDefault} - ({SizeMin},{SizeMax})";
        }
    }

    [Serializable]
    [VisibleToOtherModules]
    class SearchResultViewDescriptorList : ISerializationCallbackReceiver
    {
        [SerializeField] List<SearchResultViewDescriptor> m_CurrentDescriptors;
        [SerializeField] int m_CurrentDescriptorIndex;

        public SearchResultViewDescriptorList(IEnumerable<SearchResultViewDescriptor> descriptors)
        {
            if (descriptors == null)
                throw new ArgumentException("No valid descriptors in SearchResultViewDescriptorList");

            m_CurrentDescriptors = new();
            MergeInto(descriptors);
        }

        public int Count => m_CurrentDescriptors.Count;

        public SearchResultViewDescriptor this[int index]
        {
            get => m_CurrentDescriptors[index];
        }

        public bool isValid => m_CurrentDescriptors != null && m_CurrentDescriptors.Count > 0 && m_CurrentDescriptorIndex > -1;

        public bool SetCurrentFromItemSize(float itemSize)
        {
            foreach (var desc in m_CurrentDescriptors)
            {
                if (itemSize >= desc.SizeMin && itemSize <= desc.SizeMax)
                {
                    CurrentViewId = desc.Id;
                    return true;
                }
            }
            return false;
        }

        public bool SetCurrentFromDisplayMode(DisplayMode mode)
        {
            return SetCurrentFromItemSize(SearchUtils.GetItemSizeFromDisplayMode(mode));
        }

        public SearchResultViewDescriptor Current
        {
            get => m_CurrentDescriptors[m_CurrentDescriptorIndex];
        }

        public string CurrentViewId
        {
            get => Current.Id;
            set
            {
                for (var i = 0; i < m_CurrentDescriptors.Count; ++i)
                {
                    if (m_CurrentDescriptors[i].Id == value)
                    {
                        m_CurrentDescriptorIndex = i;
                        break;
                    }
                }
            }
        }

        public override string ToString()
        {
            return $"current:{Current.Id} [{m_CurrentDescriptors.Count}]";
        }

        public IEnumerable<SearchResultViewDescriptor> Enumerate()
        {
            foreach (var desc in this)
            {
                yield return desc;
            }
        }

        internal void MergeInto(IEnumerable<SearchResultViewDescriptor> toBeMerged)
        {
            var toRestoreViewId = m_CurrentDescriptors.Count > 0 ? CurrentViewId : null;
            foreach (var desc in toBeMerged)
            {
                if (!desc.IsValid)
                    continue;

                bool found = false;
                for (var i = 0; i < m_CurrentDescriptors.Count; ++i)
                {
                    if (m_CurrentDescriptors[i].Id == desc.Id)
                    {
                        // Assume we want to override values of existing descriptor when merging in.
                        m_CurrentDescriptors[i] = desc;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    m_CurrentDescriptors.Add(desc);
                }
            }

            ValidateDescriptors();
            if (toRestoreViewId != null)
                CurrentViewId = toRestoreViewId;
        }

        internal SearchResultViewDescriptor GetDescriptorFromSize(float itemSize)
        {
            foreach (var desc in m_CurrentDescriptors)
            {
                if (desc.SizeMin >= itemSize &&  itemSize <= desc.SizeMax)
                    return desc;
            }
            return m_CurrentDescriptors[0];
        }

        internal SearchResultViewDescriptor GetDescriptorFromMode(DisplayMode displayMode)
        {
            return GetDescriptorFromSize((float)displayMode);
        }

        internal SearchResultViewDescriptor GetDescriptorFromId(string viewId)
        {
            foreach(var desc in m_CurrentDescriptors)
            {
                if (desc.Id == viewId)
                    return desc;
            }

            return m_CurrentDescriptors[0];
        }

        public IResultView CreateView(ISearchView searchView)
        {
            return Current.ViewCreator.Invoke(searchView);
        }

        public static IReadOnlyList<SearchResultViewDescriptor> CreateDefautDescriptors()
        {
            var list = SearchListView.GetDescriptor();
            var grid = SearchGridView.GetDescriptor();
            var table = SearchTableView.GetDescriptor();
            return [list, grid, table];
        }

        public static SearchResultViewDescriptorList CreateDefaultList()
        {
            return new SearchResultViewDescriptorList(CreateDefautDescriptors());
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        public struct Enumerator
        {
            readonly SearchResultViewDescriptorList m_DescList;
            int m_Index;

            internal Enumerator(SearchResultViewDescriptorList descList)
            {
                m_DescList = descList;
                m_Index = -1;
            }

            public readonly SearchResultViewDescriptor Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return m_DescList[m_Index];
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++m_Index < m_DescList.Count;
        }

        private void ValidateDescriptors()
        {
            if (m_CurrentDescriptors.Count == 0)
            {
                foreach (var d in CreateDefautDescriptors())
                {
                    m_CurrentDescriptors.Add(d);
                }
            }

            m_CurrentDescriptors.Sort((a, b) => a.SizeMin.CompareTo(b.SizeMin));
            if (m_CurrentDescriptorIndex > m_CurrentDescriptors.Count || m_CurrentDescriptorIndex < 0)
                m_CurrentDescriptorIndex = 0;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {

        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            for(var  i = m_CurrentDescriptors.Count - 1; i >=0; --i)
            {
                if (!m_CurrentDescriptors[i].IsValid)
                {
                    m_CurrentDescriptors.RemoveAt(i);
                }
            }
            ValidateDescriptors();
        }
    }
}

