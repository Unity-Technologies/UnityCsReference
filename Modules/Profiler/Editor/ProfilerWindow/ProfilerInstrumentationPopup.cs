// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
    internal class ProfilerInstrumentationPopup : PopupWindowContent
    {
        private const string kAutoInstrumentSettingKey = "ProfilerAutoInstrumentedAssemblyTypes";
        private const int kAutoInstrumentButtonHeight = 20;
        private const int kAutoInstrumentButtonsHeight = 1 * kAutoInstrumentButtonHeight;

        private static GUIContent s_AutoInstrumentScriptsContent = new GUIContent("Auto instrument " + InstrumentedAssemblyTypes.Script.ToString() + " assemblies");

        private static Dictionary<string, int> s_InstrumentableFunctions;
        private static ProfilerInstrumentationPopup s_PendingPopup;

        private PopupList m_FunctionsList;
        private InputData m_FunctionsListInputData;
        private bool m_ShowAllCheckbox;
        private bool m_ShowAutoInstrumemtationParams;
        private InstrumentedAssemblyTypes m_AutoInstrumentedAssemblyTypes;
        private PopupList.ListElement m_AllCheckbox;

        public static bool InstrumentationEnabled
        {
            get { return false; }
        }


        class InputData : PopupList.InputData
        {
            public override IEnumerable<PopupList.ListElement> BuildQuery(string prefix)
            {
                if (prefix == "")
                    return m_ListElements;
                else
                    return m_ListElements.Where(
                        element => element.m_Content.text.Contains(prefix)
                        );
            }
        }

        public ProfilerInstrumentationPopup(Dictionary<string, int> functions, bool showAllCheckbox, bool showAutoInstrumemtationParams)
        {
            m_ShowAutoInstrumemtationParams = showAutoInstrumemtationParams;
            m_ShowAllCheckbox = showAllCheckbox;
            m_AutoInstrumentedAssemblyTypes = (InstrumentedAssemblyTypes)SessionState.GetInt(kAutoInstrumentSettingKey, 0);

            m_FunctionsListInputData = new InputData();
            m_FunctionsListInputData.m_CloseOnSelection = false;
            m_FunctionsListInputData.m_AllowCustom = true;
            m_FunctionsListInputData.m_MaxCount = 0;
            m_FunctionsListInputData.m_EnableAutoCompletion = false;
            m_FunctionsListInputData.m_SortAlphabetically = true;
            m_FunctionsListInputData.m_OnSelectCallback = ProfilerInstrumentationPopupCallback;

            SetFunctions(functions);

            m_FunctionsList = new PopupList(m_FunctionsListInputData);
        }

        private void SetFunctions(Dictionary<string, int> functions)
        {
            m_FunctionsListInputData.m_ListElements.Clear();
            if (functions == null)
            {
                // Querying functions
                var element = m_FunctionsListInputData.NewOrMatchingElement("Querying instrumentable functions...");
                element.enabled = false;
            }
            else if (functions.Count == 0)
            {
                var element = m_FunctionsListInputData.NewOrMatchingElement("No instrumentable child functions found");
                element.enabled = false;
            }
            else
            {
                m_FunctionsListInputData.m_MaxCount = Mathf.Clamp(functions.Count + 1, 0, 30);

                if (m_ShowAllCheckbox)
                {
                    m_AllCheckbox = new PopupList.ListElement(" All", false, float.MaxValue);
                    m_FunctionsListInputData.m_ListElements.Add(m_AllCheckbox);
                }

                foreach (var f in functions)
                {
                    var res = new PopupList.ListElement(f.Key, f.Value != 0);
                    res.ResetScore();
                    m_FunctionsListInputData.m_ListElements.Add(res);
                }

                if (m_ShowAllCheckbox)
                    UpdateAllCheckbox();
            }
        }

        public override void OnGUI(Rect rect)
        {
            Rect r = new Rect(rect);

            if (m_ShowAutoInstrumemtationParams)
            {
                Rect toggleRect = new Rect(r);
                toggleRect.height = kAutoInstrumentButtonHeight;

                // TODO: Use reflection and foreach on InstrumentedAssemblyTypes values
                var newAutoInstrumentedAssemblies = InstrumentedAssemblyTypes.None;
                if (GUI.Toggle(toggleRect, (m_AutoInstrumentedAssemblyTypes & InstrumentedAssemblyTypes.Script) != 0, s_AutoInstrumentScriptsContent))
                    newAutoInstrumentedAssemblies |= InstrumentedAssemblyTypes.Script;

                if (newAutoInstrumentedAssemblies != m_AutoInstrumentedAssemblyTypes)
                {
                    m_AutoInstrumentedAssemblyTypes = newAutoInstrumentedAssemblies;
                    ProfilerDriver.SetAutoInstrumentedAssemblies(m_AutoInstrumentedAssemblyTypes);
                    SessionState.SetInt(kAutoInstrumentSettingKey, (int)m_AutoInstrumentedAssemblyTypes);
                }

                r.y += kAutoInstrumentButtonsHeight;
                r.height -= kAutoInstrumentButtonsHeight;
            }
            m_FunctionsList.OnGUI(r);
        }

        public override void OnClose()
        {
            m_FunctionsList.OnClose();
        }

        public override Vector2 GetWindowSize()
        {
            Vector2 listSize = m_FunctionsList.GetWindowSize();
            listSize.x = 450;
            if (m_ShowAutoInstrumemtationParams)
                listSize.y += kAutoInstrumentButtonsHeight;
            return listSize;
        }

        public void UpdateAllCheckbox()
        {
            if (m_AllCheckbox == null)
                return;

            bool someSelected = false;
            bool allSelected = true;
            foreach (var child in m_FunctionsListInputData.m_ListElements)
            {
                if (child != m_AllCheckbox)
                {
                    if (child.selected)
                        someSelected = true;
                    else
                        allSelected = false;
                }
            }

            m_AllCheckbox.selected = allSelected;
            m_AllCheckbox.partiallySelected = someSelected && !allSelected;
        }

        static void SetFunctionNamesFromUnity(bool allFunction, string[] functionNames, int[] isInstrumentedFlags)
        {
            var dict = new Dictionary<string, int>(functionNames.Length);
            for (var i = 0; i < functionNames.Length; ++i)
                dict.Add(functionNames[i], isInstrumentedFlags[i]);

            if (allFunction)
            {
                // No parent name: We received full functions list and should update it
                s_InstrumentableFunctions = dict;
            }

            if (s_PendingPopup != null)
            {
                s_PendingPopup.SetFunctions(dict);
                s_PendingPopup = null;
            }
        }

        public static void UpdateInstrumentableFunctions()
        {
            ProfilerDriver.QueryInstrumentableFunctions();
        }

        /// <summary>
        /// Show popup with all functions availabel for instrumentation
        /// </summary>
        /// <param name="r">Display rect</param>
        public static void Show(Rect r)
        {
            var popup = new ProfilerInstrumentationPopup(s_InstrumentableFunctions, false, true);
            if (s_InstrumentableFunctions == null)
            {
                s_PendingPopup = popup;
                ProfilerDriver.QueryInstrumentableFunctions();
            }
            else
            {
                s_PendingPopup = null;
            }

            PopupWindow.Show(r, popup);
        }

        public static void Show(Rect r, string funcName)
        {
            var popup = new ProfilerInstrumentationPopup(null, true, false);
            s_PendingPopup = popup;
            ProfilerDriver.QueryFunctionCallees(funcName);

            PopupWindow.Show(r, popup);
        }

        static public bool FunctionHasInstrumentationPopup(string funcName)
        {
            return s_InstrumentableFunctions != null && s_InstrumentableFunctions.ContainsKey(funcName);
        }

        void ProfilerInstrumentationPopupCallback(PopupList.ListElement element)
        {
            if (element == m_AllCheckbox)
            {
                element.selected = !element.selected;
                foreach (var child in m_FunctionsListInputData.m_ListElements)
                {
                    if (element.selected)
                        ProfilerDriver.BeginInstrumentFunction(child.text);
                    else
                        ProfilerDriver.EndInstrumentFunction(child.text);
                    child.selected = element.selected;
                }
            }
            else
            {
                element.selected = !element.selected;
                if (element.selected)
                    ProfilerDriver.BeginInstrumentFunction(element.text);
                else
                    ProfilerDriver.EndInstrumentFunction(element.text);
            }

            UpdateAllCheckbox();
        }
    }
}
