// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base classes for view that display an aggregation of models.
    /// </summary>
    abstract class MultipleModelsView : ChildView
    {
        /// <summary>
        /// The models that backs the UI.
        /// </summary>
        public IReadOnlyList<Model> Models { get; private set; }

        /// <summary>
        /// Helper method that calls <see cref="Setup"/>, <see cref="View.BuildUI"/> and <see cref="View.UpdateFromModel"/>.
        /// </summary>
        /// <param name="models">The models that backs the instance.</param>
        /// <param name="view">The view to which the instance should be added.</param>
        /// <param name="context">The UI creation context.</param>
        public void SetupBuildAndUpdate(IReadOnlyList<Model> models, RootView view, IViewContext context = null)
        {
            Setup(models, view, context);
            BuildUI();
            UpdateFromModel();
        }

        /// <summary>
        /// Initializes the instance.
        /// </summary>
        /// <param name="models">The models that backs the instance.</param>
        /// <param name="view">The view to which the instance should be added.</param>
        /// <param name="context">The UI creation context.</param>
        public void Setup(IReadOnlyList<Model> models, RootView view, IViewContext context = null)
        {
            Models = models;
            Setup(view, context);
        }

        /// <inheritdoc />
        public override void AddToRootView(RootView view)
        {
            base.AddToRootView(view);
            ViewForModel.AddOrReplaceModelView_Internal(this);
        }

        /// <inheritdoc />
        public override void RemoveFromRootView()
        {
            ViewForModel.RemoveModelView_Internal(this);
            base.RemoveFromRootView();
        }
    }
}
