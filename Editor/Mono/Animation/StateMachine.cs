// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;

namespace UnityEditor.Animations
{
    internal struct PushUndoIfNeeded
    {
        public bool pushUndo
        {
            get { return impl.m_PushUndo; }
            set { impl.m_PushUndo = value; }
        }

        public PushUndoIfNeeded(bool pushUndo)
        {
            m_Impl = new PushUndoIfNeededImpl(pushUndo);
        }

        public void DoUndo(Object target, string undoOperation)
        {
            impl.DoUndo(target, undoOperation);
        }

        PushUndoIfNeededImpl impl
        {
            get
            {
                if (m_Impl == null)
                    m_Impl = new PushUndoIfNeededImpl(true);
                return m_Impl;
            }
        }

        PushUndoIfNeededImpl m_Impl;

        private class PushUndoIfNeededImpl
        {
            public PushUndoIfNeededImpl(bool pushUndo)
            {
                m_PushUndo = pushUndo;
            }

            public void DoUndo(Object target, string undoOperation)
            {
                if (m_PushUndo)
                {
                    Undo.RegisterCompleteObjectUndo(target, undoOperation);
                }
            }

            public bool m_PushUndo;
        };
    }


    public partial class AnimatorTransitionBase : Object
    {
        private PushUndoIfNeeded undoHandler =  new PushUndoIfNeeded(true);
        internal bool pushUndo { set { undoHandler.pushUndo = value; } }

        public void AddCondition(AnimatorConditionMode mode, float threshold, string parameter)
        {
            undoHandler.DoUndo(this, "Condition added");

            AnimatorCondition[] conditionVector = conditions;
            AnimatorCondition newCondition = new AnimatorCondition();
            newCondition.mode = mode;
            newCondition.parameter = parameter;
            newCondition.threshold = threshold;

            ArrayUtility.Add(ref conditionVector, newCondition);
            conditions = conditionVector;
        }

        public void RemoveCondition(AnimatorCondition condition)
        {
            undoHandler.DoUndo(this, "Condition removed");
            AnimatorCondition[] conditionVector = conditions;
            ArrayUtility.Remove(ref conditionVector, condition);
            conditions = conditionVector;
        }
    }


    internal class AnimatorDefaultTransition : ScriptableObject
    {
    }

    public partial class AnimatorState : Object
    {
        private PushUndoIfNeeded undoHandler = new PushUndoIfNeeded(true);
        internal bool pushUndo { set { undoHandler.pushUndo = value; } }


        public void AddTransition(AnimatorStateTransition transition)
        {
            undoHandler.DoUndo(this, "Transition added");

            AnimatorStateTransition[] transitionsVector = transitions;
            ArrayUtility.Add(ref transitionsVector, transition);
            transitions = transitionsVector;
        }

        public void RemoveTransition(AnimatorStateTransition transition)
        {
            undoHandler.DoUndo(this, "Transition removed");

            AnimatorStateTransition[] transitionsVector = transitions;
            ArrayUtility.Remove(ref transitionsVector, transition);
            transitions = transitionsVector;

            if (MecanimUtilities.AreSameAsset(this, transition))
                Undo.DestroyObjectImmediate(transition);
        }

        private AnimatorStateTransition CreateTransition(bool setDefaultExitTime)
        {
            AnimatorStateTransition newTransition = new AnimatorStateTransition();
            newTransition.hasExitTime = false;
            newTransition.hasFixedDuration = true;
            if (AssetDatabase.GetAssetPath(this) != "")
                AssetDatabase.AddObjectToAsset(newTransition, AssetDatabase.GetAssetPath(this));
            newTransition.hideFlags = HideFlags.HideInHierarchy;

            if (setDefaultExitTime)
                SetDefaultTransitionExitTime(ref newTransition);

            return newTransition;
        }

