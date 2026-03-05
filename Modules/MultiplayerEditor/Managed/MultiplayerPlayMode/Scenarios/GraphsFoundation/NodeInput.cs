// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using UnityEngine.Assertions;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    internal abstract class NodeInput : NodeParameter
    {
        internal abstract NodeOutput GetSource();
        internal abstract void Flush();
        internal abstract void Reset();
    }

    [Serializable]
    internal class NodeInput<T> : NodeInput
    {
        [SerializeReference] private ExecutionNode m_Node;
        [SerializeField] private T m_Value;
        [SerializeReference] private NodeOutput<T> m_Source;

        // These properties and methods are not supposed to be used by the nodes themselves, but by the graph.
        // Because nodes code have internal access we hide them with the EditorBrowsable attribute.

        [EditorBrowsable(EditorBrowsableState.Never)] internal override ExecutionNode GetNode() => m_Node;
        [EditorBrowsable(EditorBrowsableState.Never)] internal override NodeOutput GetSource() => m_Source;

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal override S GetValue<S>()
        {
            if (!typeof(S).IsAssignableFrom(typeof(T)))
                throw new InvalidCastException($"Cannot cast {m_Value.GetType()} to {typeof(S)}");

            if (m_Value == null || m_Value is not S value)
                return default;

            return value;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal override void SetValue<S>(S value)
        {
            if (!typeof(T).IsAssignableFrom(typeof(S)))
                throw new InvalidCastException($"Cannot cast {value.GetType()} to {typeof(T)}");

            if (value == null || value is not T castedValue)
            {
                m_Value = default;
                return;
            }

            m_Value = castedValue;
        }

        internal NodeInput(ExecutionNode node)
        {
            m_Node = node;
        }

        internal override void Flush()
        {
            Assert.IsNotNull(m_Source, "Node input is not connected to any output");
            m_Value = m_Source.GetValue<T>();
        }

        internal void Connect(NodeOutput<T> output)
        {
            m_Source = output;
        }

        internal override void Reset()
        {
            m_Value = default;
        }
    }
}
