// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    internal interface IRecyclable
    {
        // This value is always set before the OnTrash() or OnReuse() is called.
        // Note: Should only be set by the Recycler
        bool isTrashed { get; set; }

        // Called when the object is sent to the recycler.
        // Not called when the object is disposed of by the visual tree.
        void OnTrash();

        // Called before the object goes out of the recyler.
        void OnReuse();
    }

    class Recycler
    {
        // Somewhat arbitrary limit to cap memory usage of a Recycler
        public const int MaxInstancesPerType = 500;

        private Dictionary<Type, Stack<IRecyclable>> m_ReusableStacks = new Dictionary<Type, Stack<IRecyclable>>();

        public void Trash(IRecyclable recyclable)
        {
            if (recyclable.isTrashed)
            {
                throw new ArgumentException("Trying to add an element to the Recycler more than once");
            }

            Type t = recyclable.GetType();

            // Create the stack if needed
            Stack<IRecyclable> reusables;
            if (!m_ReusableStacks.TryGetValue(t, out reusables))
            {
                reusables = new Stack<IRecyclable>();
                m_ReusableStacks.Add(t, reusables);
            }

            // recycle
            recyclable.isTrashed = true;
            recyclable.OnTrash();

            if (reusables.Count < MaxInstancesPerType)
                reusables.Push(recyclable);
        }

        public void Clear()
        {
            m_ReusableStacks.Clear();
        }

        public int Count
        {
            get
            {
                int totalCount = 0;
                var e = m_ReusableStacks.GetEnumerator();
                while (e.MoveNext())
                {
                    var stack = e.Current;
                    totalCount += stack.Value.Count;
                }
                return totalCount;
            }
        }

        public TType TryReuse<TType>() where TType : IRecyclable
        {
            // TODO: allocates types.
            var type = typeof(TType);
            Stack<IRecyclable> reusables;
            TType reusable = default(TType);
            if (m_ReusableStacks.TryGetValue(type, out reusables))
            {
                // If we find something return it
                if (reusables.Count > 0)
                {
                    reusable = (TType)reusables.Pop();
                    // about to be reused
                    reusable.isTrashed = false;
                    reusable.OnReuse();
                }
            }
            return reusable;
        }
    }
}