        private void SetDefaultTransitionExitTime(ref AnimatorStateTransition newTransition)
        {
            newTransition.hasExitTime = true;

            if (motion != null && motion.averageDuration > 0.0f)
            {
                const float transitionDefaultDuration = 0.25f;
                float transitionDurationNormalized = transitionDefaultDuration / motion.averageDuration;
                newTransition.duration = transitionDefaultDuration;
                newTransition.exitTime = 1.0f - transitionDurationNormalized;
            }
            else
            {
                newTransition.duration = 0.25f;
                newTransition.exitTime = 0.75f;
            }
        }

        public AnimatorStateTransition AddTransition(AnimatorState destinationState)
        {
            AnimatorStateTransition newTransition = CreateTransition(false);
            newTransition.destinationState = destinationState;
            AddTransition(newTransition);
            return newTransition;
        }

        public AnimatorStateTransition AddTransition(AnimatorStateMachine destinationStateMachine)
        {
            AnimatorStateTransition newTransition = CreateTransition(false);
            newTransition.destinationStateMachine = destinationStateMachine;
            AddTransition(newTransition);
            return newTransition;
        }

        public AnimatorStateTransition AddTransition(AnimatorState destinationState, bool defaultExitTime)
        {
            AnimatorStateTransition newTransition = CreateTransition(defaultExitTime);
            newTransition.destinationState = destinationState;
            AddTransition(newTransition);
            return newTransition;
        }

        public AnimatorStateTransition AddTransition(AnimatorStateMachine destinationStateMachine, bool defaultExitTime)
        {
            AnimatorStateTransition newTransition = CreateTransition(defaultExitTime);
            newTransition.destinationStateMachine = destinationStateMachine;
            AddTransition(newTransition);
            return newTransition;
        }

        public AnimatorStateTransition AddExitTransition()
        {
            return AddExitTransition(false);
        }

        public AnimatorStateTransition AddExitTransition(bool defaultExitTime)
        {
            AnimatorStateTransition newTransition = CreateTransition(defaultExitTime);
            newTransition.isExit = true;
            AddTransition(newTransition);
            return newTransition;
        }

        internal AnimatorStateMachine FindParent(AnimatorStateMachine root)
        {
            if (root.HasState(this, false)) return root;
            else return root.stateMachinesRecursive.Find(sm => sm.stateMachine.HasState(this, false)).stateMachine;
        }

        internal AnimatorStateTransition FindTransition(AnimatorState destinationState) // pp todo return a list?
        {
            return (new List<AnimatorStateTransition>(transitions)).Find(t => t.destinationState == destinationState);
        }

        [System.Obsolete("uniqueName does not exist anymore. Consider using .name instead.", true)]
        public string uniqueName
        {
            get { return ""; }
        }

        [System.Obsolete("GetMotion() is obsolete. Use motion", true)]
        public Motion GetMotion()
        {
            return null;
        }

        [System.Obsolete("uniqueNameHash does not exist anymore.", true)]
        public int uniqueNameHash
        {
            get { return -1; }
        }
    }

    public partial class AnimatorStateMachine : Object
    {
        private PushUndoIfNeeded undoHandler = new PushUndoIfNeeded(true);
        internal bool pushUndo { set { undoHandler.pushUndo = value; } }

        internal class StateMachineCache
        {
            static Dictionary<AnimatorStateMachine, ChildAnimatorStateMachine[]> m_ChildStateMachines;
            static bool m_Initialized;

            static void Init()
            {
                if (!m_Initialized)
                {
                    m_ChildStateMachines = new Dictionary<AnimatorStateMachine, ChildAnimatorStateMachine[]>();
                    m_Initialized = true;
                }
            }

            static public void Clear()
            {
                Init();
                m_ChildStateMachines.Clear();
            }

            static public ChildAnimatorStateMachine[] GetChildStateMachines(AnimatorStateMachine parent)
            {
                Init();

                ChildAnimatorStateMachine[] children;
                if (m_ChildStateMachines.TryGetValue(parent, out children) == false)
                {
                    children = parent.stateMachines;
                    m_ChildStateMachines.Add(parent, children);
                }
                return children;
            }
        }
        internal List<ChildAnimatorState> statesRecursive
        {
            get
            {
                List<ChildAnimatorState> ret = new List<ChildAnimatorState>();
                ret.AddRange(states);

                for (int j = 0; j < stateMachines.Length; j++)
                {
                    ret.AddRange(stateMachines[j].stateMachine.statesRecursive);
                }
                return ret;
            }
        }

