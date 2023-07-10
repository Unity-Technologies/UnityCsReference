// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine.Pool;
using UnityEngine.Analytics;


namespace UnityEditor
{
    public class DebuggerEventListHandler
    {
        public List<string> items = new List<string>();
        // items is written to from a worker thread but read from a main thread, so lock for all accesses for thread safety
        private readonly object itemsLock = new object();
        internal static DebuggerEventListHandler handler = new DebuggerEventListHandler();

        static public event Action<Analytic> analyticSent; // Used only by the internal window

       // static DebuggerEventListHandler m_Instance = null;
      /*  public static DebuggerEventListHandler instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = new DebuggerEventListHandler();

                return m_Instance;
            }
        }*/

        public Analytic[] csharp_items = null;

        public static void AddCSharpAnalytic(Analytic analytic)
        {
            analyticSent?.Invoke(analytic);   
        }

        public static void AddAnalytic(String analytic)
        {
            handler.AddAnalyticInternal(analytic);
        }

        private void AddAnalyticInternal(String analytic)
        {
            lock (itemsLock)
            {
                items.Add(analytic);
            }
        }

        private void ClearList()
        {
            lock (itemsLock)
            {
                items.Clear();
            }
        }

        public static void ClearEventList()
        {
            handler.ClearList();
        }

        public static List<string> fetchEventList()
        {
            List<string> eventList = null;
            lock (handler.itemsLock)
            {
                eventList = handler.items;
                handler.items = new List<string>();
            }
            return eventList;
        }

        public static void fetchEventList(Action<string> processEventListItems)
        {
            lock (handler.itemsLock)
            {
                for (int i = 0; i < handler.items.Count; i++)
                {
                    processEventListItems(handler.items[i]);
                }
                handler.items.Clear();
            }
        }
    }
}
