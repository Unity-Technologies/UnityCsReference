// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.Localization.Editor;

class MetadataPopup : AnchoredPopupWindow
{
    const string k_Uxml = "LocalizationRuntime/UXML/MetadataPopup.uxml";

    string m_Title;
    SerializedObject m_SerializedObject;
    string m_MetadataPath;
    MetadataType m_Target;
    Action m_OnChanged;

    public static void Show(Rect activatorWorldBound, VisualElement view, string title, Object owner, string metadataPath, MetadataType target, Action onChanged)
    {
        var window = CreateInstance<MetadataPopup>();
        window.m_Title = title;
        window.m_SerializedObject = owner != null ? new SerializedObject(owner) : null;
        window.m_MetadataPath = metadataPath;
        window.m_Target = target;
        // Metadata is shown in more than one window at a time, so every edit has to reach the others.
        window.m_OnChanged = () => { onChanged?.Invoke(); LocalizationEditorSettings.RaiseCollectionsChanged(); };
        window.ShowUnder(activatorWorldBound, view, new Vector2(320, 220));
    }

    protected override void OnDisable()
    {
        base.OnDisable();
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
