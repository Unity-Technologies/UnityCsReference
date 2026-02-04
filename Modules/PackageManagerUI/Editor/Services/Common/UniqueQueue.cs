// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    // We created this unique queue class that removes duplicates in the queue and has performant removal of items from Queue.
    // For now, we use it in our product info fetching queue. The UI will add items to the queue and remove items from the queue
    // frequently as we scroll through the list, so having performant removal is important.
    internal sealed class UniqueQueue<T>
    {
        private readonly Queue<T> m_Queue;
        private readonly HashSet<T> m_HashSet;

        public UniqueQueue(IEnumerable<T> collection = null)
        {
            m_Queue = new Queue<T>();
            m_HashSet = new HashSet<T>();

            foreach (var item in collection ?? Array.Empty<T>())
                Enqueue(item);
        }

        public int Count => m_HashSet.Count;

        public bool Enqueue(T item)
        {
            if (!m_HashSet.Add(item))
                return false;
            m_Queue.Enqueue(item);
            return true;
        }

        public bool TryDequeue(out T item)
        {
            while (m_Queue.Count > 0)
            {
                item = m_Queue.Dequeue();
                if (m_HashSet.Remove(item))
                    return true;
            }
            item = default!;
            return false;
        }

        public IEnumerable<T> DequeueAll()
        {
            while (m_Queue.Count > 0)
            {
                var item = m_Queue.Dequeue();
                if (m_HashSet.Remove(item))
                    yield return item;
            }
        }

        public void Clear()
        {
            m_Queue.Clear();
            m_HashSet.Clear();
        }

        public bool Remove(T item)
        {
            // For quicker removal, we only remove it from the HashSet. The removal of the item in the Queue happens in `TryDequeue`
            return m_HashSet.Remove(item);
        }
    }
}
