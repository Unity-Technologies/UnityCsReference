// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Factory to create UI from models.
    /// </summary>
    static class ModelViewFactory
    {
        /// <summary>
        /// Creates an instance of a class implementing <see cref="ModelView"/> to display <paramref name="model"/>.
        /// </summary>
        /// <param name="view">The view in which to put the UI.</param>
        /// <param name="model">The model.</param>
        /// <typeparam name="T">The type of the returned object.</typeparam>
        /// <returns>An instance of <see cref="ModelView"/> that display <paramref name="model"/>.</returns>
        [CanBeNull]
        public static T CreateUI<T>(RootView view, Model model) where T : ModelView
        {
            return CreateUI<T>(view, model, null);
        }

        /// <summary>
        /// Creates an instance of a class implementing <see cref="ModelView"/> to display <paramref name="model"/>.
        /// </summary>
        /// <param name="view">The view in which to put the UI.</param>
        /// <param name="model">The model.</param>
        /// <param name="context">The context of ui creation. When a model needs different UI in
        /// different contexts, use this parameter to differentiate between contexts.</param>
        /// <typeparam name="T">The type of the returned object.</typeparam>
        /// <returns>An instance of <see cref="ModelView"/> that display <paramref name="model"/>.</returns>
        public static T CreateUI<T>(RootView view, Model model, IViewContext context) where T : ModelView
        {
            if (view == null)
            {
                Debug.LogError("GraphElementFactory could not create element because view is null.");
                return null;
            }

            if (model == null)
            {
                Debug.LogError("GraphElementFactory could not create element because model is null.");
                return null;
            }

            var ext = ExtensionMethodCache<ElementBuilder>.GetExtensionMethod(
                view.GetType(),
                model.GetType(),
                FilterMethods_Internal,
                KeySelector_Internal
            );

            T newElem = null;
            if (ext != null)
            {
                var nodeBuilder = new ElementBuilder { View = view, Context = context };
                newElem = ext.Invoke(null, new object[] { nodeBuilder, model }) as T;
            }

            if (newElem == null)
            {
                Debug.LogError($"GraphElementFactory doesn't know how to create a UI of type {typeof(T)} for model of type: {model.GetType()}");
                return null;
            }

            return newElem;
        }

        internal static Type KeySelector_Internal(MethodInfo x)
        {
            return x.GetParameters()[1].ParameterType;
        }

        internal static bool FilterMethods_Internal(MethodInfo x)
        {
            if (x.ReturnType != typeof(ModelView))
                return false;

            var parameters = x.GetParameters();
            return parameters.Length == 2 && parameters[0].ParameterType == typeof(ElementBuilder);
        }
    }
}
