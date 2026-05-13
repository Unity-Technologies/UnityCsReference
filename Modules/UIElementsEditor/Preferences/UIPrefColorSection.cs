// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements;

[UxmlElement]
partial class UIPrefColorSection : VisualElement
{
    public const string UssClass = "ui-pref-color-section";
    public const string ResetToDefaultUssClass = UssClass + "__reset-to-default";

    const string k_StyleSheet = "Settings/UIPrefColorSection.uss";

    Button m_ResetToDefault;

    string m_ColorPrefix;

    [UxmlAttribute]
    public string ColorPrefix
    {
        get => m_ColorPrefix;
        set
        {
            if (string.CompareOrdinal(m_ColorPrefix, value) == 0)
                return;
            m_ColorPrefix = value;
            RefreshColors();
        }
    }

    public UIPrefColorSection()
    {
        styleSheets.Add(EditorGUIUtility.Load(k_StyleSheet) as StyleSheet);
        AddToClassList(UssClass);
        AddToClassList("unity-inspector-element");

        m_ResetToDefault = new Button(OnResetToDefault)
        {
            text = "Use Defaults"
        };
        m_ResetToDefault.AddToClassList(ResetToDefaultUssClass);
        RegisterCallback<ChangeEvent<Color>>(OnColorChanged);
    }

    void RefreshColors()
    {
        ClearColors();

        var colors = GetAllColorsFromCategory(ColorPrefix);
        for (var i = 0; i < colors.Length; ++i)
        {
            var color = colors[i];
            var field = new ColorField(color.Name) { showAlpha = false, value = color.Color };
            field.AddToClassList(ColorField.alignedFieldUssClassName);
            field.userData = color;
            Add(field);
        }
        Add(m_ResetToDefault);
    }

    void ClearColors()
    {
        this.Query<ColorField>().ForEach(field =>
        {
            field.userData = null;
        });
        Clear();
    }

    void OnColorChanged(ChangeEvent<Color> evt)
    {
        var target = evt.elementTarget;
        var newColor = evt.newValue;

        var prefColor = target.userData as UIPrefColor;
        if (prefColor == null)
            return;
        prefColor.Color = newColor;
        PrefSettings.Set(prefColor.StorageKey, prefColor);
    }

    void OnResetToDefault()
    {
        this.Query<ColorField>().ForEach(field =>
        {
            var prefColor = (UIPrefColor)field.userData;
            prefColor.ResetToDefault();
            field.value = prefColor;
        });
        PrefSettings.settingsReverted?.Invoke();
    }

    static UIPrefColor[] GetAllColorsFromCategory(string category)
    {
        using var _= ListPool<UIPrefColor>.Get(out var list);
        foreach (var (name, prefColor) in PrefSettings.Prefs<UIPrefColor>())
        {
            if (string.CompareOrdinal(prefColor.Category, category) != 0)
                continue;
            list.Add(prefColor);
        }
        list.Sort((lhs, rhs) => string.CompareOrdinal(lhs.Name, rhs.Name));
        return list.ToArray();
    }
}
