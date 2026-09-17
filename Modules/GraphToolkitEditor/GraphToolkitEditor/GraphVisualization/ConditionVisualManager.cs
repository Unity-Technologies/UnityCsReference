// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.GraphToolkit.Editor.Implementation;
using UnityEngine;
using UnityEngine.Pool;

namespace Unity.GraphToolkit.Editor.GraphVisualization
{
    /// <summary>
    /// Sets, retrieves, and clears custom icons for conditions.
    /// </summary>
    /// <remarks>
    /// A condition has no element of its own in the graph canvas, but its row in the transition inspector does:
    /// setting an override here changes what that row, and any custom UI a graph tool developer added through
    /// <see cref="ConditionView{T}"/>, displays. Since an override never touches the graph asset, setting one is
    /// pushed directly to those views instead of going through the model/observer pipeline.
    /// Use <see cref="Context.ConditionVisuals"/> to access the condition visual manager from a visualization context.
    /// </remarks>
    sealed class ConditionVisualManager
    {
        readonly Session m_Session;
        bool m_Enabled = true;

        ConditionVisualStore ConditionVisualStore => m_Session.Store.ConditionVisualStore;

        internal ConditionVisualManager(Session session)
        {
            m_Session = session;
            m_Session.isAttached += OnAttached;
            m_Session.isDetached += OnDetached;
        }

        /// <summary>
        /// Whether the condition visual feature is enabled for this visualization context.
        /// </summary>
        /// <remarks>
        /// Condition visuals are enabled by default.
        /// When disabled, condition rows in the transition inspector revert to their own icon until this property is enabled again.
        /// </remarks>
        public bool Enabled
        {
            get => m_Enabled;
            set
            {
                if (value == m_Enabled)
                    return;

                m_Enabled = value;

                foreach (var conditionID in ConditionVisualStore.OverriddenConditionIDs)
                    NotifyChanged(conditionID);
            }
        }

        internal Texture2D GetIcon(ConditionReference conditionReference)
            => GetStoredOverride(conditionReference.ConditionID);

        internal void SetIcon(ConditionReference conditionReference, Texture2D icon)
        {
            var conditionID = conditionReference.ConditionID;

            // A condition only has a row on screen if its transition is selected. Check the model exists, not the view.
            if (m_Session.GraphView != null && !TryResolveModel(conditionID, out _))
            {
                ConditionVisualStore.Clear(conditionID);
                return;
            }

            ConditionVisualStore.Set(conditionID, icon);
            NotifyChanged(conditionID);
        }

        public void Clear(ConditionReference conditionReference)
        {
            ConditionVisualStore.Clear(conditionReference.ConditionID);
            NotifyChanged(conditionReference.ConditionID);
        }

        public void ClearAll()
        {
            using var pooled = ListPool<Hash128>.Get(out var conditionIDs);
            conditionIDs.AddRange(ConditionVisualStore.OverriddenConditionIDs);

            ConditionVisualStore.ClearAll();

            foreach (var conditionID in conditionIDs)
                NotifyChanged(conditionID);
        }

        Texture2D GetStoredOverride(Hash128 conditionID)
            => Enabled && ConditionVisualStore.TryGet(conditionID, out var icon) ? icon : null;

        bool TryResolveModel(Hash128 conditionID, out ConditionModel condition)
        {
            condition = null;
            var graphModel = m_Session.GraphView?.GraphModel;
            return graphModel != null && graphModel.TryGetModelFromGuid(conditionID, out condition);
        }

        void NotifyChanged(Hash128 conditionID)
        {
            var modelInspectorView = m_Session.ModelInspectorView;
            if (modelInspectorView == null)
                return;

            var overrideIcon = GetStoredOverride(conditionID);

            using var pooled = ListPool<ConditionView>.Get(out var views);
            conditionID.AppendAllViews(modelInspectorView, (ConditionView _) => true, views);

            foreach (var view in views)
            {
                view.SetIconOverride(overrideIcon);
                if (view is UserConditionView userView)
                    userView.NotifyConditionChanged();
            }
        }

        void OnAttached()
        {
            foreach (var conditionID in ConditionVisualStore.OverriddenConditionIDs)
                NotifyChanged(conditionID);
        }

        void OnDetached() => ClearAll();
    }
}
