// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor;

class AdbDeviceField : VisualElement
{
    const string k_Label = "Run Device";
    const string k_RefreshButtonText = "Refresh";
    const string k_NoDeviceItem = "<None>";
    const string k_NoAdbConnectedTooltip = "Cannot fetch device list. Please ensure the Android SDK path is correctly set in Preferences > External Tools.";
    const string k_NoDeviceConnectedTooltip = "No device connected. Please go to https://docs.unity3d.com/Manual/android-debugging-on-an-android-device.html for more information on how to connect a device.";
    const string k_NoDeviceSelectedTooltip = "Please select a device to run the instance on before running the scenario.";

    PopupField<string> m_DeviceField;
    SerializedProperty m_DeviceNameProperty;
    SerializedProperty m_DeviceIdProperty;
    FieldIcon<string> m_WarningIcon;

    internal Action<List<string>> FetchDeviceListOverride;
    internal Func<bool> IsAdbAvailableOverride;

    public AdbDeviceField(SerializedProperty deviceNameProperty, SerializedProperty deviceIdProperty)
    {
        var refreshButton = new Button(RefreshDeviceList) { text = k_RefreshButtonText };

        m_DeviceNameProperty = deviceNameProperty;
        m_DeviceIdProperty = deviceIdProperty;
        m_DeviceField = new PopupField<string>() { label = k_Label, value = deviceNameProperty.stringValue };
        m_DeviceField.choices = new List<string>(1);
        m_DeviceField.AddToClassList("unity-base-field__aligned");
        m_DeviceField.RegisterValueChangedCallback(evt => OnDeviceChanged(evt.newValue));
        m_WarningIcon = new FieldIcon<string>(m_DeviceField, Icons.ImageName.Warning)
        {
            tooltip = k_NoDeviceSelectedTooltip
        };

        Add(m_DeviceField);
        Add(refreshButton);

        this.RegisterCallback<AttachToPanelEvent>(evt => RefreshDeviceList());
    }

    void RefreshDeviceList()
    {
        var count = FetchDeviceList(m_DeviceField.choices);

        if (count == 0)
        {
            m_DeviceField.value = k_NoDeviceItem;
            m_DeviceField[2].SetEnabled(false);
        }
        else
        {
            m_DeviceField[2].SetEnabled(true);
            m_DeviceField.value = m_DeviceField.choices.Contains(m_DeviceField.value) ? m_DeviceField.value : k_NoDeviceItem;
        }

        OnDeviceChanged(m_DeviceField.value);
    }

    bool IsAdbAvailable()
    {
        if (IsAdbAvailableOverride != null)
        {
            return IsAdbAvailableOverride();
        }

        return AdbUtilities.IsAdbAvailable();
    }

    int FetchDeviceList(List<string> deviceList)
    {
        deviceList.Clear();
        deviceList.Add(k_NoDeviceItem);

        if (FetchDeviceListOverride != null)
        {
            FetchDeviceListOverride(deviceList);
            return deviceList.Count - 1;
        }

        deviceList.AddRange(AdbUtilities.GetADBDevicesDetailed());
        return deviceList.Count - 1;
    }

    void OnDeviceChanged(string newValue)
    {
        if (string.IsNullOrWhiteSpace(newValue))
            newValue = k_NoDeviceItem;

        if (newValue == k_NoDeviceItem)
        {
            m_DeviceNameProperty.stringValue = "";
            m_DeviceIdProperty.stringValue = "";
            m_WarningIcon.style.display = DisplayStyle.Flex;
            m_WarningIcon.tooltip = ComputeNoDeviceSelectedWarningTooltip();
        }
        else
        {
            m_DeviceNameProperty.stringValue = newValue;
            m_DeviceIdProperty.stringValue = GetDeviceIdFromLabel(newValue);
            m_WarningIcon.style.display = DisplayStyle.None;
            m_WarningIcon.tooltip = "";
        }

        m_DeviceNameProperty.serializedObject.ApplyModifiedProperties();
        m_DeviceIdProperty.serializedObject.ApplyModifiedProperties();
    }

    string ComputeNoDeviceSelectedWarningTooltip()
    {
        if (!IsAdbAvailable())
            return k_NoAdbConnectedTooltip;

        if (m_DeviceField.choices.Count <= 1)
            return k_NoDeviceConnectedTooltip;

        return k_NoDeviceSelectedTooltip;
    }

    static string GetDeviceIdFromLabel(string deviceLabel)
    {
        var openParenIndex = deviceLabel.LastIndexOf('(');
        var closeParenIndex = deviceLabel.LastIndexOf(')');
        
        if (openParenIndex == -1 || closeParenIndex == -1 || closeParenIndex <= openParenIndex)
            return string.Empty;
        
        return deviceLabel.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1);
    }
}