        internal List<ChildAnimatorStateMachine> stateMachinesRecursive
        {
            get
            {
                List<ChildAnimatorStateMachine> ret = new List<ChildAnimatorStateMachine>();
                var childStateMachines = AnimatorStateMachine.StateMachineCache.GetChildStateMachines(this);
                ret.AddRange(childStateMachines);

                for (int j = 0; j < childStateMachines.Length; j++)
                {
                    ret.AddRange(childStateMachines[j].stateMachine.stateMachinesRecursive);
                }
                return ret;
            }
        }

        internal List<AnimatorStateTransition> anyStateTransitionsRecursive
        {
            get
            {
                List<AnimatorStateTransition> childTransitions = new List<AnimatorStateTransition>();
                childTransitions.AddRange(anyStateTransitions);

                foreach (ChildAnimatorStateMachine stateMachine in stateMachines)
                {
                    childTransitions.AddRange(stateMachine.stateMachine.anyStateTransitionsRecursive);
                }

                return childTransitions;
            }
        }

        internal Vector3 GetStatePosition(AnimatorState state)
        {
            ChildAnimatorState[] animatorStates = states;
            for (int i = 0; i < animatorStates.Length; i++)
                if (state == animatorStates[i].state)
                    return animatorStates[i].position;

            System.Diagnostics.Debug.Fail("Can't find state (" + state.name + ") in parent state machine (" + name + ").");
            return Vector3.zero;
        }

        internal void SetStatePosition(AnimatorState state, Vector3 position)
        {
            ChildAnimatorState[] childStates = states;
            for (int i = 0; i < childStates.Length; i++)
                if (state == childStates[i].state)
                {
                    childStates[i].position = position;
                    states = childStates;
                    return;
                }

            System.Diagnostics.Debug.Fail("Can't find state (" + state.name + ") in parent state machine (" + name + ").");
        }

        internal Vector3 GetStateMachinePosition(AnimatorStateMachine stateMachine)
        {
            ChildAnimatorStateMachine[] childSM = stateMachines;
            for (int i = 0; i < childSM.Length; i++)
                if (stateMachine == childSM[i].stateMachine)
                    return childSM[i].position;

            System.Diagnostics.Debug.Fail("Can't find state machine (" + stateMachine.name + ") in parent state machine (" + name + ").");

            return Vector3.zero;
        }

        internal void SetStateMachinePosition(AnimatorStateMachine stateMachine, Vector3 position)
        {
            ChildAnimatorStateMachine[] childSM = stateMachines;
            for (int i = 0; i < childSM.Length; i++)
                if (stateMachine == childSM[i].stateMachine)
                {
                    childSM[i].position = position;
                    stateMachines = childSM;
                    return;
                }

            System.Diagnostics.Debug.Fail("Can't find state machine (" + stateMachine.name + ") in parent state machine (" + name + ").");
        }

        public AnimatorState AddState(string name)
        {
            return AddState(name, states.Length > 0 ? states[states.Length - 1].position + new Vector3(35, 65) : new Vector3(200, 0, 0));
        }

        public AnimatorState AddState(string name, Vector3 position)
        {
            AnimatorState state = new AnimatorState();
            state.hideFlags = HideFlags.HideInHierarchy;
            state.name = MakeUniqueStateName(name);

            if (AssetDatabase.GetAssetPath(this) != "")
                AssetDatabase.AddObjectToAsset(state, AssetDatabase.GetAssetPath(this));

            AddState(state, position);

            return state;
        }

