// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Reflection;
using System;
using UnityEngine.Events;

namespace UnityEngine
{
    [AttributeUsage(System.AttributeTargets.Method)]
    public class BeforeRenderOrderAttribute : Attribute
    {
        public int order { get; private set; }
        public BeforeRenderOrderAttribute(int order)
        {
            this.order = order;
        }
    }
    static class BeforeRenderHelper
    {
        struct OrderBlock
        {
            internal int order;
            internal UnityAction callback;
        }

        static List<OrderBlock> s_OrderBlocks = new List<OrderBlock>();

        static int GetUpdateOrder(UnityAction callback)
        {
            object[] attributes = callback.Method.GetCustomAttributes(typeof(BeforeRenderOrderAttribute), true);
            BeforeRenderOrderAttribute updateOrder = (attributes != null && attributes.Length > 0) ? attributes[0] as BeforeRenderOrderAttribute : null;

            return updateOrder != null ? updateOrder.order : 0;
        }

        public static void RegisterCallback(UnityAction callback)
        {
            int order = GetUpdateOrder(callback);

            lock (s_OrderBlocks)
            {
                int i = 0;
                for (; i < s_OrderBlocks.Count && (s_OrderBlocks[i].order <= order); i++)
                {
                    if (s_OrderBlocks[i].order == order)
                    {
                        OrderBlock element = s_OrderBlocks[i];
                        element.callback += callback;
                        s_OrderBlocks[i] = element;
                        return;
                    }
                }

                var newElement = new OrderBlock();
                newElement.order = order;
                newElement.callback += callback;

                s_OrderBlocks.Insert(i, newElement);
            }
        }

        public static void UnregisterCallback(UnityAction callback)
        {
            int order = GetUpdateOrder(callback);

            lock (s_OrderBlocks)
            {
                for (int i = 0; i < s_OrderBlocks.Count && (s_OrderBlocks[i].order <= order); i++)
                {
                    if (s_OrderBlocks[i].order == order)
                    {
                        OrderBlock element = s_OrderBlocks[i];
                        element.callback -= callback;
                        s_OrderBlocks[i] = element;

                        if (element.callback == null)
                        {
                            s_OrderBlocks.RemoveAt(i);
                        }
                        return;
                    }
                }
            }
        }

        public static void Invoke()
        {
            lock (s_OrderBlocks)
            {
                for (int i = 0; i < s_OrderBlocks.Count; i++)
                {
                    UnityAction callback = s_OrderBlocks[i].callback;
                    if (callback != null)
                        callback();
                }
            }
        }
    }
}
