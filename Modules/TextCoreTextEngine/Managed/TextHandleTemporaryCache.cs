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
    internal class TextHandleTemporaryCache
    {
        internal LinkedList<TextInfo> s_TextInfoPool = new LinkedList<TextInfo>();
        internal const int s_MinFramesInCache = 2;
        internal int currentFrame;

        private object syncRoot = new object();

       
        public void ClearTemporaryCache()
        {
            for (int i = 0; i < s_TextInfoPool.Count; i++)
            {
                s_TextInfoPool.First.Value.RemoveFromCache();
            }
            s_TextInfoPool.Clear();
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
                if (s_TextInfoPool.Count > 0 && (currentFrame - s_TextInfoPool.Last.Value.lastTimeInCache < 0 || currentFrame - s_TextInfoPool.First.Value.lastTimeInCache < 0))
                {
                    ClearTemporaryCache();
                }

                if (textHandle.IsCachedTemporary)
                {
                    RefreshCaching(textHandle);
                    return;
                }

                if (s_TextInfoPool.Count > 0 && currentFrame - s_TextInfoPool.Last.Value.lastTimeInCache > s_MinFramesInCache)
                {
                    RecycleTextInfoFromCache(textHandle);
                }
                else
                {
                    var textInfo = new TextInfo(VertexDataLayout.VBO);
                    textHandle.TextInfoNode = new LinkedListNode<TextInfo>(textInfo);
                    s_TextInfoPool.AddFirst(textHandle.TextInfoNode);
                    textInfo.lastTimeInCache = currentFrame;
                    textInfo.removedFromCache += textHandle.RemoveTextInfoFromTemporaryCache;
                }
            }

            textHandle.IsCachedTemporary = true;
            textHandle.SetDirty();
            textHandle.UpdateWithHash(hashCode);
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        public virtual void RemoveTextInfoFromCache(TextHandle textHandle)
        {
            lock (syncRoot)
            {
                if (!textHandle.IsCachedTemporary)
                    return;

                textHandle.IsCachedTemporary = false;

                textHandle.TextInfoNode.Value.lastTimeInCache = 0;
                textHandle.TextInfoNode.Value.removedFromCache = null;

                if (textHandle.TextInfoNode != null)
                {
                    s_TextInfoPool.Remove(textHandle.TextInfoNode);
                    s_TextInfoPool.AddLast(textHandle.TextInfoNode);
                }

                textHandle.TextInfoNode = null;
            }
        }

        private void RefreshCaching(TextHandle textHandle)
        {
            if (!TextGenerator.IsExecutingJob)
                currentFrame = Time.frameCount;

            textHandle.TextInfoNode.Value.lastTimeInCache = currentFrame;
            s_TextInfoPool.Remove(textHandle.TextInfoNode);
            s_TextInfoPool.AddFirst(textHandle.TextInfoNode);
        }

        private void RecycleTextInfoFromCache(TextHandle textHandle)
        {
            if (!TextGenerator.IsExecutingJob)
                currentFrame = Time.frameCount;

            textHandle.TextInfoNode = s_TextInfoPool.Last;
            textHandle.TextInfoNode.Value.RemoveFromCache();
            s_TextInfoPool.RemoveLast();
            s_TextInfoPool.AddFirst(textHandle.TextInfoNode);
            textHandle.IsCachedTemporary = true;
            textHandle.TextInfoNode.Value.removedFromCache += textHandle.RemoveTextInfoFromTemporaryCache;
            textHandle.TextInfoNode.Value.lastTimeInCache = currentFrame;
        }

        public void UpdateCurrentFrame()
        {
            currentFrame = Time.frameCount;
        }
    }
}
