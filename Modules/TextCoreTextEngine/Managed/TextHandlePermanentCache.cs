// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
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

                if (textHandle.IsCachedPermanentTextCore)
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

            textHandle.IsCachedPermanentTextCore = true;
            textHandle.IsCachedPermanent = true;
            textHandle.SetDirty();
            textHandle.Update();
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        public void RemoveFromCache(TextHandle textHandle)
        {
            lock (syncRoot)
            {
                if (!textHandle.IsCachedPermanentTextCore)
                    return;

                if (textHandle.TextInfoNode != null)
                {
                    s_Cache.AddFirst(textHandle.TextInfoNode);
                    ResetEntryState(textHandle);
                }

                textHandle.IsCachedPermanentTextCore = false;
            }
        }

        private void ResetEntryState(TextHandle handle)
        {
            handle.TextInfoNode.SetTime(0f);
            handle.TextInfoNode.SetTextHandle(null);
            handle.TextInfoNode = null;
        }
    }
}
