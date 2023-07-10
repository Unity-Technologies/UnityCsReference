// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;

namespace Unity.CodeEditor
{
    class CodeEditorAnalytics
    {
        [AnalyticInfo(eventName: "CodeEditorUsage", vendorKey: "unity.codeeditor")]
        public class CodeEditorAnalytic : IAnalytic
        {
            private IExternalCodeEditor codeEditor = null;

            public CodeEditorAnalytic(IExternalCodeEditor codeEditor)
            {
                this.codeEditor = codeEditor;
            }

            struct AnalyticsData : IAnalytic.IData
            {
                [SerializeField] public string code_editor;
            }
            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data = new AnalyticsData()
                {
                    code_editor = codeEditor.GetType().FullName
                };

                error = null;
                return true;
            }
        }

        public static void SendCodeEditorUsage(IExternalCodeEditor codeEditor)
        {
            EditorAnalytics.SendAnalytic(new CodeEditorAnalytic(codeEditor));
        }    
    }
}
