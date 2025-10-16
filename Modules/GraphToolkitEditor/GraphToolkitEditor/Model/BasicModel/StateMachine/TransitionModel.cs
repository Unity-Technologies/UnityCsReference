// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class for transition in a state machine.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class TransitionModel : GraphElementModel, IHasTitle, IRenamable, IGraphElementContainer
    {
        [FormerlySerializedAs("Enable")]
        [SerializeField, HideInInspector, InspectorUseProperty("Enabled")]
        bool m_Enabled = true;

        [SerializeField, HideInInspector]
        string m_Title;

        [FormerlySerializedAs("Condition")]
        [SerializeReference]
        GroupConditionModel m_ConditionModel;

        TransitionSupportModel m_TransitionSupportModel;

        /// <summary>
        /// The transition support model that contains this transition.
        /// </summary>
        public TransitionSupportModel TransitionSupportModel
        {
            get => m_TransitionSupportModel;
            set => m_TransitionSupportModel = value;
        }

        /// <inheritdoc />
        public override IEnumerable<GraphElementModel> DependentModels => GetGraphElementModels();

        /// <inheritdoc />
        public override IGraphElementContainer Container => TransitionSupportModel;

        /// <summary>
        /// The condition model for this transition.
        /// </summary>
        public GroupConditionModel ConditionModel
        {
            get
            {
                if (m_ConditionModel == null)
                {
                    m_ConditionModel = new GroupConditionModel();
                    GraphModel?.RegisterCondition(m_ConditionModel);
                    m_ConditionModel.GraphModel = GraphModel;
                }
                return m_ConditionModel;
            }
        }

        /// <summary>
        /// Clone the passed <see cref="GroupConditionModel"/> into this transition.
        /// </summary>
        /// <param name="original">The condition to clone.</param>
        public void CloneConditionModel(GroupConditionModel original)
        {
            m_ConditionModel = original.Clone();
        }

        /// <summary>
        /// Whether the transition is enabled.
        /// </summary>
        public bool Enabled
        {
            get => m_Enabled;
            set
            {
                m_Enabled = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <inheritdoc />
        public string Title
        {
            get => m_Title;
            set
            {
                m_Title = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        public void Rename(string name)
        {
            Title = name;
        }

        /// <inheritdoc />
        public IEnumerable<GraphElementModel> GetGraphElementModels()
        {
            if (ConditionModel != null)
            {
                yield return ConditionModel;
            }
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            if (m_ConditionModel != null)
                m_ConditionModel.Transition = this;
        }

        /// <summary>
        /// Do not use this method. It is not implemented.
        /// </summary>
        /// <param name="elementModels"></param>
        /// <exception cref="NotImplementedException"></exception>
        void IGraphElementContainer.RemoveContainerElements(IReadOnlyCollection<GraphElementModel> elementModels)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool Repair()
        {
            return false;
        }
    }
}
