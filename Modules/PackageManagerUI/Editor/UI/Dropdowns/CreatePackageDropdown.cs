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

    public override Vector2 windowSize => string.IsNullOrEmpty(errorInfoBox.text) ? k_DefaultWindowSize : k_WindowSizeWithError;

    private readonly IPackageCreator m_PackageCreator;
    private readonly IApplicationProxy m_Application;
    public CreatePackageDropdown(IResourceLoader resourceLoader, IPackageCreator packageCreator, IApplicationProxy application)
    {
        m_PackageCreator = packageCreator;
        m_Application = application;

        styleSheets.Add(resourceLoader.inputDropdownStyleSheet);

        var root = resourceLoader.GetTemplate("CreatePackageDropdown.uxml");
        Add(root);
        cache = new VisualElementCache(root);

        packageDisplayNameField.textEdition.placeholder = L10n.Tr("Package display name");


        submitButton.clickable.clicked += SubmitClicked;
    }

    public override void OnDropdownShown()
    {
        packageDisplayNameField.RegisterCallback<ChangeEvent<string>>(OnTextFieldChange);
        packageDisplayNameField.RegisterCallback<KeyDownEvent>(OnKeyDownShortcut, TrickleDown.TrickleDown);
        m_PackageCreator.onPackageCreated += OnPackageCreated;

        inputForm.SetEnabled(true);
        packageDisplayNameField.Focus();
        submitButton.SetEnabled(!string.IsNullOrWhiteSpace(packageDisplayNameField.value));
    }

    public override void OnDropdownClosed()
    {
        packageDisplayNameField.UnregisterCallback<ChangeEvent<string>>(OnTextFieldChange);
        packageDisplayNameField.UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut, TrickleDown.TrickleDown);
        m_PackageCreator.onPackageCreated -= OnPackageCreated;
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

        if (!m_PackageCreator.CanGenerateValidNamespace(packageDisplayName))
        {
            var title = L10n.Tr("Cannot generate namespace");
            var message = L10n.Tr("A valid namespace could not be generated from the display name you entered. The default namespace will be used instead. Do you want to continue?");
            if (!m_Application.DisplayDialog("fallbackToDefaultNamespace", title, message, L10n.Tr("Continue"), L10n.Tr("Cancel")))
                return;
        }

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
