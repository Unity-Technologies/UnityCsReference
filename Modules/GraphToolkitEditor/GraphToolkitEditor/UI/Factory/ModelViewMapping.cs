// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    class ModelViewMapping
    {
        List<ContextualizedModelViews> m_ContextualizedModelViews;

        public ModelViewMapping()
        {
            m_ContextualizedModelViews = new List<ContextualizedModelViews>();
        }

        public void AddOrReplaceViewForModel(ModelView modelView)
        {
            if (modelView.Model == null)
                return;

            var view = modelView.RootView;
            var context = modelView.Context;

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var contextualizedView = m_ContextualizedModelViews.FirstOrDefault(cge
#pragma warning restore RS0030
                => cge.View == view && cge.Context == context);

            if (contextualizedView == null)
            {
                contextualizedView = new ContextualizedModelViews(view, context);
                m_ContextualizedModelViews.Add(contextualizedView);
            }

            contextualizedView.ModelViews[modelView.Model.Guid] = modelView;
        }

        public void AddOrReplaceViewForModel(MultipleModelsView modelView)
        {
            if (modelView.Models == null || modelView.Models.Count == 0)
                return;

            var view = modelView.RootView;
            var context = modelView.Context;

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var contextualizedView = m_ContextualizedModelViews.FirstOrDefault(cge
#pragma warning restore RS0030
                => cge.View == view && cge.Context == context);

            if (contextualizedView == null)
            {
                contextualizedView = new ContextualizedModelViews(view, context);
                m_ContextualizedModelViews.Add(contextualizedView);
            }

            foreach (var model in modelView.Models)
            {
                contextualizedView.ModelViews[model.Guid] = modelView;
            }
        }

        public void RemoveModelView(ModelView modelView)
        {
            if (modelView.Model == null)
                return;

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var contextualizedView = m_ContextualizedModelViews.FirstOrDefault(cge => cge.View == modelView.RootView && cge.Context == modelView.Context);
#pragma warning restore RS0030

            contextualizedView?.ModelViews.Remove(modelView.Model.Guid);
        }

        public void RemoveModelView(MultipleModelsView modelView)
        {
            if (modelView.Models == null || modelView.Models.Count == 0)
                return;

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var contextualizedView = m_ContextualizedModelViews.FirstOrDefault(cge => cge.View == modelView.RootView && cge.Context == modelView.Context);
#pragma warning restore RS0030

            foreach (var model in modelView.Models)
            {
                contextualizedView?.ModelViews.Remove(model.Guid);
            }
        }

        /// <summary>
        /// Clears view for models cache for a <see cref="RootView"/>.
        /// </summary>
        /// <param name="rootView">The view where to clear the cache.</param>
        public void ResetForView(RootView rootView)
        {
            for (var i = m_ContextualizedModelViews.Count - 1; i >= 0; i--)
            {
                if (m_ContextualizedModelViews[i].View == rootView)
                    m_ContextualizedModelViews.RemoveAt(i);
            }
        }

        public ChildView FirstViewOrDefault(RootView view, IViewContext context, Hash128 modelGuid)
        {
            ContextualizedModelViews gel = null;
            for (int i = 0; i < m_ContextualizedModelViews.Count; i++)
            {
                var e = m_ContextualizedModelViews[i];
                if (e.View == view && e.Context == context)
                {
                    gel = e;
                    break;
                }
            }

            if (gel == null)
                return null;

            gel.ModelViews.TryGetValue(modelGuid, out var modelView);
            return modelView;
        }

        /// <summary>
        /// Appends all <see cref="ModelView"/>s that are associated with <paramref name="modelGuid"/> to the list <paramref name="outViewList"/>.
        /// </summary>
        /// <param name="modelGuid">The identifier of the model for which to get the <see cref="ModelView"/>s.</param>
        /// <param name="view">The view in which the <see cref="ModelView"/>s live.</param>
        /// <param name="filter">A predicate to filter the appended <see cref="ModelView"/>s.</param>
        /// <param name="outViewList">The list onto which the <see cref="ModelView"/>s are appended.</param>
        /// <typeparam name="TView">The type of view to append to the list.</typeparam>
        /// <remarks>
        /// 'AppendAllViews' appends all <see cref="ModelView"/> instances associated with the specified 'modelGuid' to the provided 'outViewList'. This method retrieves <see cref="ModelView"/>s from
        /// the given <see cref="RootView"/> and applies an optional filter to determine which <see cref="ModelView"/> instances should be included.
        /// </remarks>
        public void AppendAllViews<TView>(Hash128 modelGuid, RootView view, Predicate<TView> filter, List<TView> outViewList)
            where TView : ChildView
        {
            foreach (var contextualizedView in m_ContextualizedModelViews)
            {
                if (contextualizedView.ModelViews.TryGetValue(modelGuid, out var modelView) &&
                    modelView is TView tView &&
                    modelView.RootView == view &&
                    (filter == null || filter(tView)))
                {
                    outViewList.Add(tView);
                }
            }
        }
    }
}
