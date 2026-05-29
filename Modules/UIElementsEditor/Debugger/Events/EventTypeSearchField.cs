// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Assemblies;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using Unity.Collections;

namespace UnityEditor.UIElements.Debugger
{
    class EventTypeChoice : IComparable<EventTypeChoice>
    {
        public string Name;
        public string Group;
        public long TypeId;

        public int CompareTo(EventTypeChoice other)
        {
            if (Group == Name)
            {
                var comparison = Group.CompareTo(other.Group);
                return comparison == 0 ? -1 : comparison;
            }

            if (other.Group == other.Name)
            {
                var comparison = Group.CompareTo(other.Group);
                return comparison == 0 ? 1 : comparison;
            }

            return Group.CompareTo(other.Group) * 2 + Name.CompareTo(other.Name);
        }
    }

    [UxmlElement]
    internal partial class EventTypeSearchField : ToolbarSearchField
    {
        const int k_MaxTooltipLines = 40;
        const string EllipsisText = "...";

        VisualElement m_MenuContainer;
        VisualElement m_OuterContainer;
        ListView m_ListView;

        Dictionary<long, bool> m_State;
        Dictionary<string, List<long>> m_GroupedEvents;
        List<EventTypeChoice> m_Choices;
        List<EventTypeChoice> m_FilteredChoices;
        Dictionary<long, int> m_EventCountLog;
        bool m_IsFocused;

        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        public int GetSelectedCount() => m_Choices.Count(c => c.TypeId > 0 && m_State[c.TypeId]);
#pragma warning restore UA2001

        public new static readonly string ussClassName = "event-debugger-filter";
        public static readonly string ussContainerClassName = ussClassName + "__container";
        public static readonly string ussListViewClassName = ussClassName + "__list-view";
        public static readonly string ussItemContainerClassName = ussClassName + "__item-container";
        public static readonly string ussItemLabelClassName = ussClassName + "__item-label";
        public static readonly string ussGroupLabelClassName = ussClassName + "__group-label";
        public static readonly string ussItemCountClassName = ussClassName + "__item-count";
        public static readonly string ussItemToggleClassName = ussClassName + "__item-toggle";

        public IReadOnlyDictionary<long, bool> State => m_State;

        public void SetState(Dictionary<long, bool> state)
        {
            if (state == null) return;
            foreach (var kvp in state)
            {
                if (m_State.ContainsKey(kvp.Key))
                    m_State[kvp.Key] = kvp.Value;
            }
            UpdateTextHint();
        }

        bool IsGenericTypeOf(Type t, Type genericDefinition)
        {
            Type[] parameters = null;
            return IsGenericTypeOf(t, genericDefinition, out parameters);
        }

        bool IsGenericTypeOf(Type t, Type genericDefinition, out Type[] genericParameters)
        {
            genericParameters = Array.Empty<Type>();
            if (!genericDefinition.IsGenericType)
            {
                return false;
            }

            var isMatch = t.IsGenericType && t.GetGenericTypeDefinition() == genericDefinition.GetGenericTypeDefinition();
            if (!isMatch && t.BaseType != null)
            {
                isMatch = IsGenericTypeOf(t.BaseType, genericDefinition, out genericParameters);
            }
            if (!isMatch && genericDefinition.IsInterface && t.GetInterfaces().Length > 0)
            {
                foreach (var i in t.GetInterfaces())
                {
                    if (IsGenericTypeOf(i, genericDefinition, out genericParameters))
                    {
                        isMatch = true;
                        break;
                    }
                }
            }

            if (isMatch && genericParameters.Length == 0)
            {
                genericParameters = t.GetGenericArguments();
            }
            return isMatch;
        }

