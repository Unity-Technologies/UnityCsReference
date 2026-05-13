// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.UIElements;

[Serializable]
[VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
class UIPrefColor : IPrefType
{
    string m_Name;
    string m_Category;
    [SerializeField]
    string m_StorageKey;
    [SerializeField] Color m_Color;
    Color m_DefaultColor;

    bool m_SeparateColors;
    [SerializeField] Color m_OptionalDarkColor;
    Color m_OptionalDarkDefaultColor;

    bool m_Loaded;

    public UIPrefColor()
    {
        m_Loaded = true;
    }

    public UIPrefColor(string category, string name, Color defaultColor)
    {
        m_Name = name;
        m_Category = category;
        m_StorageKey = $"{m_Category}/{m_Name}";
        m_Color = m_DefaultColor = defaultColor;
        m_SeparateColors = false;
        m_OptionalDarkColor = m_OptionalDarkDefaultColor = Color.clear;
        PrefSettings.Add(this);
        m_Loaded = false;
    }

    public UIPrefColor(string category, string name, Color defaultColor, Color optionalDarkColor)
    {
        m_Name = name;
        m_Category = category;
        m_StorageKey = $"{m_Category}/{m_Name}";
        m_Color = m_DefaultColor = defaultColor;
        m_SeparateColors = true;
        m_OptionalDarkColor = m_OptionalDarkDefaultColor = optionalDarkColor;
        PrefSettings.Add(this);
        m_Loaded = false;
    }

    public void Load()
    {
        if (m_Loaded)
            return;

        m_Loaded = true;

        var pk = PrefSettings.Get(m_StorageKey, this);
        m_Color = pk.m_Color;
        m_OptionalDarkColor = pk.m_OptionalDarkColor;
    }

    public Color Color
    {
        get
        {
            Load();

            if (m_SeparateColors && EditorGUIUtility.isProSkin)
                return m_OptionalDarkColor;

            return m_Color;
        }
        set
        {
            Load();

            if (m_SeparateColors && EditorGUIUtility.isProSkin)
                m_OptionalDarkColor = value;
            else
                m_Color = value;
        }
    }

    public string Category
    {
        get
        {
            Load();
            return m_Category;
        }
    }

    public string Name
    {
        get
        {
            Load();
            return m_Name;
        }
    }

    public string StorageKey
    {
        get
        {
            Load();
            return m_StorageKey;
        }
    }

    public static implicit operator Color(UIPrefColor prefColor) { return prefColor.Color; }

    public string ToUniqueString()
    {
        Load();

        return JsonUtility.ToJson(this);
    }

    public void FromUniqueString(string s)
    {
        Load();
        if (string.IsNullOrEmpty(s))
            return;
        try
        {
            var prefColor = JsonUtility.FromJson<UIPrefColor>(s);
            m_Color = prefColor.m_Color;
            m_OptionalDarkColor = prefColor.m_OptionalDarkColor;
        }
        catch (Exception)
        {
            EditorPrefs.DeleteKey(StorageKey);
        }
    }

    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal void ResetToDefault()
    {
        Load();
        m_Color = m_DefaultColor;
        m_OptionalDarkColor = m_OptionalDarkDefaultColor;
        EditorPrefs.SetString(StorageKey, ToUniqueString());
        PrefSettings.settingChanged?.Invoke(StorageKey, typeof(UIPrefColor));
    }
}
