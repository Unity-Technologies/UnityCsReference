// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

using UnityObject = UnityEngine.Object;
using System.Collections.ObjectModel;

namespace UnityEditor
{
    internal class InspectorHistoryGUI
    {
        InspectorHistory m_InspectorHistory;

        public InspectorHistoryGUI(InspectorHistory inspectorHistory)
        {
            m_InspectorHistory = inspectorHistory;
        }

        static class Styles
        {
            public static readonly GUIContent backContent = EditorGUIUtility.IconContent("back");
            public static readonly GUIContent forwardContent = EditorGUIUtility.IconContent("forward");
            public static readonly GUIContent historyContent = EditorGUIUtility.TrTextContent("History");
            public static readonly GUIContent clearHistoryContent = EditorGUIUtility.TrTextContent("Clear History");

            public static readonly GUIContent unamedSelection = EditorGUIUtility.TrTextContent("<unamed>");
            public static readonly GUIContent invalidSelection = EditorGUIUtility.TrTextContent("<invalid>");
            public static readonly GUIContent emptySelection = EditorGUIUtility.TrTextContent("<empty>");
            public static readonly GUIContent multipleObjectsFormat = EditorGUIUtility.TrTextContent("({0} {1}s)");
        }

        const int maxCountInBackForwardMenus = 20;
        const int maxCountHistoryMenus = 35;

        void ShowBackMenu(Rect rect)
        {
            GenericMenu menu = new GenericMenu();
            menu.allowDuplicateNames = true;
            int cpt = 1;
            foreach (var item in m_InspectorHistory.backItems)
            {
                menu.AddItem(new GUIContent(GetItemName(item)), false, OnSelectBackItem, item);
                ++cpt;
                if (cpt > maxCountInBackForwardMenus)
                    break;
            }
            menu.DropDown(rect);
        }

        void ShowForwardMenu(Rect rect)
        {
            GenericMenu menu = new GenericMenu();
            menu.allowDuplicateNames = true;
            int cpt = 1;
            foreach (var item in m_InspectorHistory.forwardItems)
            {
                menu.AddItem(new GUIContent(GetItemName(item)), false, OnSelectBackItem, item);
                ++cpt;
                if (cpt > maxCountInBackForwardMenus)
                    break;
            }
            menu.DropDown(rect);
        }

        void OnSelectBackItem(object userData)
        {
            m_ForwardButtonInfos.timerTicked = false;
            m_BackwardButtonInfos.timerTicked = false;

            m_InspectorHistory.OnSelectBackItem(userData as InspectorHistory.Item);
        }

        void AddHistoryItem(GenericMenu menu, InspectorHistory.Item item)
        {
            menu.AddItem(EditorGUIUtility.TextContent(GetItemName(item)), m_InspectorHistory.IsSameAsSelection(item), () => m_InspectorHistory.Select(item));
        }

        string GetItemName(InspectorHistory.Item item)
        {
            string name = null;

            var objs = item.objects;
            if (objs.Length == 0)
            {
                name = Styles.emptySelection.text;
            }
            else if (objs.Length == 1)
            {
                UnityObject obj = objs[0];
                if (obj)
                {
                    name = obj.name;
                    if (string.IsNullOrEmpty(name))
                        name = Styles.unamedSelection.text;
                }
                else
                {
                    name = Styles.invalidSelection.text;
                }
            }
            else
            {
                // find the common base type which must be UnityObject or derived from UnityObject.
                System.Type type = objs[0].GetType();
                for (int i = 1; i < objs.Length; ++i)
                {
                    System.Type otherType = objs[i].GetType();

                    if (!type.IsAssignableFrom(otherType))
                    {
                        while (!otherType.IsAssignableFrom(type))
                        {
                            otherType = otherType.BaseType;
                        }
                        type = otherType;
                    }

                    if (type == typeof(UnityObject))
                        break;
                }

                name = string.Format(Styles.multipleObjectsFormat.text, objs.Length, type.Name);
            }

            name = name.Replace('/', '_');

            if (name.Length < 1)
            {
                name = Styles.unamedSelection.text;
            }
            return name;
        }