        public EventTypeSearchField()
        {
            m_Choices = new List<EventTypeChoice>();
            m_State = new Dictionary<long, bool>();
            m_GroupedEvents = new Dictionary<string, List<long>>();

            HashSet<string> userAssemblies = new HashSet<string>(ScriptingRuntime.GetAllUserAssemblies());
            foreach (Assembly assembly in CurrentAssemblies.GetLoadedAssemblies())
            {
                if (userAssemblies.Contains(assembly.GetName().Name + ".dll"))
                    continue;

                try
                {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    foreach (var type in assembly.GetTypes().Where(t => typeof(EventBase).IsAssignableFrom(t) && !t.ContainsGenericParameters))
#pragma warning restore UA2001
#pragma warning restore RS0030
                    {
                        // Only select Pointer events on startup
                        AddType(type, IsGenericTypeOf(type, typeof(PointerEventBase<>)));
                    }

                    // Special case for ChangeEvent<>.
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    var implementingTypes = GetAllTypesImplementingOpenGenericType(typeof(INotifyValueChanged<>), assembly).ToList();
#pragma warning restore UA2001
                    foreach (var valueChangedType in implementingTypes)
                    {
                        var baseType = valueChangedType.BaseType;
                        if (baseType == null || baseType.GetGenericArguments().Length <= 0)
                            continue;

                        var argumentType = baseType.GetGenericArguments()[0];
                        if (!argumentType.IsGenericParameter)
                        {
                            AddType(typeof(ChangeEvent<>).MakeGenericType(argumentType), false);
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

            m_State.Add(0, false);

            // Add groups, with negative ids.
            var keyIndex = -1;
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var key in m_GroupedEvents.Keys.OrderBy(k => k))
#pragma warning restore UA2001
            {
                m_Choices.Add(new EventTypeChoice() { Name = key, Group = key, TypeId = keyIndex });
                m_State.Add(keyIndex--, key.Contains("IPointerEvent"));
            }

            m_Choices.Sort();
            m_Choices.Insert(0, new EventTypeChoice() { Name = "IAll", Group = "IAll", TypeId = 0 });
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_FilteredChoices = m_Choices.ToList();
#pragma warning restore UA2001

            m_MenuContainer = new VisualElement();
            m_MenuContainer.AddToClassList(ussClassName);

            m_OuterContainer = new VisualElement();
            m_OuterContainer.AddToClassList(ussContainerClassName);
            m_MenuContainer.Add(m_OuterContainer);

            m_ListView = new ListView();
            m_ListView.AddToClassList(ussListViewClassName);
            m_ListView.pickingMode = PickingMode.Position;
            m_ListView.showBoundCollectionSize = false;
            m_ListView.fixedItemHeight = 20;
            m_ListView.selectionType = SelectionType.None;
            m_ListView.showAlternatingRowBackgrounds = AlternatingRowBackground.All;

            m_ListView.makeItem = () =>
            {
                var container = new VisualElement();
                container.AddToClassList(ussItemContainerClassName);

                var toggle = new Toggle();
                toggle.labelElement.AddToClassList(ussItemLabelClassName);
                toggle.visualInput.AddToClassList(ussItemToggleClassName);
                toggle.RegisterValueChangedCallback(OnToggleValueChanged);
                container.Add(toggle);

                var label = new Label();
                label.AddToClassList(ussItemCountClassName);
                label.pickingMode = PickingMode.Ignore;
                container.Add(label);

                return container;
            };

            m_ListView.bindItem = (element, i) =>
            {
                var toggle = element[0] as Toggle;
                var countLabel = element[1] as Label;
                var choice = m_FilteredChoices[i];
                toggle.SetValueWithoutNotify(m_State[choice.TypeId]);
                var isGroup = choice.Name == choice.Group;

                toggle.label = isGroup ? $"{choice.Group.Substring(1).Replace("Event", "")} Events" : choice.Name;
                toggle.labelElement.RemoveFromClassList(isGroup ? ussItemLabelClassName : ussGroupLabelClassName);
                toggle.labelElement.AddToClassList(isGroup ? ussGroupLabelClassName : ussItemLabelClassName);
                toggle.userData = i;

                if (m_EventCountLog != null && m_EventCountLog.ContainsKey(choice.TypeId))
                {
                    countLabel.style.display = DisplayStyle.Flex;
                    countLabel.text = m_EventCountLog[choice.TypeId].ToString();
                }
                else
                {
                    countLabel.text = "";
                    countLabel.style.display = DisplayStyle.None;
                }
            };

            m_ListView.itemsSource = m_FilteredChoices;
            m_OuterContainer.Add(m_ListView);

            UpdateTextHint();

            m_MenuContainer.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            m_MenuContainer.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            textInputField.RegisterValueChangedCallback(OnValueChanged);

            RegisterCallback<FocusInEvent>(OnFocusIn);
            RegisterCallback<FocusEvent>(OnFocus);
            RegisterCallback<FocusOutEvent>(OnFocusOut);
        }

        public void SetEventLog(Dictionary<long, int> log)
        {
            m_EventCountLog = log;
        }

        static IEnumerable<Type> GetAllTypesImplementingOpenGenericType(Type openGenericType, Assembly assembly)
        {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return from x in assembly.GetTypes()
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                from z in x.GetInterfaces()
#pragma warning restore UA2001
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                let y = x.BaseType
#pragma warning restore UA2001
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    where (y != null && y.IsGenericType && openGenericType.IsAssignableFrom(y.GetGenericTypeDefinition())) ||
#pragma warning restore UA2001
                    (z.IsGenericType && openGenericType.IsAssignableFrom(z.GetGenericTypeDefinition()))
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    select x;
#pragma warning restore UA2001
#pragma warning restore RS0030
        }

        void AddType(Type type, bool value)
        {
            var methodInfo = type.GetMethod("TypeId", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (methodInfo == null || methodInfo.ContainsGenericParameters)
                return;

            var typeId = (long)methodInfo.Invoke(null, null);

            if (m_State.ContainsKey(typeId))
                return;

            m_State.Add(typeId, value);
            var nextType = type;

            bool InterfacePredicate(Type t) => t.IsPublic && t.Namespace != nameof(System);

            Type interfaceType;
            do
            {
                var previousType = nextType;
                nextType = previousType.BaseType;
#pragma warning disable UA2001, UA2011 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                interfaceType = previousType.GetInterfaces().Where(InterfacePredicate).Except(nextType.GetInterfaces().Where(InterfacePredicate)).FirstOrDefault();
#pragma warning restore UA2001, UA2011
            }
            while (interfaceType == null && nextType != typeof(EventBase));

            var readableTypeName = EventDebugger.GetTypeDisplayName(type);
            if (interfaceType != null)
            {
                if (!m_GroupedEvents.ContainsKey(interfaceType.Name))
                {
                    m_GroupedEvents.Add(interfaceType.Name, new List<long>());
                }

                m_GroupedEvents[interfaceType.Name].Add(typeId);
                m_Choices.Add(new EventTypeChoice { Name = readableTypeName, TypeId = typeId, Group = interfaceType.Name });
            }
            else
            {
                if (!m_GroupedEvents.ContainsKey("IUncategorized"))
                    m_GroupedEvents.Add("IUncategorized", new List<long>());

                m_GroupedEvents["IUncategorized"].Add(typeId);
                m_Choices.Add(new EventTypeChoice { Name = readableTypeName, TypeId = typeId, Group = "IUncategorized" });
            }
        }

        void OnToggleValueChanged(ChangeEvent<bool> e)
        {
            var element = e.elementTarget;
            var index = (int)element.userData;
            var choice = m_FilteredChoices[index];
            m_State[choice.TypeId] = e.newValue;

            if (choice.TypeId < 0)
            {
                foreach (var eventTypeId in m_GroupedEvents[choice.Group])
                {
                    m_State[eventTypeId] = e.newValue;
                }
            }
            else if (choice.TypeId == 0)
            {
                foreach (var c in m_Choices)
                {
                    m_State[c.TypeId] = e.newValue;
                }
            }

            // All toggling
            #pragma warning disable UA2001, UA2008 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (m_State.Where(s => s.Key > 0).All(s => s.Value))
#pragma warning restore UA2001, UA2008
            {
                m_State[0] = true;
            }
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
#pragma warning disable UA2006 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            else if (m_State.Where(s => s.Key > 0).Any(s => !s.Value))
#pragma warning restore UA2001
#pragma warning restore UA2006
            {
                m_State[0] = false;
            }

            // Group toggling
            if (choice.TypeId != 0)
            {
                var events = m_GroupedEvents[choice.Group];
                if (events.TrueForAll(id => m_State[id]))
                {
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    var group = m_Choices.First(c => c.TypeId < 0 && c.Group == choice.Group);
#pragma warning restore UA2001
                    m_State[group.TypeId] = true;
                }
                else if (events.Count > 0) // At least one element must be false, as we already checked TrueForAll and it was false
                {
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    var group = m_Choices.First(c => c.TypeId < 0 && c.Group == choice.Group);
#pragma warning restore UA2001
                    m_State[group.TypeId] = false;
                }
            }

            FilterEvents(value);
            using (var evt = ChangeEvent<string>.GetPooled(null, null))
            {
                evt.elementTarget = this;
                SendEvent(evt);
            }

            // Toggling "All Events" or a group of events will modify the appearance of individual items below it
            if (choice.TypeId <= 0)
            {
                m_ListView.RefreshItems();
            }
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (evt.destinationPanel == null)
                return;

            m_ListView.RegisterCallback<GeometryChangedEvent>(EnsureVisibilityInParent);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (evt.originPanel == null)
                return;

            m_ListView.UnregisterCallback<GeometryChangedEvent>(EnsureVisibilityInParent);
            m_MenuContainer.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            m_MenuContainer.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnFocusIn(FocusInEvent evt)
        {
            DropDown();
        }

        void OnFocus(FocusEvent evt)
        {
            if (!m_IsFocused)
            {
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                m_FilteredChoices = m_Choices.ToList();
#pragma warning restore UA2001
                m_ListView.itemsSource = m_FilteredChoices;
                m_ListView.RefreshItems();
                RefreshLayout();
                SetValueWithoutNotify("");
            }

            m_IsFocused = true;
        }

        void OnFocusOut(FocusOutEvent evt)
        {
            var focusedElement = evt.relatedTarget as VisualElement;
            if (focusedElement?.FindCommonAncestor(m_ListView) != m_ListView && focusedElement?.FindCommonAncestor(this) != this)
            {
                Hide();
                UpdateTextHint();
                m_IsFocused = false;
            }
            else
            {
                m_MenuContainer.schedule.Execute(Focus);
            }
        }

        void UpdateTextHint()
        {
            UpdateTooltip();
            var choiceCount = GetSelectedCount();
            base.SetValueWithoutNotify($"{choiceCount} selected event type{(choiceCount > 1 ? "s" : "")}");
        }

        void OnValueChanged(ChangeEvent<string> changeEvent)
        {
            FilterEvents(changeEvent.newValue.Trim());
        }

        // Use quicksearch instead?
        const string k_IsKeyword = "is:";
        static readonly string[] k_OnKeywords = { "on", "enabled", "true" };
        static readonly string[] k_OffKeywords = { "off", "disabled", "false" };

        void FilterEvents(string filter)
        {
            m_FilteredChoices.Clear();
            var filterLower = filter.ToLower();

            bool? isOn = null;
            var checkIsParameter = filter.StartsWith(k_IsKeyword);
            if (checkIsParameter)
            {
                var parameter = filter.Substring(k_IsKeyword.Length);
                if (k_OnKeywords.Contains(parameter))
                    isOn = true;
                else if (k_OffKeywords.Contains(parameter))
                    isOn = false;
            }

            foreach (var choice in m_Choices)
            {
                if (isOn != null && m_State[choice.TypeId] == isOn.Value)
                {
                    m_FilteredChoices.Add(choice);
                }
                else if (string.IsNullOrEmpty(filter) || choice.Name.ToLower().Contains(filterLower) || choice.Group.ToLower().Contains(filterLower))
                {
                    m_FilteredChoices.Add(choice);
                }
            }

            m_ListView.itemsSource = m_FilteredChoices;
            m_ListView.RefreshItems();
            RefreshLayout();
        }

        void DropDown()
        {
            var root = panel.GetRootVisualElement();

            if (m_MenuContainer.parent != root)
                root.Add(m_MenuContainer);

            m_MenuContainer.style.left = root.layout.x;
            m_MenuContainer.style.top = root.layout.y;
            m_MenuContainer.style.width = root.layout.width;
            m_MenuContainer.style.height = root.layout.height;

            m_OuterContainer.style.left = worldBound.x - root.layout.x;
            m_OuterContainer.style.top = worldBound.y - root.layout.y;

            ClearTooltip();
        }

        void Hide()
        {
            m_MenuContainer.RemoveFromHierarchy();
        }

        void EnsureVisibilityInParent(GeometryChangedEvent evt)
        {
            RefreshLayout();
        }

        void RefreshLayout()
        {
            var root = panel.GetRootVisualElement();
            if (root != null && !float.IsNaN(m_OuterContainer.layout.width) && !float.IsNaN(m_OuterContainer.layout.height))
            {
                var listViewPadding = m_ListView.resolvedStyle.paddingTop +
                    m_ListView.resolvedStyle.paddingBottom +
                    m_ListView.resolvedStyle.borderTopWidth + m_ListView.resolvedStyle.borderBottomWidth;
                var availableHeight = m_MenuContainer.layout.height - m_MenuContainer.layout.y - m_OuterContainer.layout.y;
                var contentHeight = m_ListView.fixedItemHeight * m_ListView.itemsSource.Count + listViewPadding
                    + m_OuterContainer.resolvedStyle.borderTopWidth + m_OuterContainer.resolvedStyle.borderBottomWidth
                    + m_OuterContainer.resolvedStyle.paddingTop + m_OuterContainer.resolvedStyle.paddingBottom;
                m_OuterContainer.style.height = Mathf.Min(availableHeight, Mathf.Max(contentHeight, m_ListView.fixedItemHeight + listViewPadding));

                if (resolvedStyle.width > m_OuterContainer.resolvedStyle.width)
                {
                    m_OuterContainer.style.width = resolvedStyle.width;
                }
            }
        }

        void UpdateTooltip()
        {
            var tooltipStr = new StringBuilder();
            var lineCount = 0;
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var selectedChoice in m_Choices.Where(c => c.TypeId > 0 && m_State[c.TypeId]))
#pragma warning restore UA2001
            {
                if (lineCount++ >= k_MaxTooltipLines)
                {
                    tooltipStr.AppendLine(EllipsisText);
                    break;
                }

                tooltipStr.AppendLine(selectedChoice.Name);
            }

            textInputField.tooltip = tooltipStr.ToString();
        }

        void ClearTooltip()
        {
            textInputField.tooltip = "Type in event name to filter the list. You can also use the keyword is:{on/off}.";
        }
    }
}
