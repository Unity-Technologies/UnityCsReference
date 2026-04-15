// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
abstract partial class CodePreview<T> : VisualElement
    where T: ScriptableObject
{
    [Serializable]
    public new class UxmlSerializedData : VisualElement.UxmlSerializedData
    {
        [Conditional("UNITY_EDITOR")]
        public new static void Register()
            => UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), [], true);
    }

    const string k_VisualTreeAsset = "UIToolkitAuthoring/Controls/CodePreview.uxml";
    const string k_StyleSheetDark = "UIToolkitAuthoring/Controls/CodePreviewDark.uss";
    const string k_StyleSheetLight = "UIToolkitAuthoring/Controls/CodePreviewLight.uss";
    const int k_OpenInIDELineNumber = 1;

    public const string UssClass = "unity-code-preview";
    public const string TitleUssClass = UssClass + "__title";
    public const string AssetPathUssClass = UssClass + "__asset-path";
    public const string OpenInIdeUssClass = UssClass + "__open-source-file-button";
    public const string HideOpenInIdeUssClass = OpenInIdeUssClass + "--hidden";
    public const string ScrollViewUssClass = UssClass + "__scroll-view";
    public const string LineNumbersUssClass = UssClass + "__code-line-numbers";
    public const string CodeUssClass = UssClass + "__code-text";

    readonly Label m_Title;
    readonly Label m_AssetPath;
    readonly Button m_OpenInIde;
    readonly Label m_LineNumbers;
    readonly TextField m_Code;

    readonly IAuthoringLiveReloadAssetTracker<T> m_Tracker;

    T m_Asset;

    public T Asset
    {
        get => m_Asset;
        set
        {
            if (m_Asset == value)
                return;
            Release(m_Asset);
            m_Asset = value;
            Acquire(m_Asset);
            Refresh();
        }
    }

    public CodePreview()
    {
        AddToClassList(UssClass);
        var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        if (vta)
            vta.CloneTree(this);

        var styleSheetPath = EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight;
        var styleSheet = EditorGUIUtility.Load(styleSheetPath) as StyleSheet;
        if (styleSheet)
            styleSheets.Add(styleSheet);

        var scrollView = this.Q<ScrollView>(className: ScrollViewUssClass);
        scrollView.horizontalScroller.lowButton.focusable = true;
        scrollView.horizontalScroller.highButton.focusable = true;
        scrollView.verticalScroller.lowButton.focusable = true;
        scrollView.verticalScroller.highButton.focusable = true;

        m_Title = this.Q<Label>(className: TitleUssClass);
        m_Title.text = GetTitle();
        m_AssetPath = this.Q<Label>(className: AssetPathUssClass);
        m_OpenInIde = this.Q<Button>(className: OpenInIdeUssClass);
        m_OpenInIde.clicked += OnOpenSourceFileButtonClick;

        m_LineNumbers = this.Q<Label>(className: LineNumbersUssClass);
        m_LineNumbers.RemoveFromClassList(TextElement.ussClassName);

        m_Code = this.Q<TextField>(className: CodeUssClass);
        m_Code.textSelection.isSelectable = true;
        m_Code.RemoveFromClassList(TextField.ussClassName);

        m_Tracker = CreateTracker();
        Refresh();
    }

    protected void Refresh()
    {
        var assetPath = AssetDatabase.GetAssetPath(Asset);
        var isAssetOnDisk = !string.IsNullOrEmpty(assetPath);
        m_AssetPath.text = isAssetOnDisk
            ? Path.GetFileName(assetPath)
            : "<unsaved file>" + GetExtension();
        m_OpenInIde.EnableInClassList(HideOpenInIdeUssClass, !isAssetOnDisk);
        SetCode(GenerateCodePreview());
    }

    protected abstract string GetTitle();
    protected abstract string GetExtension();

    protected abstract IAuthoringLiveReloadAssetTracker<T> CreateTracker();
    protected abstract void RegisterTracker(ILiveReloadSystem liveReloadSystem, IAuthoringLiveReloadAssetTracker<T> tracker, T asset);
    protected abstract void UnregisterTracker(ILiveReloadSystem liveReloadSystem, IAuthoringLiveReloadAssetTracker<T> tracker, T asset);

    protected abstract string GenerateCodePreview();

    void SetCode(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            m_LineNumbers.text = string.Empty;
            m_Code.value = string.Empty;
            return;
        }

        var lineCount = CountLines(code);
        var lineNumbersText = "";
        for (var i = 1; i <= lineCount; ++i)
        {
            if (!string.IsNullOrEmpty(lineNumbersText))
                lineNumbersText += "\n";

            lineNumbersText += i.ToString();
        }

        m_LineNumbers.text = lineNumbersText;

        m_Code.value = code;
    }

    void Release(T asset)
    {
        if (!asset)
            return;

        var liveReloadSystem = ((Panel)panel)?.liveReloadSystem;
        if (liveReloadSystem != null)
            UnregisterTracker(liveReloadSystem, m_Tracker, asset);
    }

    void Acquire(T asset)
    {
        if (!asset)
            return;

        var liveReloadSystem = ((Panel)panel)?.liveReloadSystem;
        if (liveReloadSystem != null)
            RegisterTracker(liveReloadSystem, m_Tracker, asset);
    }

    protected override void HandleEventBubbleUp(EventBase evt)
    {
        switch (evt)
        {
            case AttachToPanelEvent attachToPanelEvent:
                if (attachToPanelEvent.destinationPanel != null)
                    Acquire(Asset);
                break;

            case DetachFromPanelEvent detachFromPanel:
                if (detachFromPanel.originPanel != null)
                    Release(Asset);
                break;
        }
        base.HandleEventBubbleUp(evt);
    }

    int CountLines(string code)
    {
        var count = 0;
        foreach (var t in code)
            if (t == '\n')
                count++;

        return count + (!string.IsNullOrEmpty(code) ? 1 : 0);
    }

    void OnOpenSourceFileButtonClick()
    {
        AssetDatabase.OpenAsset(Asset, k_OpenInIDELineNumber);
    }
}
