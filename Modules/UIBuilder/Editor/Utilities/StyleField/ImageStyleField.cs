// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using JetBrains.Annotations;
using Object = UnityEngine.Object;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor.PackageManager;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    [UsedImplicitly]
    [UxmlElement]
    partial class ImageStyleField : MultiTypeField
    {
        // Non-Type popup entry — swaps the sub-control to the embedded gradient editor.
        internal const string k_GradientTypeName = "Gradient";

        readonly BackgroundGradientField m_GradientField;
        const double k_TimeoutMilliseconds = 10000;
        const int k_TimeDeltaMilliseconds = 10;

        const string k_UssPath = BuilderConstants.UtilitiesPath + "/StyleField/ImageStyleField.uss";
        const string k_2DSpriteEditorPackageName = "com.unity.2d.sprite";

        const string k_2DSpriteEditorButtonString = "Open in Sprite Editor";
        const string k_No2DSpriteEditorPackageInstalledTitle = "Package required - 2D Sprite Editor";
        const string k_No2DSpriteEditorPackageInstalledMessage =
            "You must install the 2D Sprite Editor package to edit Sprites.\n" +
            "If you do not install the package, you can use existing Sprites, but you cannot create or modify them.\n" +
            "Do you want to install the package now?";
        const string k_2DSpriteEditorInstallationURL =
            "https://docs.unity3d.com/Packages/com.unity.2d.sprite@1.0/manual/index.html";
        const string k_FieldInputName = "unity-visual-input";
        const string k_ImageStyleFieldContainerClassName = "unity-image-style-field__container";
        const string k_ImageStyleFieldEditButtonHiddenClassName = "unity-image-style-field__button--hidden";

        private const string k_2DSpriteEditorButtonTooltip_Installed =
            "Use the Sprite Editor to 9-slice the image or edit its 9-slicing values.";

        private const string k_2DSpriteEditorButtonTooltip_NotInstalled =
            k_2DSpriteEditorButtonTooltip_Installed +
            " Unity will prompt you to install the com.unity.2d.sprite package first.";

        string m_2DSpriteEditorButtonTooltip = k_2DSpriteEditorButtonTooltip_NotInstalled;

        public ImageStyleField() : this(null) {}

        public ImageStyleField(string label) : base(label, new VisualElement())
        {
            AddType(typeof(Texture2D), "Texture");
            AddType(typeof(RenderTexture), "Render Texture");

            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssPath));

            m_2DSpriteEditorButtonTooltip = BuilderExternalPackages.is2DSpriteEditorInstalled
                ? k_2DSpriteEditorButtonTooltip_Installed
                : k_2DSpriteEditorButtonTooltip_NotInstalled;

            var fieldInput = this.Q(k_FieldInputName);

            // Move visual input over to field container
            visualInput.Add(fieldInput);
            visualInput.name = StyleField<int>.VisualInputName;
            visualInput.AddToClassList(k_ImageStyleFieldContainerClassName);

            var editButton = new Button(OnEditButton)
            {
                text = k_2DSpriteEditorButtonString,
                tooltip = m_2DSpriteEditorButtonTooltip
            };
            editButton.RegisterCallback<PointerEnterEvent>(OnEnterEditButton);
            visualInput.Add(editButton);

            var optionsPopup = this.Q<PopupField<string>>();
            optionsPopup.formatSelectedValueCallback += formatValue =>
            {
                editButton.EnableInClassList(k_ImageStyleFieldEditButtonHiddenClassName, !formatValue.Equals("Sprite"));
                return formatValue;
            };

            AddType(typeof(Sprite), "Sprite");
            AddType(typeof(VectorImage), "Vector");

            objectField.objectFieldDisplay.RegisterDefaultDragAndDrop(new List<Type>()
            {
                typeof(Texture2D),
                typeof(RenderTexture),
                typeof(Sprite),
                typeof(VectorImage)
            });

            // Non-Type popup entry — AddType would wire it as an ObjectField filter.
            if (!typePopup.choices.Contains(k_GradientTypeName))
                typePopup.choices.Add(k_GradientTypeName);

            m_GradientField = new BackgroundGradientField();
            m_GradientField.style.display = DisplayStyle.None;
            m_GradientField.style.flexGrow = 1f;
            visualInput.Add(m_GradientField);

            typePopup.RegisterValueChangedCallback(evt =>
            {
                UpdateGradientVisibility();
                // On mode swap, emit through the destination writer so the USS source updates.
                if (evt.newValue == k_GradientTypeName && evt.previousValue != k_GradientTypeName)
                    m_GradientField.NotifyCurrentValue();
                else if (evt.previousValue == k_GradientTypeName && evt.newValue != k_GradientTypeName)
                    NotifyObjectFieldValue();
            });
            UpdateGradientVisibility();
        }

        void NotifyObjectFieldValue()
        {
            using var changeEvt = ChangeEvent<UnityEngine.Object>.GetPooled(objectField.value, objectField.value);
            changeEvt.target = this;
            SendEvent(changeEvt);
        }

        // Must be called after SetTypePopupValueWithoutNotify, which bypasses the popup change callback.
        internal void SyncGradientVisibility() => UpdateGradientVisibility();

        // Called on selection change to avoid stale gradient state from the previous element.
        internal void ResetGradientToAuthoringDefault()
        {
            m_GradientField.SetValueWithoutNotify(BackgroundGradientField.defaultAuthoringGradient);
        }

        void UpdateGradientVisibility()
        {
            bool isGradient = typePopup.value == k_GradientTypeName;
            m_GradientField.style.display = isGradient ? DisplayStyle.Flex : DisplayStyle.None;
            objectField.style.display = isGradient ? DisplayStyle.None : DisplayStyle.Flex;
            // The Sprite edit button's visibility is handled by the popup formatSelectedValueCallback,
            // which hides it whenever the selected type isn't "Sprite" — including "Gradient".
        }

        // Push the gradient in and flip the popup to "Gradient" without notifying.
        internal void SetGradientWithoutNotify(BackgroundGradient gradient)
        {
            m_GradientField.SetValueWithoutNotify(gradient);
            typePopup.SetValueWithoutNotify(k_GradientTypeName);
            UpdateGradientVisibility();
        }

        internal bool isGradientSelected => typePopup.value == k_GradientTypeName;
        internal BackgroundGradientField gradientField => m_GradientField;

        private void OnEnterEditButton(PointerEnterEvent evt)
        {
            m_2DSpriteEditorButtonTooltip = BuilderExternalPackages.is2DSpriteEditorInstalled
                ? k_2DSpriteEditorButtonTooltip_Installed
                : k_2DSpriteEditorButtonTooltip_NotInstalled;
        }

        private void OnEditButton()
        {
            if (BuilderExternalPackages.is2DSpriteEditorInstalled)
            {
                // Open 2D Sprite Editor with current image loaded
                BuilderExternalPackages.Open2DSpriteEditor((Object)value);
                return;
            }

            // Handle the missing 2D Sprite Editor package case.
            if (BuilderDialogsUtility.DisplayDialog(
                k_No2DSpriteEditorPackageInstalledTitle,
                k_No2DSpriteEditorPackageInstalledMessage,
                "Install",
                "Cancel"))
            {
                if (!Install2DSpriteEditorPackage())
                    Application.OpenURL(k_2DSpriteEditorInstallationURL);
            }
        }

        bool Install2DSpriteEditorPackage()
        {
            var startTime = DateTime.Now;
            var addRequest = Client.Add(k_2DSpriteEditorPackageName);

            while (!addRequest.IsCompleted)
            {
                var timeDelta = DateTime.Now - startTime;
                if (timeDelta.TotalMilliseconds >= k_TimeoutMilliseconds)
                {
                    Debug.LogError(
                        $"Could not install package \"{k_2DSpriteEditorPackageName}\" within reasonable time.\n" +
                        "Please note that the installation might be taking longer than expected and may still end successfully.");
                    return false;
                }

                Thread.Sleep(k_TimeDeltaMilliseconds);
            }

            if (addRequest.Result == null)
                Debug.LogError($"Could not install package \"{k_2DSpriteEditorPackageName}\".  Error: {addRequest.Error.message}");
            else
                Debug.Log($"Successfully installed package \"{k_2DSpriteEditorPackageName}\".");

            return addRequest.Result != null;
        }
    }
}
