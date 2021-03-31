using JetBrains.Annotations;
using System;
using System.Threading;
using UnityEditor.PackageManager;
using UnityEditor.UIElements;
using Object = UnityEngine.Object;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    [UsedImplicitly]
    class ImageStyleField : MultiTypeField
    {
        [UsedImplicitly]
        public new class UxmlFactory : UxmlFactory<ImageStyleField, UxmlTraits> {}

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
        const string k_ImageStyleFieldContainerName = "unity-image-style-field-container";
        const string k_ImageStyleFieldContainerClassName = "unity-image-style-field__container";
        const string k_ImageStyleFieldEditButtonHiddenClassName = "unity-image-style-field__button--hidden";

        private const string k_2DSpriteEditorButtonTooltip_Installed =
            "Use the Sprite Editor to 9-slice the image or edit its 9-slicing values.";

        private const string k_2DSpriteEditorButtonTooltip_NotInstalled =
            k_2DSpriteEditorButtonTooltip_Installed +
            " Unity will prompt you to install the com.unity.2d.sprite package first.";

        string m_2DSpriteEditorButtonTooltip = k_2DSpriteEditorButtonTooltip_NotInstalled;

        public ImageStyleField() : this(null) {}

        public ImageStyleField(string label) : base(label)
        {
            AddType(typeof(Texture2D), "Texture");

            AddType(typeof(RenderTexture), "Render Texture");

            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssPath));
            var fieldContainer = new VisualElement {name = k_ImageStyleFieldContainerName};
            fieldContainer.AddToClassList(k_ImageStyleFieldContainerClassName);

            m_2DSpriteEditorButtonTooltip = BuilderExternalPackages.is2DSpriteEditorInstalled
                ? k_2DSpriteEditorButtonTooltip_Installed
                : k_2DSpriteEditorButtonTooltip_NotInstalled;
            var fieldInput = this.Q(k_FieldInputName);
            // Move visual input over to field container
            fieldContainer.Add(fieldInput);

            var editButton = new Button(OnEditButton)
            {
                text = k_2DSpriteEditorButtonString,
                tooltip = m_2DSpriteEditorButtonTooltip
            };
            editButton.RegisterCallback<PointerEnterEvent>(OnEnterEditButton);
            fieldContainer.Add(editButton);

            Add(fieldContainer);

            var optionsPopup = this.Q<PopupField<string>>();
            optionsPopup.formatSelectedValueCallback += formatValue =>
            {
                editButton.EnableInClassList(k_ImageStyleFieldEditButtonHiddenClassName, !formatValue.Equals("Sprite"));
                return formatValue;
            };

            AddType(typeof(Sprite), "Sprite");
        }

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


        public void TryEnableVectorGraphicTypeSupport()
        {
            AddType(typeof(VectorImage), "Vector");
        }
    }
}
