using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Debugger
{
    class StringValuePairs<T> : IComparable<StringValuePairs<T>>
    {
        public string s;
        public T v;

        public int CompareTo(StringValuePairs<T> other)
        {
            return s.CompareTo(other.s);
        }
    }

    class EventTypeSelectField : BasePopupField<ulong, StringValuePairs<long>>
    {
        public new class UxmlFactory : UxmlFactory<EventTypeSelectField, UxmlTraits>
        {
        }

        public new class UxmlTraits : BasePopupField<ulong, StringValuePairs<long>>.UxmlTraits
        {
        }

        public Dictionary<long, bool> m_State;
        public Dictionary<string, Color> m_Color;

        public EventTypeSelectField()
        {
            m_Choices = new List<StringValuePairs<long>>();
            m_State = new Dictionary<long, bool>();
            m_Color = new Dictionary<string, Color>();

            AppDomain currentDomain = AppDomain.CurrentDomain;
            HashSet<string> userAssemblies = new HashSet<string>(ScriptingRuntime.GetAllUserAssemblies());
            foreach (Assembly assembly in currentDomain.GetAssemblies())
            {
                if (userAssemblies.Contains(assembly.GetName().Name + ".dll"))
                    continue;

                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (typeof(EventBase).IsAssignableFrom(type) && !type.ContainsGenericParameters)
                        {
                            var methodInfo = type.GetMethod("TypeId", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                            if (methodInfo != null)
                            {
                                if (m_State.Count == 64)
                                {
                                    Debug.LogError("Too many values");
                                    break;
                                }

                                var typeId = (long)methodInfo.Invoke(null, null);
                                m_Choices.Add(new StringValuePairs<long> { s = type.Name, v = typeId });
                                m_State.Add(typeId, true);

                                m_Color.Add(type.Name, Color.white);
                            }
                        }
                    }

                    if (m_Color.Count > 0)
                    {
                        List<Color> listOfColors = GetListOfColors();
                        int indexCounter = 0;
                        var listOfKeys = m_Color.Keys.ToList();
                        foreach (var key in listOfKeys)
                        {
                            Color newColor = listOfColors[indexCounter];
                            m_Color[key] = newColor;
                            indexCounter++;
                            if (indexCounter >= listOfColors.Count)
                            {
                                indexCounter = 0;
                            }
                        }
                    }
                }
                catch (TypeLoadException e)
                {
                    Debug.LogWarningFormat("Error while loading types from assembly {0}: {1}", assembly.FullName, e);
                }
                catch (ReflectionTypeLoadException e)
                {
                    for (var i = 0; i < e.LoaderExceptions.Length; i++)
                    {
                        if (e.LoaderExceptions[i] != null)
                        {
                            Debug.LogError(e.Types[i] + ": " + e.LoaderExceptions[i].Message);
                        }
                    }
                }
            }

            m_Choices.Sort();
            UpdateValue();
        }

        List<Color> GetListOfColors()
        {
            List<Color> list = new List<Color>();
            list.Add(new Color(1, 0, 0));
            list.Add(new Color(0, 1, 0));
            list.Add(new Color(0, 0, 1));
            list.Add(new Color(1, 1, 0));
            list.Add(new Color(1, 0, 1));
            list.Add(new Color(0, 1, 1));
            list.Add(new Color(0.5f, 0, 0));
            list.Add(new Color(0, 0.5f, 0));
            list.Add(new Color(0, 0, 0.5f));
            list.Add(new Color(0.5f, 0.5f, 0));
            list.Add(new Color(0.5f, 0, 0.5f));
            list.Add(new Color(0, 0.5f, 0.5f));
            list.Add(new Color(0.25f, 0, 0));
            list.Add(new Color(0, 0.25f, 0));
            list.Add(new Color(0, 0, 0.25f));
            list.Add(new Color(0.25f, 0.25f, 0));
            list.Add(new Color(0.25f, 0, 0.25f));
            list.Add(new Color(0, 0.25f, 0.25f));
            list.Add(new Color(1, 0.5f, 0));
            list.Add(new Color(0.5f, 1, 0));
            list.Add(new Color(0.5f, 0, 1));
            list.Add(new Color(1, 1, 0.5f));
            list.Add(new Color(1, 0.5f, 1));
            list.Add(new Color(0.5f, 1, 1));
            list.Add(new Color(1, 0.25f, 0.75f));
            list.Add(new Color(0.5f, 1, 0.25f));
            list.Add(new Color(0.5f, 0.25f, 1));
            list.Add(new Color(0.5f, 0.5f, 0.5f));
            list.Add(new Color(1, 0.5f, 0.5f));
            list.Add(new Color(0.5f, 0.5f, 1));
            return list;
        }

        internal override string GetValueToDisplay()
        {
            return "Event types";
        }

        internal override string GetListItemToDisplay(ulong item)
        {
            StringValuePairs<long> v = null;

            foreach (var c in m_Choices)
            {
                if ((ulong)c.v == item)
                {
                    v = c;
                }
            }

            if (v != null)
            {
                if (m_FormatSelectedValueCallback != null)
                    return m_FormatSelectedValueCallback(v);
                return v.s;
            }
            return string.Empty;
        }

        enum EventTypeSelection
        {
            Mouse,
            Drag,
            Keyboard,
            Command,
            All
        }

        internal override void AddMenuItems(IGenericMenu menu)
        {
            menu.AddItem("Select all", false, () => SelectAllTypes(true));
            menu.AddItem("Select all mouse events (basic)", false, () => SelectAllTypes(true, EventTypeSelection.Mouse));
            menu.AddItem("Select all drag events", false, () => SelectAllTypes(true, EventTypeSelection.Drag));
            menu.AddItem("Select all keyboard events", false, () => SelectAllTypes(true, EventTypeSelection.Keyboard));
            menu.AddItem("Select all command events", false, () => SelectAllTypes(true, EventTypeSelection.Command));
            menu.AddSeparator(String.Empty);
            menu.AddItem("Unselect all", false, () => SelectAllTypes(false));
            menu.AddSeparator(String.Empty);
            foreach (var item in m_Choices)
            {
                menu.AddItem(item.s, m_State[item.v], () => ChangeValueFromMenu(item));
            }
        }

        void ChangeValueFromMenu(StringValuePairs<long> item)
        {
            m_State[item.v] = !m_State[item.v];
            UpdateValue();
        }

        void SelectAllTypes(bool state, EventTypeSelection eventTypeSelection = EventTypeSelection.All)
        {
            foreach (KeyValuePair<long, bool> v in m_State.ToList())
            {
                long eventTypeId = v.Key;

                if (eventTypeSelection == EventTypeSelection.All ||
                    (eventTypeSelection == EventTypeSelection.Mouse &&
                     (eventTypeId == MouseMoveEvent.TypeId() ||
                      eventTypeId == MouseOverEvent.TypeId() ||
                      eventTypeId == MouseDownEvent.TypeId() ||
                      eventTypeId == MouseUpEvent.TypeId() ||
                      eventTypeId == WheelEvent.TypeId() ||
                      eventTypeId == ContextClickEvent.TypeId())) ||
                    (eventTypeSelection == EventTypeSelection.Keyboard &&
                     (eventTypeId == KeyDownEvent.TypeId() ||
                      eventTypeId == KeyUpEvent.TypeId())) ||
                    (eventTypeSelection == EventTypeSelection.Drag &&
                     (eventTypeId == DragUpdatedEvent.TypeId() ||
                      eventTypeId == DragPerformEvent.TypeId() ||
                      eventTypeId == DragExitedEvent.TypeId())) ||
                    (eventTypeSelection == EventTypeSelection.Command &&
                     (eventTypeId == ValidateCommandEvent.TypeId() ||
                      eventTypeId == ExecuteCommandEvent.TypeId())))
                {
                    m_State[eventTypeId] = state;
                }
                else
                {
                    // Unaffected should be reset to false
                    m_State[eventTypeId] = false;
                }
            }

            UpdateValue();
        }

        void UpdateValue()
        {
            ulong v = 0;
            ulong flag = 1;

            foreach (var state in m_State)
            {
                if (state.Value)
                {
                    v |= flag;
                    flag <<= 1;
                }
            }

            value = v;
        }
    }
}
