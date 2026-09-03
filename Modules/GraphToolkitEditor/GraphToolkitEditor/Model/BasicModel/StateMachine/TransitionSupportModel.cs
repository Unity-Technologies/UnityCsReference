// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor.ContextualMenuItems;
using Unity.GraphToolkit.Editor.Implementation;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The side of the state on which the transition is anchored.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal enum AnchorSide
    {
        /// <summary>
        /// Not anchored.
        /// </summary>
        None,

        /// <summary>
        /// Anchored on the top side.
        /// </summary>
        Top,

        /// <summary>
        /// Anchored on the right side.
        /// </summary>
        Right,

        /// <summary>
        /// Anchored on the bottom side.
        /// </summary>
        Bottom,

        /// <summary>
        /// Anchored on the left side.
        /// </summary>
        Left
    }

    /// <summary>
    /// A wire that holds transitions.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal abstract partial class TransitionSupportModel : WireModel, IHasTitle, IGraphElementContainer, ITransition
    {
        [SerializeField]
        [HideInInspector]
        [FormerlySerializedAs("m_FromStateAnchorSide")]
        internal AnchorSide m_FromNodeAnchorSide;

        [SerializeField]
        [HideInInspector]
        [FormerlySerializedAs("m_FromStateAnchorOffset")]
        internal float m_FromNodeAnchorOffset;

        [SerializeField]
        [HideInInspector]
        [FormerlySerializedAs("m_ToStateAnchorSide")]
        internal AnchorSide m_ToNodeAnchorSide;

        [SerializeField]
        [HideInInspector]
        [FormerlySerializedAs("m_ToStateAnchorOffset")]
        internal float m_ToNodeAnchorOffset;

        [SerializeReference]
        [FormerlySerializedAs("m_StoreTransitions")]
        List<TransitionModel> m_Transitions = new();

        Color m_DefaultColor;

        string m_Tooltip;

        /// <summary>
        /// The default color of the transition.
        /// </summary>
        public virtual Color DefaultColor
        {
            get => m_DefaultColor;
            set
            {
                if (m_DefaultColor == value)
                    return;
                m_DefaultColor = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Style);
            }
        }

        public virtual string IconPath => null;

        /// <summary>
        /// The text displayed when hovering over the transition.
        /// </summary>
        public virtual string Tooltip
        {
            get => m_Tooltip;
            set
            {
                if (m_Tooltip == value)
                    return;
                m_Tooltip = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Style);
            }
        }

        /// <summary>
        /// The transitions in this transition support.
        /// </summary>
        public virtual IReadOnlyList<TransitionModel> Transitions => m_Transitions;

        /// <inheritdoc cref="ITransition.FromState" />
        public IState FromState => GetPublicState(FromPort?.NodeModel);

        /// <inheritdoc cref="ITransition.ToState" />
        public IState ToState => GetPublicState(ToPort?.NodeModel);

        // A user-authored state is exposed through its State object, every other kind of state is exposed through its model.
        static IState GetPublicState(PortNodeModel nodeModel)
        {
            if (nodeModel is UserStateModelImp stateImp)
                return stateImp.Node;

            return nodeModel as IState;
        }

        /// <inheritdoc cref="ITransition.GetRules" />
        public IEnumerable<ITransitionRule> GetRules() => Transitions;

        /// <inheritdoc cref="ITransition.RuleCount" />
        public int RuleCount => Transitions.Count;

        /// <inheritdoc cref="ITransition.GetRule" />
        public ITransitionRule GetRule(int index)
        {
            if (index < 0 || index >= Transitions.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return Transitions[index];
        }

        /// <inheritdoc cref="ITransition.AddRule()" />
        public ITransitionRule AddRule()
        {
            CheckModificationLock();

            var rule = CreateTransition();
            AddTransition(rule);
            return rule;
        }

        /// <inheritdoc cref="ITransition.AddRule(ITransitionRule)" />
        public void AddRule(ITransitionRule rule)
        {
            CheckModificationLock();

            if (rule == null)
                throw new ArgumentNullException(nameof(rule));
            if (rule is not TransitionModel model)
                throw new ArgumentException("The rule is not a valid transition rule.", nameof(rule));
            // A rule's conditions stay bound to the graph it was created in.
            if (model.TransitionSupportModel != null && model.TransitionSupportModel.GraphModel != GraphModel)
                throw new ArgumentException("The rule belongs to a transition in another state machine.", nameof(rule));
            if (!AcceptsTransition(model))
                throw new ArgumentException("This transition does not accept the given rule.", nameof(rule));
            // Moving a rule out of its transition must not empty that transition: a transition always keeps at least one rule.
            if (model.TransitionSupportModel != null && !ReferenceEquals(model.TransitionSupportModel, this) && model.TransitionSupportModel.Transitions.Count == 1)
                throw new InvalidOperationException("The rule cannot be moved because it is the last rule of the transition it belongs to. Use StateMachine.Disconnect to remove that transition instead.");

            AddTransition(model);
        }

        /// <inheritdoc cref="ITransition.RemoveRule" />
        public void RemoveRule(ITransitionRule rule)
        {
            CheckModificationLock();

            if (rule == null)
                throw new ArgumentNullException(nameof(rule));

            if (rule is not TransitionModel model || !ReferenceEquals(model.TransitionSupportModel, this))
                throw new ArgumentException("The rule does not belong to this transition.", nameof(rule));

            if (m_Transitions.Count == 1)
                throw new InvalidOperationException("The rule cannot be removed because it is the last rule of this transition.");

            RemoveTransitions(new[] { model });
        }

        /// <summary>
        /// Returns the public <see cref="ITransition"/> to hand back through the read API. When this support
        /// backs a user-authored transition, the user object is returned instead of the internal model.
        /// </summary>
        internal virtual ITransition AsPublicTransition() => this;

        /// <inheritdoc />
        public override string WireBubbleText
        {
            get => m_WireBubbleText;
            set => base.WireBubbleText = value;
        }

        /// <summary>
        /// The side of the From state on which the transition is anchored.
        /// </summary>
        public AnchorSide FromNodeAnchorSide
        {
            get => m_FromNodeAnchorSide;
            set
            {
                if (m_FromNodeAnchorSide == value)
                    return;

                m_FromNodeAnchorSide = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// The offset on the From state on which the transition is anchored, where zero is the top or left side.
        /// </summary>
        public float FromNodeAnchorOffset
        {
            get => m_FromNodeAnchorOffset;
            set
            {
                if (Math.Abs(m_FromNodeAnchorOffset - value) < 0.05f)
                    return;

                m_FromNodeAnchorOffset = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// The side of the To state on which the transition is anchored.
        /// </summary>
        public AnchorSide ToNodeAnchorSide
        {
            get => m_ToNodeAnchorSide;
            set
            {
                if (m_ToNodeAnchorSide == value)
                    return;

                m_ToNodeAnchorSide = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// The offset on the To state on which the transition is anchored, where zero is the top or left side.
        /// </summary>
        public float ToNodeAnchorOffset
        {
            get => m_ToNodeAnchorOffset;
            set
            {
                if (Math.Abs(m_ToNodeAnchorOffset - value) < 0.05f)
                    return;

                m_ToNodeAnchorOffset = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// Whether the transition is a transition to the same state.
        /// </summary>
        public virtual bool IsSelfTransition => true;

        /// <summary>
        /// The type used to identify this transition support when matching or recreating it.
        /// </summary>
        /// <remarks>
        /// Defaults to the runtime type. Implementations that back a user-defined transition with a single shared
        /// model type override this to return the user-facing type, so that distinct user transition types are not
        /// collapsed together.
        /// </remarks>
        public virtual Type TransitionSupportType => GetType();

        /// <inheritdoc />
        public override IEnumerable<GraphElementModel> DependentModels => GetGraphElementModels();

        /// <summary>
        /// Initializes a new instance of the <see cref="TransitionSupportModel"/> class.
        /// </summary>
        public TransitionSupportModel()
        {
            m_FromNodeAnchorSide = AnchorSide.None;
            m_FromNodeAnchorOffset = 0.0f;
            m_ToNodeAnchorSide = AnchorSide.None;
            m_ToNodeAnchorOffset = 0.0f;

            m_Capabilities.Remove(Editor.Capabilities.Ascendable);
        }

        /// <summary>
        /// Sets the anchor of the To state.
        /// </summary>
        /// <param name="side">The new anchor side.</param>
        /// <param name="offset">The new anchor offset.</param>
        public virtual void SetToAnchor(AnchorSide side, float offset)
        {
            ToNodeAnchorOffset = offset;
            ToNodeAnchorSide = side;
        }

        /// <summary>
        /// Sets the anchor of the From state.
        /// </summary>
        /// <param name="side">The new anchor side.</param>
        /// <param name="offset">The new anchor offset.</param>
        public virtual void SetFromAnchor(AnchorSide side, float offset)
        {
            FromNodeAnchorOffset = offset;
            FromNodeAnchorSide = side;
        }

        /// <inheritdoc />
        public override void SetPorts(PortModel toPortModel, PortModel fromPortModel)
        {
            base.SetPorts(toPortModel, fromPortModel);

            if (IsSelfTransition && toPortModel is StatePortModel statePortModel)
            {
                var anchorPos = statePortModel.ComputeOffsetForNewSingleStateTransition();
                SetToAnchor(AnchorSide.Top, anchorPos);
            }
        }

        /// <summary>
        /// Creates a new transition.
        /// </summary>
        /// <returns>The new transition.</returns>
        public virtual TransitionModel CreateTransition()
        {
            return new TransitionModel();
        }

        /// <summary>
        /// Whether this transition support accepts the transition.
        /// </summary>
        /// <param name="transitionModel">The transition to check.</param>
        /// <returns>True if the transition is accepted, false otherwise.</returns>
        public virtual bool AcceptsTransition(TransitionModel transitionModel)
        {
            return true;
        }

        /// <summary>
        /// Adds a transition to this transition support.
        /// </summary>
        /// <param name="transitionModel">The transition to add.</param>
        public void AddTransition(TransitionModel transitionModel)
        {
            if (AcceptsTransition(transitionModel))
            {
                transitionModel.TransitionSupportModel?.RemoveTransitions(new[] { transitionModel });
                transitionModel.GraphModel = GraphModel;
                transitionModel.TransitionSupportModel = this;
                transitionModel.ConditionModel.Transition = transitionModel;
                m_Transitions.Add(transitionModel);
                GraphModel?.RegisterTransition(transitionModel);
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
                GraphModel?.CurrentGraphChangeDescription.AddNewModel(transitionModel);
            }
        }

        /// <summary>
        /// Removes transitions from this transition support.
        /// </summary>
        /// <param name="transitionsToRemove">The list of transitions to remove.</param>
        public void RemoveTransitions(IReadOnlyList<TransitionModel> transitionsToRemove)
        {
            var modified = false;
            foreach (var transition in transitionsToRemove)
            {
                if (m_Transitions.Remove(transition))
                {
                    modified = true;
                    transition.TransitionSupportModel = null;
                    GraphModel?.UnregisterTransition(transition);
                    transition.GraphModel = null;
                    GraphModel?.CurrentGraphChangeDescription.AddDeletedModel(transition);
                }
            }

            if (modified)
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
        }

        /// <summary>
        /// Removes all transitions from this transition support.
        /// </summary>
        public void RemoveAllTransitionRules()
        {
            if (m_Transitions.Count == 0)
                return;

            foreach (var transition in m_Transitions)
            {
                transition.TransitionSupportModel = null;
                GraphModel?.UnregisterTransition(transition);
                transition.GraphModel = null;
                GraphModel?.CurrentGraphChangeDescription.AddDeletedModel(transition);
            }
            m_Transitions.Clear();

            GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
        }

        /// <summary>
        /// Reorders transitions in this transition support list of transitions.
        /// </summary>
        /// <param name="transitionsToReorder">The list of transitions to reorder.</param>
        /// <param name="position">The position to move the transitions to.</param>
        public void ReorderTransitions(IReadOnlyList<TransitionModel> transitionsToReorder, int position = -1)
        {
            foreach (var transition in transitionsToReorder)
            {
                m_Transitions.Remove(transition);
            }

            var p = position > m_Transitions.Count ? m_Transitions.Count : position;
            foreach (var transition in transitionsToReorder)
            {
                m_Transitions.Insert(p, transition);
                ++p;
            }

            GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
        }

        /// <summary>
        /// Copies transitions from a source transition support to this transition support. Existing transitions are kept.
        /// </summary>
        /// <param name="source">The source transition support to copy transitions from.</param>
        public virtual void CopyTransitions(TransitionSupportModel source)
        {
            foreach (var transition in source.Transitions)
            {
                if (AcceptsTransition(transition))
                {
                    AddTransition(transition.Clone());
                }
                else
                {
                    var newTransition = CreateTransition();
                    newTransition.CloneConditionModel(transition.ConditionModel);
                    AddTransition(newTransition);
                }
            }
            GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
        }

        /// <summary>
        /// Replace transitions in this transition support with a copy of the transitions from a source transition support.
        /// </summary>
        /// <param name="source">The source transition support to copy transitions from.</param>
        public void ReplaceTransitions(TransitionSupportModel source)
        {
            RemoveAllTransitionRules();
            CopyTransitions(source);
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            foreach (var transition in Transitions)
            {
                transition.TransitionSupportModel = this;
            }
        }

        /// <inheritdoc />
        public IEnumerable<GraphElementModel> GetGraphElementModels()
        {
            return Transitions;
        }

        /// <inheritdoc />
        public void RemoveContainerElements(IReadOnlyCollection<GraphElementModel> elementModels)
        {
            RemoveTransitions(elementModels.OfTypeToList<TransitionModel, GraphElementModel>());
        }

        /// <inheritdoc />
        public bool Repair()
        {
            return false;
        }

        /// <summary>
        /// The title of the transition support.
        /// </summary>
        public virtual string Title
        {
            get => string.Empty;
            set {}
        }

        /// <inheritdoc />
        /// <inheritdoc />
        public override IReadOnlyList<ContextualMenuItem> ContextualMenuItems
        {
            get
            {
                var wiresMenuItems = base.ContextualMenuItems;
                var menuItems = new List<ContextualMenuItem>(wiresMenuItems);
                menuItems.AddRange(k_ContextualMenuItems);
                return menuItems;
            }
        }

        [AutoStaticsCleanupOnCodeReload]
        static List<ContextualMenuItem> k_ContextualMenuItems = new() {
            ContextualMenuHelpers.copyItem,
            ContextualMenuHelpers.pasteAsNewMenuItem,
        };
    }
}
