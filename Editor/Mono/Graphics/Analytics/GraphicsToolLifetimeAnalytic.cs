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
    // schema = com.unity3d.data.schemas.editor.analytics.uGraphicsToolLifetimeAnalytic_v2
    // taxonomy = editor.analytics.uGraphicsToolLifetimeAnalytic.v2
    public class GraphicsToolLifetimeAnalytic
    {
        static bool IsInternalAssembly(Type type)
        {
            var assemblyName = type.Assembly.FullName;
            if (assemblyName.StartsWith("UnityEditor.", StringComparison.InvariantCultureIgnoreCase) ||
                assemblyName.StartsWith("Unity.", StringComparison.InvariantCultureIgnoreCase))
                return true;
            return false;
        }

        static List<string> GatherCurrentlyOpenWindowNames(Type editorWindowType)
        {
            var openWindows = Resources.FindObjectsOfTypeAll(typeof(EditorWindow));
            var openWindowNames = new List<string>(openWindows.Length);
            foreach (var w in openWindows)
            {
                var currentWindowType = w.GetType();
                if (IsInternalAssembly(currentWindowType) && currentWindowType != editorWindowType)
                {
                    openWindowNames.Add((w as EditorWindow).titleContent.text);
                }
            }
            return openWindowNames;
        }

        static string[] UnionWithoutLinq(List<string> a, List<string> b)
        {
            HashSet<string> aAndB = new HashSet<string>(a);
            aAndB.UnionWith(b);
            String[] aAndBArray = new String[aAndB.Count];
            aAndB.CopyTo(aAndBArray);
            return aAndBArray;
        }

        [AnalyticInfo(eventName: "uGraphicsToolLifetimeAnalytic", vendorKey: "unity.graphics", version: 2, maxEventsPerHour: 100, maxNumberOfElements: 1000)]
        internal class Analytic : IAnalytic
        {
            public Analytic(WindowOpenedMetadata windowOpenedMetadata)
            {
                List<string> currentlyOpenEditorWindows = GatherCurrentlyOpenWindowNames(windowOpenedMetadata.editorWindowType);
                var elapsed = DateTime.Now - windowOpenedMetadata.openedTime;
                using (UnityEngine.Pool.GenericPool<Data>.Get(out var data))
                {
                    data.window_id = windowOpenedMetadata.editorWindowType.Name;
                    data.seconds_opened = elapsed.Seconds;
                    data.other_open_windows = UnionWithoutLinq(currentlyOpenEditorWindows, windowOpenedMetadata.openEditorWindows);

                    m_Data = data;
                }
            }

            [DebuggerDisplay("[{window_id}] Open time: {seconds_opened} - Other Windows Count: {other_open_windows.Length}")]
            [Serializable]
            class Data : IAnalytic.IData
            {
                // Naming convention for analytics data is lower case and and connecting with _
                public string window_id;
                public int seconds_opened;
                public string[] other_open_windows;
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data = m_Data;
                error = null;
                return true;
            }

            Data m_Data;
        }

        internal struct WindowOpenedMetadata
        {
            public Type editorWindowType;
            public List<string> openEditorWindows;
            public DateTime openedTime;
        }

        static Dictionary<Type, WindowOpenedMetadata> s_WindowOpenedMetadata = new Dictionary<Type, WindowOpenedMetadata>();

        public static void WindowOpened<T>()
            where T : EditorWindow
        {
            if (!s_WindowOpenedMetadata.TryGetValue(typeof(T), out var metadata))
            {
                metadata = new WindowOpenedMetadata
                {
                    editorWindowType = typeof(T),
                    openEditorWindows = GatherCurrentlyOpenWindowNames(typeof(T)),
                    openedTime = DateTime.Now
                };

                s_WindowOpenedMetadata.Add(typeof(T), metadata);
            }
        }

        public static void WindowClosed<T>()
            where T : EditorWindow
        {
            if (s_WindowOpenedMetadata.TryGetValue(typeof(T), out var metadata))
            {
                EditorAnalytics.SendAnalytic(new Analytic(metadata));
                s_WindowOpenedMetadata.Remove(typeof(T));
            }
        }
    }
}
