// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System;
using System.Text;
using Unity.Jobs.LowLevel.Unsafe;
using System.Diagnostics;
using UnityEngine.Bindings;

namespace UnityEngine.TextCore.Text
{
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal class TextHandlePermanentCache
    {
        internal LinkedList<TextCacheEntry> s_Cache = new LinkedList<TextCacheEntry>();
        private object syncRoot = new object();

        public void AddToCache(TextHandle textHandle)
        {
            lock (syncRoot)
            {
                if (textHandle.IsCachedPermanent)
                    return;

                if (textHandle.IsCachedTemporary)
                {
                    textHandle.RemoveFromTemporaryCache();
                }

                if (s_Cache.Count > 0)
                {
                    textHandle.TextInfoNode = s_Cache.Last;
                    textHandle.TextInfoNode.SetTextHandle(textHandle);
                    s_Cache.RemoveLast();
                }
                else
                {
                    textHandle.TextInfoNode = new LinkedListNode<TextCacheEntry>(new TextCacheEntry(textHandle,  new TextInfo()));
                }
            }

            textHandle.IsCachedPermanent = true;
            textHandle.SetDirty();
            textHandle.Update();
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        public void RemoveFromCache(TextHandle textHandle)
        {
            lock (syncRoot)
            {
                if (!textHandle.IsCachedPermanent)
                    return;

                s_Cache.AddFirst(textHandle.TextInfoNode);
                ResetEntryState(textHandle);
            }
        }

        internal void ResetEntryState(TextHandle handle)
        {
            if (!handle.IsCachedPermanent)
                return;

            handle.IsCachedPermanent = false;
            handle.TextInfoNode.SetTime(0f);
            handle.TextInfoNode.SetTextHandle(null);
            handle.TextInfoNode = null;
        }
    }
}
