// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Multiplayer.Internal;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor;

class FilteredInstancesListProperty : Foldout
{
    internal const string k_FreeRunRemoveDialogTitle = "Cannot Remove Free Running Instance";
    internal const string k_FreeRunRemoveDialogText = "This instance is currently running. To remove it, please terminate it in the Active Scenario Window.";
    internal const string k_DisabledInstanceHelpBoxUssClass = "unity-instance-field__disabled-helpbox";
    internal const string k_DisabledInstanceHelpBoxText = "This instance is currently running. To modify it, please terminate this instance in the Active Scenario Window.";
    internal const string k_FreeRunRemoveDialogOkButtonText = "Ok";
    internal const string k_DefaultInstanceName = "Instance";
    internal const string k_ItemFieldUssClass = "unity-instance-field__list-item";
    internal const string k_DuplicatedNameUssClass = "unity-instance-field__duplicated-name";
    internal const string k_DuplicatedServerUssClass = "unity-instance-field__multiple-servers";
} 

class FilteredInstancesListProperty<TController, TSettings> : FilteredInstancesListProperty
    where TController : InstanceController<TSettings>
    where TSettings : struct
{
    readonly List<Item> m_FilteredInstances = new();
    readonly ListView m_ListView;
    readonly OrchestratedScenario m_Scenario;
    readonly SerializedObject m_SerializedObject;
    readonly int m_CountLimit;
    readonly Dictionary<string, int> m_NameCounts = new();
    int m_ServersCount = 0;

    internal Func<IInstanceItem, bool> IsInstanceStartedAsFreeRunningOverride;
    internal Func<string, string, string, bool> DisplayDialogOverride;

    public FilteredInstancesListProperty(OrchestratedScenario scenario, int countLimit)
    {
        m_Scenario = scenario;
        m_SerializedObject = new SerializedObject(scenario);
        m_CountLimit = countLimit;

        m_ListView = new ListView();
        m_ListView.itemsSource = m_FilteredInstances;
        m_ListView.showFoldoutHeader = false;
        m_ListView.showBoundCollectionSize = false;
        m_ListView.reorderable = false;
        m_ListView.canStartDrag += _ => false;
        m_ListView.showAlternatingRowBackgrounds = AlternatingRowBackground.All;
        m_ListView.showBorder = true;
        m_ListView.showAddRemoveFooter = true;
        m_ListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
        m_ListView.viewDataKey = nameof(m_ListView);

        m_ListView.makeItem = MakeItem;
        m_ListView.bindItem = BindItem;
        m_ListView.onAdd += OnAdd;
        m_ListView.onRemove += OnRemove;

        Add(m_ListView);

        this.TrackSerializedObjectValue(m_SerializedObject, _ => OnScenarioUpdated());
        OnScenarioUpdated();
    }

    public new string tooltip
    {
        get => this.Q<Toggle>().tooltip;
        set => this.Q<Toggle>().tooltip = value;
    }

    void OnScenarioUpdated()
    {
        UpdateNameAndServersCount();

        var i = 0;
        var requiresRefresh = false;
        foreach (var item in GetAllInstancesOfType())
        {
            if (i >= m_FilteredInstances.Count
                || item.InstanceId != m_FilteredInstances[i].InstanceId
                || item.ScenarioIndex != m_FilteredInstances[i].ScenarioIndex)
            {
                requiresRefresh = true;
                break;
            }
            i++;
        }

        if (i != m_FilteredInstances.Count || requiresRefresh)
        {
            RefreshListForced();
        }
        else
        {
            RefreshAllWarningIcons();
        }
    }

    void UpdateNameAndServersCount()
    {
        m_NameCounts.Clear();
        m_ServersCount = 0;
        foreach (var item in m_Scenario.GetAllInstances())
        {
            var name = item.GetName();
            m_NameCounts.TryGetValue(name, out var count);
            m_NameCounts[name] = count + 1;

            if (ScenarioFactory.GetRoleForInstance(item).HasFlag(MultiplayerRoleFlags.Server))
            {
                m_ServersCount++;
            }
        }
    }

    void RefreshListForced()
    {
        m_FilteredInstances.Clear();

        foreach (var item in GetAllInstancesOfType())
        {
            m_FilteredInstances.Add(item);
        }

        m_ListView.allowAdd = m_FilteredInstances.Count < m_CountLimit;
        m_ListView.RefreshItems();
    }

    void RefreshAllWarningIcons()
    {
        m_ListView.Query(className: k_ItemFieldUssClass).ForEach(field =>
        {
            if (field.userData is not Item item)
                return;

            var instanceItem = GetInstanceItemAtScenarioIndex(item.ScenarioIndex);
            var name = instanceItem.GetName();
            var role = ScenarioFactory.GetRoleForInstance(instanceItem);

            var isDuplicatedName = m_NameCounts.TryGetValue(name, out var nameCount) && nameCount > 1;
            var isDuplicatedServer = role.HasFlag(MultiplayerRoleFlags.Server) && m_ServersCount > 1;

            SetClassState(field, k_DuplicatedNameUssClass, isDuplicatedName);
            SetClassState(field, k_DuplicatedServerUssClass, isDuplicatedServer);
        });
    }

    static void SetClassState(VisualElement element, string className, bool state)
    {
        if (state)
        {
            element.AddToClassList(className);
        }
        else
        {
            element.RemoveFromClassList(className);
        }
    }

    IEnumerable<Item> GetAllInstancesOfType()
    {
        var index = 0;
        foreach (var item in m_Scenario.Settings.GetAllInstanceItems())
        {
            if (item is InstanceItem<TController, TSettings>
                && m_Scenario.IsInstanceEnabled(item))
            {
                yield return new Item { ScenarioIndex = index, InstanceId = item.GetId() };
            }
            index++;
        }
    }

    VisualElement MakeItem() => new();
    void BindItem(VisualElement element, int index)
    {
        var item = m_FilteredInstances[index];
        var property = m_SerializedObject.FindProperty(ScenarioConfigEditor.k_InstancesListPropertyPath).GetArrayElementAtIndex(item.ScenarioIndex);
        var propertyField = new PlainPropertyField(property);
        propertyField.Bind(m_SerializedObject);

        element.Clear();
        element.AddToClassList(k_ItemFieldUssClass);
        element.userData = item;

        var instanceItem = GetInstanceItemAtScenarioIndex(item.ScenarioIndex);
        var instanceName = instanceItem.GetName();
        var instanceRole = ScenarioFactory.GetRoleForInstance(instanceItem);
        var isDuplicatedName = m_NameCounts.TryGetValue(instanceName, out var nameCount) && nameCount > 1;
        var isDuplicatedServer = instanceRole.HasFlag(MultiplayerRoleFlags.Server) && m_ServersCount > 1;
        SetClassState(element, k_DuplicatedNameUssClass, isDuplicatedName);
        SetClassState(element, k_DuplicatedServerUssClass, isDuplicatedServer);

        if (HasStartedAsFreeRunning(instanceItem))
        {
            propertyField.SetEnabled(false);
            var helpBox = new HelpBox(k_DisabledInstanceHelpBoxText, HelpBoxMessageType.Info);
            helpBox.AddToClassList(k_DisabledInstanceHelpBoxUssClass);
            element.Add(helpBox);
        }

        element.Add(propertyField);
    }

    bool HasStartedAsFreeRunning(IInstanceItem instanceItem)
    {
        if (IsInstanceStartedAsFreeRunningOverride != null)
        {
            return IsInstanceStartedAsFreeRunningOverride(instanceItem);
        }

        var instance = m_Scenario.Scenario?.GetInstanceById(instanceItem.GetId());
        return instance != null && instance.IsFreeRunMode() && instance.HasStartedAsFreeRunning();
    }

    void OnAdd(BaseListView listView)
    {
        AddSerializedInstance(m_SerializedObject, GenerateNewInstanceName());
        m_SerializedObject.ApplyModifiedProperties();
        RefreshListForced();
        listView.ScrollToItem(m_FilteredInstances.Count - 1);
    }

    static SerializedProperty AddSerializedInstance(SerializedObject serializedScenario, string name)
    {
        var serializedItemsList = serializedScenario.FindProperty(ScenarioConfigEditor.k_InstancesListPropertyPath);
        var instanceItem = new InstanceItem<TController, TSettings>(
            name,
            InstanceController<TSettings>.GetDefaultSettings());

        serializedItemsList.arraySize++;
        var property = serializedItemsList.GetArrayElementAtIndex(serializedItemsList.arraySize - 1);
        property.boxedValue = instanceItem;
        return property;
    }

    void OnRemove(BaseListView listView)
    {
        var last = m_FilteredInstances.Count - 1;
        var index = listView.selectedIndex == -1 ? last : Mathf.Min(listView.selectedIndex, last);
        if (index < 0)
            return;

        listView.selectedIndex = -1;

        if (HasStartedAsFreeRunning(GetInstanceItemAtFilteredIndex(index)))
        {
            DisplayDialog(k_FreeRunRemoveDialogTitle, k_FreeRunRemoveDialogText, k_FreeRunRemoveDialogOkButtonText);
            return;
        }

        var instancesProperty = m_SerializedObject.FindProperty(ScenarioConfigEditor.k_InstancesListPropertyPath);
        instancesProperty.DeleteArrayElementAtIndex(MapToIndexInScenario(index));
        m_SerializedObject.ApplyModifiedProperties();
        RefreshListForced();
        listView.ScrollToItem(index - 1);
    }

    bool DisplayDialog(string title, string message, string okButton)
    {
        if (DisplayDialogOverride != null)
        {
            return DisplayDialogOverride(title, message, okButton);
        }

        return EditorUtility.DisplayDialog(title, message, okButton);
    }

    string GenerateNewInstanceName()
    {
        if (!NameExists(k_DefaultInstanceName))
            return k_DefaultInstanceName;

        var count = 1;
        while (NameExists($"{k_DefaultInstanceName} ({count})"))
        {
            count++;
        }
        return $"{k_DefaultInstanceName} ({count})";
    }

    bool NameExists(string name)
    {
        foreach (var item in m_Scenario.GetAllInstances())
        {
            if (item.GetName() == name)
            {
                return true;
            }
        }

        return false;
    }

    internal int MapToIndexInScenario(int filteredIndex)
    {
        return m_FilteredInstances[filteredIndex].ScenarioIndex;
    }

    internal InstanceItem<TController, TSettings> GetInstanceItemAtFilteredIndex(int filteredIndex)
    {
        return GetInstanceItemAtScenarioIndex(MapToIndexInScenario(filteredIndex));
    }

    internal InstanceItem<TController, TSettings> GetInstanceItemAtScenarioIndex(int scenarioIndex)
    {
        return (InstanceItem<TController, TSettings>)m_Scenario.Settings[scenarioIndex];
    }

    struct Item
    {
        public int ScenarioIndex;
        public GUID InstanceId;
    }
}
