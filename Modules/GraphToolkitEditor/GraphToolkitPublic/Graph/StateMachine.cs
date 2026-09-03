// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Represents the core definition of a state machine and defines its behavior.
    /// </summary>
    /// <remarks>
    /// <c>StateMachine</c> serves as the central entry point for:
    ///
    ///- Lifecycle management (via <see cref="OnEnable"/>, <see cref="OnDisable"/>)
    ///- Change tracking (via <see cref="OnStateMachineChanged"/>)
    ///- Access to states and variables
    ///
    /// To register a state machine type and associate it with a custom file extension and configuration options,
    /// apply the <see cref="StateMachineAttribute"/> to your custom <c>StateMachine</c> class.
    ///
    /// You can further control the state machine's behavior using the
    /// <see cref="StateMachineOptions"/> enum, which defines traits.
    ///
    /// Use the <see cref="StateMachineDatabase"/> utility class to create, load, and save state machine assets in the Unity Editor.
    /// State machines are serialized assets. You can create them through the editor UI with
    /// <see cref="StateMachineDatabase.PromptInProjectBrowserToCreateNewAsset{T}"/> or load them from disk with
    /// <see cref="StateMachineDatabase.LoadStateMachine{T}"/>.
    /// </remarks>
    [Serializable]
    public partial class StateMachine
    {
        /// <summary>
        /// The name of the state machine.
        /// </summary>
        public string Name
        {
            get
            {
                CheckImplementation();
                return m_Implementation.Name;
            }
        }

        /// <summary>
        /// The number of <see cref="IVariable"/>s declared in the state machine.
        /// </summary>
        public int VariableCount
        {
            get
            {
                CheckImplementation();
                return m_Implementation.VariableModels.Count;
            }
        }


        /// <summary>
        /// The number of <see cref="IState"/>s in the state machine.
        /// </summary>
        public int StateCount
        {
            get
            {
                CheckImplementation();
                return m_Implementation.States.Count;
            }
        }

        /// <summary>
        /// The globally unique identifier for this state machine.
        /// </summary>
        public Hash128 ID
        {
            get
            {
                CheckImplementation();
                return m_Implementation.Guid;
            }
        }

        /// <summary>
        /// The `GUID` of the asset file associated with this state machine.
        /// </summary>
        /// <remarks>
        /// For state machines that are persistent assets, this property contains the valid unique identifier of the
        /// asset file on disk.
        /// </remarks>
        public GUID AssetGuid
        {
            get
            {
                CheckImplementation();
                return m_Implementation.GetGraphReference(true).AssetGuid;
            }
        }

        /// <summary>
        /// The set of types offered in the blackboard for variable creation.
        /// </summary>
        /// <remarks>
        /// Override <see cref="BuildAvailableVariableTypes"/> to customize this set.
        /// This controls what the UI offers, not what can be created. Variables of any type can still be created
        /// programmatically via <see cref="CreateVariable(string, Type, object, VariableKind)"/> regardless of this set.
        /// </remarks>
        public IReadOnlyCollection<Type> AvailableVariableTypes
        {
            get
            {
                CheckImplementation();
                return m_Implementation.AvailableVariableTypes;
            }
        }

        /// <summary>
        /// Builds the set of variable types offered in the blackboard for variable creation.
        /// </summary>
        /// <returns>The types to offer in the blackboard for variable creation, or <see langword="null"/> to offer none.</returns>
        /// <remarks>
        /// Override this method to define the set of variable types available from the blackboard. The default implementation returns
        /// an empty set.
        /// This controls what the UI offers, not what can be created. Variables of any type can still be created
        /// programmatically via <see cref="CreateVariable(string, Type, object, VariableKind)"/> regardless of this set.
        /// </remarks>
        /// <example>
        /// <code><![CDATA[
        /// protected override IEnumerable<Type> BuildAvailableVariableTypes()
        /// {
        ///     yield return typeof(float);
        ///     yield return typeof(int);
        ///     yield return typeof(bool);
        /// }
        /// ]]></code>
        /// </example>
        protected virtual IEnumerable<Type> BuildAvailableVariableTypes()
            => null;

        /// <summary>
        /// Creates and adds a new variable to the state machine.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="valueType">The data type of the variable.</param>
        /// <param name="defaultValue">The default value. Must be compatible with <paramref name="valueType"/> and work with Unity serialization rules for <see cref="SerializeField"/>.</param>
        /// <param name="kind">The kind of variable, defined by <see cref="VariableKind"/>.</param>
        /// <returns>The newly created variable.</returns>
        /// <remarks>
        /// Enclose this method with <see cref="UndoBeginRecordStateMachine"/> and <see cref="UndoEndRecordStateMachine"/> to
        /// add this operation to the undo stack and to update the graph view with the changes.
        /// </remarks>
        public IVariable CreateVariable(string name, Type valueType, object defaultValue = null, VariableKind kind = VariableKind.Local)
        {
            CheckImplementation();
            return m_Implementation.CreateVariable(name, valueType, defaultValue, kind);
        }

        /// <summary>
        /// Creates and adds a new variable to the state machine.
        /// </summary>
        /// <typeparam name="T">The data type of the variable.</typeparam>
        /// <param name="name">The name of the variable.</param>
        /// <param name="defaultValue">The default value. Must be compatible with <typeparamref name="T"/> and work with Unity serialization rules for <see cref="SerializeField"/>.</param>
        /// <param name="kind">The kind of variable, defined by <see cref="VariableKind"/>.</param>
        /// <returns>The newly created variable.</returns>
        /// <remarks>
        /// Enclose this method with <see cref="UndoBeginRecordStateMachine"/> and <see cref="UndoEndRecordStateMachine"/> to
        /// add this operation to the undo stack and to update the graph view with the changes.
        /// </remarks>
        public IVariable CreateVariable<T>(string name, T defaultValue = default, VariableKind kind = VariableKind.Local)
        {
            return CreateVariable(name, typeof(T), defaultValue, kind);
        }

        /// <summary>
        /// Removes a variable from the state machine.
        /// </summary>
        /// <param name="variable">The variable to remove. Must belong to this graph.</param>
        /// <param name="forceRemove">If true, removes the variable and all variable nodes referencing it. If false, removal fails if nodes exist.</param>
        /// <returns>True if the variable was removed; otherwise false.</returns>
        /// <remarks>
        /// Enclose this method with <see cref="UndoBeginRecordStateMachine"/> and <see cref="UndoEndRecordStateMachine"/> to
        /// add this operation to the undo stack and to update the graph view with the changes.
        /// </remarks>
        public bool RemoveVariable(IVariable variable, bool forceRemove = false)
        {
            CheckImplementation();
            return m_Implementation.RemoveVariable(variable, forceRemove);
        }

        /// <summary>
        /// Retrieves a variable declared in the state machine by index.
        /// </summary>
        /// <param name="index">The index of the variable to retrieve.</param>
        /// <returns>The <see cref="IVariable"/> at the specified index.</returns>
        /// <remarks>
        /// The index is zero-based and reflects the order in which the variables were created.
        /// The index must be within the valid range of the variable list (see: <see cref="VariableCount"/>).
        /// </remarks>
        public IVariable GetVariable(int index)
        {
            CheckImplementation();
            return m_Implementation.VariableModels[index];
        }

        /// <summary>
        /// Retrieves all variables declared in the state machine.
        /// </summary>
        /// <returns>An <c>IEnumerable</c> of all <see cref="IVariable"/>s declared in the state machine.</returns>
        /// <remarks>
        /// The collection reflects the variables as declared, in their order of creation.
        /// To get the variables in a specific order, use <see cref="GetVariables(SortMethod)"/>.
        /// </remarks>
        public IEnumerable<IVariable> GetVariables()
        {
            CheckImplementation();
            return m_Implementation.VariableModels;
        }

        /// <summary>
        /// Retrieves all variables declared in the state machine in a specific order using <see cref="SortMethod"/>.
        /// </summary>
        /// <param name="sort">The sorting method.</param>
        /// <returns>An <c>IEnumerable</c> of all <see cref="IVariable"/>s declared in the state machine, ordered using the provided <see cref="SortMethod"/>.</returns>
        /// <remarks>
        ///- The <see cref="SortMethod.Creation"/> option returns variables in their order of creation.
        ///- The <see cref="SortMethod.Display"/> option returns variables in the order they are displayed in the blackboard.
        /// </remarks>
        public IEnumerable<IVariable> GetVariables(SortMethod sort)
        {
            CheckImplementation();

            switch (sort)
            {
                case SortMethod.Creation:
                    return m_Implementation.VariableModels;
                case SortMethod.Display:
                    return m_Implementation.VariableModelsByDisplayOrder;
                default:
                    throw new ArgumentException("Not expected sort method", nameof(sort));
            }
        }

        /// <summary>
        /// Adds a state to the state machine.
        /// </summary>
        /// <param name="state">The state to add.</param>
        /// <remarks>
        /// If the state is already in this state machine, this method does nothing.
        /// If the state is currently in another state machine, it is removed from that state machine and added to this one.
        /// A state type is compatible when it is decorated with <see cref="UseWithStateMachineAttribute"/> for this state machine type,
        /// or when it is defined in the same assembly as the state machine (unless
        /// <see cref="StateMachineOptions.DisableAutoInclusionOfStatesFromStateMachineAssembly"/> is set).
        /// Enclose this method with <see cref="UndoBeginRecordStateMachine"/> and <see cref="UndoEndRecordStateMachine"/> to
        /// add this operation to the undo stack and to update the graph view with the changes.
        /// </remarks>
        public void AddState(State state)
        {
            CheckImplementation();
            m_Implementation.AddState(state);
        }

        /// <summary>
        /// Removes a state from the state machine.
        /// </summary>
        /// <param name="state">The state to remove. Must belong to this state machine.</param>
        /// <remarks>
        /// Removing a state also removes any transitions connected to it.
        /// Enclose this method with <see cref="UndoBeginRecordStateMachine"/> and <see cref="UndoEndRecordStateMachine"/> to
        /// add this operation to the undo stack and to update the graph view with the changes.
        /// </remarks>
        public void RemoveState(IState state)
        {
            CheckImplementation();
            m_Implementation.RemoveState(state);
        }

        /// <summary>
        /// Retrieves a state in the state machine by its index.
        /// </summary>
        /// <param name="index">The zero-based index of the state to retrieve.</param>
        /// <returns>The <see cref="IState"/> at the specified index.</returns>
        /// <remarks>
        /// Use this method to access a state based on its creation order in the state machine. The index is zero-based and must be within range (see: <see cref="StateCount"/>).
        /// </remarks>
        public IState GetState(int index)
        {
            CheckImplementation();
            return m_Implementation.States[index];
        }

        /// <summary>
        /// Retrieves all states in the state machine.
        /// </summary>
        /// <returns>An <c>IEnumerable</c> of all <see cref="IState"/>s in the state machine.</returns>
        /// <remarks>
        /// Use this method to access every state in the state machine. States are returned in the order they were created.
        ///
        /// The list includes:
        ///
        ///- Your own <see cref="State"/>s
        ///- <see cref="ISubgraphState"/>s
        ///
        /// </remarks>
        public IEnumerable<IState> GetStates()
        {
            CheckImplementation();
            return m_Implementation.States;
        }

        /// <summary>
        /// Called when the state machine is created or loaded in the editor.
        /// </summary>
        /// <remarks>
        /// Override this method to perform setup tasks, such as allocating resources, initializing internal state, or
        /// preparing data for editing. This method is invoked each time the graph becomes active in the editor,
        /// including after domain reload or when reopening the asset.
        /// </remarks>
        public virtual void OnEnable() { }

        /// <summary>
        /// Called when the state machine is unloaded, or goes out of scope in the editor.
        /// </summary>
        /// <remarks>
        /// Override this method to release resources, clear temporary data, or perform any required cleanup.
        /// </remarks>
        public virtual void OnDisable() { }

        /// <summary>
        /// Called after the state machine has changed.
        /// </summary>
        /// <param name="stateMachineLogger">The <see cref="StateMachineLogger"/> that receives any errors or warnings related to the state machine,
        /// and provides access to information about what changes (added/deleted/modified states).</param>
        /// <remarks>
        /// Unity calls this method after any change to the state machine. Override it to validate the state machine's
        /// integrity and report issues using the provided <see cref="StateMachineLogger"/>,  or react to specific changes via
        /// <see cref="StateMachineLogger.StateMachineChanges"/>.
        /// Use this method to detect invalid configurations, highlight issues in the editor, or provide user feedback.
        /// You can iterate the changed states efficiently without checking all states in the graph.
        ///
        /// Do not modify the state machine within this method, as it may cause instability or recursive updates.
        /// </remarks>
        public virtual void OnStateMachineChanged(StateMachineLogger stateMachineLogger) { }

        /// <summary>
        /// Signals the beginning of an undoable operation.
        /// </summary>
        /// <param name="actionName">The name of the operation, which is displayed in the undo menu.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if an undo operation has already been registered to the graph.
        /// </exception>
        /// <remarks>
        /// Call this before you trigger a sequence of graph modification methods to record those operations to the undo stack.
        /// Call <see cref="UndoEndRecordStateMachine"/> after the sequence to signal that the operation is complete.
        /// </remarks>
        public void UndoBeginRecordStateMachine(string actionName)
        {
            CheckImplementation();
            m_Implementation.UndoBeginRecordGraph(actionName);
        }

        /// <summary>
        /// Signals the beginning of an undoable operation and opts specific conditions into full serialized-state
        /// capture.
        /// </summary>
        /// <param name="actionName">The name of the operation, which is displayed in the undo menu.</param>
        /// <param name="conditionsToRecord">
        /// The conditions whose serialized state you plan to mutate inside this scope. Each condition must belong to
        /// this state machine.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if an undo operation has already been registered to the state machine.
        /// </exception>
        /// <remarks>
        /// Use this overload when your custom <see cref="Condition"/> subclasses expose serialized fields that you
        /// mutate directly, for example from a <see cref="ConditionView{T}"/> callback, rather than through the
        /// built-in editing controls.
        ///
        /// Passing conditions here has two effects at <see cref="UndoEndRecordStateMachine"/> time:
        /// <list type="bullet">
        /// <item><description>
        /// Unity's undo system captures the full serialized state of the containing state machine asset, so undo
        /// restores the previous values of your serialized fields.
        /// </description></item>
        /// <item><description>
        /// The conditions are reported as changed and the state machine asset is marked dirty.
        /// </description></item>
        /// </list>
        ///
        /// <para>
        /// The change-notification side effects run only when the state machine is currently displayed in a graph
        /// window. From an editor script with no open window, undo capture still occurs and undo restores your
        /// serialized values as expected, but the asset is not automatically marked dirty. To persist your changes in
        /// that case, call <see cref="StateMachineDatabase.SaveStateMachine"/> explicitly.
        /// </para>
        ///
        /// Throws <see cref="InvalidOperationException"/> when an undo operation has already been registered to the
        /// state machine. If you only call the built-in modification methods, use the
        /// <see cref="UndoBeginRecordStateMachine(string)"/> overload instead.
        /// </remarks>
        /// <example>
        /// <code lang="cs">
        /// <![CDATA[
        /// class HealthConditionView : ConditionView<HealthCondition>
        /// {
        ///     protected override bool DisplayValueField => false;
        ///
        ///     public override void OnViewBuilt()
        ///     {
        ///         var slider = new Slider(0f, 100f) { value = Condition.Value };
        ///         slider.RegisterValueChangedCallback(evt =>
        ///         {
        ///             // Record the write so it is undoable and marks the asset dirty.
        ///             var stateMachine = Condition.StateMachine;
        ///             stateMachine.UndoBeginRecordStateMachine("Change health threshold", Condition);
        ///             Condition.Value = evt.newValue;
        ///             stateMachine.UndoEndRecordStateMachine();
        ///         });
        ///         View.Root.Add(slider);
        ///     }
        /// }
        /// ]]>
        /// </code>
        /// </example>
        public void UndoBeginRecordStateMachine(string actionName, params Condition[] conditionsToRecord)
        {
            CheckImplementation();
            m_Implementation.UndoBeginRecordGraph(actionName, conditionsToRecord);
        }

        /// <summary>
        /// Signals the end of an undoable operation. Sends the undo data to the editor undo system and refreshes the
        /// graph view with the changes made.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is no undo operation currently registered to the graph.
        /// </exception>
        /// <remarks>
        /// Call this after you trigger a sequence of graph modification methods to finalize recording the operations to
        /// the undo stack. Call <see cref="UndoBeginRecordStateMachine"/> before the sequence to signal the operations to be recorded.
        /// </remarks>
        public void UndoEndRecordStateMachine()
        {
            CheckImplementation();
            m_Implementation.UndoEndRecordGraph();
        }

        /// <summary>
        /// Retrieves the transitions that connect one state to another.
        /// </summary>
        /// <param name="fromState">The state the transitions originate from.</param>
        /// <param name="toState">The state the transitions go to.</param>
        /// <returns>
        /// An <c>IEnumerable</c> of the <see cref="ITransition"/>s that go from <paramref name="fromState"/> to
        /// <paramref name="toState"/>, or an empty sequence if there is none.
        /// </returns>
        /// <remarks>
        /// This is the state machine equivalent of retrieving the wire between two ports. Only transitions that
        /// leave <paramref name="fromState"/> and enter <paramref name="toState"/> are returned; the opposite
        /// direction is not included. To enumerate every transition on a single state, use
        /// <see cref="IState.GetIncomingTransitions"/> and <see cref="IState.GetOutgoingTransitions"/>.
        /// </remarks>
        public IEnumerable<ITransition> GetTransitions(IState fromState, IState toState)
        {
            CheckImplementation();
            return m_Implementation.GetTransitions(fromState, toState);
        }

        /// <summary>
        /// Creates a transition from one state to another.
        /// </summary>
        /// <param name="fromState">The state the transition originates from. Must belong to this state machine.</param>
        /// <param name="toState">The state the transition goes to. Must belong to this state machine.</param>
        /// <returns>The <see cref="ITransition"/> the connection was made on. If <paramref name="fromState"/> and
        /// <paramref name="toState"/> are the same state, the returned transition is a self transition
        /// (an <see cref="ISelfTransition"/>), and it is the state's existing self transition when it already has
        /// one.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="fromState"/> or <paramref name="toState"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown if either state does not belong to this state machine.</exception>
        /// <remarks>
        /// This is the state machine equivalent of connecting two ports with a wire. Between two different states, a
        /// new transition is created on every call, so calling this method twice on the same pair of states produces
        /// two distinct transitions, each seeded with a single empty rule.
        ///
        /// A state holds a single self transition, so when <paramref name="fromState"/> and <paramref name="toState"/>
        /// are the same state and that state already has a self transition, no second transition is created: a new
        /// rule is added to the existing self transition, which is returned. To retrieve the rule that was added, read the last
        /// rule of the returned transition. Use <see cref="ITransition.GetRules"/> to inspect the rules of a
        /// transition.
        ///
        /// Enclose this method with <see cref="UndoBeginRecordStateMachine"/> and <see cref="UndoEndRecordStateMachine"/> to
        /// add this operation to the undo stack and to update the graph view with the changes.
        /// Throws <see cref="ArgumentNullException"/> when <paramref name="fromState"/> or <paramref name="toState"/> is <c>null</c>.
        /// Throws <see cref="ArgumentException"/> when either state does not belong to this state machine.
        /// </remarks>
        /// <example>
        /// The following example connects two states, then gives the second state a self transition with two rules.
        /// <code>
        /// <![CDATA[
        /// void BuildTransitions(StateMachine stateMachine, IState idle, IState patrol)
        /// {
        ///     stateMachine.UndoBeginRecordStateMachine("Build transitions");
        ///
        ///     // Idle -> Patrol, a new transition holding one rule.
        ///     stateMachine.Connect(idle, patrol);
        ///
        ///     // Patrol -> Patrol, a self transition holding one rule.
        ///     var patrolLoop = stateMachine.Connect(patrol, patrol);
        ///
        ///     // Patrol already has a self transition, so this adds a second rule to it rather than
        ///     // creating another transition: it returns patrolLoop, whose RuleCount is now 2.
        ///     stateMachine.Connect(patrol, patrol);
        ///
        ///     stateMachine.UndoEndRecordStateMachine();
        /// }
        /// ]]>
        /// </code>
        /// </example>
        /// <seealso cref="GetTransitions"/>
        /// <seealso cref="Disconnect"/>
        public ITransition Connect(IState fromState, IState toState)
        {
            CheckImplementation();
            return m_Implementation.Connect(fromState, toState);
        }

        /// <summary>
        /// Removes all transitions that go from one state to another.
        /// </summary>
        /// <param name="fromState">The state the transitions originate from. Must belong to this state machine.</param>
        /// <param name="toState">The state the transitions go to. Must belong to this state machine.</param>
        /// <returns><c>true</c> if at least one transition existed and was removed; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="fromState"/> or <paramref name="toState"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown if either state does not belong to this state machine.</exception>
        /// <remarks>
        /// Only transitions that leave <paramref name="fromState"/> and enter <paramref name="toState"/> are removed;
        /// transitions in the opposite direction are left untouched. When <paramref name="fromState"/> and
        /// <paramref name="toState"/> are the same state, self transitions on that state are removed, together with
        /// all the rules they hold. To remove a single rule and keep the transition, use
        /// <see cref="ITransition.RemoveRule"/> instead.
        /// Enclose this method with <see cref="UndoBeginRecordStateMachine"/> and <see cref="UndoEndRecordStateMachine"/> to
        /// add this operation to the undo stack and to update the graph view with the changes.
        /// Throws <see cref="ArgumentNullException"/> when <paramref name="fromState"/> or <paramref name="toState"/> is <c>null</c>.
        /// Throws <see cref="ArgumentException"/> when either state does not belong to this state machine.
        /// </remarks>
        /// <example>
        /// The following example removes every transition that goes from one state to another.
        /// <code>
        /// <![CDATA[
        /// void ClearTransitions(StateMachine stateMachine, IState idle, IState patrol)
        /// {
        ///     stateMachine.UndoBeginRecordStateMachine("Clear transitions");
        ///
        ///     // Removes all Idle -> Patrol transitions, and returns true if there was at least one.
        ///     // Patrol -> Idle transitions are left untouched.
        ///     stateMachine.Disconnect(idle, patrol);
        ///
        ///     // Removes the self transition on Patrol, with all the rules it holds.
        ///     stateMachine.Disconnect(patrol, patrol);
        ///
        ///     stateMachine.UndoEndRecordStateMachine();
        /// }
        /// ]]>
        /// </code>
        /// </example>
        /// <seealso cref="GetTransitions"/>
        /// <seealso cref="Connect"/>
        public bool Disconnect(IState fromState, IState toState)
        {
            CheckImplementation();
            return m_Implementation.Disconnect(fromState, toState);
        }
    }
}
