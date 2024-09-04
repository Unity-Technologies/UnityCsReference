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
        internal LinkedList<TextInfo> s_TextInfoPool = new LinkedList<TextInfo>();
        private object syncRoot = new object();

        public virtual void AddTextInfoToCache(TextHandle textHandle)
        {
            lock (syncRoot)
            {
                if (textHandle.IsCachedPermanent)
                    return;

                if (textHandle.IsCachedTemporary)
                {
                    textHandle.RemoveTextInfoFromTemporaryCache();
                }

                if (s_TextInfoPool.Count > 0)
                {
                    textHandle.TextInfoNode = s_TextInfoPool.Last;
                    s_TextInfoPool.RemoveLast();
                }
                else
                {
                    var textInfo = new TextInfo(VertexDataLayout.VBO);
                    textHandle.TextInfoNode = new LinkedListNode<TextInfo>(textInfo);
                }
            }

            textHandle.IsCachedPermanent = true;
            textHandle.SetDirty();
            textHandle.Update();
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        public void RemoveTextInfoFromCache(TextHandle textHandle)
        {
            lock (syncRoot)
            {
                if (!textHandle.IsCachedPermanent)
                    return;

                s_TextInfoPool.AddFirst(textHandle.TextInfoNode);
                textHandle.TextInfoNode = null;
                textHandle.IsCachedPermanent = false;
            }
        }
    }
}
