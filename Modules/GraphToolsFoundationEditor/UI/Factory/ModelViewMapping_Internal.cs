// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    class ModelViewMapping_Internal
    {
        List<ContextualizedModelViews_Internal> m_ContextualizedModelViews;

        public ModelViewMapping_Internal()
        {
            m_ContextualizedModelViews = new List<ContextualizedModelViews_Internal>();
        }

        public void AddOrReplaceViewForModel(ModelView modelView)
        {
            if (modelView.Model == null)
                return;

            var view = modelView.RootView;
            var context = modelView.Context;

            var contextualizedView = m_ContextualizedModelViews.FirstOrDefault(cge
                => cge.View == view && cge.Context == context);

            if (contextualizedView == null)
            {
                contextualizedView = new ContextualizedModelViews_Internal(view, context);
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

            var contextualizedView = m_ContextualizedModelViews.FirstOrDefault(cge
                => cge.View == view && cge.Context == context);

            if (contextualizedView == null)
            {
                contextualizedView = new ContextualizedModelViews_Internal(view, context);
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

            var contextualizedView = m_ContextualizedModelViews.FirstOrDefault(cge => cge.View == modelView.RootView && cge.Context == modelView.Context);

            contextualizedView?.ModelViews.Remove(modelView.Model.Guid);
        }

        public void RemoveModelView(MultipleModelsView modelView)
        {
            if (modelView.Models == null || modelView.Models.Count == 0)
                return;

            var contextualizedView = m_ContextualizedModelViews.FirstOrDefault(cge => cge.View == modelView.RootView && cge.Context == modelView.Context);

            foreach (var model in modelView.Models)
            {
                contextualizedView?.ModelViews.Remove(model.Guid);
            }
        }

        /// <summary>
        /// Clears view for models cache for a <see cref="RootView"/>.
        /// </summary>
        /// <param name="rootView">The view where to clear the cache.</param>
        internal void ResetForView_Internal(RootView rootView)
        {
            for (var i = m_ContextualizedModelViews.Count - 1; i >= 0; i--)
            {
                if (m_ContextualizedModelViews[i].View == rootView)
                    m_ContextualizedModelViews.RemoveAt(i);
            }
        }

        public ChildView FirstViewOrDefault(RootView view, IViewContext context, Hash128 modelGuid)
        {
            ContextualizedModelViews_Internal gel = null;
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

        public void AppendAllViews(Hash128 modelGuid, RootView view, Predicate<ChildView> filter, List<ChildView> outViewList)
        {
            foreach (var contextualizedView in m_ContextualizedModelViews)
            {
                if (contextualizedView.ModelViews.TryGetValue(modelGuid, out var modelView) &&
                    modelView.RootView == view &&
                    (filter == null || filter(modelView)))
                {
                    outViewList.Add(modelView);
                }
            }
        }
    }
}
