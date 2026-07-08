// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Provides a small, chainable API handed to a control's default/variant configurator so it can set up a freshly
/// added element without exposing the underlying <see cref="VisualTreeAsset"/> / <see cref="VisualElementAsset"/> interface.
/// </summary>
readonly struct ElementConfigurationContext
{
    readonly VisualTreeAsset m_VisualTreeAsset;
    readonly VisualElementAsset m_ElementAsset;

    internal ElementConfigurationContext(VisualTreeAsset visualTreeAsset, VisualElementAsset elementAsset)
    {
        m_VisualTreeAsset = visualTreeAsset;
        m_ElementAsset = elementAsset;
    }

    internal VisualTreeAsset visualTreeAsset => m_VisualTreeAsset;
    internal VisualElementAsset elementAsset => m_ElementAsset;

    public ElementConfigurationContext SetAttribute<T>(string uxmlName, T value)
    {
        var serializedData = m_ElementAsset.serializedData;
        if (serializedData == null)
        {
            Debug.LogError($"Cannot set attribute '{uxmlName}': '{m_ElementAsset.fullTypeName}' has no serialized data.");
            return this;
        }

        var description = UxmlDescriptionRegistry.GetDescription(serializedData.GetType());
        if (!description.uxmlNameToIndex.TryGetValue(uxmlName, out var index))
        {
            Debug.LogError($"Unknown UXML attribute '{uxmlName}' on '{m_ElementAsset.fullTypeName}'.");
            return this;
        }

        var attribute = description.attributeDescriptions[index];
        attribute.serializedField.SetValue(serializedData, value);
        attribute.serializedFieldAttributeFlags.SetValue(serializedData, UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml);

        // Mirror the value into the UXML properties so it is exported and persisted.
        string stringValue = string.Empty;
        if (value != null && !UxmlAttributeConverter.TryConvertToString(value, m_VisualTreeAsset, out stringValue))
            stringValue = value.ToString();
        m_ElementAsset.SetAttribute(attribute.uxmlName, stringValue);

        return this;
    }

    public ElementConfigurationContext AddClass(string ussClassName)
    {
        if (!string.IsNullOrEmpty(ussClassName))
            m_ElementAsset.AddStyleClass(ussClassName);
        return this;
    }

    public ElementConfigurationContext SetStyle(string ussPropertyName, Color value)
    {
        if (TryGetOrCreateInlineProperty(ussPropertyName, out var sheet, out var property))
            property.SetColor(sheet, value);
        return this;
    }

    public ElementConfigurationContext SetStyle(string ussPropertyName, float value)
    {
        if (TryGetOrCreateInlineProperty(ussPropertyName, out var sheet, out var property))
            property.SetFloat(sheet, value);
        return this;
    }

    public ElementConfigurationContext SetStyle(string ussPropertyName, int value)
    {
        if (TryGetOrCreateInlineProperty(ussPropertyName, out var sheet, out var property))
            property.SetInt(sheet, value);
        return this;
    }

    public ElementConfigurationContext SetStyle(string ussPropertyName, Length value)
    {
        if (TryGetOrCreateInlineProperty(ussPropertyName, out var sheet, out var property))
            property.SetLength(sheet, value);
        return this;
    }

    public ElementConfigurationContext SetStyle(string ussPropertyName, string value)
    {
        if (TryGetOrCreateInlineProperty(ussPropertyName, out var sheet, out var property))
            property.SetString(sheet, value);
        return this;
    }

    public ElementConfigurationContext SetStyle(string ussPropertyName, Background value)
    {
        if (TryGetOrCreateInlineProperty(ussPropertyName, out var sheet, out var property))
            property.SetBackground(sheet, value);
        return this;
    }

    public ElementConfigurationContext SetStyle(string ussPropertyName, StyleKeyword value)
    {
        if (TryGetOrCreateInlineProperty(ussPropertyName, out var sheet, out var property))
            property.SetKeyword(sheet, value);
        return this;
    }

    public ElementConfigurationContext SetStyleEnum<TEnum>(string ussPropertyName, TEnum value) where TEnum : Enum
    {
        if (TryGetOrCreateInlineProperty(ussPropertyName, out var sheet, out var property))
            property.SetEnum(sheet, value);
        return this;
    }

    public ElementConfigurationContext AddChild<T>() where T : VisualElement => AddChild(typeof(T));

    public ElementConfigurationContext AddChild(Type elementType)
    {
        var child = m_VisualTreeAsset.AddElementOfType(m_ElementAsset, elementType.FullName);
        child.serializedData = UxmlSerializedDataCreator.CreateUxmlSerializedData(elementType);
        return new ElementConfigurationContext(m_VisualTreeAsset, child);
    }

    bool TryGetOrCreateInlineProperty(string ussPropertyName, out StyleSheet sheet, out StyleProperty property)
    {
        property = null;
        sheet = m_VisualTreeAsset.GetOrCreateInlineStyleSheet();

        if (!StylePropertyUtil.propertyNameToStylePropertyId.TryGetValue(ussPropertyName, out var id))
        {
            Debug.LogError($"Unknown USS property '{ussPropertyName}'.");
            return false;
        }

        StyleRule rule;
        if (m_ElementAsset.ruleIndex >= 0)
        {
            rule = sheet.rules[m_ElementAsset.ruleIndex];
        }
        else
        {
            m_ElementAsset.ruleIndex = sheet.rules.Length;
            rule = sheet.AddRule();
        }

        property = rule.AddProperty(id);
        return true;
    }
}
