// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

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

        public void RemoveModelView(ModelView modelView)
        {
            if (modelView.Model == null)
                return;

            var contextualizedView = m_ContextualizedModelViews.FirstOrDefault(cge => cge.View == modelView.RootView && cge.Context == modelView.Context);

            contextualizedView?.ModelViews.Remove(modelView.Model.Guid);
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

        public ModelView FirstViewOrDefault(RootView view, IViewContext context, SerializableGUID modelGuid)
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

        public void AppendAllViews(SerializableGUID modelGuid, RootView view, Predicate<ModelView> filter, List<ModelView> outViewList)
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
