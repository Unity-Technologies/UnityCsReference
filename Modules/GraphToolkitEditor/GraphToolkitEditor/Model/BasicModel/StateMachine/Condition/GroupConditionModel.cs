// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A base class for a group that holds conditions.
    /// </summary>
    /// <remarks>
    /// 'GroupConditionModel' is a base class for a group that holds conditions. It is used to organize multiple conditions within a <see cref="TransitionModel"/>,
    /// which allows for more structured and manageable condition logic in a graph. With 'GroupConditionModel', you  can group related conditions and apply logical
    /// operations such as 'AND' and 'OR' to evaluate them collectively. This allows for more flexible and expressive condition handling in transitions.
    /// </remarks>
    [Serializable]
    [MovedFrom(false, "Unity.Motion.Editor", "Unity.Motion.Editor")]
    [UnityRestricted]
    internal class GroupConditionModel : ConditionModel, IGraphElementContainer
    {
        /// <summary>
        /// Create an instance of <see cref="GroupConditionModel"/>.
        /// </summary>
        public GroupConditionModel()
        {
        }

        /// <summary>
        /// The type of operation to apply to the sub-conditions.
        /// </summary>
        [UnityRestricted]
        internal enum Operation
        {
            /// <summary>
            /// An operation that represents a logical "AND."
            /// </summary>
            And,

            /// <summary>
            /// An operation that represents a logical "OR."
            /// </summary>
            Or
        }

        [SerializeField]
        [FormerlySerializedAs("GroupOperation")]
        Operation m_GroupOperation = Operation.And;

        [SerializeReference]
        [FormerlySerializedAs("ListSubConditions")]
        List<ConditionModel> m_SubConditions;

        /// <summary>
        /// The type of operation to apply to the sub-conditions.
        /// </summary>
        public Operation GroupOperation
        {
            get => m_GroupOperation;
            set
            {
                m_GroupOperation = value;

                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <inheritdoc />
        public override TransitionModel Transition
        {
            get => base.Transition;
            set
            {
                base.Transition = value;
                foreach (var condition in SubConditions)
                {
                    condition.Transition = value;
                }
            }
        }

        /// <summary>
        /// The sub-conditions of this <see cref="GroupConditionModel"/>.
        /// </summary>
        public IReadOnlyList<ConditionModel> SubConditions => m_SubConditions ??= new List<ConditionModel>();

        /// <inheritdoc />
        public override IEnumerable<GraphElementModel> DependentModels => GetGraphElementModels();

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = base.GetHashCode() ^ (int)GroupOperation * 379;
            foreach (var c in SubConditions)
            {
                hashCode ^= c?.GetHashCode() ?? 0;
            }
            return hashCode;
        }

        /// <summary>
        /// Inserts a <see cref="ConditionModel"/> at the specified position.
        /// </summary>
        /// <param name="condition">The condition to insert.</param>
        /// <param name="position">The position of the condition. -1 means the end of the list.</param>
        public void InsertCondition(ConditionModel condition, int position = -1)
        {
            if (condition.Parent != null)
            {
                condition.Parent.RemoveCondition(condition);
            }

            condition.SetParent(this);
            GraphModel?.RegisterCondition(condition);

            if (position == -1 || position >= SubConditions.Count)
            {
                condition.SetIndexInParent(SubConditions.Count);
                m_SubConditions.Add(condition);
            }
            else
            {
                m_SubConditions.Insert(position, condition);
                for (int i = position; i < SubConditions.Count; ++i)
                {
                    SubConditions[i].SetIndexInParent(i);
                }
            }
            GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            GraphModel?.CurrentGraphChangeDescription.AddNewModel(condition);
        }

        /// <summary>
        /// Adds a condition to the list of sub-conditions.
        /// </summary>
        /// <param name="condition">The condition to insert.</param>
        public void AddCondition(ConditionModel condition)
        {
            InsertCondition(condition);
        }

        /// <summary>
        /// Removes a condition from the list of sub-conditions.
        /// </summary>
        public void RemoveCondition(ConditionModel condition)
        {
            Assert.IsTrue(condition.Parent == this);
            Assert.IsTrue(condition.IndexInParent < SubConditions.Count);
            Assert.IsTrue(condition == SubConditions[condition.IndexInParent]);
            m_SubConditions.RemoveAt(condition.IndexInParent);
            GraphModel?.UnregisterCondition(condition);
            condition.GraphModel = null;
            condition.SetParent(null);
            condition.Transition = null;
            for (int i = condition.IndexInParent; i < SubConditions.Count; ++i)
                SubConditions[i].SetIndexInParent(i);
            GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            GraphModel?.CurrentGraphChangeDescription.AddDeletedModel(condition);
        }

        /// <summary>
        /// Moves conditions to a new position.
        /// </summary>
        /// <param name="conditions">The list of conditions to move.</param>
        /// <param name="position">The new position of the conditions.</param>
        /// <remarks>
        /// This method moves a list of conditions to a new position within a group. The conditions can either be part of the same group or come from another group.
        /// If the position is set to -1, the conditions are placed at the end of the list.
        /// </remarks>
        public void MoveConditions(IReadOnlyList<ConditionModel> conditions, int position = -1)
        {
            foreach (var condition in conditions)
            {
                var originalGroup = condition.Parent;
                Assert.IsTrue(originalGroup != null);
                if (originalGroup == this && position > condition.IndexInParent)
                    --position;
            }
            int absolutePosition = position;
            foreach (var condition in conditions)
            {
                InsertCondition(condition, position == -1 ? -1 : absolutePosition);
                ++absolutePosition;
            }
        }

        internal enum DisplayMode
        {
            Consise,
            Verbose
        }

        internal static DisplayMode DefaultDisplayMode = DisplayMode.Consise;

        internal string GetLabel()
        {
            if (DefaultDisplayMode == DisplayMode.Consise)
            {
                return GroupOperation == Operation.And ? "AND" : "OR";
            }
            return GroupOperation == Operation.And ? "All conditions need to be true" : "One condition needs to be true";
        }

        /// <inheritdoc />
        public override string ToString(int indentLevel = 0)
        {
            string indentLevelStr = GetIndentationString(indentLevel);
            StringBuilder sb = new StringBuilder();
            sb.Append(indentLevelStr);
            sb.Append(GetLabel());
            sb.AppendLine();
            foreach (var condition in SubConditions)
            {
                sb.Append(condition.ToString(indentLevel + 1));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            // undo redo can cause deserialization and IndexInParent to be wrong.
            for (int i = 0; i < SubConditions.Count; i++)
            {
                if (SubConditions[i] != null)
                {
                    SubConditions[i].Transition = Transition;
                    SubConditions[i].SetParent(this);
                    SubConditions[i].SetIndexInParent(i);
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<GraphElementModel> GetGraphElementModels()
        {
            return SubConditions;
        }

        /// <inheritdoc />
        void IGraphElementContainer.RemoveContainerElements(IReadOnlyCollection<GraphElementModel> elementModels)
        {
            foreach (var elementModel in elementModels)
            {
                if (elementModel is ConditionModel conditionModel)
                    RemoveCondition(conditionModel);
            }
        }

        /// <inheritdoc />
        public bool Repair()
        {
            return false;
        }
    }
}
