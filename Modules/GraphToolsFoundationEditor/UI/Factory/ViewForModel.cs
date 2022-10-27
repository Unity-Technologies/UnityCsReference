// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Utility to get the <see cref="ModelView"/> that have been created for a <see cref="GraphElementModel"/>.
    /// </summary>
    static class ViewForModel
    {
        static ModelViewMapping_Internal s_ViewForModel = new ModelViewMapping_Internal();

        internal static void AddOrReplaceModelView_Internal(ModelView modelView)
        {
            s_ViewForModel.AddOrReplaceViewForModel(modelView);
        }

        [CanBeNull]
        internal static ModelView GetView_Internal(this Model model, RootView view, IViewContext context = null)
        {
            return model == null ? null : GetView_Internal(model.Guid, view, context);
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
        internal static ModelView GetView_Internal(this SerializableGUID modelGuid, RootView view, IViewContext context = null)
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
        public static T GetView<T>(this SerializableGUID modelGuid, RootView view, IViewContext context = null) where T : ModelView
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
        public static void GetAllViews(this Model model, RootView view, Predicate<ModelView> filter, List<ModelView> outUIList)
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
        public static void GetAllViews(this SerializableGUID guid, RootView view, Predicate<ModelView> filter, List<ModelView> outUIList)
        {
            s_ViewForModel.AppendAllViews(guid, view, filter, outUIList);
        }

        internal static IEnumerable<ModelView> GetAllViewsInList_Internal(this IEnumerable<Model> models, RootView view,
            Predicate<ModelView> filter, List<ModelView> outViewList)
        {
            outViewList.Clear();
            var modelList = models.ToList();
            foreach (var model in modelList)
            {
                model.GetAllViews(view, filter, outViewList);
            }

            return outViewList;
        }

        internal static IEnumerable<ModelView> GetAllViewsRecursivelyInList_Internal(this IEnumerable<Model> models, RootView view,
            Predicate<ModelView> filter, List<ModelView> outViewList)
        {
            outViewList.Clear();
            return RecurseGetAllViewsInList(models, view, filter, outViewList);
        }

        static IEnumerable<ModelView> RecurseGetAllViewsInList(this IEnumerable<Model> models, RootView view,
            Predicate<ModelView> filter, List<ModelView> outViewList)
        {
            var modelList = models.ToList();
            foreach (var model in modelList)
            {
                if (model is IGraphElementContainer container)
                {
                    RecurseGetAllViewsInList(container.GraphElementModels, view, filter, outViewList);
                }
                model.GetAllViews(view, filter, outViewList);
            }

            return outViewList;
        }

        internal static void RemoveModelView_Internal(ModelView modelView)
        {
            s_ViewForModel.RemoveModelView(modelView);
        }

        /// <summary>
        /// Clears view for models cache for a <see cref="RootView"/>.
        /// </summary>
        /// <remarks>Designed to run after some tests.</remarks>
        /// <param name="rootView">The view where to clear the cache.</param>
        internal static void Reset_Internal(RootView rootView)
        {
            s_ViewForModel.ResetForView_Internal(rootView);
        }
    }
}
