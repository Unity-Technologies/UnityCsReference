// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.EditorTools
{
    abstract class EditorToolStateManager<TManager, TState> : ScriptableSingleton<TManager>, ISerializationCallbackReceiver where TManager: ScriptableObject where TState: EditorToolStateBase, new()
    {
        [SerializeField]
        TState m_DefaultState = null;

        public TState defaultState
        {
            get
            {
                if (m_DefaultState == null)
                {
                    m_DefaultState = new TState();
                    m_DefaultState.Initialize(typeof(SceneView));
                }

                return m_DefaultState;
            }
        }

        [SerializeField]
        List<TState> m_CustomStates = new();

        internal IReadOnlyCollection<TState> customStates => m_CustomStates;

        public virtual void OnEnable()
        {
            defaultState.OnEnable();
            foreach (var existingOwnerDef in EditorToolUtility.allToolOwnerDefinitions)
            {
                var ownerType = existingOwnerDef.toolOwnerType;
                if (ownerType != typeof(SceneView))
                {
                    var state = GetOrCreateStateForType(ownerType);
                    if (state != null)
                        state.OnEnable();
                }
            }
        }

        public virtual void OnDisable()
        {
            defaultState.OnDisable();
            foreach (var existingOwnerDef in EditorToolUtility.allToolOwnerDefinitions)
            {
                var ownerType = existingOwnerDef.toolOwnerType;
                if (ownerType != typeof(SceneView))
                {
                    var state = GetOrCreateStateForType(ownerType, dontCreate:true);
                    if (state != null)
                        state.OnDisable();
                }
            }
        }
        
        public TState GetOrCreateStateForType(Type ownerType, bool dontCreate = false)
        {
            if (ownerType == typeof(SceneView))
                return defaultState;

            TState matchingState = null;
            for (int i = m_CustomStates.Count - 1; i >= 0; i--)
            {
                var state = m_CustomStates[i];
                if (state != null && state.stateToolOwnerType == ownerType)
                {
                    matchingState = state;
                    break;
                }
            }

            if (matchingState != null)
                return matchingState;

            if (dontCreate)
                return null;

            var allOwnerDefs = EditorToolUtility.allToolOwnerDefinitions;
            foreach (var ownerDef in allOwnerDefs)
            {
                if (ownerDef.toolOwnerType == ownerType)
                {
                    var newState = new TState();
                    newState.Initialize(ownerType);
                    m_CustomStates.Add(newState);

                    return newState;
                }
            }
            
            return null;
        }
        
        public virtual void OnBeforeSerialize()
        {
        }

        public virtual void OnAfterDeserialize()
        {
            // This can happen when deserializing from asset that was serialized prior to tools becoming per-owner or when it's malformed.
            if (m_DefaultState != null &&
                (string.IsNullOrEmpty(m_DefaultState.stateOwnerWindowTypeName) ||
                 m_DefaultState.stateToolOwnerType != typeof(SceneView)))
            {
                m_DefaultState = new TState();
                m_DefaultState.Initialize(typeof(SceneView));
            }

            // Clean up custom states if needed. Example case would be when a tool owner window was removed from the code
            // but some users still have an old serialized state referencing it.
            if (m_CustomStates != null)
            {
                for (int i = m_CustomStates.Count - 1; i >= 0; i--)
                {
                    var state = m_CustomStates[i];
                    if (state == null || 
                        string.IsNullOrEmpty(state.stateOwnerWindowTypeName) ||
                        Type.GetType(state.stateOwnerWindowTypeName) == null)
                    {
                        m_CustomStates.RemoveAt(i);
                    }
                }
            }
        }
    }
    
    [Serializable]
    abstract class EditorToolStateBase
    {
        [SerializeField]
        string m_StateToolOwnerTypeName;

        public string stateOwnerWindowTypeName => m_StateToolOwnerTypeName;

        Type m_StateToolOwnerType;
        public Type stateToolOwnerType
        {
            get
            {
                if (m_StateToolOwnerType == null && !string.IsNullOrEmpty(m_StateToolOwnerTypeName))
                    m_StateToolOwnerType = Type.GetType(m_StateToolOwnerTypeName);

                return m_StateToolOwnerType;
            }
        }

        public Type defaultToolContextType
        {
            get
            {
                if (EditorToolUtility.GetToolOwnerDefinition(stateToolOwnerType, out var ownerDef))
                    return ownerDef.defaultContext;

                return typeof(GameObjectToolContext);
            }
        }

        public void Initialize(Type windowType)
        {
            m_StateToolOwnerTypeName = windowType.AssemblyQualifiedName;
        }
        
        public virtual void OnEnable() { }

        public virtual void OnDisable() { }
    }
}
