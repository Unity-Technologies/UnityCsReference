// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Audio
{
    [NativeType(Header = "Modules/DSPGraph/Public/DSPGraphHandles.h")]
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct Handle : IHandle<Handle>
    {
        [NativeDisableUnsafePtrRestriction]
        private IntPtr m_Node;
        public int Version;

        public Node* AtomicNode
        {
            get { return (Node*)m_Node;  }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                m_Node = (IntPtr)value;
                Version = value->Version;
            }
        }

        public int Id
        {
            get { return Valid ? AtomicNode->Id : Node.InvalidId; }
            set
            {
                if (value == Node.InvalidId)
                    throw new ArgumentException("Invalid ID");
                if (!Valid)
                    throw new InvalidOperationException("Handle is invalid or has been destroyed");
                if (AtomicNode->Id != Node.InvalidId)
                    throw new InvalidOperationException($"Trying to overwrite id on live node {AtomicNode->Id}");
                AtomicNode->Id = value;
            }
        }

        public Handle(Node* node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (node->Id != Node.InvalidId)
                throw new InvalidOperationException($"Reusing unflushed node {node->Id}");
            Version = node->Version;
            this.m_Node = (IntPtr)node;
        }

        public void FlushNode()
        {
            if (!Valid)
                throw new InvalidOperationException("Attempting to flush invalid audio handle");
            AtomicNode->Id = Node.InvalidId;
            ++AtomicNode->Version;
        }

        public bool Equals(Handle other)
        {
            return m_Node == other.m_Node && Version == other.Version;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Handle && Equals((Handle)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)m_Node * 397) ^ Version;
            }
        }

        public bool Valid => m_Node != IntPtr.Zero && AtomicNode->Version == Version;
        public bool Alive => Valid && AtomicNode->Id != Node.InvalidId;

        [StructLayout(LayoutKind.Sequential)]
        internal struct Node
        {
            public long Next;
            public int Id;
            public int Version;
            public int DidAllocate;
            public const int InvalidId = -1;
        }
    }
}

