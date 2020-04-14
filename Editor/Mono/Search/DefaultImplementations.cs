// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    // Default implementations for built-in search engines.
    abstract class DefaultSearchEngineBase : SearchService.ISearchEngineBase
    {
        public const string engineId = "default";
        public string id => engineId;
        public string displayName => engineId.ToPascalCase();

        public virtual void BeginSession(SearchService.ISearchContext context)
        {}

        public virtual void EndSession(SearchService.ISearchContext context)
        {}

        public virtual void BeginSearch(string query, SearchService.ISearchContext context)
        {}

        public virtual void EndSearch(SearchService.ISearchContext context)
        {}
    }

    abstract class DefaultSearchEngine<T> : DefaultSearchEngineBase, SearchService.ISearchEngine<T>
    {
        public virtual IEnumerable<T> Search(string query, SearchService.ISearchContext context, Action<IEnumerable<T>> asyncItemsReceived)
        {
            return null;
        }
    }

    abstract class DefaultFilterEngine<T> : DefaultSearchEngineBase, SearchService.IFilterEngine<T>
    {
        public virtual bool Filter(string query, T objectToFilter, SearchService.ISearchContext context)
        {
            return false;
        }
    }

    abstract class DefaultObjectSelectorEngine : DefaultSearchEngineBase, SearchService.ISelectorEngine
    {
        public virtual bool SelectObject(SearchService.ISearchContext context, Action<Object, bool> onObjectSelectorClosed, Action<Object> onObjectSelectedUpdated)
        {
            return false;
        }
    }

    // Default project search engine. Nothing is overriden. We keep returning null because
    // of how the search is implemented in the project browser. The null value is handled there,
    // and the default behavior is used when null is returned.
    [SearchService.Project.Engine]
    class ProjectSearchEngine : DefaultSearchEngine<string>, SearchService.Project.IEngine
    {}

    // Custom search context. Used internally to pass the SearchFilter instance to our default
    // search engine.
    class HierarchySearchContext : SearchService.Scene.SearchContext
    {
        public SearchFilter filter;
    }

    // Default scene search engine.
    [SearchService.Scene.Engine]
    class HierarchySearchEngine : DefaultFilterEngine<HierarchyProperty>, SearchService.Scene.IEngine
    {
        public override bool Filter(string query, HierarchyProperty objectToFilter, SearchService.ISearchContext context)
        {
            // Returning true here, since the properties have already been filtered. See BeginSearch().
            return true;
        }

        public override void BeginSearch(string query, SearchService.ISearchContext context)
        {
            // To get the original behavior and performance, we set the filter on the root property
            // at the beginning of a search. This will have the effect of filtering the properties
            // during a call to Next() or NextWithDepthCheck(), so Filter() should always return true.
            var hierarchySearchContext = context as HierarchySearchContext;
            hierarchySearchContext?.rootProperty.SetSearchFilter(hierarchySearchContext.filter);
        }
    }

    // Default object selector engine. Nothing is overriden. We keep returning false because
    // of how the selector is implemented in the object selector. The bool value is handled there,
    // and the default behavior is used when false is returned.
    [SearchService.ObjectSelector.Engine]
    class ObjectPickerEngine : DefaultObjectSelectorEngine, SearchService.ObjectSelector.IEngine
    {}
}
