// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor.ContextualMenuItems;
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
    /// The kind of <see cref="TransitionSupportModel"/>.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal enum TransitionSupportKind
    {
        /// <summary>
        /// A transition that can come from any state at the current level of the state machine.
        /// </summary>
        Local,

        /// <summary>
        /// A transition to the same state.
        /// </summary>
        Self,

        /// <summary>
        /// A transition that is triggered when entering the state machine.
        /// </summary>
        OnEnter,

        /// <summary>
        /// A transition between two states.
        /// </summary>
        StateToState
    }

    /// <summary>
    /// A wire that holds transitions.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class TransitionSupportModel : WireModel, IRenamable, IHasTitle, IGraphElementContainer, IHasElementColor
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

        /* We use a TransitionSupportKind enum instead of subclassing TransitionSupportModel to make it easier for
         clients to derive from it, only having a single class to derive from. */
        [SerializeField]
        TransitionSupportKind m_TransitionSupportKind;

        [SerializeField]
        string m_Title;

        [SerializeField]
        protected ElementColor m_ElementColor;

        /// <inheritdoc />
        public ElementColor ElementColor => m_ElementColor = new ElementColor(this);

        /// <inheritdoc />
        public void SetColor(Color color) => m_ElementColor.Color = color;

        /// <inheritdoc />
        public Color DefaultColor => default;

        /// <inheritdoc />
        public bool UseColorAlpha => true;

        /// <summary>
        /// The transitions in this transition support.
        /// </summary>
        public virtual IReadOnlyList<TransitionModel> Transitions => m_Transitions;

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
        /// The kind of transition.
        /// </summary>
        public TransitionSupportKind TransitionSupportKind
        {
            get => m_TransitionSupportKind;
            set => m_TransitionSupportKind = value;
        }

        /// <summary>
        /// Whether the transition is a transition to the same state.
        /// </summary>
        public virtual bool IsSingleStateTransition => (TransitionSupportKind != TransitionSupportKind.StateToState) || (FromPort == ToPort);

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
            m_Capabilities.Add(Editor.Capabilities.Colorable);

            m_TransitionSupportKind = TransitionSupportKind.StateToState;
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

            if (IsSingleStateTransition && toPortModel is StatePortModel statePortModel)
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
        public void RemoveAllTransitions()
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
            RemoveAllTransitions();
            CopyTransitions(source);
        }

        /// <inheritdoc />
        public void Rename(string newName)
        {
            Title = newName;
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            foreach (var transition in Transitions)
            {
                transition.TransitionSupportModel = this;
            }

            m_ElementColor.OwnerElementModel = this;
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
        public string Title
        {
            get => m_Title;
            set
            {
                m_Title = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
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

        static readonly List<ContextualMenuItem> k_ContextualMenuItems = new() {
            ContextualMenuHelpers.copyItem,
            ContextualMenuHelpers.pasteAsNewMenuItem,
        };
    }
}
