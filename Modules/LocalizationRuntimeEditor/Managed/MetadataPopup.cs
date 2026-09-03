// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.Localization.Editor;

class MetadataPopup : EditorWindow
{
    const string k_Uxml = "LocalizationRuntime/UXML/MetadataPopup.uxml";

    string m_Title;
    SerializedObject m_SerializedObject;
    string m_MetadataPath;
    MetadataType m_Target;
    Action m_OnChanged;

    public static void Show(Rect activatorWorldBound, string title, Object owner, string metadataPath, MetadataType target, Action onChanged)
    {
        var window = CreateInstance<MetadataPopup>();
        window.m_Title = title;
        window.m_SerializedObject = owner != null ? new SerializedObject(owner) : null;
        window.m_MetadataPath = metadataPath;
        window.m_Target = target;
        window.m_OnChanged = onChanged;
        var screenRect = GUIUtility.GUIToScreenRect(activatorWorldBound);
        window.ShowAsDropDown(screenRect, new Vector2(320, 220));
    }

    void OnDisable()
    {
        m_SerializedObject?.Dispose();
        m_SerializedObject = null;
    }

    void CreateGUI()
    {
        var root = rootVisualElement;
        (EditorGUIUtility.Load(k_Uxml) as VisualTreeAsset).CloneTree(root);
        root.Q<Label>("title").text = m_Title;

        var prop = string.IsNullOrEmpty(m_MetadataPath) ? null : m_SerializedObject?.FindProperty(m_MetadataPath);
        if (prop != null)
            root.Q<VisualElement>("body").Add(MetadataEditor.Create(prop, m_Target, m_OnChanged));
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
