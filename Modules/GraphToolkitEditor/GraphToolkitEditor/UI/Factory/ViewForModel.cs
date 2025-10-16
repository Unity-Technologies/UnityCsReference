// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Utility to get the <see cref="ModelView"/> that have been created for a <see cref="GraphElementModel"/>.
    /// </summary>
    [UnityRestricted]
    internal static class ViewForModel
    {
        static ModelViewMapping s_ViewForModel = new();

        internal static void AddOrReplaceModelView(ModelView modelView)
        {
            s_ViewForModel.AddOrReplaceViewForModel(modelView);
        }

        internal static void AddOrReplaceModelView(MultipleModelsView modelView)
        {
            s_ViewForModel.AddOrReplaceViewForModel(modelView);
        }

        [CanBeNull]
        internal static ChildView GetView(this Model model, RootView view, IViewContext context = null)
        {
            return model == null ? null : GetView(model.Guid, view, context);
        }

        /// <summary>
        /// Gets the view for a model, given a root view and a context.
        /// </summary>
        /// <param name="model">The model for which to get the view.</param>
        /// <param name="view">The root view holding the view to return.</param>
        /// <param name="context">The creation context of the view.</param>
        /// <typeparam name="T">The type of view to get.</typeparam>
        /// <returns>The view for the model, or null if no view was found.</returns>
        [CanBeNull]
        public static T GetView<T>(this Model model, RootView view, IViewContext context = null) where T : ModelView
        {
            return model == null ? null : GetView<T>(model.Guid, view, context);
        }

        [CanBeNull]
        internal static ChildView GetView(this Hash128 modelGuid, RootView view, IViewContext context = null)
        {
            return s_ViewForModel.FirstViewOrDefault(view, context, modelGuid);
        }

        /// <summary>
        /// Gets the view for a model guid, given a root view and a context.
        /// </summary>
        /// <param name="modelGuid">The model guid for which to get the view.</param>
        /// <param name="view">The root view holding the view to return.</param>
        /// <param name="context">The creation context of the view.</param>
        /// <typeparam name="T">The type of view to get.</typeparam>
        /// <returns>The view for the model, or null if no view was found.</returns>
        [CanBeNull]
        public static T GetView<T>(this Hash128 modelGuid, RootView view, IViewContext context = null) where T : ChildView
        {
            return s_ViewForModel.FirstViewOrDefault(view, context, modelGuid) as T;
        }

        /// <summary>
        /// Appends all <see cref="ModelView"/>s that represents <paramref name="model"/> to the list <paramref name="outUIList"/>.
        /// </summary>
        /// <param name="model">The model for which to get the <see cref="ModelView"/>s.</param>
        /// <param name="view">The view in which the UI elements live.</param>
        /// <param name="filter">A predicate to filter the appended elements.</param>
        /// <param name="outUIList">The list onto which the elements are appended.</param>
        /// <typeparam name="TView">The type of view to append to the list.</typeparam>
        public static void AppendAllViews<TView>(this Model model, RootView view, Predicate<TView> filter, List<TView> outUIList)
            where TView : ChildView
        {
            if (model == null)
                return;

            s_ViewForModel.AppendAllViews(model.Guid, view, filter, outUIList);
        }

        /// <summary>
        /// Appends all <see cref="ModelView"/>s that represents the model <paramref name="guid"/> to the list <paramref name="outUIList"/>.
        /// </summary>
        /// <param name="guid">The model guid for which to get the <see cref="ModelView"/>s.</param>
        /// <param name="view">The view in which the UI elements live.</param>
        /// <param name="filter">A predicate to filter the appended elements.</param>
        /// <param name="outUIList">The list onto which the elements are appended.</param>
        /// <typeparam name="TView">The type of view to append to the list.</typeparam>
        public static void AppendAllViews<TView>(this Hash128 guid, RootView view, Predicate<TView> filter, List<TView> outUIList)
            where TView : ChildView
        {
            s_ViewForModel.AppendAllViews(guid, view, filter, outUIList);
        }

        internal static IEnumerable<ChildView> GetAllViews(this IEnumerable<Model> models, RootView view,
            Predicate<ChildView> filter, List<ChildView> outViewList)
        {
            outViewList.Clear();
            foreach (var model in models)
            {
                model.AppendAllViews(view, filter, outViewList);
            }

            return outViewList;
        }

        internal static void GetAllViewsRecursively(this IEnumerable<Model> models, RootView view,
            Predicate<ChildView> filter, List<ChildView> outViewList)
        {
            outViewList.Clear();
            RecurseAppendAllViews(models, view, filter, outViewList);
        }

        static void RecurseAppendAllViews(this IEnumerable<Model> models, RootView view,
            Predicate<ChildView> filter, List<ChildView> outViewList)
        {
            foreach (var model in models)
            {
                if (model is IGraphElementContainer container)
                {
                    RecurseAppendAllViews(container.GetGraphElementModels(), view, filter, outViewList);
                }
                model.AppendAllViews(view, filter, outViewList);
            }
        }

        internal static void RemoveModelView(ModelView modelView)
        {
            s_ViewForModel.RemoveModelView(modelView);
        }

        internal static void RemoveModelView(MultipleModelsView modelView)
        {
            s_ViewForModel.RemoveModelView(modelView);
        }

        /// <summary>
        /// Clears view for models cache for a <see cref="RootView"/>.
        /// </summary>
        /// <remarks>Designed to run after some tests.</remarks>
        /// <param name="rootView">The view where to clear the cache.</param>
        internal static void Reset(RootView rootView)
        {
            s_ViewForModel.ResetForView(rootView);
        }
    }
}
