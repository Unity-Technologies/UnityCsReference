// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Extension methods for managing dependencies.
    /// </summary>
    static class UIDependenciesExtensions
    {
        /// <summary>
        /// Gets the UIs that depends on a model. They need to be updated when the model changes.
        /// </summary>
        /// <param name="model">The model for which we're querying the UI.</param>
        public static IEnumerable<ModelView> GetModelDependencies(this GraphElementModel model)
        {
            return model == null ? Enumerable.Empty<ModelView>() : UIDependencies.GetModelDependencies(model.Guid);
        }

        /// <summary>
        /// Gets the UIs that depends on a model. They need to be updated when the model changes.
        /// </summary>
        /// <param name="modelGUID">The model guid for which we're querying the UI.</param>
        public static IEnumerable<ModelView> GetModelDependencies(this Hash128 modelGUID)
        {
            return UIDependencies.GetModelDependencies(modelGUID);
        }
    }
}
