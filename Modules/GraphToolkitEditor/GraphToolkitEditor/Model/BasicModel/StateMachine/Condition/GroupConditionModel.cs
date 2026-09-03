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
    internal class GroupConditionModel : ConditionModel, IGraphElementContainer, IGroupCondition
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

        /// <inheritdoc />
        public override GraphModel GraphModel
        {
            get => base.GraphModel;
            set
            {
                var previousGraphModel = base.GraphModel;
                base.GraphModel = value;

                // When detaching from a graph, unregister all sub-conditions
                if (previousGraphModel != null && value == null)
                {
                    foreach (var condition in SubConditions)
                    {
                        if (condition != null)
                            previousGraphModel.UnregisterCondition(condition);
                    }
                }
                // When attaching a detached group to a graph, register all sub-conditions
                else if (value != null && previousGraphModel == null)
                {
                    foreach (var condition in SubConditions)
                    {
                        if (condition != null)
                            value.RegisterCondition(condition);
                    }
                }
            }
        }

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
                    if (condition != null)
                        condition.Transition = value;
                }
            }
        }

        /// <summary>
        /// The sub-conditions of this <see cref="GroupConditionModel"/>.
        /// </summary>
        public IReadOnlyList<ConditionModel> SubConditions => m_SubConditions ??= new List<ConditionModel>();

        /// <summary>
        /// Returns the index of the specified condition among the sub-conditions, or -1 if it is not present.
        /// </summary>
        /// <param name="condition">The condition to locate.</param>
        /// <returns>The index of <paramref name="condition"/>, or -1 if it is not a sub-condition of this group.</returns>
        internal int IndexOf(ConditionModel condition)
        {
            return m_SubConditions?.IndexOf(condition) ?? -1;
        }

        /// <inheritdoc />
        GroupConditionOperation IGroupCondition.Operation => GroupOperation == Operation.And ? GroupConditionOperation.And : GroupConditionOperation.Or;

        /// <inheritdoc />
        IEnumerable<ICondition> IGroupCondition.Get()
        {
            foreach (var subCondition in SubConditions)
            {
                if (subCondition == null)
                    continue;

                yield return UnwrapCondition(subCondition);
            }
        }

        /// <inheritdoc />
        int IGroupCondition.Count
        {
            get
            {
                int count = 0;
                foreach (var subCondition in SubConditions)
                    if (subCondition != null)
                        ++count;
                return count;
            }
        }

        /// <inheritdoc />
        ICondition IGroupCondition.Get(int index)
        {
            if (index >= 0)
            {
                foreach (var subCondition in SubConditions)
                {
                    if (subCondition == null)
                        continue;
                    if (index == 0)
                        return UnwrapCondition(subCondition);
                    --index;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        /// <inheritdoc />
        void IGroupCondition.Insert(int index, ICondition condition)
        {
            CheckModificationLock();

            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            if (index < 0 || index > SubConditions.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            InsertCondition(ResolveConditionModel(condition), index);
        }

        /// <inheritdoc />
        void IGroupCondition.Clear()
        {
            CheckModificationLock();

            for (int i = SubConditions.Count - 1; i >= 0; --i)
            {
                var subCondition = SubConditions[i];
                if (subCondition != null)
                    RemoveCondition(subCondition);
                else
                    m_SubConditions.RemoveAt(i);
            }
        }

        // Returns the public condition instance for a sub-condition: the user-authored condition for user
        // conditions, or the model itself otherwise. Mirrors what GetConditions yields for each element.
        static ICondition UnwrapCondition(ConditionModel subCondition)
        {
            return subCondition is IUserConditionModel { UserCondition: not null } userConditionModel
                ? userConditionModel.UserCondition
                : subCondition;
        }

        /// <inheritdoc />
        void IGroupCondition.Add(ICondition condition)
        {
            CheckModificationLock();

            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            AddCondition(ResolveConditionModel(condition));
        }

        /// <inheritdoc />
        void IGroupCondition.Remove(ICondition condition)
        {
            CheckModificationLock();

            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            var model = ResolveConditionModel(condition);

            if (model.Parent != this)
                throw new ArgumentException("Condition is not a child of this group.", nameof(condition));

            RemoveCondition(model);
        }

        static ConditionModel ResolveConditionModel(ICondition condition)
        {
            switch (condition)
            {
                case null:
                    throw new ArgumentNullException(nameof(condition));
                case ConditionModel model:
                    return model;
                case Condition userCondition:
                    return userCondition.GetImplementation();
                default:
                    throw new ArgumentException($"Unsupported condition type '{condition.GetType()}'.", nameof(condition));
            }
        }

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
            if (condition is GroupConditionModel group && IsSameOrDescendantOf(this, group))
                throw new ArgumentException("Cannot move a condition group into itself or into one of its own descendants.", nameof(condition));

            if (GraphModel != null)
                CheckVariableConditionsBelongToGraph(condition, GraphModel);

            if (condition.Parent != null)
            {
                condition.Parent.CheckModificationLock();
                condition.Parent.RemoveCondition(condition);
            }

            condition.SetParent(this);
            GraphModel?.RegisterCondition(condition);
            condition.Transition = Transition;

            m_SubConditions ??= new List<ConditionModel>();
            if (position == -1 || position >= m_SubConditions.Count)
                m_SubConditions.Add(condition);
            else
                m_SubConditions.Insert(position, condition);

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

        // True when `descendant` is `ancestor` itself or nested somewhere inside it -- the case where moving
        // `ancestor` into `descendant` would create a cycle.
        static bool IsSameOrDescendantOf(GroupConditionModel descendant, GroupConditionModel ancestor)
        {
            for (var current = descendant; current != null; current = current.Parent)
            {
                if (current == ancestor)
                    return true;
            }
            return false;
        }

        // A variable condition's variable is resolved by GUID against its own GraphModel, so moving it (or a
        // group containing it) into a different graph would silently make it unable to find its variable. The
        // check goes through CanUseVariableIn rather than VariableConditionModel.Variable, because a condition
        // sitting in a group that is not attached to a graph yet cannot resolve its own variable.
        static void CheckVariableConditionsBelongToGraph(ConditionModel condition, GraphModel graphModel)
        {
            switch (condition)
            {
                case GroupConditionModel group:
                    foreach (var subCondition in group.SubConditions)
                        CheckVariableConditionsBelongToGraph(subCondition, graphModel);
                    break;
                case VariableConditionModel variableCondition when !variableCondition.CanUseVariableIn(graphModel):
                    throw new ArgumentException("A variable condition's variable does not belong to this graph.", nameof(condition));
            }
        }

        /// <summary>
        /// Removes a condition from the list of sub-conditions.
        /// </summary>
        public void RemoveCondition(ConditionModel condition)
        {
            Assert.IsTrue(condition.Parent == this);
            bool removed = m_SubConditions.Remove(condition);
            Assert.IsTrue(removed);
            GraphModel?.UnregisterCondition(condition);
            condition.GraphModel = null;
            condition.SetParent(null);
            condition.Transition = null;
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
                if (originalGroup == this && position > IndexOf(condition))
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

        internal const DisplayMode DefaultDisplayMode = DisplayMode.Consise;

        internal string GetLabel()
        {
            return GroupOperation == Operation.And ? "AND" : "OR";
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
                if (condition == null)
                    continue;

                sb.Append(condition.ToString(indentLevel + 1));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            for (int i = 0; i < SubConditions.Count; i++)
            {
                if (SubConditions[i] != null)
                {
                    SubConditions[i].Transition = Transition;
                    SubConditions[i].SetParent(this);
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