        static readonly int s_TitleHash = "InspectorTitle".GetHashCode();

        struct TimedButtonInfo
        {
            public bool         startDown;
            public bool         timerTicked;
            public float        timeDown;

            public const float Delay = 0.5f;

            public void OnGUI(InspectorWindow window, Rect buttonRect, GUIContent name, Action<Rect> menuAction, Action explicitAction)
            {
                if (buttonRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.type == EventType.MouseDown)
                    {
                        if (Event.current.button == 1)
                            menuAction(buttonRect);
                        else if (Event.current.button == 0)
                        {
                            startDown = true;
                            timeDown = Time.realtimeSinceStartup;
                            window.Repaint();
                        }
                    }
                    else if (startDown)
                    {
                        if (timeDown + Delay < Time.realtimeSinceStartup)
                        {
                            startDown = false;
                            menuAction(buttonRect);
                            window.Repaint();
                            timerTicked = true;
                        }
                        else
                        {
                            window.Repaint();
                        }
                    }
                }
                if (GUI.Button(buttonRect, name, EditorStyles.toolbarButton))
                {
                    startDown = false;
                    if (!timerTicked)
                        explicitAction();
                    timerTicked = false;
                }
            }
        }

        TimedButtonInfo m_ForwardButtonInfos;
        TimedButtonInfo m_BackwardButtonInfos;


        internal void OnHistoryGUI(InspectorWindow window)
        {
            Rect dndRect = new Rect(0, 0, window.position.width, EditorStyles.toolbarButton.fixedHeight);

            int dndControlID = GUIUtility.GetControlID(s_TitleHash, FocusType.Keyboard, dndRect);

            if (Event.current.type == EventType.DragUpdated)
            {
                if (dndRect.Contains(Event.current.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;

                    DragAndDrop.activeControlID = dndControlID;
                    Event.current.Use();
                }
                else
                {
                    DragAndDrop.activeControlID = 0;
                }
            }
            else if (Event.current.type == EventType.DragPerform)
            {
                if (dndRect.Contains(Event.current.mousePosition))
                {
                    UnityObject[] references = DragAndDrop.objectReferences;
                    window.SetObjectsLocked(references.ToList());
                    m_InspectorHistory.PushHistory(references);
                    DragAndDrop.AcceptDrag();
                    DragAndDrop.activeControlID = 0;
                    window.Repaint();
                    Event.current.Use();
                    GUIUtility.ExitGUI();
                }
            }

            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            //Back Button
            Rect buttonRect = GUILayoutUtility.GetRect(Styles.backContent, EditorStyles.toolbarButton);

            GUI.enabled = m_InspectorHistory.canGoBack;
            m_BackwardButtonInfos.OnGUI(window, buttonRect, Styles.backContent, ShowBackMenu, m_InspectorHistory.GoBack);

            //Forward Button
            buttonRect = GUILayoutUtility.GetRect(Styles.forwardContent, EditorStyles.toolbarButton);

            GUI.enabled = m_InspectorHistory.canGoForward;
            m_ForwardButtonInfos.OnGUI(window, buttonRect, Styles.forwardContent, ShowForwardMenu, m_InspectorHistory.GoForward);

            GUILayout.FlexibleSpace();


            //History popup
            GUI.enabled = true;
            GUIStyle dropStyle = EditorStyles.toolbarDropDown;
            Rect r = GUILayoutUtility.GetRect(Styles.historyContent, dropStyle);
            if (EditorGUI.DropdownButton(r, Styles.historyContent, FocusType.Passive, dropStyle))
            {
                GenericMenu menu = new GenericMenu();

                var items = m_InspectorHistory.globalHistoryItems;

                foreach (var item in items)
                {
                    AddHistoryItem(menu, item);
                }

                if (items.Count() > 0)
                {
                    menu.AddSeparator("");
                }
                menu.AddItem(Styles.clearHistoryContent, false, m_InspectorHistory.ClearHistory);
                menu.DropDown(r);
            }


            GUILayout.Space(10);


            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }
    }
}
