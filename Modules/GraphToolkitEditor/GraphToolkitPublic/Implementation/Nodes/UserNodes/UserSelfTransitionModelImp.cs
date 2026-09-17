// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.Implementation
{
    /// <summary>
    /// Internal model that backs a user-defined <see cref="SelfTransition"/> in a <see cref="StateMachine"/> graph.
    /// </summary>
    /// <remarks>
    /// This mirrors <see cref="UserStateModelImp"/> for single-state transitions. It derives from
    /// <see cref="SelfTransitionModel"/> (a <c>WireModel</c>), so it has no node-creation hooks; the user
    /// <see cref="SelfTransition.OnEnable"/>/<see cref="SelfTransition.OnDisable"/> lifecycle is driven from the
    /// state machine model (see <c>StateMachineImp</c>). The <see cref="SelfTransition"/> appearance
    /// is applied each session in <c>OnEnable</c>, mirroring how custom states reapply their non-serialized style.
    /// </remarks>
    [Serializable]
    class UserSelfTransitionModelImp : SelfTransitionModel
    {
        [SerializeReference]
        SelfTransition m_Transition;

        [NonSerialized]
        Color m_CustomDefaultColor;

        [NonSerialized]
        Color m_CustomLineColor;

        [NonSerialized]
        string m_CustomTooltip;

        public SelfTransition Transition => m_Transition;

        /// <inheritdoc />
        internal override ITransition AsPublicTransition() => (ISelfTransition)m_Transition ?? this;

        public override Type TransitionSupportType => m_Transition?.GetType() ?? typeof(SelfTransition);

        public override string IconPath => m_Transition?.GetType().GetAttribute<TransitionAttribute>()?.IconPath ?? base.IconPath;

        public override string Title => m_Transition?.GetType().GetAttribute<TransitionAttribute>()?.Title ?? m_Transition?.GetType().Name.Nicify();

        [NonSerialized]
        bool m_OnEnableCalled;

        public bool OnEnableCalled => m_OnEnableCalled;

        public void InitCustomTransition(SelfTransition transition)
        {
            m_Transition = transition;
            m_Transition.SetImplementation(this);
        }

        public override Color DefaultColor
        {
            get => m_CustomDefaultColor;
            set
            {
                if (m_CustomDefaultColor == value)
                    return;

                m_CustomDefaultColor = value;
                using var assetDirtyScope = GraphModel?.BlockAssetDirtyScope();
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Style);
            }
        }

        public override Color LineColor
        {
            get => m_CustomLineColor;
            set
            {
                if (m_CustomLineColor == value)
                    return;

                m_CustomLineColor = value;
                using var assetDirtyScope = GraphModel?.BlockAssetDirtyScope();
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Style);
            }
        }

        public override string Tooltip
        {
            get => !string.IsNullOrEmpty(m_CustomTooltip) ? m_CustomTooltip : base.Tooltip;
            set
            {
                if (m_CustomTooltip == value)
                    return;

                m_CustomTooltip = value;
                using var assetDirtyScope = GraphModel?.BlockAssetDirtyScope();
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Style);
            }
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            m_Transition?.SetImplementation(this);
        }

        public void CallOnEnable()
        {
            m_OnEnableCalled = true;
            m_Transition?.OnEnable();
        }

        public void CallOnDisable()
        {
            m_OnEnableCalled = false;
            m_Transition?.OnDisable();
        }
    }
}