        public void AddState(AnimatorState state, Vector3 position)
        {
            ChildAnimatorState[] childStates = states;
            if (System.Array.Exists(childStates, childState => childState.state == state))
            {
                Debug.LogWarning(System.String.Format("State '{0}' already exists in state machine '{1}', discarding new state.", state.name, name));
                return;
            }

            undoHandler.DoUndo(this, "State added");
            ChildAnimatorState newState = new ChildAnimatorState();
            newState.state = state;
            newState.position = position;

            ArrayUtility.Add(ref childStates, newState);
            states = childStates;
        }

        public void RemoveState(AnimatorState state)
        {
            undoHandler.DoUndo(this, "State removed");
            undoHandler.DoUndo(state, "State removed");
            RemoveStateInternal(state);
        }

        public AnimatorStateMachine AddStateMachine(string name)
        {
            return AddStateMachine(name, Vector3.zero);
        }

        public AnimatorStateMachine AddStateMachine(string name, Vector3 position)
        {
            AnimatorStateMachine stateMachine = new AnimatorStateMachine();
            stateMachine.hideFlags = HideFlags.HideInHierarchy;
            stateMachine.name = MakeUniqueStateMachineName(name);

            AddStateMachine(stateMachine, position);

            if (AssetDatabase.GetAssetPath(this) != "")
                AssetDatabase.AddObjectToAsset(stateMachine, AssetDatabase.GetAssetPath(this));

            return stateMachine;
        }

        public void AddStateMachine(AnimatorStateMachine stateMachine, Vector3 position)
        {
            ChildAnimatorStateMachine[] childStateMachines = stateMachines;
            if (System.Array.Exists(childStateMachines, childStateMachine => childStateMachine.stateMachine == stateMachine))
            {
                Debug.LogWarning(System.String.Format("Sub state machine '{0}' already exists in state machine '{1}', discarding new state machine.", stateMachine.name, name));
                return;
            }

            undoHandler.DoUndo(this, "StateMachine " + stateMachine.name + " added");
            ChildAnimatorStateMachine newStateMachine = new ChildAnimatorStateMachine();
            newStateMachine.stateMachine = stateMachine;
            newStateMachine.position = position;

            ArrayUtility.Add(ref childStateMachines, newStateMachine);
            stateMachines = childStateMachines;
        }

        public void RemoveStateMachine(AnimatorStateMachine stateMachine)
        {
            undoHandler.DoUndo(this, "StateMachine removed");
            undoHandler.DoUndo(stateMachine, "StateMachine removed");
            RemoveStateMachineInternal(stateMachine);
        }

        private AnimatorStateTransition AddAnyStateTransition()
        {
            undoHandler.DoUndo(this, "AnyState Transition Added");

            AnimatorStateTransition[] transitionsVector = anyStateTransitions;
            AnimatorStateTransition newTransition = new AnimatorStateTransition();
            newTransition.hasExitTime = false;
            newTransition.hasFixedDuration = true;
            newTransition.duration = 0.25f;
            newTransition.exitTime = 0.75f;

            if (AssetDatabase.GetAssetPath(this) != "")
                AssetDatabase.AddObjectToAsset(newTransition, AssetDatabase.GetAssetPath(this));

            newTransition.hideFlags = HideFlags.HideInHierarchy;
            ArrayUtility.Add(ref transitionsVector, newTransition);
            anyStateTransitions = transitionsVector;


            return newTransition;
        }

        public AnimatorStateTransition AddAnyStateTransition(AnimatorState destinationState)
        {
            AnimatorStateTransition newTransition = AddAnyStateTransition();
            newTransition.destinationState = destinationState;
            return newTransition;
        }

        public AnimatorStateTransition AddAnyStateTransition(AnimatorStateMachine destinationStateMachine)
        {
            AnimatorStateTransition newTransition = AddAnyStateTransition();
            newTransition.destinationStateMachine = destinationStateMachine;
            return newTransition;
        }

        public bool RemoveAnyStateTransition(AnimatorStateTransition transition)
        {
            if ((new List<AnimatorStateTransition>(anyStateTransitions)).Any(t => t == transition))
            {
                undoHandler.DoUndo(this, "AnyState Transition Removed");

                AnimatorStateTransition[] transitionsVector = anyStateTransitions;
                ArrayUtility.Remove(ref transitionsVector, transition);
                anyStateTransitions = transitionsVector;

                if (MecanimUtilities.AreSameAsset(this, transition))
                    Undo.DestroyObjectImmediate(transition);

                return true;
            }

            return false;
        }

