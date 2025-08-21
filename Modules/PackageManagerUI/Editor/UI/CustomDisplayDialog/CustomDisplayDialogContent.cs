// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal;

internal class CustomDisplayDialogContent : ModalContent
{
    public CustomDialogArgsBase args { get; }
    public DialogResult result { get; private set; } = DialogResult.Cancel;

    private string m_ReadMoreUrl;

    private IApplicationProxy m_ApplicationProxy;
    private IResourceLoader m_ResourceLoader;

    private void ResolveDependencies(IApplicationProxy applicationProxy, IResourceLoader resourceLoader)
    {
        m_ApplicationProxy = applicationProxy;
        m_ResourceLoader = resourceLoader;
    }

    public CustomDisplayDialogContent(IApplicationProxy applicationProxy, IResourceLoader resourceLoader, CustomDialogArgsBase dialogArgs)
    {
        ResolveDependencies(applicationProxy, resourceLoader);

        args = dialogArgs;
        windowTitle = args.windowTitle;

        Init();
    }

    private void Init()
    {
        var root = m_ResourceLoader.GetTemplate("CustomDisplayDialog.uxml");
        cache = new VisualElementCache(root);
        styleSheets.Add(m_ResourceLoader.packageManagerCommonStyleSheet);
        styleSheets.Add(m_ResourceLoader.customDisplayDialogStyleSheet);
        Add(root);

        headerIcon.AddToClassList(args.headerIcon.ClassName());
        headerMainLabel.text = args.headerMainText;
        headerSubLabel.text = args.headerSubText;

        helpBoxIcon.AddToClassList(args.headerInfoBoxIcon.ClassName());
        helpBoxLabel.text = args.headerInfoBoxText;

        var showInfoBox = !string.IsNullOrEmpty(args.headerInfoBoxText);
        var showHeaderMainLabel = !string.IsNullOrEmpty(args.headerMainText);
        var showHeaderSubLabel = !string.IsNullOrEmpty(args.headerSubText);
        var showHeader = showHeaderMainLabel || showHeaderSubLabel;
        UIUtils.SetElementDisplay(helpBoxContainer, showInfoBox);
        UIUtils.SetElementDisplay(headerContainer, showHeader);
        UIUtils.SetElementDisplay(upperContainer, showHeader || showInfoBox);
        UIUtils.SetElementDisplay(headerMainLabel, showHeaderMainLabel);
        UIUtils.SetElementDisplay(headerSubLabel, showHeaderSubLabel);

        bodyLabel.text = args.bodyText;
        UIUtils.SetElementDisplay(bodyScroll, !string.IsNullOrEmpty(args.bodyText));

        bodyScroll.style.maxHeight = 500f;

        if (string.IsNullOrEmpty(args.readMoreUrl))
            UIUtils.SetElementDisplay(readMoreButton, false);
        else
        {
            m_ReadMoreUrl = args.readMoreUrl;
            readMoreButton.clicked += OnReadMoreClicked;
        }

        foreach (var button in args.buttons)
        {
            var buttonElement = new Button { text = button.text };
            buttonElement.clicked += () =>
            {
                result = button.result;
                container?.Close();
            };
            buttonsContainer.Add(buttonElement);

            if (button.result == DialogResult.DefaultAction)
                buttonElement.Focus();
        }

        RegisterCallback<GeometryChangedEvent>(OnFirstLayout);
    }

    public override void OnBeforeShowModal() { }

    public override void OnModalClosed() {}

    private void OnFirstLayout(GeometryChangedEvent evt)
    {
        UnregisterCallback<GeometryChangedEvent>(OnFirstLayout);

        var fixedSize = new Vector2(500f, lowerContainer.layout.height + upperContainer.layout.height);
        container.minSize = fixedSize;
        container.maxSize = fixedSize;
    }

    private void OnReadMoreClicked()
    {
        if (string.IsNullOrEmpty(m_ReadMoreUrl))
            return;

        m_ApplicationProxy.OpenURL(m_ReadMoreUrl);
        PackageManagerReadMoreClickedAnalytics.SendEvent(args.readMoreClickedAnalyticsId, m_ReadMoreUrl);
    }

    private VisualElementCache cache { get; set; }
    private VisualElement upperContainer => cache.Get<VisualElement>("upperContainer");
    private VisualElement headerContainer => cache.Get<VisualElement>("headerContainer");
    private Label headerMainLabel => cache.Get<Label>("headerMainLabel");
    private Label headerSubLabel => cache.Get<Label>("headerSubLabel");
    private VisualElement headerIcon => cache.Get<VisualElement>("headerIcon");
    private VisualElement helpBoxContainer => cache.Get<VisualElement>("helpBoxContainer");
    private VisualElement helpBoxIcon => cache.Get<VisualElement>("helpBoxIcon");
    private Label helpBoxLabel => cache.Get<Label>("helpBoxLabel");
    private ScrollView bodyScroll => cache.Get<ScrollView>("bodyScroll");
    private VisualElement lowerContainer => cache.Get<VisualElement>("lowerContainer");
    private Label bodyLabel => cache.Get<Label>("bodyLabel");
    private Button readMoreButton => cache.Get<Button>("readMoreButton");
    private VisualElement buttonsContainer => cache.Get<VisualElement>("buttonsContainer");
}
