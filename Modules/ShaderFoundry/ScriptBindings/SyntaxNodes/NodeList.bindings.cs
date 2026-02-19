// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/ListElementNode.h")]
    internal struct NodeList
    {
        NodeHandle m_Head;
        NodeHandle m_Tail;

        [NativeMethod(IsThreadSafe = true)] internal extern void Add(SyntaxTree syntaxTree, NodeHandle handle);

        // Only adds the given handle if it is valid
        internal void TryAdd(SyntaxTree syntaxTree, NodeHandle handle)
        {
            if (handle.IsValid)
                Add(syntaxTree, handle);
        }

        internal NodeListEnumerable GetEnumerable(SyntaxTree syntaxTree) => NodeListEnumerable.Construct(syntaxTree, this);
        internal void AddRange<T>(SyntaxTree syntaxTree, IEnumerable<T> items) where T : ISyntaxNode
        {
            foreach (var item in items)
                Add(syntaxTree, item.handle);
        }
    }

    [NativeHeader("Modules/ShaderFoundry/Parser/Nodes/ListElementNode.h")]
    internal struct NodeListEnumerable : IEnumerable<NodeHandle>
    {
        UIntPtr ast_Begin;
        UIntPtr node_Begin;
        UIntPtr ast_End;
        UIntPtr node_End;

        [NativeMethod(IsThreadSafe = true)] internal extern static NodeListEnumerable Construct(SyntaxTree syntaxTree, NodeList list);
        [NativeMethod(IsThreadSafe = true)] extern void MoveNext();
        [NativeMethod(IsThreadSafe = true)] extern bool Empty();
        [NativeMethod(IsThreadSafe = true)] extern NodeHandle Current();

        public IEnumerator<NodeHandle> GetEnumerator()
        {
            while (!Empty())
            {
                yield return Current();
                MoveNext();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
