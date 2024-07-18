// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace UnityEditor
{
    internal class UndoSerializationWindow : EditorWindow
    {
        static UndoSerializationWindow s_Instance;
        public static UndoSerializationWindow instance => s_Instance;
        
        ListView m_HistoryListView;
        List<HistoryItem> m_History = new List<HistoryItem>();
        List<string> m_UndoList = new List<string>();
        int m_UndoCursor;
        
        public enum HistoryType
        {
            None,
            Undo,
            Redo
        }
        
        public readonly struct HistoryItem
        {
            public readonly HistoryType type;
            public readonly string undoName;
            public readonly int index;

            public HistoryItem(HistoryType type, string undoName, int index)
            {
                this.type = type;
                this.undoName = undoName;
                this.index = index;
            }
        }
        
        const string k_StyleCommon = "StyleSheets/UndoHistory/UndoHistoryCommon.uss";
        const string k_StyleDark = "StyleSheets/UndoHistory/UndoHistoryDark.uss";
        const string k_StyleLight = "StyleSheets/UndoHistory/UndoHistoryLight.uss";

        [MenuItem("Window/Internal/Undo Serialization", false, 2013)]
        static void CreateUndoSerializationWindow()
        {
            if (s_Instance == null)
                s_Instance = EditorWindow.GetWindow<UndoSerializationWindow>();
            else
                EditorWindow.GetWindow<UndoSerializationWindow>();
        }
        
        public UndoSerializationWindow()
        {
        }

        void OnEnable()
        {
            if (s_Instance == null)
            {
                    this.titleContent = EditorGUIUtility.TrTextContent("Last Known Project Actions");

                    //root of the editorwindow
                    VisualElement root = rootVisualElement;
                    root.styleSheets.Add(EditorGUIUtility.Load(k_StyleCommon) as StyleSheet);
                    root.styleSheets.Add(EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? k_StyleDark : k_StyleLight) as StyleSheet);
                    root.EnableInClassList("root", true);
                    
                    Label label = new Label("Available only in Developer Mode");
                    label.style.unityFontStyleAndWeight = FontStyle.Bold;
                    root.Add(label);
                    
                    Button convertorButton = new Button();
                    convertorButton.name = "convertorButton";
                    convertorButton.text = "Convert Serialized Data To Readable Format";
                    rootVisualElement.Add(convertorButton);

                    convertorButton.RegisterCallback<ClickEvent>(ConvertSerializedData);

                    var theListView = EditorGUIUtility.Load("UXML/Undo/UndoListView.uxml") as VisualTreeAsset;
                    theListView.CloneTree(root);

                    var visualTree = EditorGUIUtility.Load("UXML/Undo/UndoListItem.uxml") as VisualTreeAsset;

                    Func<VisualElement> makeItem = visualTree.Instantiate;

                    Action<VisualElement, int> bindItem = (e, i) =>
                    {
                        //get info on this action
                        var item = m_History[i];

                        //get the main container
                        var container = e.Q("HistoryItem");

                        //is this the the current state?
                        bool isCurrent = item.index == m_UndoCursor;

                        //set style class for the whole Container
                        container.EnableInClassList("current", isCurrent);
                        container.EnableInClassList("redo", !isCurrent && item.type == HistoryType.Redo);
                        container.EnableInClassList("undo", !isCurrent && (item.type == HistoryType.Undo || item.type == HistoryType.None));

                        //get the Action label and set it's text to this undo action
                        var label = e.Q("Text-Action") as Label;
                        label.text = item.undoName;

                        if (isCurrent)
                            m_HistoryListView.selectedIndex = i;
                    };

                    m_HistoryListView = root.Q<ListView>();
                    m_HistoryListView.bindItem = bindItem;
                    m_HistoryListView.makeItem = makeItem;
                    m_HistoryListView.itemsSource = m_History;
                    m_HistoryListView.RefreshItems();

                    s_Instance = this;
                }
            wantsLessLayoutEvents = true;
        }

        private void ConvertSerializedData(ClickEvent evt)
        {
            bool success = Undo.ConvertSerializedData();
            if (success)
            {
                EditorUtility.DisplayDialog("Undo Serialization", "Undo actions have been converted and available in project's Library folder", "OK");
            }
            else
            {
                // Don't get the list if conversion failed
                Debug.LogWarning("Unable to retrieve last known undoable actions from previous Editor session");
                return;
            }
            
            Undo.GetUndoList(m_UndoList, out m_UndoCursor, true);

            for (int i = 0; i < m_UndoList.Count; i++)
            m_UndoList[i] = m_UndoList[i].Replace("\n", "");

            // rebuild the history list
            m_History.Clear();
            for (int i = m_UndoList.Count - 1; i >= 0; i--)
                m_History.Add(new HistoryItem(i < m_UndoCursor ? HistoryType.Undo : HistoryType.Redo, m_UndoList[i], i));
            m_History.Add(new HistoryItem(HistoryType.None, "Scene Open", -1));

            s_Instance.m_HistoryListView.RefreshItems();
            s_Instance.m_HistoryListView.ScrollToItem(m_UndoCursor);
        }

        void OnDisable()
        {

        }
    }
}
