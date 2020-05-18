// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.SearchService
{
    // Default implementations for built-in search engines.
    abstract class DefaultSearchEngineBase : ISearchEngineBase
    {
        public string name => "Default";

        public virtual void BeginSession(ISearchContext context)
        {}

        public virtual void EndSession(ISearchContext context)
        {}

        public virtual void BeginSearch(ISearchContext context, string query)
        {}

        public virtual void EndSearch(ISearchContext context)
        {}
    }

    abstract class DefaultSearchEngine<T> : DefaultSearchEngineBase, ISearchEngine<T>
    {
        public virtual IEnumerable<T> Search(ISearchContext context, string query, Action<IEnumerable<T>> asyncItemsReceived)
        {
            return null;
        }
    }

    abstract class DefaultFilterEngine<T> : DefaultSearchEngineBase, IFilterEngine<T>
    {
        public virtual bool Filter(ISearchContext context, string query, T objectToFilter)
        {
            return false;
        }
    }

    abstract class DefaultObjectSelectorEngine : DefaultSearchEngineBase, ISelectorEngine
    {
        public virtual bool SelectObject(ISearchContext context, Action<Object, bool> onObjectSelectorClosed, Action<Object> onObjectSelectedUpdated)
        {
            return false;
        }

        public virtual void SetSearchFilter(ISearchContext context, string searchFilter)
        {}
    }

    // Default project search engine. Nothing is overriden. We keep returning null because
    // of how the search is implemented in the project browser. The null value is handled there,
    // and the default behavior is used when null is returned.
    [ProjectSearchEngine]
    class ProjectSearchEngine : DefaultSearchEngine<string>, IProjectSearchEngine
    {}

    // Custom search context. Used internally to pass the SearchFilter instance to our default
    // search engine.
    class HierarchySearchContext : SceneSearchContext
    {
        public SearchFilter filter;
    }

    // Default scene search engine.
    [SceneSearchEngine]
    class HierarchySearchEngine : DefaultFilterEngine<HierarchyProperty>, ISceneSearchEngine
    {
        public override bool Filter(ISearchContext context, string query, HierarchyProperty objectToFilter)
        {
            // Returning true here, since the properties have already been filtered. See BeginSearch().
            return true;
        }

        public override void BeginSearch(ISearchContext context, string query)
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
    [ObjectSelectorEngine]
    class ObjectPickerEngine : DefaultObjectSelectorEngine, IObjectSelectorEngine
    {}
}
