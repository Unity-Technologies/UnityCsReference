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
    internal static class TextCacheEntryNodeExtensions
    {
        public static void SetTime(this LinkedListNode<TextCacheEntry> node, float newTime)
        {
            var entry = node.Value;
            entry.lastTimeInCache = newTime;
            node.Value = entry;
        }

        public static void SetTextHandle(this LinkedListNode<TextCacheEntry> node, TextHandle newTextHandle)
        {
            var entry = node.Value;
            entry.textHandle = newTextHandle;
            node.Value = entry;
        }
    }

    internal struct TextCacheEntry
    {
        public TextHandle textHandle;
        public TextInfo   textInfo;
        public float      lastTimeInCache;

        public TextCacheEntry(TextHandle handle, TextInfo info, float time = 0.0f)
        {
            textHandle = handle;
            textInfo   = info;
            lastTimeInCache = time;
        }
    }

    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal class TextHandleTemporaryCache
    {
        internal LinkedList<TextCacheEntry> s_Cache = new LinkedList<TextCacheEntry>();
        internal const int s_MinFramesInCache = 2;
        internal int currentFrame;

        private object syncRoot = new object();

        public void ClearTemporaryCache()
        {
            foreach (var entry in s_Cache)
                ResetEntryState(entry.textHandle);

            s_Cache.Clear();
        }

        public void AddTextInfoToCache(TextHandle textHandle, int hashCode)
        {
            lock (syncRoot)
            {
                if (textHandle.IsCachedPermanent)
                    return;

                bool canWriteOnAsset = !TextGenerator.IsExecutingJob;
                if (canWriteOnAsset)
                    currentFrame = Time.frameCount;

                //cache is invalid and we need to clear it
                if (s_Cache.Count > 0 && (currentFrame - s_Cache.Last.Value.lastTimeInCache < 0 || currentFrame - s_Cache.First.Value.lastTimeInCache < 0))
                {
                    ClearTemporaryCache();
                }

                if (textHandle.IsCachedTemporary)
                {
                    RefreshCaching(textHandle);
                    return;
                }

                if (s_Cache.Count > 0 && currentFrame - s_Cache.Last.Value.lastTimeInCache > s_MinFramesInCache)
                {
                    RecycleTextInfoFromCache(textHandle);
                }
                else
                {
                    var textInfo = new TextInfo();
                    textHandle.TextInfoNode = new LinkedListNode<TextCacheEntry>(new TextCacheEntry(textHandle, textInfo, currentFrame));
                    s_Cache.AddFirst(textHandle.TextInfoNode);
                }
            }

            textHandle.IsCachedTemporary = true;
            textHandle.SetDirty();
            textHandle.UpdateWithHash(hashCode);
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal void RemoveFromCache(TextHandle handle)
        {
            lock (syncRoot)
            {
                if (!handle.IsCachedTemporary)
                    return;

                if (handle.TextInfoNode != null)
                {
                    s_Cache.Remove(handle.TextInfoNode);
                    s_Cache.AddLast(handle.TextInfoNode);
                }

                ResetEntryState(handle);
            }
        }

        internal void ResetEntryState(TextHandle handle)
        {
            if (handle == null || !handle.IsCachedTemporary)
                return;

            handle.IsCachedTemporary = false;
            handle.TextInfoNode.SetTime(0f);
            handle.TextInfoNode.SetTextHandle(null);
            handle.TextInfoNode = null;
        }

        private void RefreshCaching(TextHandle textHandle)
        {
            if (!TextGenerator.IsExecutingJob)
                currentFrame = Time.frameCount;

            textHandle.TextInfoNode.SetTime(currentFrame);
            s_Cache.Remove(textHandle.TextInfoNode);
            s_Cache.AddFirst(textHandle.TextInfoNode);
        }

        private void RecycleTextInfoFromCache(TextHandle textHandle)
        {
            if (!TextGenerator.IsExecutingJob)
                currentFrame = Time.frameCount;

            textHandle.RemoveFromTemporaryCache();
            if (s_Cache.Last.Value.textHandle != null)
                s_Cache.Last.Value.textHandle.RemoveFromTemporaryCache();

            textHandle.TextInfoNode = s_Cache.Last;
            textHandle.TextInfoNode.SetTextHandle(textHandle);
            textHandle.TextInfoNode.SetTime(currentFrame);
            textHandle.IsCachedTemporary = true;

            s_Cache.RemoveLast();
            s_Cache.AddFirst(textHandle.TextInfoNode);
        }

        public void UpdateCurrentFrame()
        {
            currentFrame = Time.frameCount;
        }
    }
}