        internal void RemoveAnyStateTransitionRecursive(AnimatorStateTransition transition)
        {
            if (RemoveAnyStateTransition(transition))
                return;

            List<ChildAnimatorStateMachine> childStateMachines = stateMachinesRecursive;
            foreach (ChildAnimatorStateMachine sm in childStateMachines)
            {
                if (sm.stateMachine.RemoveAnyStateTransition(transition))
                    return;
            }
        }

        public AnimatorTransition AddStateMachineTransition(AnimatorStateMachine sourceStateMachine)
        {
            AnimatorStateMachine sm = null;
            return AddStateMachineTransition(sourceStateMachine, sm);
        }

        public AnimatorTransition AddStateMachineTransition(AnimatorStateMachine sourceStateMachine, AnimatorStateMachine destinationStateMachine)
        {
            undoHandler.DoUndo(this, "StateMachine Transition Added");

            AnimatorTransition[] transitionsVector = GetStateMachineTransitions(sourceStateMachine);
            AnimatorTransition newTransition = new AnimatorTransition();
            if (destinationStateMachine)
            {
                newTransition.destinationStateMachine = destinationStateMachine;
            }

            if (AssetDatabase.GetAssetPath(this) != "")
                AssetDatabase.AddObjectToAsset(newTransition, AssetDatabase.GetAssetPath(this));

            newTransition.hideFlags = HideFlags.HideInHierarchy;
            ArrayUtility.Add(ref transitionsVector, newTransition);
            SetStateMachineTransitions(sourceStateMachine, transitionsVector);

            return newTransition;
        }

        public AnimatorTransition AddStateMachineTransition(AnimatorStateMachine sourceStateMachine, AnimatorState destinationState)
        {
            AnimatorTransition newTransition = AddStateMachineTransition(sourceStateMachine);
            newTransition.destinationState = destinationState;
            return newTransition;
        }

        public AnimatorTransition AddStateMachineExitTransition(AnimatorStateMachine sourceStateMachine)
        {
            AnimatorTransition newTransition = AddStateMachineTransition(sourceStateMachine);
            newTransition.isExit = true;
            return newTransition;
        }

        public bool RemoveStateMachineTransition(AnimatorStateMachine sourceStateMachine, AnimatorTransition transition)
        {
            undoHandler.DoUndo(this, "StateMachine Transition Removed");

            AnimatorTransition[] transitionsVector = GetStateMachineTransitions(sourceStateMachine);
            int baseSize = transitionsVector.Length;
            ArrayUtility.Remove(ref transitionsVector, transition);
            SetStateMachineTransitions(sourceStateMachine, transitionsVector);

            if (MecanimUtilities.AreSameAsset(this, transition))
                Undo.DestroyObjectImmediate(transition);

            return baseSize != transitionsVector.Length;
        }

        private AnimatorTransition AddEntryTransition()
        {
            undoHandler.DoUndo(this, "Entry Transition Added");
            AnimatorTransition[] transitionsVector = entryTransitions;
            AnimatorTransition newTransition = new AnimatorTransition();

            if (AssetDatabase.GetAssetPath(this) != "")
                AssetDatabase.AddObjectToAsset(newTransition, AssetDatabase.GetAssetPath(this));

            newTransition.hideFlags = HideFlags.HideInHierarchy;
            ArrayUtility.Add(ref transitionsVector, newTransition);
            entryTransitions = transitionsVector;

            return newTransition;
        }

        public AnimatorTransition AddEntryTransition(AnimatorState destinationState)
        {
            AnimatorTransition newTransition = AddEntryTransition();
            newTransition.destinationState = destinationState;
            return newTransition;
        }

        public AnimatorTransition AddEntryTransition(AnimatorStateMachine destinationStateMachine)
        {
            AnimatorTransition newTransition = AddEntryTransition();
            newTransition.destinationStateMachine = destinationStateMachine;
            return newTransition;
        }

