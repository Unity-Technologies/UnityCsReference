// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal;

internal class CreatePackageDropdown : DropdownContent
{
    private static readonly Vector2 k_DefaultWindowSize = new(320, 52);
    private static readonly Vector2 k_WindowSizeWithError = new(320, 94);
    private static readonly string k_GeneralExceptionErrorMessage = L10n.Tr("An error occured while creating the package. See console for more details.");

    internal override Vector2 windowSize => string.IsNullOrEmpty(errorInfoBox.text) ? k_DefaultWindowSize : k_WindowSizeWithError;
    private TextFieldPlaceholder m_PackageNamePlaceholder;
    private EditorWindow m_AnchorWindow;

    private IResourceLoader m_ResourceLoader;
    private IPackageCreator m_PackageCreator;
    private void ResolveDependencies(IResourceLoader resourceLoader, IPackageCreator packageCreator)
    {
        m_ResourceLoader = resourceLoader;
        m_PackageCreator = packageCreator;
    }

    public CreatePackageDropdown(IResourceLoader resourceLoader, IPackageCreator packageCreator, EditorWindow anchorWindow)
    {
        ResolveDependencies(resourceLoader, packageCreator);

        styleSheets.Add(m_ResourceLoader.inputDropdownStyleSheet);

        var root = m_ResourceLoader.GetTemplate("CreatePackageDropdown.uxml");
        Add(root);
        cache = new VisualElementCache(root);

        Init(anchorWindow);
    }

    private void Init(EditorWindow anchorWindow)
    {
        m_AnchorWindow = anchorWindow;

        packageDisplayNameField.RegisterCallback<ChangeEvent<string>>(OnTextFieldChange);
        packageDisplayNameField.RegisterCallback<KeyDownEvent>(OnKeyDownShortcut, TrickleDown.TrickleDown);
        m_PackageCreator.onPackageCreated += OnPackageCreated;

        m_PackageNamePlaceholder = new TextFieldPlaceholder(packageDisplayNameField, L10n.Tr("Package display name"));

        submitButton.clickable.clicked += SubmitClicked;
    }

    internal override void OnDropdownShown()
    {
        inputForm.SetEnabled(true);

        // If we show a DropdownElement (dropdown filled with url values), we don't use the anchor window
        if (container != null)
            m_AnchorWindow?.rootVisualElement?.SetEnabled(false);

        packageDisplayNameField.Focus();
        submitButton.SetEnabled(!string.IsNullOrWhiteSpace(packageDisplayNameField.value));
    }

    internal override void OnDropdownClosed()
    {
        m_PackageNamePlaceholder.OnDisable();

        packageDisplayNameField.UnregisterCallback<ChangeEvent<string>>(OnTextFieldChange);
        packageDisplayNameField.UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut, TrickleDown.TrickleDown);
        m_PackageCreator.onPackageCreated -= OnPackageCreated;

        if (m_AnchorWindow != null)
        {
            m_AnchorWindow.rootVisualElement.SetEnabled(true);
            m_AnchorWindow = null;
        }

        submitButton.clickable.clicked -= SubmitClicked;
    }

    private void OnTextFieldChange(ChangeEvent<string> evt)
    {
        submitButton.SetEnabled(!string.IsNullOrWhiteSpace(packageDisplayNameField.value));
    }

    private void OnKeyDownShortcut(KeyDownEvent evt)
    {
        switch (evt.keyCode)
        {
            case KeyCode.Escape:
                Close();
                break;

            case KeyCode.Return:
            case KeyCode.KeypadEnter:
                SubmitClicked();
                break;
        }
    }

    private void SubmitClicked()
    {
        var packageDisplayName = packageDisplayNameField.value?.Trim();
        if (string.IsNullOrEmpty(packageDisplayName))
            return;

        inputForm.SetEnabled(false);
        try
        {
            m_PackageCreator.CreatePackage(packageDisplayName);
        }
        catch (ArgumentException e)
        {
            RefreshErrors(e.Message);
        }
        catch (Exception)
        {
            RefreshErrors(k_GeneralExceptionErrorMessage);
        }
    }

    private void RefreshErrors(string errorMessage = null)
    {
        AddToClassList("inputError");
        errorInfoBox.text = errorMessage;
        packageDisplayNameField.AddToClassList("error");
        ShowWithNewWindowSize();
    }

    private void OnPackageCreated(string packageName)
    {
        PackageManagerWindowAnalytics.SendEvent("createPackage", packageName);
        Close();
        PackageManagerWindow.OpenAndSelectPackage(packageName);
    }

    private VisualElementCache cache { get; }
    private VisualElement inputForm => cache.Get<VisualElement>("inputForm");
    private HelpBox errorInfoBox => cache.Get<HelpBox>("errorInfoBox");
    private TextField packageDisplayNameField => cache.Get<TextField>("newPackageDisplayName");
    private Button submitButton => cache.Get<Button>("submitButton");
}
