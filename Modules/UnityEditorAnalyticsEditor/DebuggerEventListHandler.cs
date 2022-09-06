// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System;
using System.IO;
using UnityEditorInternal;
using Button = UnityEngine.UIElements.Button;

namespace UnityEditor
{

    public class DebuggerEventListHandler
    {
        public List<string> items = new List<string>();
        internal static DebuggerEventListHandler handler = new DebuggerEventListHandler();

        static void Init()
        {
            if (handler == null)
            {
                handler = new DebuggerEventListHandler();
            }
        }


        public static void AddAnalytic(String analytic)
        {
            handler.AddAnalyticInternal(analytic);
        }

        private void AddAnalyticInternal(String analytic)
        {
            items.Add(analytic);
        }

        private void ClearList()
        {
            items.Clear();
        }

        public static void ClearEventList()
        {
            handler.ClearList();
        }

        public static List<string> fetchEventList()
        {
            List<string> eventList = handler.items;
            handler.ClearList();
            return eventList;
        }

        public static void fetchEventList(Action<string> processEventListItems)
        {
            for (int i = 0; i < handler.items.Count; i++)
            {
                processEventListItems(handler.items[i]);
                handler.items.RemoveAt(i--);
            }
        }

    }
}
