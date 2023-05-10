// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Stores dependencies of a UI to other UI and additional models.
    /// </summary>
    /// <remarks>
    /// - A dependency exists between two UI when one UI needs to be updated when another UI changes.
    /// - A dependency exists between a UI and a model when one UI needs to be updated when the model changes.
    ///   There is already an intrinsic dependency between a UI and its Model. However we sometimes need to
    ///   make a UI dependent on additional models.
    /// </remarks>
    class UIDependencies
    {
        static Dictionary<Hash128, HashSet<ModelView>> s_ModelDependencies = new Dictionary<Hash128, HashSet<ModelView>>();

        ModelView m_Owner;

        // Graph elements that we affect when we change.
        HashSet<(ChildView, DependencyTypes)> m_ForwardDependencies;
        // Graph elements that affect us when they change.
        HashSet<(ChildView, DependencyTypes)> m_BackwardDependencies;
        // Additional models that influence us.
        HashSet<GraphElementModel> m_ModelDependencies;

        readonly EventCallback<CustomStyleResolvedEvent> m_OnBackwardDependencyCustomStyleResolved;
        readonly EventCallback<GeometryChangedEvent> m_OnBackwardDependencyGeometryChanged;
        readonly EventCallback<DetachFromPanelEvent> m_OnBackwardDependencyDetachedFromPanel;

        /// <summary>
        /// Initializes a new instance of the <see cref="UIDependencies"/> class.
        /// </summary>
        /// <param name="owner">The UI for which these dependencies are declared.</param>
        public UIDependencies(ModelView owner)
        {
            m_Owner = owner;
            m_OnBackwardDependencyCustomStyleResolved = OnBackwardDependencyCustomStyleResolved;
            m_OnBackwardDependencyGeometryChanged = OnBackwardDependencyGeometryChanged;
            m_OnBackwardDependencyDetachedFromPanel = OnBackwardDependencyDetachedFromPanel;
        }

        /// <summary>
        /// Removes all dependencies.
        /// </summary>
        public void ClearDependencyLists()
        {
            if (m_Owner.HasForwardsDependenciesChanged())
                m_ForwardDependencies?.Clear();

            if (m_BackwardDependencies != null && m_Owner.HasBackwardsDependenciesChanged())
            {
                foreach (var (graphElement, dependencyType) in m_BackwardDependencies)
                {
                    var ve = graphElement as VisualElement;
                    if (ve == null)
                        continue;
                    if (dependencyType.HasFlagFast(DependencyTypes.Style))
                        ve.UnregisterCallback(m_OnBackwardDependencyCustomStyleResolved);
                    if (dependencyType.HasFlagFast(DependencyTypes.Geometry))
                        ve.UnregisterCallback(m_OnBackwardDependencyGeometryChanged);
                    if (dependencyType.HasFlagFast(DependencyTypes.Removal))
                        ve.UnregisterCallback(m_OnBackwardDependencyDetachedFromPanel);
                }
                m_BackwardDependencies.Clear();
            }

            if (m_ModelDependencies != null && m_Owner.HasModelDependenciesChanged())
            {
                foreach (var model in m_ModelDependencies)
                {
                    RemoveModelDependency(model, m_Owner);
                }
                m_ModelDependencies.Clear();
            }
        }

        /// <summary>
        /// Asks the owner UI to update its dependencies.
        /// </summary>
        public void UpdateDependencyLists()
        {
            ClearDependencyLists();

            if (m_Owner.HasForwardsDependenciesChanged())
                m_Owner.AddForwardDependencies();

            if (m_Owner.HasBackwardsDependenciesChanged())
            {
                m_Owner.AddBackwardDependencies();
                if (m_BackwardDependencies != null)
                {
                    foreach (var (graphElement, dependencyType) in m_BackwardDependencies)
                    {
                        var ve = graphElement as VisualElement;
                        if (ve == null)
                            continue;
                        if (dependencyType.HasFlagFast(DependencyTypes.Style))
                            ve.RegisterCallback(m_OnBackwardDependencyCustomStyleResolved);
                        if (dependencyType.HasFlagFast(DependencyTypes.Geometry))
                            ve.RegisterCallback(m_OnBackwardDependencyGeometryChanged);
                        if (dependencyType.HasFlagFast(DependencyTypes.Removal))
                            ve.RegisterCallback(m_OnBackwardDependencyDetachedFromPanel);
                    }
                }
            }

            if (m_Owner.HasModelDependenciesChanged())
            {
                m_Owner.AddModelDependencies();
                if (m_ModelDependencies != null)
                {
                    foreach (var model in m_ModelDependencies)
                    {
                        AddModelDependency(model, m_Owner);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a <see cref="ModelView"/> to the forward dependencies list. A forward dependency is
        /// a UI that should be updated whenever this object's owner is updated.
        /// </summary>
        public void AddForwardDependency(ChildView dependency, DependencyTypes dependencyType)
        {
            if (m_ForwardDependencies == null)
                m_ForwardDependencies = new HashSet<(ChildView, DependencyTypes)>();

            m_ForwardDependencies.Add((dependency, dependencyType));
        }

        /// <summary>
        /// Adds a <see cref="ModelView"/> to the backward dependencies list. A backward dependency is
        /// a UI that causes this object's owner to be updated whenever it is updated.
        /// </summary>
        public void AddBackwardDependency(ChildView dependency, DependencyTypes dependencyType)
        {
            if (m_BackwardDependencies == null)
                m_BackwardDependencies = new HashSet<(ChildView, DependencyTypes)>();

            m_BackwardDependencies.Add((dependency, dependencyType));
        }

        /// <summary>
        /// Adds <see cref="GraphElementModel"/> to the model dependencies list. A model dependency is
        /// a graph element model that causes this object's owner to be updated whenever it is updated.
        /// </summary>
        public void AddModelDependency(GraphElementModel model)
        {
            if (m_ModelDependencies == null)
                m_ModelDependencies = new HashSet<GraphElementModel>();

            m_ModelDependencies.Add(model);
            AddModelDependency(model, m_Owner);
        }

        void OnBackwardDependencyGeometryChanged(GeometryChangedEvent evt)
        {
            m_Owner.UpdateFromModel();
        }

        void OnBackwardDependencyCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            m_Owner.UpdateFromModel();
        }

        void OnBackwardDependencyDetachedFromPanel(DetachFromPanelEvent evt)
        {
            m_Owner.UpdateFromModel();
        }

        public void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (m_ForwardDependencies != null)
            {
                foreach (var(graphElement, dependencyType) in m_ForwardDependencies)
                {
                    if (dependencyType.HasFlagFast(DependencyTypes.Geometry))
                        graphElement.UpdateFromModel();
                }
            }
        }

        public void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (m_ForwardDependencies != null)
            {
                foreach (var(graphElement, dependencyType) in m_ForwardDependencies)
                {
                    if (dependencyType.HasFlagFast(DependencyTypes.Style))
                        graphElement.UpdateFromModel();
                }
            }
        }

        public void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            if (m_ForwardDependencies != null)
            {
                foreach (var(graphElement, dependencyType) in m_ForwardDependencies)
                {
                    if (dependencyType.HasFlagFast(DependencyTypes.Removal))
                        graphElement.UpdateFromModel();
                }
            }
        }

        /// <summary>
        /// Adds a dependency between a model and a UI.
        /// </summary>
        /// <param name="model">The model side of the dependency.</param>
        /// <param name="ui">The UI side of the dependency.</param>
        static void AddModelDependency(GraphElementModel model, ModelView ui)
        {
            if (!s_ModelDependencies.TryGetValue(model.Guid, out var uiList))
            {
                uiList = new HashSet<ModelView>();
                s_ModelDependencies[model.Guid] = uiList;
            }

            uiList.Add(ui);
        }

        /// <summary>
        /// Removes a dependency between a model and a UI.
        /// </summary>
        /// <param name="model">The model side of the dependency.</param>
        /// <param name="ui">The UI side of the dependency.</param>
        static void RemoveModelDependency(GraphElementModel model, ModelView ui)
        {
            if (s_ModelDependencies.TryGetValue(model.Guid, out var uiList))
            {
                uiList.Remove(ui);
            }
        }

        /// <summary>
        /// Gets the UIs that depends on a model. They need to be updated when the model changes.
        /// </summary>
        /// <param name="modelGuid">The model GUID for which we're querying the UI.</param>
        public static IEnumerable<ModelView> GetModelDependencies(Hash128 modelGuid)
        {
            return s_ModelDependencies.TryGetValue(modelGuid, out var uiList) ? uiList : Enumerable.Empty<ModelView>();
        }
    }
}
