// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    internal abstract class NodeOutput : NodeParameter { }

    [Serializable]
    internal class NodeOutput<T> : NodeOutput
    {
        [SerializeReference] private Node m_Node;
        [SerializeField] private T m_Value;

        // These properties and methods are not supposed to be used by the nodes themselves, but by the graph.
        // Because nodes code have internal access we hide them with the EditorBrowsable attribute.

        [EditorBrowsable(EditorBrowsableState.Never)] internal override Node GetNode() => m_Node;

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

        internal NodeOutput(Node node)
        {
            m_Node = node;
        }
    }
}