        public bool RemoveEntryTransition(AnimatorTransition transition)
        {
            if ((new List<AnimatorTransition>(entryTransitions)).Any(t => t == transition))
            {
                undoHandler.DoUndo(this, "Entry Transition Removed");
                AnimatorTransition[] transitionsVector = entryTransitions;
                ArrayUtility.Remove(ref transitionsVector, transition);
                entryTransitions = transitionsVector;

                if (MecanimUtilities.AreSameAsset(this, transition))
                    Undo.DestroyObjectImmediate(transition);

                return true;
            }

            return false;
        }

        internal ChildAnimatorState FindState(int nameHash)
        {
            return (new List<ChildAnimatorState>(states)).Find(s => s.state.nameHash == nameHash);
        }

        internal ChildAnimatorState FindState(string name)
        {
            return (new List<ChildAnimatorState>(states)).Find(s => s.state.name == name);
        }

        internal bool HasState(AnimatorState state)
        {
            return statesRecursive.Any(s => s.state == state);
        }

        internal bool IsDirectParent(AnimatorStateMachine stateMachine)
        {
            return stateMachines.Any(sm => sm.stateMachine == stateMachine);
        }

        internal bool HasStateMachine(AnimatorStateMachine child)
        {
            return stateMachinesRecursive.Any(sm => sm.stateMachine == child);
        }

        internal bool HasTransition(AnimatorState stateA, AnimatorState stateB)
        {
            return stateA.transitions.Any(t => t.destinationState == stateB) ||
                stateB.transitions.Any(t => t.destinationState == stateA);
        }

        internal AnimatorStateMachine FindParent(AnimatorStateMachine stateMachine)
        {
            if (stateMachines.Any(childSM => childSM.stateMachine == stateMachine))
                return this;
            else
                return stateMachinesRecursive.Find(sm => sm.stateMachine.stateMachines.Any(childSM => childSM.stateMachine == stateMachine)).stateMachine;
        }

        internal AnimatorStateMachine FindStateMachine(string path)
        {
            string[] smNames = path.Split('.');

            // first element is always Root statemachine 'this'
            AnimatorStateMachine currentSM = this;
            // last element is state name, we don't care
            for (int i = 1; i < smNames.Length - 1 && currentSM != null; ++i)
            {
                var childStateMachines = AnimatorStateMachine.StateMachineCache.GetChildStateMachines(currentSM);
                int index = System.Array.FindIndex(childStateMachines, t => t.stateMachine.name == smNames[i]);
                currentSM = index >= 0 ? childStateMachines[index].stateMachine : null;
            }

            return (currentSM == null) ? this : currentSM;
        }

        internal AnimatorStateMachine FindStateMachine(AnimatorState state)
        {
            if (HasState(state, false))
                return this;

            List<ChildAnimatorStateMachine> childStateMachines = stateMachinesRecursive;
            int index = childStateMachines.FindIndex(sm => sm.stateMachine.HasState(state, false));
            return index >= 0 ? childStateMachines[index].stateMachine : null;
        }

        internal AnimatorStateTransition FindTransition(AnimatorState destinationState)
        {
            return (new List<AnimatorStateTransition>(anyStateTransitions)).Find(t => t.destinationState == destinationState);
        }

        [System.Obsolete("stateCount is obsolete. Use .states.Length  instead.", true)]
        int stateCount
        {
            get { return 0; }
        }

        [System.Obsolete("stateMachineCount is obsolete. Use .stateMachines.Length instead.", true)]
        int stateMachineCount
        {
            get { return 0; }
        }

        [System.Obsolete("GetTransitionsFromState is obsolete. Use AnimatorState.transitions instead.", true)]
        AnimatorState GetTransitionsFromState(AnimatorState state)
        {
            return null;
        }

        [System.Obsolete("uniqueNameHash does not exist anymore.", true)]
        int uniqueNameHash
        {
            get { return -1; }
        }
    }
}
