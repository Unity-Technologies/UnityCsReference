// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Analytics;

namespace UnityEditor.Rendering.Analytics
{
    // schema = com.unity3d.data.schemas.editor.analytics.uGraphicsToolUsageAnalytic_v1
    // taxonomy = editor.analytics.uGraphicsToolUsageAnalytic.v1
    public class GraphicsToolUsageAnalytic
    {
        [AnalyticInfo(eventName: "uGraphicsToolUsageAnalytic", vendorKey: "unity.graphics", version: 1, maxEventsPerHour: 100, maxNumberOfElements: 1000)]
        internal class Analytic<T> : IAnalytic
        {
            public Analytic(string action, string[] context)
            {
                using (UnityEngine.Pool.GenericPool<Data>.Get(out var data))
                {
                    data.window_id = typeof(T).Name;
                    data.action = action;
                    data.context = context;

                    m_Data = data;
                }
            }

            [DebuggerDisplay("[{window_id}] Action: {action} - Context: {context}")]
            [Serializable]
            class Data : IAnalytic.IData
            {
                // Naming convention for analytics data is lower case and and connecting with _
                public string window_id;
                public string action;
                public string[] context;
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data = m_Data;
                error = null;
                return true;
            }

            Data m_Data;
        }

        public static void ActionPerformed<T>(string action, string[] context)
            where T : EditorWindow
        {
            if (string.IsNullOrEmpty(action))
                return;

            EditorAnalytics.SendAnalytic(new Analytic<T>(action, context));
        }
    }
}
